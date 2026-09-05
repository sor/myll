using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Computes the static type of every expression after name resolution has finished.
	/// Results are cached on the expression nodes themselves via <see cref="Expr.Type"/>.
	/// </summary>
	public sealed class TypeResolver
	{
		private readonly ResolutionResult result;

		public TypeResolver( ResolutionResult result )
		{
			this.result = result;
		}

		/// <summary>
		/// Computes and stores the type of <paramref name="expr"/> and all sub-expressions.
		/// Returns null if the type cannot be determined.
		/// </summary>
		public Typespec? Resolve( Expr expr )
		{
			if( expr == null )
				return null;

			if( expr.Type != null )
				return expr.Type;

			Typespec? type = ResolveCore( expr );
			expr.Type = type;
			return type;
		}

		private Typespec? ResolveCore( Expr expr )
		{
			return expr switch {
				Literal lit                         => ResolveLiteral( lit ),
				SelfExpr self                       => self.Type,
				ThisExpr thisExpr                   => thisExpr.Type,
				IdExpr id                           => ResolveId( id ),
				ScopedExpr scoped                   => ResolveScoped( scoped ),
				FuncCallExpr call                   => ResolveFuncCall( call ),
				CastExpr cast                       => ResolveCast( cast ),
				UnOp unOp                           => ResolveUnOp( unOp ),
				BinOp binOp                         => ResolveBinOp( binOp ),
				TernOp ternOp                       => ResolveTernOp( ternOp ),
				NewExpr newExpr                     => ResolveNew( newExpr ),
				InitListExpr initList               => ResolveInitList( initList ),
				Lambda lambda                       => ResolveLambda( lambda ),
				_                                   => null,
			};
		}

		private static Typespec? ResolveLiteral( Literal lit )
		{
			string t = lit.text;

			if( t.Length >= 2 && t[0] == '"' && t[t.Length - 1] == '"' )
				return new TypespecBasic { kind = TypespecBasic.Kind.String, size = TypespecBasic.SizeUndetermined };

			// char literal: '\'' escaped or plain char.
			if( t.Length >= 3 && t[0] == '\'' && t[t.Length - 1] == '\'' )
				return new TypespecBasic { kind = TypespecBasic.Kind.Char, size = 1 };

			if( t == "true" || t == "false" )
				return new TypespecBasic { kind = TypespecBasic.Kind.Bool, size = 1 };

			// Hexadecimal integer literals such as 0x1E contain an 'E' but are still
			// integers, so check for a radix prefix before treating the token as a float.
			bool isHex = t.Length >= 2
			          && t[0] == '0'
			          && ( t[1] == 'x' || t[1] == 'X' );

			if( !isHex
			 && ( t.Contains( "." )
			   || t.Contains( "e", StringComparison.OrdinalIgnoreCase )
			   || t.Contains( "E", StringComparison.OrdinalIgnoreCase ) ) )
				return new TypespecBasic {
					kind        = TypespecBasic.Kind.UntypedFloat,
					size        = TypespecBasic.SizeUndetermined,
					literalText = t,
				};

			// Integer literals without a concrete-size suffix are untyped until bound to a target.
			return new TypespecBasic {
				kind        = TypespecBasic.Kind.UntypedInteger,
				size        = TypespecBasic.SizeUndetermined,
				literalText = t,
			};
		}

		private Typespec? ResolveInitList( InitListExpr initList )
		{
			if( initList.args.Count == 0 )
				return null;

			Typespec? common = null;
			foreach( Arg arg in initList.args ) {
				if( arg.name != null )
					return null;

				Typespec? elementType = ResolveElementType( arg.expr );
				if( elementType == null )
					return null;

				common = common == null
					? elementType
					: CommonType( common, elementType );
				if( common == null )
					return null;
			}

			return new TypespecNested {
				srcPos     = initList.srcPos,
				isInitList = true,
				idTpls     = new List<IdTplArgs> {
					new() { id = "std" },
					new() {
						id      = "initializer_list",
						tplArgs = new List<TplArg> { new() { typespec = common } },
					},
				},
			};
		}

		private Typespec? ResolveElementType( Expr expr )
		{
			Typespec? type = Resolve( expr );
			if( type is TypespecBasic basic ) {
				if( basic.kind == TypespecBasic.Kind.UntypedInteger )
					return new TypespecBasic { kind = TypespecBasic.Kind.Integer, size = 4 };
				if( basic.kind == TypespecBasic.Kind.UntypedFloat )
					return new TypespecBasic { kind = TypespecBasic.Kind.Float, size = Dialect.DefaultFloatSize() };
			}
			return type;
		}

		private Typespec? ResolveId( IdExpr id )
		{
			// `null` is represented as ExplicitAuto. ConversionRules has a special case
			// that lets this bind to raw/smart pointer targets.
			if( id.idTplArgs.id == "null" )
				return new TypespecBasic { kind = TypespecBasic.Kind.ExplicitAuto, size = TypespecBasic.SizeUndetermined };

			if( !result.TryGetResolved( id, out Decl? decl ) )
				return null;

			return ResolveDeclValueType( decl );
		}

		private Typespec? ResolveScoped( ScopedExpr scoped )
		{
			if( !result.TryGetResolved( scoped, out Decl? decl ) )
				return null;

			return ResolveDeclValueType( decl );
		}

		private Typespec? ResolveUnOp( UnOp unOp )
		{
			Typespec? operandType = Resolve( unOp.expr );

			switch( unOp.op ) {
				case Operand.Parens:
					return operandType;

				case Operand.PrePlus:
				case Operand.PreMinus:
				case Operand.PreIncr:
				case Operand.PreDecr:
				case Operand.PostIncr:
				case Operand.PostDecr:
					return operandType;

				case Operand.Dereference:
					return StripOutermostPointer( operandType );

				case Operand.AddressOf:
					return AddRawPointer( operandType );

				case Operand.Negation:
					return new TypespecBasic { kind = TypespecBasic.Kind.Bool, size = 1 };

			case Operand.Complement:
				if( OperatorRules.IsScalarBitFamily( operandType ) )
					return operandType;

				return PromoteInteger( operandType );

			default:
				return null;
			}
		}

		private Typespec? ResolveBinOp( BinOp binOp )
		{
			if( IsMemberAccessOperation( binOp.op ) )
				return ResolveMemberAccess( binOp );

			Typespec? leftType  = Resolve( binOp.left );
			Typespec? rightType = Resolve( binOp.right );

			if( TryResolveBitOperation( binOp.op, leftType, rightType, out Typespec? bitResult ) )
				return bitResult;

			if( IsArithmeticOperation( binOp.op ) ) {
				if( binOp.op == Operand.Modulo )
					return CommonIntegerType( leftType, rightType );
				if( binOp.op == Operand.FractionalDivide )
					return new TypespecBasic { kind = TypespecBasic.Kind.Float, size = 8 };

				return CommonArithmeticType( leftType, rightType );
			}

			if( IsComparisonOperation( binOp.op ) || IsLogicalOperation( binOp.op ) )
				return new TypespecBasic { kind = TypespecBasic.Kind.Bool, size = 1 };

			if( IsAssignmentOperation( binOp.op ) )
				return leftType;

			if( IsBitwiseOperation( binOp.op ) )
				return CommonIntegerType( leftType, rightType );

			if( IsShiftOperation( binOp.op ) )
				return PromoteInteger( leftType );

			return null;
		}

		private Typespec? ResolveMemberAccess( BinOp binOp )
		{
			if( binOp.right is not IdExpr member )
				return null;

			if( !result.TryGetResolvedMember( member, out Decl? decl ) )
				return null;

			return ResolveDeclValueType( decl );
		}

		private Typespec? ResolveTernOp( TernOp ternOp )
		{
			Typespec? thenType = Resolve( ternOp.mid );
			Typespec? elseType = Resolve( ternOp.right );
			return CommonType( thenType, elseType );
		}

		private Typespec? ResolveFuncCall( FuncCallExpr call )
		{
			return ResolveCallReturnType( call.expr );
		}

		private Typespec? ResolveNew( NewExpr newExpr )
		{
			// `new T` yields a raw pointer to T. If the user already wrote an explicit
			// pointer type (e.g. `new T*`), return it unchanged.
			if( newExpr.type.ptrs != null && newExpr.type.ptrs.Count > 0 )
				return newExpr.type;

			return AddRawPointer( newExpr.type );
		}

		private Typespec? ResolveCast( CastExpr cast )
		{
			// (move) and (forward) are not real type casts; they produce an rvalue
			// or forwarding-reference of the operand's own type.
			if( cast.op is Operand.MoveCast || cast.op is Operand.ForwardCast )
				return Resolve( cast.expr );

			return cast.type;
		}

		private Typespec? ResolveLambda( Lambda lambda )
		{
			return lambda.func.retType;
		}

		private Typespec? ResolveDeclValueType( Decl? decl )
		{
			return decl switch {
				VarDecl vd      => vd.type,
				Func func       => func.retType,
				_               => null,
			};
		}

		private Typespec? ResolveCallReturnType( Expr callee )
		{
			if( callee is IdExpr id && result.TryGetResolved( id, out Decl? d1 ) ) {
				if( d1 is Func f1 )
					return f1.retType;

				// Constructor call: the resolved declaration is a Structor inside a class.
				// The expression's value type is the surrounding class by value.
				if( d1 is Structor stc )
					return StructorParentType( stc );
			}

			if( callee is ScopedExpr scoped && result.TryGetResolved( scoped, out Decl? d2 ) ) {
				if( d2 is Func f2 )
					return f2.retType;
				if( d2 is Structor stc2 )
					return StructorParentType( stc2 );
			}

			if( callee is BinOp binOp
			 && binOp.right is IdExpr member
			 && result.TryGetResolvedMember( member, out Decl? d3 ) ) {
				if( d3 is Func f3 )
					return f3.retType;
				if( d3 is Structor stc3 )
					return StructorParentType( stc3 );
			}

			return null;
		}

		private static Typespec? StructorParentType( Structor stc )
		{
			// The Structor lives inside a class/struct scope. Its containing declaration is the type to return.
			if( stc.scope?.parent?.decl is Hierarchical parentH )
				return new TypespecNested {
					resolvedDecl = parentH,
					idTpls       = new() { new() { id = parentH.name } },
				};

			return null;
		}

		private static Typespec? CommonArithmeticType( Typespec? left, Typespec? right )
		{
			if( left == null || right == null )
				return null;

			if( !OperatorRules.IsScalarNumber( left ) || !OperatorRules.IsScalarNumber( right ) )
				return null;

			bool leftUntyped  = IsUntyped( left );
			bool rightUntyped = IsUntyped( right );

			if( leftUntyped && !rightUntyped )
				return right;
			if( rightUntyped && !leftUntyped )
				return left;

			if( leftUntyped && rightUntyped ) {
				TypespecBasic.Kind kind = OperatorRules.IsScalarFloat( left ) || OperatorRules.IsScalarFloat( right )
					? TypespecBasic.Kind.UntypedFloat
					: TypespecBasic.Kind.UntypedInteger;
				return new TypespecBasic { kind = kind, size = TypespecBasic.SizeUndetermined };
			}

			bool leftIsFloat  = OperatorRules.IsScalarFloat( left );
			bool rightIsFloat = OperatorRules.IsScalarFloat( right );

			if( leftIsFloat || rightIsFloat ) {
				int size = System.Math.Max( FloatSize( left ), FloatSize( right ) );
				return new TypespecBasic { kind = TypespecBasic.Kind.Float, size = size };
			}

			return CommonIntegerType( left, right );
		}

		private static Typespec? CommonIntegerType( Typespec? left, Typespec? right )
		{
			if( left == null || right == null )
				return null;

			// bool promotes to int so counting comparisons like `bool + bool` works.
			Typespec? l = PromoteBool( left );
			Typespec? r = PromoteBool( right );
			if( l == null || r == null )
				return null;

			if( l is not TypespecBasic lb || r is not TypespecBasic rb )
				return null;

			return UsualArithmeticConversions( lb, rb );
		}

		private static Typespec? UsualArithmeticConversions( TypespecBasic left, TypespecBasic right )
		{
			if( left.kind == right.kind )
				return new TypespecBasic { kind = left.kind, size = System.Math.Max( left.size, right.size ) };

			if( left.kind == TypespecBasic.Kind.Unsigned )
				return MixedIntegerResult( right, left );

			// left is signed, right is unsigned
			return MixedIntegerResult( left, right );
		}

		private static Typespec? MixedIntegerResult( TypespecBasic signedType, TypespecBasic unsignedType )
		{
			return Dialect.MixedSignedness switch {
				MixedSignednessMode.SignedPreferred
					=> SignedPreferredIntegerResult( signedType, unsignedType ),
				_ => CStyleIntegerResult( signedType, unsignedType ),
			};
		}

		private static Typespec? CStyleIntegerResult( TypespecBasic signedType, TypespecBasic unsignedType )
		{
			// If the unsigned type has greater or equal rank, the result is unsigned.
			if( unsignedType.size >= signedType.size )
				return new TypespecBasic { kind = TypespecBasic.Kind.Unsigned, size = unsignedType.size };

			// Otherwise the signed type can represent every value of the unsigned type.
			return new TypespecBasic { kind = TypespecBasic.Kind.Integer, size = signedType.size };
		}

		private static Typespec? SignedPreferredIntegerResult( TypespecBasic signedType, TypespecBasic unsignedType )
		{
			// Signed wins when it is at least as wide as the unsigned type.
			if( signedType.size >= unsignedType.size )
				return new TypespecBasic { kind = TypespecBasic.Kind.Integer, size = signedType.size };

			// Otherwise the unsigned type is strictly wider and must win to avoid overflow.
			return new TypespecBasic { kind = TypespecBasic.Kind.Unsigned, size = unsignedType.size };
		}

		private static Typespec? PromoteBool( Typespec? type )
		{
			if( type is TypespecBasic { kind: TypespecBasic.Kind.Bool } )
				return new TypespecBasic { kind = TypespecBasic.Kind.Integer, size = 4 };

			if( OperatorRules.IsScalarInteger( type ) )
				return type;

			return null;
		}

		private static Typespec? PromoteInteger( Typespec? type )
		{
			if( type is TypespecBasic { kind: TypespecBasic.Kind.Bool } )
				return new TypespecBasic { kind = TypespecBasic.Kind.Integer, size = 4 };

			if( OperatorRules.IsScalarInteger( type ) )
				return type;

			return null;
		}

		private static bool IsUntyped( Typespec? type )
			=> type is TypespecBasic basic
			 && ( basic.kind == TypespecBasic.Kind.UntypedInteger
			   || basic.kind == TypespecBasic.Kind.UntypedFloat );

		private static int FloatSize( Typespec? type )
		{
			if( type is TypespecBasic basic && basic.kind == TypespecBasic.Kind.Float )
				return basic.size;
			return 0;
		}

		internal static Typespec? CommonType( Typespec? a, Typespec? b )
		{
			if( a == null ) return b;
			if( b == null ) return a;
			if( ConversionRules.IsExactMatch( a, b ) )
				return a;
			return CommonArithmeticType( a, b );
		}

		private static Typespec? StripOutermostPointer( Typespec? type )
		{
			if( type?.ptrs == null || type.ptrs.Count == 0 )
				return null;

			Typespec ret = CloneTypespecBase( type );
			ret.ptrs = type.ptrs.Take( type.ptrs.Count - 1 ).ToList();
			return ret;
		}

		private static Typespec? AddRawPointer( Typespec? type )
		{
			if( type == null )
				return null;

			Typespec ret = CloneTypespecBase( type );
			ret.ptrs ??= new List<Pointer>();
			ret.ptrs.Add( new() { kind = Pointer.Kind.RawPtr } );
			return ret;
		}

		private static Typespec CloneTypespecBase( Typespec source )
		{
			Typespec ret = source switch {
				TypespecBasic b   => new TypespecBasic { kind = b.kind, size = b.size },
				TypespecNested n  => new TypespecNested {
					resolvedDecl = n.resolvedDecl,
					idTpls       = n.idTpls.Select( it => new IdTplArgs { id = it.id, tplArgs = it.tplArgs } ).ToList(),
				},
				TypespecFunc f    => new TypespecFunc {
					paras   = f.paras,
					retType = f.retType,
				},
				_                 => throw new InvalidOperationException( "unknown Typespec variant" ),
			};

			ret.srcPos = source.srcPos;
			ret.qual   = source.qual;
			return ret;
		}

		private static bool TryResolveBitOperation(
			Operand op,
			Typespec? leftType,
			Typespec? rightType,
			out Typespec? result )
		{
			result = null;

			if( !IsBitOperator( op ) )
				return false;

			bool leftTyped  = IsTypedBitFamily( leftType );
			bool rightTyped = IsTypedBitFamily( rightType );

			if( !leftTyped && !rightTyped )
				return false;

			if( !SameBitFamily( leftType, rightType ) )
				return false;

			if( IsShiftOperation( op ) ) {
				if( !leftTyped )
					return false;

				result = leftType;
				return true;
			}

			result = CommonBitFamilyType( leftType, rightType );
			return result != null;
		}

		private static Typespec? CommonBitFamilyType( Typespec? left, Typespec? right )
		{
			TypespecBasic? leftTyped  = AsBitFamilyType( left );
			TypespecBasic? rightTyped = AsBitFamilyType( right );

			if( leftTyped != null && rightTyped != null ) {
				if( leftTyped.size != rightTyped.size )
					return null;

				return new TypespecBasic { kind = leftTyped.kind, size = leftTyped.size };
			}

			if( leftTyped != null )
				return new TypespecBasic { kind = leftTyped.kind, size = leftTyped.size };

			if( rightTyped != null )
				return new TypespecBasic { kind = rightTyped.kind, size = rightTyped.size };

			return null;
		}

		private static bool IsTypedBitFamily( Typespec? type )
			=> OperatorRules.IsScalarBitFamily( type );

		private static bool SameBitFamily( Typespec? left, Typespec? right )
		{
			bool leftBit  = OperatorRules.IsScalarBit( left );
			bool rightBit = OperatorRules.IsScalarBit( right );
			bool leftByte = OperatorRules.IsScalarByte( left );
			bool rightByte = OperatorRules.IsScalarByte( right );

			// A typed bit/byte operand may not mix with the other family.
			if( ( leftBit && rightByte ) || ( leftByte && rightBit ) )
				return false;

			// If one side is untyped, it can bind to either family.
			return leftBit || rightBit || leftByte || rightByte;
		}

		private static TypespecBasic? AsBitFamilyType( Typespec? type )
		{
			if( type is TypespecBasic basic
			 && ( basic.kind == TypespecBasic.Kind.Bitwise || basic.kind == TypespecBasic.Kind.Byte )
			 && ( type.ptrs == null || type.ptrs.Count == 0 ) )
				return basic;

			return null;
		}

		private static bool IsBitOperator( Operand op )
			=> op is Operand.Add or Operand.Subtract or Operand.Multiply
			|| op is Operand.Divide
			|| op is Operand.BitAnd or Operand.BitOr or Operand.BitXor
			|| op is Operand.LeftShift or Operand.RightShift;

		private static bool IsMemberAccessOperation( Operand op )
			=> op is Operand.MemberAccess
			|| op is Operand.NCMemberAccess
			|| op is Operand.MemberPtrAccess
			|| op is Operand.MemberAccessPtr
			|| op is Operand.NCMemberAccessPtr
			|| op is Operand.MemberPtrAccessPtr;

		private static bool IsArithmeticOperation( Operand op )
			=> op is Operand.Add or Operand.Subtract or Operand.Multiply
			|| op is Operand.FractionalDivide or Operand.Modulo;

		private static bool IsComparisonOperation( Operand op )
			=> op is Operand.Equal or Operand.NotEqual or Operand.LessThan
			|| op is Operand.LessEqual or Operand.GreaterThan or Operand.GreaterEqual
			|| op is Operand.Comparison;

		private static bool IsLogicalOperation( Operand op )
			=> op is Operand.And or Operand.Or;

		private static bool IsAssignmentOperation( Operand op )
			=> op is Operand.Equal;

		private static bool IsBitwiseOperation( Operand op )
			=> op is Operand.BitAnd or Operand.BitOr or Operand.BitXor;

		private static bool IsShiftOperation( Operand op )
			=> op is Operand.LeftShift or Operand.RightShift;

		private Hierarchical? FindEnclosingStructural( Expr expr )
		{
			Decl? decl = expr is IdExpr id && result.TryGetResolved( id, out Decl? d )
				? d
				: null;

			ScopeLeaf? leaf = decl?.scope;
			while( leaf != null ) {
				if( leaf.decl is Hierarchical h && h is Namespace == false )
					return h;

				leaf = leaf.parent;
			}

			return null;
		}
	}
}

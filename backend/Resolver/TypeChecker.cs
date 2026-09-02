using System;
using System.Collections.Generic;
using System.Linq;
using Myll.Core;

namespace Myll.Resolver
{
	/// <summary>
	/// Validates type-correctness after name and overload resolution. Emits diagnostics for
	/// mismatched assignments, initializers, returns, and function arguments.
	/// </summary>
	public sealed class TypeChecker
	{
		private readonly ResolutionResult result;
		private readonly TypeResolver typeResolver;
		private readonly List<Diagnostic> diagnostics;

		public TypeChecker( ResolutionResult result, List<Diagnostic> diagnostics )
		{
			this.result        = result;
			this.typeResolver  = new TypeResolver( result );
			this.diagnostics   = diagnostics;
		}

		public void Validate(
			IReadOnlyList<(GlobalNamespace Module, CompilationContext Context)> modules )
		{
			foreach( (GlobalNamespace module, CompilationContext context) in modules ) {
				ValidateCalls( context );
				ValidateShadowing( context );
				ValidateDecl( module );
			}
		}

		private void ValidateCalls( CompilationContext context )
		{
			foreach( UnresolvedCall call in context.UnresolvedCalls ) {
				Decl? calleeDecl = ResolveCalleeDecl( call.Callee );
				if( calleeDecl == null )
					continue;

				List<Param> paras = calleeDecl switch {
					Func func      => func.paras,
					Structor stc   => stc.paras,
					_              => new List<Param>(),
				};

				if( paras.Count != call.Call.args.Count )
					continue; // arity mismatch is already reported by overload resolution

				for( int i = 0; i < paras.Count; i++ ) {
					ValidateExpression( call.Call.args[i].expr );

					Typespec? argType = typeResolver.Resolve( call.Call.args[i].expr );
					if( argType == null )
						continue; // cannot determine argument type yet

				if( !ConversionRules.IsImplicitlyConvertible( argType, paras[i].type ) ) {
					ReportTypeConversionError(
						call.Call.args[i].expr.srcPos,
						argType,
						paras[i].type,
						String.Format(
							"Cannot convert argument type '{0}' to parameter type '{1}'",
							FormatType( argType ),
							FormatType( paras[i].type ) ) );
					continue;
				}

				call.Call.args[i].expr = BindToBitType( call.Call.args[i].expr, paras[i].type );
				}
			}
		}

		private Decl? ResolveCalleeDecl( Expr callee )
		{
			return callee switch {
				IdExpr     id     when result.TryGetResolved( id, out Decl? d )     => d,
				ScopedExpr scoped when result.TryGetResolved( scoped, out Decl? d ) => d,
				BinOp      binOp  when binOp.right is IdExpr m
				             && result.TryGetResolvedMember( m, out Decl? d )       => d,
				_                                                                   => null,
			};
		}

		private void ValidateDecl( Decl decl )
		{
			switch( decl ) {
				case Func func:
					if( func.body != null )
						ValidateStmt( func.body, func );
					break;

				case Structor stc:
					if( stc.body != null )
						ValidateStmt( stc.body, null );
					break;

			case VarDecl vd:
				ValidateVarAttributes( vd );
				ValidateDefaultSizedField( vd );
				if( vd.init != null ) {
						if( vd.type is TypespecBasic { kind: TypespecBasic.Kind.Auto } )
							vd.type = InferAutoType( typeResolver.Resolve( vd.init ) ) ?? vd.type;

						CheckAssignment( vd.type, vd.init, vd.init.srcPos );
						vd.init = BindToBitType( vd.init, vd.type );
					}
					break;

				case Hierarchical h:
					ValidateUniqueVarNames( h );
					foreach( Decl child in h.children )
						ValidateDecl( child );
					break;
			}
		}

		private void ValidateStmt( Stmt stmt, Func? currentFunction )
		{
			switch( stmt ) {
				case MultiStmt multi:
					foreach( Stmt s in multi.stmts )
						ValidateStmt( s, currentFunction );
					break;

			case VarStmt vs:
					if( vs.init != null ) {
						if( vs.type is TypespecBasic { kind: TypespecBasic.Kind.Auto } )
							vs.type = InferAutoType( typeResolver.Resolve( vs.init ) ) ?? vs.type;

						CheckAssignment( vs.type, vs.init, vs.init.srcPos );
						vs.init = BindToBitType( vs.init, vs.type );
					}
					break;

				case ReturnStmt ret:
					ValidateReturn( ret, currentFunction );
					break;

				case MultiAssign multiAssign:
					ValidateMultiAssign( multiAssign );
					break;

				case AggrAssign aggrAssign:
					CheckAssignment( aggrAssign.leftExpr, aggrAssign.rightExpr, aggrAssign.rightExpr.srcPos );
					aggrAssign.rightExpr = BindToBitType( aggrAssign.rightExpr, typeResolver.Resolve( aggrAssign.leftExpr ) );
					break;

				case ExprStmt exprStmt: {
					ValidateExpression( exprStmt.expr );

					if( exprStmt.expr is BinOp { op: Operand.Equal } assign ) {
						CheckAssignment( assign.left, assign.right, assign.right.srcPos );
						assign.right = BindToBitType( assign.right, typeResolver.Resolve( assign.left ) );
					}

					break;
				}

				case IfStmt ifStmt:
					foreach( IfStmt.CondThen ifThen in ifStmt.ifThens ) {
						ValidateCondition( ifThen.cond, ifThen.cond.srcPos, "if" );
						ValidateStmt( ifThen.then, currentFunction );
					}
					if( ifStmt.els != null )
						ValidateStmt( ifStmt.els, currentFunction );
					break;

				case ForStmt forStmt:
					if( forStmt.init != null )
						ValidateStmt( forStmt.init, currentFunction );
					if( forStmt.cond != null )
						ValidateCondition( forStmt.cond, forStmt.cond.srcPos, "for" );
					if( forStmt.body != null )
						ValidateStmt( forStmt.body, currentFunction );
					if( forStmt.els != null )
						ValidateStmt( forStmt.els, currentFunction );
					break;

				case WhileStmt whileStmt:
					ValidateCondition( whileStmt.cond, whileStmt.cond.srcPos, "while" );
					ValidateStmt( whileStmt.body, currentFunction );
					if( whileStmt.els != null )
						ValidateStmt( whileStmt.els, currentFunction );
					break;

				case DoWhileStmt doWhile:
					ValidateStmt( doWhile.body, currentFunction );
					ValidateCondition( doWhile.cond, doWhile.cond.srcPos, "do-while" );
					break;

				case TimesStmt times:
					ValidateStmt( times.body, currentFunction );
					break;

				case SwitchStmt sw:
					foreach( SwitchStmt.CaseBlock c in sw.cases )
						ValidateStmt( c.then, currentFunction );
					if( sw.els != null )
						ValidateStmt( sw.els, currentFunction );
					break;

				case TryCatchStmt tryCatch:
					// try and catch bodies are currently generic Stmt? Check actual properties if available.
					break;
			}
		}

		private void ValidateReturn( ReturnStmt ret, Func? currentFunction )
		{
			if( currentFunction == null ) {
				if( ret.expr != null ) {
					diagnostics.Add( new Diagnostic(
						ret.expr.srcPos,
						DiagnosticKind.Error,
						"Cannot return a value from a constructor/destructor" ) );
				}

				return;
			}

			Typespec expected = currentFunction.retType;
			bool isVoid = expected is TypespecBasic { kind: TypespecBasic.Kind.Void };

			if( ret.expr == null ) {
				if( !isVoid ) {
					diagnostics.Add( new Diagnostic(
						ret.srcPos,
						DiagnosticKind.Error,
						String.Format( "Function '{0}' must return a value of type '{1}'",
							currentFunction.name, FormatType( expected ) ) ) );
				}

				return;
			}

			if( isVoid ) {
				diagnostics.Add( new Diagnostic(
					ret.expr.srcPos,
					DiagnosticKind.Error,
					String.Format( "Function '{0}' returns void and may not return a value",
						currentFunction.name ) ) );
				return;
			}

			ValidateExpression( ret.expr );

			Typespec? actual = typeResolver.Resolve( ret.expr );
			if( actual == null )
				return;

			if( !ConversionRules.IsImplicitlyConvertible( actual, expected ) ) {
				ReportTypeConversionError(
					ret.expr.srcPos,
					actual,
					expected,
					String.Format(
						"Cannot return type '{0}' from function '{1}' expecting '{2}'",
						FormatType( actual ),
						currentFunction.name,
						FormatType( expected ) ) );
			}
			else {
				ret.expr = BindToBitType( ret.expr, expected );
			}
		}

		private void ValidateMultiAssign( MultiAssign multiAssign )
		{
			// a = b = c => check b -> a, c -> b
			for( int i = 0; i + 1 < multiAssign.exprs.Count; i++ ) {
				Expr left  = multiAssign.exprs[i];
				Expr right = multiAssign.exprs[i + 1];
				CheckAssignment( left, right, right.srcPos );
				multiAssign.exprs[i + 1] = BindToBitType( right, typeResolver.Resolve( left ) );
			}
		}

		private void CheckAssignment( Expr left, Expr right, SrcPos srcPos )
		{
			Typespec? leftType  = typeResolver.Resolve( left );
			Typespec? rightType = typeResolver.Resolve( right );

			ValidateExpression( right );

			if( leftType == null || rightType == null )
				return;

			if( !ConversionRules.IsImplicitlyConvertible( rightType, leftType ) ) {
				ReportTypeConversionError(
					srcPos,
					rightType,
					leftType,
					String.Format(
						"Cannot convert '{0}' to '{1}'",
						FormatType( rightType ),
						FormatType( leftType ) ) );
			}
		}

		private void CheckAssignment( Typespec leftType, Expr right, SrcPos srcPos )
		{
			ValidateExpression( right );

			Typespec? rightType = typeResolver.Resolve( right );
			if( rightType == null )
				return;

			if( !ConversionRules.IsImplicitlyConvertible( rightType, leftType ) ) {
				ReportTypeConversionError(
					srcPos,
					rightType,
					leftType,
					String.Format(
						"Cannot convert '{0}' to '{1}'",
						FormatType( rightType ),
						FormatType( leftType ) ) );
			}
		}

		private void ValidateCondition( Expr cond, SrcPos srcPos, string context )
		{
			CheckBooleanOperand( cond, context );
		}

		private void CheckBooleanOperand( Expr expr, string context )
		{
			switch( expr ) {
				case BinOp binOp when binOp.op is Operand.And or Operand.Or: {
					CheckBooleanOperand( binOp.left, context );
					CheckBooleanOperand( binOp.right, context );
					break;
				}

				case UnOp unOp when unOp.op == Operand.Negation: {
					CheckBooleanOperand( unOp.expr, context );
					break;
				}

				default: {
					Typespec? type = typeResolver.Resolve( expr );
					if( type != null && !IsBoolType( type ) ) {
						diagnostics.Add( new Diagnostic(
							expr.srcPos,
							DiagnosticKind.Error,
							String.Format(
								"Condition of '{0}' must be bool, found '{1}'",
								context,
								FormatType( type ) ) ) );
					}

					break;
				}
			}
		}

		private static bool IsBoolType( Typespec type )
		{
			if( type is not TypespecBasic basic )
				return false;

			return basic.kind == TypespecBasic.Kind.Bool && ( type.ptrs == null || type.ptrs.Count == 0 );
		}

		private void ValidateExpression( Expr expr )
		{
			// Logical/conditional operands and a subset of built-in scalar operators are
			// checked here. Anything involving classes, references, or user-defined
			// operator overloads is left for C++ to resolve.
			switch( expr ) {
				case TernOp tern: {
					ValidateCondition( tern.left, tern.left.srcPos, "?:" );
					break;
				}

				case BinOp binOp when binOp.op is Operand.And or Operand.Or: {
					CheckBooleanOperand( binOp.left, binOp.op.ToString().ToLowerInvariant() );
					CheckBooleanOperand( binOp.right, binOp.op.ToString().ToLowerInvariant() );
					break;
				}

				case UnOp unOp when unOp.op == Operand.Negation: {
					CheckBooleanOperand( unOp.expr, "!" );
					break;
				}

				case BinOp binOp when IsBitOperator( binOp.op )
				               && ( IsBitOperand( binOp.left ) || IsBitOperand( binOp.right ) ): {
					ValidateBitOperator( binOp );
					break;
				}

				case BinOp binOp when IsArithmeticOperator( binOp.op ): {
					ValidateArithmeticOperator( binOp );
					break;
				}

				case BinOp binOp when IsBitwiseOperator( binOp.op ): {
					ValidateBitwiseOperator( binOp );
					break;
				}

				case BinOp binOp when IsShiftOperator( binOp.op ): {
					ValidateShiftOperator( binOp );
					break;
				}

				case BinOp binOp when IsComparisonOperator( binOp.op ): {
					ValidateComparisonOperator( binOp );
					break;
				}

				case UnOp unOp when IsArithmeticUnaryOperator( unOp.op ): {
					ValidateArithmeticUnaryOperator( unOp );
					break;
				}

				case UnOp unOp when unOp.op == Operand.Complement: {
					ValidateComplementOperator( unOp );
					break;
				}

				case NewExpr newExpr: {
					if( Dialect.StrictNew
					 && ( newExpr.type.ptrs == null || newExpr.type.ptrs.Count == 0 ) )
						AddError( newExpr, "StrictNew is enabled: bare `new T` is not allowed; use an explicit pointer type such as `new T*` or `new T*!`." );

					break;
				}

				case CastExpr cast: {
					Typespec? sourceType = typeResolver.Resolve( cast.expr );
					if( sourceType != null
					 && ConversionRules.IsSlicingAttempt( sourceType, cast.type ) )
						diagnostics.Add( new Diagnostic(
							cast.srcPos,
							DiagnosticKind.Warning,
							String.Format(
								"Explicit cast from derived type '{0}' to base type '{1}' slices the object.",
								FormatType( sourceType ),
								FormatType( cast.type ) ) ) );

					break;
				}
			}
		}

		private static bool IsArithmeticOperator( Operand op )
			=> op is Operand.Add or Operand.Subtract or Operand.Multiply
			 || op is Operand.FractionalDivide or Operand.Divide or Operand.Modulo;

		private static bool IsBitOperator( Operand op )
			=> op is Operand.Add or Operand.Subtract or Operand.Multiply
			 || op is Operand.Divide
			 || op is Operand.BitAnd or Operand.BitOr or Operand.BitXor
			 || op is Operand.LeftShift or Operand.RightShift;

		private static bool IsBitOperand( Expr expr )
			=> OperatorRules.IsScalarBitFamily( expr.Type );

		private static bool IsBitwiseOperator( Operand op )
			=> op is Operand.BitAnd or Operand.BitOr or Operand.BitXor;

		private static bool IsShiftOperator( Operand op )
			=> op is Operand.LeftShift or Operand.RightShift;

		private static bool IsComparisonOperator( Operand op )
			=> op is Operand.Equal or Operand.NotEqual or Operand.LessThan
			 || op is Operand.LessEqual or Operand.GreaterThan or Operand.GreaterEqual
			 || op is Operand.Comparison;

		private static bool IsArithmeticUnaryOperator( Operand op )
			=> op is Operand.PrePlus or Operand.PreMinus
			 || op is Operand.PreIncr or Operand.PreDecr
			 || op is Operand.PostIncr or Operand.PostDecr;

		private void ValidateArithmeticOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			// Leave class-typed operands and their operator overloads to C++.
			if( leftType is not TypespecBasic || rightType is not TypespecBasic )
				return;

			if( binOp.op is Operand.Add or Operand.Subtract
			 && ( OperatorRules.HasPointer( leftType ) || OperatorRules.HasPointer( rightType ) ) ) {
				AddError( binOp, "Pointer arithmetic is disabled; use indexing instead." );
				return;
			}

			if( binOp.op == Operand.Modulo ) {
				if( !OperatorRules.IsScalarInteger( leftType ) || !OperatorRules.IsScalarInteger( rightType ) )
					AddError( binOp, String.Format( "Operator '{0}' requires integer operands, found '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );

				return;
			}

			if( !OperatorRules.IsScalarNumber( leftType ) || !OperatorRules.IsScalarNumber( rightType ) )
				AddError( binOp, String.Format( "Operator '{0}' cannot be applied to types '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
		}

		private void ValidateBitwiseOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			if( leftType is not TypespecBasic || rightType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarInteger( leftType ) || !OperatorRules.IsScalarInteger( rightType ) )
				AddError( binOp, String.Format( "Operator '{0}' requires integer operands, found '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
		}

		private void ValidateBitOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			// Bit-family operations are defined only for a single family (Bit or Byte)
			// and untyped integer literals used together with a bit operand.
			bool leftIsFamily  = IsBitFamilyType( leftType ) || IsUntypedBitOperand( leftType, rightType );
			bool rightIsFamily = IsBitFamilyType( rightType ) || IsUntypedBitOperand( rightType, leftType );

			if( !leftIsFamily || !rightIsFamily ) {
				AddError( binOp, String.Format(
					"Bit operator '{0}' requires bit operands, found '{1}' and '{2}'",
					OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
				return;
			}

			if( !SameBitFamily( leftType, rightType ) ) {
				AddError( binOp, String.Format(
					"Bit operator '{0}' cannot mix bit and byte operands, found '{1}' and '{2}'",
					OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
				return;
			}

			if( IsShiftOperator( binOp.op ) )
				return;

			TypespecBasic? leftConcrete  = AsConcreteBitFamilyType( leftType ) ?? AsConcreteBitFamilyType( rightType );
			TypespecBasic? rightConcrete = AsConcreteBitFamilyType( rightType ) ?? AsConcreteBitFamilyType( leftType );

			if( leftConcrete != null && rightConcrete != null && leftConcrete.size != rightConcrete.size )
				AddError( binOp, String.Format(
					"Bit operator '{0}' requires operands of the same bit width, found '{1}' and '{2}'",
					OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
		}

		private static bool IsBitFamilyType( Typespec? type )
			=> type is TypespecBasic { kind: TypespecBasic.Kind.Bitwise or TypespecBasic.Kind.Byte }
			 && ( type.ptrs == null || type.ptrs.Count == 0 );

		private static bool SameBitFamily( Typespec? left, Typespec? right )
		{
			bool leftBit  = left  is TypespecBasic { kind: TypespecBasic.Kind.Bitwise };
			bool rightBit = right is TypespecBasic { kind: TypespecBasic.Kind.Bitwise };
			bool leftByte = left  is TypespecBasic { kind: TypespecBasic.Kind.Byte };
			bool rightByte = right is TypespecBasic { kind: TypespecBasic.Kind.Byte };

			if( ( leftBit && rightByte ) || ( leftByte && rightBit ) )
				return false;

			return leftBit || rightBit || leftByte || rightByte;
		}

		private static bool IsUntypedBitOperand( Typespec? candidate, Typespec? other )
			=> candidate is TypespecBasic { kind: TypespecBasic.Kind.UntypedInteger }
			 && other is TypespecBasic { kind: TypespecBasic.Kind.Bitwise or TypespecBasic.Kind.Byte };

		private static TypespecBasic? AsConcreteBitFamilyType( Typespec? type )
			=> type is TypespecBasic basic
			 && ( basic.kind == TypespecBasic.Kind.Bitwise || basic.kind == TypespecBasic.Kind.Byte )
			 && ( type.ptrs == null || type.ptrs.Count == 0 )
			 ? basic
			 : null;

		private void ValidateShiftOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			// Class-typed left operands are using << for stream insertion or a user-defined overload.
			if( leftType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarInteger( leftType ) )
				AddError( binOp.left, String.Format( "Left operand of '{0}' must be an integer type, found '{1}'", OperatorName( binOp.op ), FormatType( leftType ) ) );

			if( rightType is TypespecBasic && !OperatorRules.IsScalarInteger( rightType ) )
				AddError( binOp.right, String.Format( "Right operand of '{0}' must be an integer type, found '{1}'", OperatorName( binOp.op ), FormatType( rightType ) ) );
		}

		private void ValidateComparisonOperator( BinOp binOp )
		{
			Typespec? leftType  = typeResolver.Resolve( binOp.left );
			Typespec? rightType = typeResolver.Resolve( binOp.right );

			if( leftType == null || rightType == null )
				return;

			if( leftType is not TypespecBasic || rightType is not TypespecBasic )
				return;

			// Bit-family types only support equality; no ordering.
			if( OperatorRules.IsScalarBitFamily( leftType ) || OperatorRules.IsScalarBitFamily( rightType ) ) {
				if( binOp.op is not (Operand.Equal or Operand.NotEqual) ) {
					AddError( binOp, String.Format(
						"Bit types only support equality comparisons; operator '{0}' is not allowed",
						OperatorName( binOp.op ) ) );
				}
				else {
					Typespec? target = OperatorRules.IsScalarBitFamily( leftType )
						? leftType
						: rightType;
					binOp.left  = BindToBitType( binOp.left, target );
					binOp.right = BindToBitType( binOp.right, target );
				}

				return;
			}

			if( !OperatorRules.IsScalarComparable( leftType ) || !OperatorRules.IsScalarComparable( rightType ) ) {
				AddError( binOp, String.Format( "Operator '{0}' cannot compare types '{1}' and '{2}'", OperatorName( binOp.op ), FormatType( leftType ), FormatType( rightType ) ) );
				return;
			}

			// Mixed signed/unsigned comparisons are allowed by C++ but produce warnings
			// in some configurations; for now we leave them alone.
		}

		private void ValidateArithmeticUnaryOperator( UnOp unOp )
		{
			Typespec? operandType = typeResolver.Resolve( unOp.expr );
			if( operandType == null )
				return;

			if( operandType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarNumber( operandType ) )
				AddError( unOp, String.Format( "Operator '{0}' cannot be applied to type '{1}'", OperatorName( unOp.op ), FormatType( operandType ) ) );
		}

		private void ValidateComplementOperator( UnOp unOp )
		{
			Typespec? operandType = typeResolver.Resolve( unOp.expr );
			if( operandType == null )
				return;

			if( operandType is not TypespecBasic )
				return;

			if( !OperatorRules.IsScalarInteger( operandType )
			 && !OperatorRules.IsScalarBitFamily( operandType ) )
				AddError( unOp, String.Format( "Operator '~' cannot be applied to type '{0}'", FormatType( operandType ) ) );
		}

		private static string OperatorName( Operand op )
		{
			return op switch {
				Operand.Add             => "+",
				Operand.Subtract        => "-",
				Operand.Multiply        => "*",
				Operand.FractionalDivide          => "\u00f7",
				Operand.Divide => "/",
				Operand.Modulo          => "%",
				Operand.BitAnd          => "&",
				Operand.BitOr           => "|",
				Operand.BitXor          => "^",
				Operand.LeftShift       => "<<",
				Operand.RightShift      => ">>",
				Operand.Equal           => "==",
				Operand.NotEqual        => "!=",
				Operand.LessThan        => "<",
				Operand.LessEqual       => "<=",
				Operand.GreaterThan     => ">",
				Operand.GreaterEqual    => ">=",
				Operand.Comparison      => "<=>",
				Operand.PrePlus         => "+",
				Operand.PreMinus        => "-",
				Operand.PreIncr         => "++",
				Operand.PreDecr         => "--",
				Operand.PostIncr        => "++",
				Operand.PostDecr        => "--",
				_                       => op.ToString(),
			};
		}

		private static Typespec? InferAutoType( Typespec? initType )
		{
			if( initType == null )
				return null;

			return initType switch {
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedInteger
					=> new TypespecBasic { kind = TypespecBasic.Kind.Integer, size = 4 },
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedFloat
					=> new TypespecBasic { kind = TypespecBasic.Kind.Float, size = Dialect.DefaultFloatSize() },
				_ => initType,
			};
		}

		private void ValidateVarAttributes( VarDecl vd )
		{
			bool isInsideStruct = vd.IsInStruct;
			bool isStatic       = vd.IsStatic;
			bool isHidden       = vd.IsHidden;
			bool isCompileTime  = vd.IsCompileTime;
			bool isInline       = vd.IsInline;
			bool isExtern       = vd.IsExternal;
			bool isConstType    = (vd.type.qual & Qualifier.Const) != 0;

			if( isInsideStruct ) {
				if( isHidden )                                                AddError( vd, "[hide]/[hidden] is only valid at module/namespace scope." );
				if( isExtern )                                                AddError( vd, "[extern] is only valid at module/namespace scope." );
				if( isInline && !isStatic )                                   AddError( vd, "[inline] on a class field requires [static]." );
				if( isCompileTime && !isStatic )                              AddError( vd, "[ct] on a class field requires [static]." );
			} else {
				if( isStatic )                                                AddError( vd, "[static] is valid only on class fields; use [hide] for module variables." );
				if( isInline && isHidden )                                    AddError( vd, "[inline] and [hide] are mutually exclusive." );
				if( isExtern && isInline )                                    AddError( vd, "[extern] and [inline] are mutually exclusive." );
				if( isExtern && isHidden )                                    AddError( vd, "[extern] and [hide]/[hidden] are mutually exclusive." );
				if( isExtern && isCompileTime )                               AddError( vd, "[extern] cannot be used with [ct]." );
				if( isExtern && isConstType )                                 AddError( vd, "[extern] cannot be used with const." );
				if( isExtern && vd.init != null )                             AddError( vd, "[extern] variables cannot have an initializer." );
			}
		}

		private void ValidateDefaultSizedField( VarDecl vd )
		{
			if( !vd.IsInStruct )
				return;

			if( vd.type is not TypespecBasic basic )
				return;

			DefaultTypeMode mode = basic.kind switch {
				TypespecBasic.Kind.Integer => Dialect.DefaultInt,
				TypespecBasic.Kind.Unsigned => Dialect.DefaultUInt,
				TypespecBasic.Kind.Float   => Dialect.DefaultFloat,
				TypespecBasic.Kind.Bitwise     => Dialect.DefaultBint,
				_                          => DefaultTypeMode.SizeIndeterminate,
			};

			if( (mode & DefaultTypeMode.ForbiddenInStruct) != 0 ) {
				AddError( vd, String.Format(
					"Default-sized type '{0}' is not allowed as a struct/class field in the active dialect",
					FormatType( basic ) ) );
			}
		}

		private void ValidateUniqueVarNames( Hierarchical h )
		{
			HashSet<string> seen = new();

			foreach( Decl child in h.children ) {
				if( child is not VarDecl vd )
					continue;

				if( !seen.Add( vd.name ) )
					AddError( vd, String.Format( "Duplicate variable/field declaration: {0}", vd.name ) );
			}
		}

		/// <summary>
		/// Wraps untyped integer literals in a static_cast when they are used where a
		/// bit-family type (<c>bint</c>, <c>b8</c> ... or <c>byte</c>) is expected.
		/// This is required because C++ <c>std::byte</c> and small fixed-width integers
		/// do not accept integer literals directly.
		/// </summary>
		private Expr BindToBitType( Expr expr, Typespec? targetType )
		{
			if( targetType == null )
				return expr;

			if( targetType is not TypespecBasic targetBasic )
				return expr;

			if( targetBasic.kind is not (TypespecBasic.Kind.Bitwise or TypespecBasic.Kind.Byte) )
				return expr;

			if( targetType.ptrs is { Count: > 0 } )
				return expr;

			Typespec? sourceType = typeResolver.Resolve( expr );

			if( sourceType is TypespecBasic { kind: TypespecBasic.Kind.Bitwise or TypespecBasic.Kind.Byte } )
				return expr;

			if( sourceType is not TypespecBasic { kind: TypespecBasic.Kind.UntypedInteger } )
				return expr;

			Typespec castType = new TypespecBasic {
				kind           = targetBasic.kind,
				size           = targetBasic.size,
				isDefaultSized = targetBasic.isDefaultSized,
				qual           = targetBasic.qual,
			};

			return new CastExpr {
				op     = Operand.StaticCast,
				type   = castType,
				expr   = expr,
				Type   = castType,
				srcPos = expr.srcPos,
			};
		}

		private void ReportTypeConversionError(
			SrcPos     srcPos,
			Typespec   sourceType,
			Typespec   targetType,
			string     genericMessage )
		{
			if( ConversionRules.IsSlicingAttempt( sourceType, targetType ) ) {
				diagnostics.Add( new Diagnostic(
					srcPos,
					DiagnosticKind.Error,
					String.Format(
						"Slicing is not allowed in Myll; use a pointer or reference to '{0}' instead of a value",
						FormatType( targetType ) ) ) );
			}
			else {
				diagnostics.Add( new Diagnostic( srcPos, DiagnosticKind.Error, genericMessage ) );
			}
		}

		private void AddError( Decl decl, string message )
			=> AddError( decl.srcPos, message );

		private void AddError( Expr expr, string message )
			=> AddError( expr.srcPos, message );

		private void AddError( SrcPos srcPos, string message )
		{
			diagnostics.Add( new Diagnostic( srcPos, DiagnosticKind.Error, message ) );
		}

		private void AddWarning( SrcPos srcPos, string message )
		{
			diagnostics.Add( new Diagnostic( srcPos, DiagnosticKind.Warning, message ) );
		}

		private void ValidateShadowing( CompilationContext context )
		{
			if( Dialect.Shadowing == ShadowingMode.None )
				return;

			// Detect duplicate declarations in the same scope first.
			HashSet<(Scope Scope, string Name)> reported = new();
			foreach( (Decl local, Scope scope) in context.LocalDecls ) {
				if( !reported.Add( (scope, local.name) ) )
					continue;

				int count = context.LocalDecls.Count( ld
					=> ld.Scope == scope
					 && ld.Local.name == local.name );

				if( count > 1 ) {
					ReportLocalShadowing(
						local,
						local,
						"local variable or parameter",
						"local variable or parameter" );
				}
			}

			foreach( (Decl local, Scope scope) in context.LocalDecls ) {
				CheckOuterScopeShadowing( local, scope, context );
				CheckMemberCollisions( local, scope );
			}
		}

		private void CheckOuterScopeShadowing( Decl local, Scope scope, CompilationContext context )
		{
			foreach( (Decl other, Scope otherScope) in context.LocalDecls ) {
				if( other == local )
					continue;
				if( other.name != local.name )
					continue;
				if( !IsAncestorScope( otherScope, scope ) )
					continue;

				ReportLocalShadowing(
					local,
					other,
					"local variable or parameter",
					"outer local variable or parameter" );
				return; // one report per local is enough
			}
		}

		private static bool IsAncestorScope( Scope? ancestor, Scope? descendant )
		{
			while( descendant != null ) {
				if( descendant == ancestor )
					return true;
					descendant = descendant.parent;
			}

			return false;
		}

		private void CheckMemberCollisions( Decl local, Scope scope )
		{
			if( (Dialect.Shadowing & (ShadowingMode.WarnLocalMemberCollision | ShadowingMode.ErrorLocalMemberCollision)) == 0 )
				return;

			for( Scope? outer = scope.parent; outer != null; outer = outer.parent ) {
				if( outer.decl is not Structural )
					continue;

				if( !outer.children.TryGetValue( local.name, out List<ScopeLeaf>? leaves ) )
					continue;

				foreach( ScopeLeaf leaf in leaves ) {
					Decl? member = leaf.decl;
					if( member == null )
						continue;

					if( member.IsInStruct && !member.IsStatic ) {
						ReportLocalMemberCollision( local, member );
						return; // one report per local is enough
					}
				}
			}
		}

		private void ReportLocalShadowing( Decl decl, Decl shadowed, string declKind, string shadowedKind )
		{
			bool asError = (Dialect.Shadowing & ShadowingMode.ErrorLocalShadowing) != 0;
			bool asWarn  = (Dialect.Shadowing & ShadowingMode.WarnLocalShadowing) != 0;

			if( !asError && !asWarn )
				return;

			string message = String.Format(
				"{0} '{1}' shadows {2} '{3}'",
				declKind, decl.name, shadowedKind, shadowed.name );

			if( asError )
				AddError( decl, message );
			else
				AddWarning( decl.srcPos, message );
		}

		private void ReportLocalMemberCollision( Decl local, Decl member )
		{
			bool asError = (Dialect.Shadowing & ShadowingMode.ErrorLocalMemberCollision) != 0;
			bool asWarn  = (Dialect.Shadowing & ShadowingMode.WarnLocalMemberCollision) != 0;

			if( !asError && !asWarn )
				return;

			string memberKind = member switch {
				Func => "method",
				_    => "field",
			};

			string message = String.Format(
				"Local '{0}' collides with instance {1} '{2}'",
				local.name, memberKind, member.name );

			if( asError )
				AddError( local, message );
			else
				AddWarning( local.srcPos, message );
		}

		private static string FormatType( Typespec type )
		{
			return type switch {
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedInteger
				                       => "untyped integer",
				TypespecBasic basic when basic.kind == TypespecBasic.Kind.UntypedFloat
				                       => "untyped float",
				_                      => type.GenType(),
			};
		}
	}
}

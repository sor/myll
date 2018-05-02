using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MyLang.Core;

namespace MyLang {
	partial class MyLangVisitor : MyLangBaseVisitor<IBase> {
		public MyType VisitType( MyLangParser.AnyTypeContext context ) {
			MyType t;
			if( context.basicType() != null )
			{
				t = (MyBasicType)VisitBasicType( context.basicType() );
			}
			else
			{
				t = (MyAdvancedType)VisitAdvancedType( context.advancedType() );
			}
			t.ptr = context.typePtr().Select( q => (Pointer)VisitTypePtr( q ) );
			t.qualifier = VisitTypeQualifiers( context.typeQualifier() );
			return t;
		}

		public new MyBasicType VisitBasicType( MyLangParser.BasicTypeContext context ) {
			if( context.VOID() != null )		return new MyBasicType( MyBasicType.Type.Void );
			else if( context.BOOL() != null )	return new MyBasicType( MyBasicType.Type.Bool );

			var ct = context.characterType(); // TODO: restliche chars
			if( ct != null )					return new MyBasicType( MyBasicType.Type.Char );

			var	ft = context.floatingType();
			if( ft != null )
			{
				if( ft.FLOAT() != null )		return new MyBasicType( MyBasicType.Type.Float );
				else if( ft.LONG() != null )	return new MyBasicType( MyBasicType.Type.LongDouble );
				else							return new MyBasicType( MyBasicType.Type.Double );
			}

			var	it = context.integerType();
			if( it != null )
			{
				var t = new MyBasicType();
				t.sign = context.signQualifier().ToSignedness();

				if( it.CHAR() != null )			t.type = MyBasicType.Type.Char;
				else if( it.SHORT() != null )	t.type = MyBasicType.Type.Short;
				else
				{
					var longs = it.LONG();
					switch( longs.Length )
					{
						case 2:					t.type = MyBasicType.Type.LongLong;		break;
						case 1:					t.type = MyBasicType.Type.Long;			break;
						default:				t.type = MyBasicType.Type.Int;			break;
					}
					if( t.sign == MyBasicType.Signedness.DontCare )
						t.sign = it.signQualifier().ToSignedness();
				}
				return t;
			}

			throw new Exception( "This Point will never be reached, hopefully..." );
		}

		public new MyAdvancedType VisitAdvancedType( MyLangParser.AdvancedTypeContext context ) {
			var t = new MyAdvancedType();
			t.segments = context.idTplType().Select( q => (MySegment)Visit( q ) );
			return t;
		}
	}

	partial class MyLangVisitor : MyLangBaseVisitor<IBase> {
		public MyExpression VisitExpr( /*Antlr4.Runtime.Tree.IParseTree*/ MyLangParser.ExprContext context ) {
			if( context == null )
				return null;

			return (MyExpression)Visit( context );
		}

		public new MyExpressions VisitExprs( MyLangParser.ExprsContext context ) {
			if( context == null )
				return new MyExpressions( 0 );

			var ret = new MyExpressions( context.expr().Length );
			foreach( var s in context.expr() )
			{
				ret.Add( VisitExpr( s ) );
			}
			return ret;
		}

		public override IBase VisitPreOpExpr( MyLangParser.PreOpExprContext context ) {
			return new PreOpExpr( VisitExpr( context.expr() ), context.pre_OP().GetText() );
		}

		public override IBase VisitNewExpr( MyLangParser.NewExprContext context ) {
			return new NewExpr( VisitType( context.anyType() ), VisitExprs( context.exprs() ) );
		}

		public override IBase VisitDeleteExpr( MyLangParser.DeleteExprContext context ) {
			return new DeleteExpr( VisitExpr( context.expr() ), context.ary != null );
		}
	}
}

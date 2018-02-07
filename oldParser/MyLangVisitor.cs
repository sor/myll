using System;
using System.Collections.Generic;
using System.Linq;
//using System.Text;

using JanSordid.MyLang.Backend;

namespace JanSordid.MyLang
{
	partial class MyLangVisitor : MyLangBaseVisitor<MyBase>
	{
		static readonly Dictionary<int, MyQualifier> typeToQual =
					new Dictionary<int, MyQualifier> {
			{	MyLangParser.CONST,		MyQualifier.Const		},
			{	MyLangParser.VOLATILE,	MyQualifier.Volatile	},
			{	MyLangParser.MUTABLE,	MyQualifier.Mutable		},
		};

		static readonly Dictionary<int, MyPointer.Type> typeToPtr =
					new Dictionary<int, MyPointer.Type> {
			{	MyLangParser.STAR,		MyPointer.Type.Raw		},
			{	MyLangParser.AMP,		MyPointer.Type.LVRef	},
			{	MyLangParser.DBL_AMP,	MyPointer.Type.RVRef	},
			{	MyLangParser.AT_BANG,	MyPointer.Type.Unique	},
			{	MyLangParser.AT_PLUS,	MyPointer.Type.Shared	},
			{	MyLangParser.AT_QUEST,	MyPointer.Type.Weak		},
		};

		const int indent_mul = 2;
		protected int indent = 0;
		protected string I { get { return new String( ' ', indent * indent_mul ); } }

		protected bool is_static = false;

		public MyQualifier VisitTypeQualifiers( MyLangParser.TypeQualifierContext[] context )
		{
			return context
					.Select( q => typeToQual[q.qual.Type] )
					.Aggregate( MyQualifier.None, ( a, q ) => a |= q );
		}

		// MyPointer -> ToPointer
		public override MyBase VisitTypePtr( MyLangParser.TypePtrContext context )
		{
			var p = new MyPointer();

			if ( context.ptr != null )	// Pointer
			{
				p.type = typeToPtr[context.ptr.Type];
			}
			else	// Array
			{
				if ( context.content != null )
					p.content = context.content.Text;

				switch ( context.ary.Type )
				{
					case MyLangParser.LBRACK:
						p.type = MyPointer.Type.RawArray;
						break;

					case MyLangParser.AT_LBRACK:
						p.type = (p.content != null)
							? MyPointer.Type.Array
							: MyPointer.Type.Vector;
						break;
				}
			}
			p.qualifier = VisitTypeQualifiers( context.typeQualifier() );
			return p;
		}

		/*public override int VisitInt( MyLangParser.IntContext context )
		{
			return int.Parse( context.INTEGER_LIT().GetText() );
		}*/

		public MyStatement VisitStmt( MyLangParser.StatementContext tree )
		{
			if ( tree == null )
				return null;

			return (MyStatement) Visit( tree );
		}

		

		public MyStatements VisitStmts( MyLangParser.StatementsContext context )
		{
			var stmts	= context.statement();
			var ret		= new MyStatements( stmts.Length );
			foreach ( var s in stmts )
			{
				ret.Add( VisitStmt( s ) );
			}
			return ret;
		}
		/*|	expr	(	post_OP
				|	'['		expr		']'
				|	'('		expr_list?	')'
				|	mem_OP	ID
				)						# Tier2*/
		public override MyBase VisitTier2( MyLangParser.Tier2Context context )
		{
			if( context.post_OP() != null )
				return new Backend.MyPostOp( VisitExpr( context.expr( 0 ) ), context.post_OP().GetText() );
			else if( context.mem_OP() != null )
				return new Backend.MyBinaryOperator(
					context.mem_OP().GetText(),
					VisitExpr( context.expr( 0 ) ),
					new MyId( context.ID().Symbol.Text )
					);
			else if ( context.expr( 1 ) != null )
				return new Backend.MySubscript(
					VisitExpr( context.expr( 0 ) ),
					VisitExpr( context.expr( 1 ) )
					);
			else
				return new Backend.MyFuncMethCall(
					VisitExpr( context.expr( 0 ) ),
					VisitExprs( context.exprs() )
					);
		}

		public override MyBase VisitTier6( MyLangParser.Tier6Context context )
		{
			return new Backend.MyBinaryOperator(
				context.add_OP().GetText(),
				VisitExpr( context.expr( 0 ) ),
				VisitExpr( context.expr( 1 ) )
				);
		}

		public override MyBase VisitTier8( MyLangParser.Tier8Context context )
		{
			return new Backend.MyBinaryOperator(
				context.order_OP().GetText(),
				VisitExpr( context.expr( 0 ) ),
				VisitExpr( context.expr( 1 ) )
				);
		}

		public override MyBase VisitTier15( MyLangParser.Tier15Context context )
		{
			if( context.assign_OP() != null )
				return new Backend.MyBinaryOperator(
					context.assign_OP().GetText(),
					VisitExpr( context.expr( 0 ) ),
					VisitExpr( context.expr( 1 ) )
				);
			else
				return new Backend.MyNopExpr();
		}

		public override MyBase VisitTier200( MyLangParser.Tier200Context context ) {
			var ai = context.AUTOINDEX();
			return new MyId( ai.Symbol.Text );
		}

		public new Backend.MyParameters VisitParameters( MyLangParser.ParametersContext context )
		{
			var paras	= context.parameter();
			var ret = new Backend.MyParameters( paras.Length );
			foreach ( var p in paras )
			{
				ret.Add( (MyNamedType) Visit( p ) );
			}
			return ret;
		}

		//public override MyBase VisitAddSub( MyLangParser.AddSubContext context )
		//{
		//	return new MyBinaryOperatorExpr(
		//		context.op.Type == MyLangParser.PLUS ? "+" : "-",
		//		VisitExpr( context.expr( 0 ) ),
		//		VisitExpr( context.expr( 1 ) )
		//		);
		//}

		//public override MyBase VisitMulDivMod( MyLangParser.MulDivModContext context )
		//{
		//	return new MyBinaryOperatorExpr(
		//		context.op.Type == MyLangParser.STAR
		//			? "*"
		//			: context.op.Type == MyLangParser.SLASH
		//				? "/"
		//				: "%",
		//		VisitExpr( context.expr( 0 ) ),
		//		VisitExpr( context.expr( 1 ) )
		//		);
		//}
		/*public override MyLang VisitPow( MyLangParser.PowContext context )
		{
			int left = Visit( context.expr( 0 ) );
			int right = Visit( context.expr( 1 ) );
			return (int) Math.Round( Math.Pow( left, right ) );
		}
		public override MyLang VisitSqrt( MyLangParser.SqrtContext context )
		{
			int value = Visit( context.expr() );
			return (int) Math.Sqrt( value );
		}
		 * */

		//public override MyBase VisitLShift( MyLangParser.LShiftContext context )
		//{
		//	return new MyBinaryOperatorExpr(
		//		"<<",
		//		VisitExpr( context.expr( 0 ) ),
		//		VisitExpr( context.expr( 1 ) )
		//		);
		//}

		//public override MyBase VisitRShift( MyLangParser.RShiftContext context )
		//{
		//	return new MyBinaryOperatorExpr(
		//		">>",
		//		VisitExpr( context.expr( 0 ) ),
		//		VisitExpr( context.expr( 1 ) )
		//		);
		//}

		// MyExpression
		public override MyBase VisitIdOrLit( MyLangParser.IdOrLitContext context )
		{
			switch ( context.arg.Type )
			{
				case MyLangParser.ID:			return new MyId(				context.ID			().Symbol.Text ); // same as context.arg.Text, context.ID().Symbol.Text
				case MyLangParser.INTEGER_LIT:	return new MyIntegerLiteral(	context.INTEGER_LIT	().Symbol.Text );
				case MyLangParser.FLOAT_LIT:	return new MyFloatingLiteral(	context.FLOAT_LIT	().Symbol.Text );
				case MyLangParser.STRING_LIT:	return new MyStringLiteral(		context.STRING_LIT	().Symbol.Text );
				case MyLangParser.CHAR_LIT:		return new MyCharLiteral(		context.CHAR_LIT	().Symbol.Text );

				default:						throw new Exception( "This Point will never be reached, hopefully..." );
			}
		}

		public override MyBase VisitIdOrLitExpr( MyLangParser.IdOrLitExprContext context )
		{
			return Visit( context.idOrLit() );
		}

		// 'if'		LPAREN expr RPAREN statement ( 'else' statement )?	# IfStmt
		public override MyBase VisitIfStmt( MyLangParser.IfStmtContext context ) {
			var ifs = new MyIfStmt(
				VisitExpr( context.expr() ),
				VisitStmt( context.statement( 0 ) ),
				VisitStmt( context.statement( 1 ) )
				);

			return ifs;
		}

		public override MyBase VisitTimesStmt( MyLangParser.TimesStmtContext context ) {
			var ts = new MyTimesStmt(
				VisitExpr( context.expr() ),
				VisitStmt( context.statement() )
				);

			return ts;
		}

		public override MyBase VisitBlockStmt( MyLangParser.BlockStmtContext context )
		{
			var block = new MyBlockStatement();
			block.list = VisitStmts( context.statements() );
			return block;
		}

		public override MyBase VisitParenExpr( MyLangParser.ParenExprContext context ) {
			var paren = new Backend.MyParenExpr( VisitExpr( context.expr() ) );
			return paren;
		}

		public override MyBase VisitReturnStmt( MyLangParser.ReturnStmtContext context )
		{
			var r = new MyReturnStmt();
			r.expr = VisitExpr( context.expr() );
			return r;
		}

		public override MyBase VisitExpressionStmt( MyLangParser.ExpressionStmtContext context )
		{
			var tmp = new MyExpressionStatement( VisitExpr( context.expr() ) );
			return tmp;
		}

		public override MyBase VisitNamespace( MyLangParser.NamespaceContext context )
		{
			Backend.MyNamespace ns = null;
			int push_count = 0;
			foreach ( var id in context.ID() )
			{
				ns = new Backend.MyNamespace( id.IDToString() );
				push_count++;

				Console.WriteLine(
					I + "namespace " + id + "\n" +
					I + "{" );
				++indent;
			}

			var ret = base.VisitNamespace( context );

			if ( context.SEMI() == null )
			{
				for ( ; push_count > 0; push_count-- )
				{
					ns.ContextUp();

					--indent;
					Console.WriteLine(
						I + "}" );
				}
			}

			return ret;
		}

		public override MyBase VisitEnumDecl( MyLangParser.EnumDeclContext context )
		{
			var enm = new Backend.MyEnum( context.ID().IDToString() );

			foreach ( var kv in context.id_opt_value() )
			{
				if( kv.expr() == null )	enm.Add( kv.ID().Symbol.Text );
				else					enm.Add( kv.ID().Symbol.Text, (MyExpression) Visit( kv.expr() ) );
			}

			return base.VisitEnumDecl( context );
		}

		public override MyBase VisitClassDecl( MyLangParser.ClassDeclContext context )
		{
			var seg = (MySegment) Visit( context.idTplType() );
			var cls = new Backend.MyClass( seg.ToString() );

			Console.WriteLine(
				I + "class " + context.idTplType().GetText() + "\n" +
				I + "{" );
			++indent;
			var ret = base.VisitClassDecl( context );
			--indent;
			Console.WriteLine(
				I + "};" );

			cls.GatherIdentifier();

			cls.ContextUp();

			return ret;
		}

		// MySegment
		public override MyBase VisitIdTplType( MyLangParser.IdTplTypeContext context )
		{
			var t = new MySegment();
			t.name = context.name.Text;
			var tpl  = context.tpl;
			if ( tpl != null )
			{
				t.template_params = tpl.anyTypeOrConst().Select( q => (MyTemplateParam) Visit( q ) );
			}
			return t;
		}

		// MyTemplateParam
		public override MyBase VisitAnyTypeOrConst( MyLangParser.AnyTypeOrConstContext context )
		{
			var t		= new MyTemplateParam();
			var cnst	= context.INTEGER_LIT();

			if ( cnst != null )	t.constant	= cnst.Symbol.Text;
			else				t.type		= VisitType( context.anyType() );

			return t;
		}

		// MyNamedType
		public override MyBase VisitParameter( MyLangParser.ParameterContext context )
		{
			var t	= new MyNamedType();
			t.type = VisitType( context.anyType() );
			t.name = context.ID().Symbol.Text;

			return t;
		}

		public override MyBase VisitVariableDecl( MyLangParser.VariableDeclContext context ) {
			return new Backend.MyVarDecl(
							(Backend.MyFields) Visit( context.field_expr() )
						);
		}

		// anyType ID (COMMA ID)*
		public override MyBase VisitFieldDecl( MyLangParser.FieldDeclContext context )
		{
			var t		= VisitType( context.anyType() );
			var fields	= new Backend.MyFields( context.id_opt_value().Length );

			foreach( var kv in context.id_opt_value() )
			{
				Backend.MyField mf;

				if( kv.expr() == null )	mf = new Backend.MyField( kv.ID().IDToString(), t );
				else					mf = new Backend.MyField( kv.ID().IDToString(), t, VisitExpr( kv.expr() ) );
				
				fields.Add( mf );
			}

			if( MyHierarchic.current is MyStructural )
			{
				foreach( var field in fields.list )
				{
					((MyStructural)MyHierarchic.current).AddField( field.name, field );
				}
			}

			return fields;
		}

		public override MyBase VisitCtorDef( MyLangParser.CtorDefContext context )
		{
			var paras = VisitParameters( context.parameters() );
			Backend.MyCtor ctor	= new Backend.MyCtor( "ctor(" + paras.ToTypeString() + ")" );
			ctor.parameters		= paras;
			ctor.statements		= VisitStmts( context.statements() );

			Console.WriteLine( ctor );
			return ctor;
		}
		/*(	anyType	ID parameterList
				  |			ID parameterList RARROW anyType
				  ) LCURLY statements RCURLY
					  */
		public override MyBase VisitFuncMeth( MyLangParser.FuncMethContext context )
		{
			var paras				= VisitParameters( context.parameters() );
			var name				= context.ID().Symbol.Text;
			var name_with_types		= name + "(" + paras.ToTypeString() + ")";
			var funcmeth			= new Backend.MyFuncMeth( name_with_types );
			funcmeth.parameters		= paras;
			funcmeth.statements		= VisitStmts( context.statements() );
			funcmeth.return_type	= ( context.anyType() != null )
				? VisitType( context.anyType() )
				: new MyBasicType( MyBasicType.Type.Void );

			Console.WriteLine( funcmeth.ToString() );

			if( MyHierarchic.current is MyStructural )
			{
				((MyStructural)MyHierarchic.current).AddMethod( name, paras, funcmeth );
			}

			return funcmeth;
		}

#if false
		public override int VisitTypeDecl( MyLangParser.TypeDeclContext context )
		{
			string ret;

			ret = context.any_type().GetText();
			/*
			if ( context.blubb.Type == MyLangParser.UNIQ_PTR )
				ret = "std::unique_ptr<" + ret + ">";
			else
				ret = ret + context.blubb.Text;
			*/
			Console.WriteLine(
				I + "typename "
				+ context.any_type().GetText()
				+ " :: typeptr "
				+ String.Join( ",", context.type_ptr().Select( a => a.GetText() ) )
				+ " :: result "
				+ ret
			);
			return base.VisitTypeDecl( context );
		}
#endif

		/*
		"*"		"int *"		"{1} *"
		"@!"	"int @!"	"std::unique_ptr<{1}>"
		*/
		public override MyBase VisitComment( MyLangParser.CommentContext context )
		{
			Console.WriteLine( I + "commi " + context.COMMENT().GetText() );
			return null;
		}

		public override MyBase VisitParensExpr( MyLangParser.ParensExprContext context )
		{
			return Visit( context.expr() );
		}

		//public override MyBase VisitFunctionDecl( MyLangParser.FunctionDeclContext context )
		//{
		//	MyFunction func = new MyFunction();

		//	var fmd = context.funcmeth_decl();
		//	// ID parameterList (RARROW anyType)? LCURLY RCURLY
			
		//	func.name			= fmd.ID().GetText();
		//	func.return_type	= VisitType( fmd.anyType() );
		//	func.parameters		= fmd.parameterList().parameter().Select( q => (MyNamedType) Visit( q ) );
		//	func.statements		= fmd.statements().statement().Select( q => q.GetText() );
		//	Console.WriteLine( func.Gen() );
		//	return base.VisitFunctionDecl( context );
		//}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
//using System.Text;

using MyLang.Core;

namespace MyLang
{
	partial class MyLangVisitor : MyLangBaseVisitor<IBase>
	{
		static readonly Dictionary<int, Qualifier.Type> typeToQual =
					new Dictionary<int, Qualifier.Type> {
			{	MyLangParser.CONST,		Qualifier.Type.Const		},
			{	MyLangParser.VOLATILE,	Qualifier.Type.Volatile	},
			{	MyLangParser.MUTABLE,	Qualifier.Type.Mutable		},
		};

		static readonly Dictionary<int, Pointer.Type> typeToPtr =
					new Dictionary<int, Pointer.Type> {
			{	MyLangParser.STAR,		Pointer.Type.RawPtr			},
			{	MyLangParser.PTR_TO_ARY,Pointer.Type.RawPtrToArray	},
			{	MyLangParser.AMP,		Pointer.Type.LVRef			},
			{	MyLangParser.DBL_AMP,	Pointer.Type.RVRef			},
			{	MyLangParser.AT_BANG,	Pointer.Type.Unique			},
			{	MyLangParser.AT_PLUS,	Pointer.Type.Shared			},
			{	MyLangParser.AT_QUEST,	Pointer.Type.Weak				},
		};

		public MyLangVisitor() {
			hierarchy.Push( new MyGlobal() );
		}
		/*
		const int indent_mul = 2;
		protected int indent = 0;
		protected string I { get { return new String( ' ', indent * indent_mul ); } }

		protected bool is_static = false;
		*/

		Stack<MyHierarchic> hierarchy = new Stack<MyHierarchic>();

		public Qualifier VisitTypeQualifiers( MyLangParser.TypeQualifierContext[] context )
		{
			return new Qualifier()
			{
				type = context
					.Select( q => typeToQual[q.qual.Type] )
					.Aggregate( Qualifier.Type.None, (a, q) => a |= q )
			};
		}

		// MyPointer -> ToPointer
		public override IBase VisitTypePtr( MyLangParser.TypePtrContext context )
		{
			var p = new Pointer();

			if ( context.ptr != null )	// Pointer
			{
				p.type = typeToPtr[context.ptr.Type];
			}
			else	// Array
			{
				if ( context.expr() != null )
					p.content = VisitExpr( context.expr() ); // TODO: echte behandlung von allem darin

				switch ( context.ary.Type )
				{
					case MyLangParser.LBRACK:
						p.type = Pointer.Type.RawArray;
						break;

					case MyLangParser.AT_LBRACK:
						p.type = (p.content != null)
							? Pointer.Type.Array
							: Pointer.Type.Vector;
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
		public override IBase VisitTier2( MyLangParser.Tier2Context context )
		{
			if( context.post_OP() != null )
				return new PostOpExpr( VisitExpr( context.expr( 0 ) ), context.post_OP().GetText() );
			else if( context.mem_OP() != null )
				return new BinOpExpr(
					context.mem_OP().GetText(),
					VisitExpr( context.expr( 0 ) ),
					new MyId( context.ID().Symbol.Text )
					);
			else if ( context.expr( 1 ) != null )
				return new SubscriptExpr(
					VisitExpr( context.expr( 0 ) ),
					VisitExpr( context.expr( 1 ) )
					);
			else
				return new MyFuncMethCall(
					VisitExpr( context.expr( 0 ) ),
					VisitExprs( context.exprs() )
					);
		}

		public override IBase VisitTier5( MyLangParser.Tier5Context context ) {
			return new BinOpExpr(
				context.mult_OP().GetText(),
				VisitExpr( context.expr( 0 ) ),
				VisitExpr( context.expr( 1 ) )
				);
		}

		public override IBase VisitTier6( MyLangParser.Tier6Context context )
		{
			return new BinOpExpr(
				context.add_OP().GetText(),
				VisitExpr( context.expr( 0 ) ),
				VisitExpr( context.expr( 1 ) )
				);
		}

		public override IBase VisitTier8( MyLangParser.Tier8Context context )
		{
			return new BinOpExpr(
				context.order_OP().GetText(),
				VisitExpr( context.expr( 0 ) ),
				VisitExpr( context.expr( 1 ) )
				);
		}

		public override IBase VisitTier15( MyLangParser.Tier15Context context )
		{
			if( context.assign_OP() != null )
				return new BinOpExpr(
					context.assign_OP().GetText(),
					VisitExpr( context.expr( 0 ) ),
					VisitExpr( context.expr( 1 ) ),
					Assoc.Right
				);
			else
				return new NopExpr();
		}

		public override IBase VisitTier200( MyLangParser.Tier200Context context ) {
			var ai = context.AUTOINDEX();
			return new MyId( ai.Symbol.Text );
		}

		public new MyParameters VisitParameters( MyLangParser.ParametersContext context )
		{
			var paras = context.parameter();
			var ret = new MyParameters( paras.Length );
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
		public override IBase VisitIdOrLit( MyLangParser.IdOrLitContext context )
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

		public override IBase VisitIdOrLitExpr( MyLangParser.IdOrLitExprContext context )
		{
			return Visit( context.idOrLit() );
		}

		// 'if'		LPAREN expr RPAREN statement ( 'else' statement )?	# IfStmt
		public override IBase VisitIfStmt( MyLangParser.IfStmtContext context ) {
			var ifs = new MyIfStmt(
				VisitExpr( context.expr() ),
				VisitStmt( context.statement( 0 ) ),
				VisitStmt( context.statement( 1 ) )
				);

			return ifs;
		}

		public MyId VisitID( Antlr4.Runtime.Tree.ITerminalNode node ) {
			if( node == null )
				return null;

			return new MyId( node.Symbol.Text );
		}

		public override IBase VisitTimesStmt( MyLangParser.TimesStmtContext context ) {
			var ts = new MyTimesStmt(
				VisitExpr( context.expr() ),
				VisitStmt( context.statement() ),
				VisitID( context.ID() )
				);

			return ts;
		}

		public override IBase VisitBlockStmt( MyLangParser.BlockStmtContext context )
		{
			var block = new MyBlockStatement();
			block.list = VisitStmts( context.statements() );
			return block;
		}

		public override IBase VisitParenExpr( MyLangParser.ParenExprContext context ) {
			var paren = new ParenExpr( VisitExpr( context.expr() ) );
			return paren;
		}

		public override IBase VisitReturnStmt( MyLangParser.ReturnStmtContext context )
		{
			var r = new MyReturnStmt();
			r.expr = VisitExpr( context.expr() );
			return r;
		}

		public override IBase VisitExpressionStmt( MyLangParser.ExpressionStmtContext context )
		{
			var tmp = new MyExpressionStatement( VisitExpr( context.expr() ) );
			return tmp;
		}
		/*
		public override IBase VisitAssignmentStmt( MyLangParser.AssignmentStmtContext context ) {
			AssignmentStmt astmt = new AssignmentStmt();
			var exprs = context.expr();
			foreach( var expr in exprs )
				astmt.exprs.Add( VisitExpr( expr ) );
			astmt.ops.AddRange( context.assign_OP().Select( q => q.GetText() ) );

			return astmt;
		}*/

		public override IBase VisitNamespace( MyLangParser.NamespaceContext context )
		{
			MyNamespace ns = null;
			int push_count = 0;

			foreach ( var id in context.ID() )
			{
				ns = new MyNamespace( id.IDToString(), hierarchy.Peek() );

				hierarchy.Push( ns );

				push_count++;

				Console.WriteLine( "namespace " + id + "\n" + "{" );
			}

			var ret = base.VisitNamespace( context );

			if ( context.SEMI() == null )
			{
				while( push_count-- > 0 )
				{
					hierarchy.Pop();

					Console.WriteLine( "}" );
				}
			}

			return ret;
		}

		public override IBase VisitEnumDecl( MyLangParser.EnumDeclContext context )
		{
			var enm = new MyEnum( context.ID().IDToString(), hierarchy.Peek() );

			foreach ( var kv in context.id_opt_value() )
			{
				if( kv.expr() == null )	enm.Add( kv.ID().Symbol.Text );
				else					enm.Add( kv.ID().Symbol.Text, (MyExpression) Visit( kv.expr() ) );
			}

			return base.VisitEnumDecl( context );
		}

		public override IBase VisitClassDecl( MyLangParser.ClassDeclContext context )
		{
			IBase ret;
			var seg = (MySegment) Visit( context.idTplType() );
			var cls = new MyClass( seg.name, hierarchy.Peek(), seg.template_params );

			hierarchy.Push( cls );
			{
				ret = base.VisitClassDecl( context );

				Generator g = new Generator();
				g.Generate( cls );
				Console.WriteLine( g.ToString() );
			}
			hierarchy.Pop();

			//cls.GatherIdentifier();

			return ret;
		}

		public override IBase VisitAnyTypeOrConsts( MyLangParser.AnyTypeOrConstsContext context ) {
			MyTemplateArgs list = new MyTemplateArgs( context.anyTypeOrConst().Length );
			foreach( var atoc in context.anyTypeOrConst() )
			{
				list.Add( (MyTemplateArg)VisitAnyTypeOrConst( atoc ) );
			}
			return list;
		}

		// MySegment
		public override IBase VisitIdTplType( MyLangParser.IdTplTypeContext context )
		{
			var t = new MySegment( context.ID().IDToString() );
			if( context.anyTypeOrConsts() != null )
			{
				t.template_params = (MyTemplateArgs)VisitAnyTypeOrConsts( context.anyTypeOrConsts() );
			}
			return t;
		}

		// MyTemplateParam
		public override IBase VisitAnyTypeOrConst( MyLangParser.AnyTypeOrConstContext context )
		{
			if( context.INTEGER_LIT() != null )
				return new MyTemplateArgLiteral() { lit = new MyIntegerLiteral( context.INTEGER_LIT().Symbol.Text ) };
			else
				return new MyTemplateArgType() { type = VisitType( context.anyType() ) };

			/*
			var t		= new MyTemplateArg();
			var cnst	= context.INTEGER_LIT();

			if ( cnst != null )	t.constant	= cnst.Symbol.Text;
			else				t.type		= VisitType( context.anyType() );

			return t;
			*/
		}

		// MyNamedType
		public override IBase VisitParameter( MyLangParser.ParameterContext context )
		{
			var t	= new MyNamedType();
			t.type = VisitType( context.anyType() );
			t.name = context.ID().Symbol.Text;

			return t;
		}

		public override IBase VisitVariableDecl( MyLangParser.VariableDeclContext context ) {
			var vd = (MyFields)Visit( context.vars_decl() );
			// vd[0].lib == null, why?
			var mvd = new MyVarDecl( vd );
			foreach( var v in vd.list )
			{
				hierarchy.Peek().AddField( v );
			}
			return mvd;
		}

		public override IBase VisitPropDecl( MyLangParser.PropDeclContext context ) {
			var t		= VisitType( context.anyType() );
			var props	= new MyProperties( context.id_opt_value().Length );

			foreach( var kv in context.id_opt_value() )
			{
				MyProperty mp;

				if( kv.expr() == null )
					mp = new MyProperty( kv.ID().IDToString(), hierarchy.Peek(), t );
				else
					mp = new MyProperty( kv.ID().IDToString(), hierarchy.Peek(), t, VisitExpr( kv.expr() ) );

				props.Add( mp );
			}

			foreach( var prop in props.list )
			{
				hierarchy.Peek().AddProperty( prop );
			}

			return props;
		}

		public List<KeyValuePair<string,MyExpression>>
		VisitIOV( MyLangParser.Id_opt_valueContext[] iovs ) {
			var ret = new List<KeyValuePair<string, MyExpression>>( iovs.Length );
			foreach( var iov in iovs )
			{
				ret.Add( new KeyValuePair<string,MyExpression>(
								iov.ID().IDToString(),
								VisitExpr( iov.expr() ) ) );
			}
			return ret;
		}

		// anyType ID (COMMA ID)*
		// anyType id_opt_value (COMMA id_opt_value)*
		public override IBase VisitFieldDecl( MyLangParser.FieldDeclContext context )
		{
			var t		= VisitType( context.anyType() );
			var fields	= new MyFields( context.id_opt_value().Length );
			var parent	= hierarchy.Peek();

			foreach( var mf in VisitIOV( context.id_opt_value() ).Select( q => new MyField( q.Key, parent, t, q.Value ) ) )
				fields.Add( mf );
			/*
			foreach( var iov in context.id_opt_value() )
			{
				MyField mf;

				//if( iov.expr() == null )	mf = new MyField( iov.ID().IDToString(), hierarchy.Peek(), t );
				//else
				mf = new MyField( iov.ID().IDToString(), hierarchy.Peek(), t, VisitExpr( iov.expr() ) );

				fields.Add( mf );
			}*/

			foreach( var field in fields.list )
			{
				hierarchy.Peek().AddField( field );
			}

			return fields;
		}

		// anyType ID (COMMA ID)*
		// anyType id_opt_value (COMMA id_opt_value)*
		public override IBase VisitVarsDecl( MyLangParser.VarsDeclContext context ) {
			var t		= VisitType( context.anyType() );
			var fields	= new MyFields( context.id_opt_value().Length );
			var parent	= hierarchy.Peek();

			foreach( var mf in VisitIOV( context.id_opt_value() )
								.Select( q => new MyField( q.Key, parent, t, q.Value ) ) )
				fields.Add( mf );
			/*
			foreach( var iov in context.id_opt_value() )
			{
				MyField mf;

				//if( iov.expr() == null )	mf = new MyField( iov.ID().IDToString(), hierarchy.Peek(), t );
				//else
				mf = new MyField( iov.ID().IDToString(), hierarchy.Peek(), t, VisitExpr( iov.expr() ) );

				fields.Add( mf );
			}*/

			foreach( var field in fields.list )
			{
				parent.AddField( field );
			}

			return fields;
		}

		public override IBase VisitCtorDef( MyLangParser.CtorDefContext context )
		{
			var paras	= VisitParameters( context.parameters() );
			MyFuncMeth ctor	= new MyFuncMeth( "ctor(" + paras.ToTypeString() + ")", hierarchy.Peek() );
			ctor.parameters		= paras;
			ctor.statements		= VisitStmts( context.statements() );

			hierarchy.Peek().AddMethod( "ctor", paras, ctor );

			Console.WriteLine( ctor );
			return ctor;
		}
		/*(	anyType	ID parameterList
				  |			ID parameterList RARROW anyType
				  ) LCURLY statements RCURLY
					  */
		public override IBase VisitFuncMeth( MyLangParser.FuncMethContext context )
		{
			var paras				= VisitParameters( context.parameters() );
			var name				= context.ID().Symbol.Text;
			var name_with_types		= name + "(" + paras.ToTypeString() + ")";
			var funcmeth			= new MyFuncMeth( name_with_types, hierarchy.Peek() );

			hierarchy.Push( funcmeth );
			{
				//Generator g = new Generator();
				//g.Generate( cls );
				//Console.WriteLine( g.ToString() );

				funcmeth.parameters		= paras;
				funcmeth.statements		= VisitStmts( context.statements() );
				funcmeth.return_type	= ( context.anyType() != null )
					? VisitType( context.anyType() )
					: new MyBasicType( MyBasicType.Type.Void );

				Console.WriteLine( funcmeth.ToString() );
			}
			hierarchy.Pop();

			hierarchy.Peek().AddMethod( name, paras, funcmeth );

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
		public override IBase VisitComment( MyLangParser.CommentContext context )
		{
			Console.WriteLine( "commi " + context.COMMENT().GetText() );
			return null;
		}

		public override IBase VisitParensExpr( MyLangParser.ParensExprContext context )
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

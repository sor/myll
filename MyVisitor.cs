using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Myll.Core;

using Array = Myll.Core.Array;
using Enum = Myll.Core.Enum;

using static Myll.MyParser;

namespace Myll
{
	public class MyVisitor : MyParserBaseVisitor<object>
	{
		#region Types
		static readonly Dictionary<int, Qualifier> ToQual =
			new Dictionary<int, Qualifier> {
				{	MyParser.CONST,		Qualifier.Const		},
				{	MyParser.MUTABLE,	Qualifier.Mutable	},
				{	MyParser.VOLATILE,	Qualifier.Volatile	},
				{	MyParser.STABLE,	Qualifier.Stable	},
			};

		static readonly Dictionary<int, Pointer.Kind> ToPtr =
			new Dictionary<int, Pointer.Kind> {
				{	MyParser.AT_BANG,	Pointer.Kind.Unique	},
				{	MyParser.AT_PLUS,	Pointer.Kind.Shared	},
				{	MyParser.AT_QUEST,	Pointer.Kind.Weak	},
				{	MyParser.STAR,		Pointer.Kind.RawPtr	},
				{	MyParser.PTR_TO_ARY,Pointer.Kind.PtrToAry	},
				{	MyParser.AMP,		Pointer.Kind.LVRef	},
				{	MyParser.DBL_AMP,	Pointer.Kind.RVRef	},
				{	MyParser.AT_LBRACK,	Pointer.Kind.Vector	},
				{	MyParser.LBRACK,	Pointer.Kind.Array	},
			};

		private static readonly Dictionary<int, int> ToSize =
			new Dictionary<int, int>
			{
				{MyParser.FLOAT, 4}, // TODO: native best float
				{MyParser.F80, 10},
				{MyParser.F64, 8},
				{MyParser.F32, 4},
				{MyParser.F16, 2},
				{MyParser.BYTE, 1},
				{MyParser.B64, 8},
				{MyParser.B32, 4},
				{MyParser.B16, 2},
				{MyParser.B8, 1},
				{MyParser.INT, 4},   // TODO: native best int
				{MyParser.ISIZE, 8}, // TODO: native ptr/container sized int / offset
				{MyParser.I64, 8},
				{MyParser.I32, 4},
				{MyParser.I16, 2},
				{MyParser.I8, 1},
				{MyParser.UINT, 4},  // TODO: native best int
				{MyParser.USIZE, 8}, // TODO: native ptr/container sized int
				{MyParser.U64, 8},
				{MyParser.U32, 4},
				{MyParser.U16, 2},
				{MyParser.U8, 1},
			};
		
		public new Typespec VisitTypeSpec(TypeSpecContext c)
		{
			Typespec ret;
			if (c.basicType()		!= null) ret = VisitBasicType(c.basicType());
			else if (c.funcType()	!= null) ret = VisitFuncType(c.funcType());
			else if (c.nestedType()	!= null) ret = VisitNestedType(c.nestedType());
			else throw new Exception("unknown typespec");
			ret.qual = VisitTypeQuals(c.typeQuals());
			ret.ptrs = c.typePtr().Select(VisitTypePtr).ToList();
			return ret;
		}

		public new Pointer VisitTypePtr(TypePtrContext c)
		{
			Pointer ret;
			if (c.ptr != null)
			{
				ret = new Pointer();
				ret.kind = ToPtr[c.ptr.Type];
			}
			else if (c.ary != null)
			{
				Array a = new Array{ expr = VisitExpr(c.expr()) };
				ret = a;
				ret.kind = ToPtr[c.ary.Type];
			}
			else throw new Exception("unknown ptr type");

			ret.qual = VisitTypeQuals(c.typeQuals());
			return ret;
		}

		public new Qualifier VisitTypeQuals(TypeQualsContext c)
		{
			Qualifier ret = c.typeQual().Aggregate(Qualifier.None, (a,q) => a | ToQual[0]);
			return ret;
		}

		public new Typespec VisitBasicType(MyParser.BasicTypeContext c)
		{
			TypespecBase ret = new TypespecBase
			{
				align = -1,
				size = -1
			};
			var cc = c.GetChild<ParserRuleContext>(0);
			switch (cc.RuleIndex)
			{
				case RULE_specialType: {
					var t = c.specialType().v.Type;
					switch (t)
					{
						case MyParser.AUTO:
							ret.kind = TypespecBase.Kind.Auto;
							break;
						case MyParser.VOID:
							ret.kind = TypespecBase.Kind.Void;
							ret.size = -2;
							break;
						case MyParser.BOOL:
							ret.kind = TypespecBase.Kind.Bool;
							ret.size = 1;
							break;
						default: throw new Exception("unknown typespec");
					}
					break;
				}
				case RULE_charType: {
					var tc = c.charType();
					if (tc.STRING() != null)
					{
						ret.kind = TypespecBase.Kind.Char;
						ret.size = (tc.CHAR() != null) ? 1 : 4;
					}
					else
					{
						ret.kind = TypespecBase.Kind.String;
					}
					break;
				}
				case RULE_floatingType: {
					var t    = c.floatingType().v.Type;
					ret.kind = TypespecBase.Kind.Float;
					ret.size = ToSize[t];
					break;
				}
				case RULE_binaryType: {
					var t    = c.binaryType().v.Type;
					ret.kind = TypespecBase.Kind.Binary;
					ret.size = ToSize[t];
					break;
				}
				case RULE_signedIntType: {
					var t    = c.signedIntType().v.Type;
					ret.kind = TypespecBase.Kind.Integer;
					ret.size = ToSize[t];
					break;
				}
				case RULE_unsignIntType: {
					var t    = c.unsignIntType().v.Type;
					ret.kind = TypespecBase.Kind.Unsigned;
					ret.size = ToSize[t];
					break;
				}
				default:
					throw new Exception("unknown typespec");
			}
			return ret;
		}

		public new Typespec VisitFuncType(MyParser.FuncTypeContext c)
		{
			TypespecFunc ret = new TypespecFunc
			{
				templateArgs = VisitTplArgs(c.tplArgs()),
				paras        = VisitFuncDef(c.funcDef()),
				retType      = VisitTypeSpec(c.typeSpec()),
			};
			return ret;
		}

		public new Typespec VisitNestedType(MyParser.NestedTypeContext c)
		{
			TypespecNested ret = new TypespecNested
			{
				identifiers = c.idTplArgs().Select(VisitIdTplArgs).ToList()
			};
			return ret;
		}
		#endregion

		public new Func.Param VisitParam(ParamContext c)
		{
			Func.Param ret = new Func.Param
			{
				name = VisitId(c.id()),
				type = VisitTypeSpec(c.typeSpec())
			};
			return ret;
		}

		public new List<Func.Param> VisitFuncDef(FuncDefContext c)
		{
			List<Func.Param> ret = c.param().Select(VisitParam).ToList();
			return ret;
		}

		public new IdentifierTpl VisitIdTplArgs(IdTplArgsContext c)
		{
			IdentifierTpl ret = new IdentifierTpl
			{
				name         = VisitId(c.id()),
				templateArgs = VisitTplArgs(c.tplArgs())
			};
			return ret;
		}

		public new TemplateArg VisitTplArg(TplArgContext c)
		{
			TemplateArg ret;
			if (c.typeSpec()  != null) ret = new TemplateArg {type = VisitTypeSpec(c.typeSpec())};
			else if (c.id()   != null) ret = new TemplateArg {name = VisitId(c.id())};
			else if (c.expr() != null) ret = new TemplateArg {expr = VisitExpr(c.expr())};
			else throw new Exception("unknown template arg kind");
			return ret;
		}

		public new List<TemplateArg> VisitTplArgs(TplArgsContext c)
		{
			List<TemplateArg> ret = c?.tplArg().Select(VisitTplArg).ToList();
			return ret;
		}

		public Expr VisitExpr(ExprContext c)
		{
			if (c == null)
				return null;
			
			Expr ex = new Expr();
			return ex;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public new string VisitId(IdContext c)
		{
			return c.GetText();
		}

		public Enum.Entry VisitEnumEntry(IdExprContext c)
		{
			Enum.Entry ret = new Enum.Entry
			{
				name  = VisitId(c.id()),
				value = VisitExpr(c.expr()),
			};
			return ret;
		}
		
		public override object VisitEnumDecl(EnumDeclContext c)
		{
			Enum ret = new Enum();
			ret.name = VisitId(c.id());
			// add to hierarchy stack
			ret.entries = c.idExpr().Select(VisitEnumEntry).ToList();
			return ret;
		}

		public TemplateParam VisitTplParam(IdContext c)
		{
			TemplateParam tp = new TemplateParam
			{
				name = VisitId(c)
			};
			return tp;
		}
		
		public new List<TemplateParam> VisitTplParams(TplParamsContext c)
		{
			List<TemplateParam> ret = c.id().Select(VisitTplParam).ToList();
			return ret;
		}

		public override object VisitFunctionDecl(FunctionDeclContext c)
		{
			FuncDeclContext cc = c.funcDecl();
			Func ret = new Func
			{
				name           = VisitId(cc.id()),
				templateParams = VisitTplParams(cc.tplParams()),
				paras          = VisitFuncDef(cc.funcDef()),
				retType        = VisitTypeSpec(cc.typeSpec()),
			};
			if (cc.stmtBlk() != null)
			{
				ret.block = VisitStmtBlk(cc.stmtBlk());
			}
			else if (cc.expr() != null)
			{
				// TODO
				VisitExpr(cc.expr());
				ret.block = new List<Stmt>();
			}
			return ret;
		}

		private void Visit(TerminalNodeImpl node)
		{
			Console.WriteLine(" Visit Symbol={0}", node.Symbol.Text);
		}
		
		public override object VisitProg(ProgContext context)
		{
			Console.WriteLine("HelloVisitor VisitR");
			context
				.children
				.OfType<TerminalNodeImpl>()
				.ToList()
				.ForEach(child => Visit(child));
			return null;
		}
	}
}

/*
[ReturnIfInactive]
int a(){
	1+2;
}

aspect ReturnIfInactive : Function {
	pre {
		if (IsInactive)
		{
			return;
		}
	}
}

aspect IgnoreErrors : Function, Statement {
	pre @{
		try
		{
	@}
	post @{
		}
		catch(e){}
	@}
}

aspect Binary : Operator {
	param: const CLASS & other;
	
	pre @{
		try
		{
	@}
	post @{
		}
		catch(e){}
	@}
}
*/
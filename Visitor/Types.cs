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

using static Myll.MyllParser;

namespace Myll
{
	public partial class MyllVisitor : MyllParserBaseVisitor<object>
	{
		static readonly Dictionary<int, Qualifier> ToQual =
			new Dictionary<int, Qualifier> {
				{MyllParser.CONST,		Qualifier.Const		},
				{MyllParser.MUTABLE,	Qualifier.Mutable	},
				{MyllParser.VOLATILE,	Qualifier.Volatile	},
				{MyllParser.STABLE,		Qualifier.Stable	},
			};

		static readonly Dictionary<int, Pointer.Kind> ToPtr =
			new Dictionary<int, Pointer.Kind> {
				{MyllParser.AT_BANG,	Pointer.Kind.Unique	},
				{MyllParser.AT_PLUS,	Pointer.Kind.Shared	},
				{MyllParser.AT_QUEST,	Pointer.Kind.Weak	},
				{MyllParser.STAR,		Pointer.Kind.RawPtr	},
				{MyllParser.PTR_TO_ARY,	Pointer.Kind.PtrToAry	},
				{MyllParser.AMP,		Pointer.Kind.LVRef	},
				{MyllParser.DBL_AMP,	Pointer.Kind.RVRef	},
				{MyllParser.AT_LBRACK,	Pointer.Kind.Vector	},
				{MyllParser.LBRACK,		Pointer.Kind.Array	},
			};

		private static readonly Dictionary<int, int> ToSize =
			new Dictionary<int, int>
			{
				{MyllParser.FLOAT, 4}, // TODO: native best float
				{MyllParser.F80, 10},
				{MyllParser.F64, 8},
				{MyllParser.F32, 4},
				{MyllParser.F16, 2},
				{MyllParser.BYTE, 1},
				{MyllParser.B64, 8},
				{MyllParser.B32, 4},
				{MyllParser.B16, 2},
				{MyllParser.B8, 1},
				{MyllParser.INT, 4},   // TODO: native best int
				{MyllParser.ISIZE, 8}, // TODO: native ptr/container sized int / offset
				{MyllParser.I64, 8},
				{MyllParser.I32, 4},
				{MyllParser.I16, 2},
				{MyllParser.I8, 1},
				{MyllParser.UINT, 4},  // TODO: native best int
				{MyllParser.USIZE, 8}, // TODO: native ptr/container sized int
				{MyllParser.U64, 8},
				{MyllParser.U32, 4},
				{MyllParser.U16, 2},
				{MyllParser.U8, 1},
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

		public new Typespec VisitBasicType(MyllParser.BasicTypeContext c)
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
						case MyllParser.AUTO:
							ret.kind = TypespecBase.Kind.Auto;
							break;
						case MyllParser.VOID:
							ret.kind = TypespecBase.Kind.Void;
							ret.size = -2;
							break;
						case MyllParser.BOOL:
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

		public new Typespec VisitFuncType(MyllParser.FuncTypeContext c)
		{
			TypespecFunc ret = new TypespecFunc
			{
				templateArgs = VisitTplArgs(c.tplArgs()),
				paras        = VisitFuncDef(c.funcDef()),
				retType      = VisitTypeSpec(c.typeSpec()),
			};
			return ret;
		}

		public new Typespec VisitNestedType(MyllParser.NestedTypeContext c)
		{
			TypespecNested ret = new TypespecNested
			{
				identifiers = c.idTplArgs().Select(VisitIdTplArgs).ToList()
			};
			return ret;
		}
	}
}
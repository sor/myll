using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

using Myll.Core;

using Array = Myll.Core.Array;
using Enum = Myll.Core.Enum;

using static Myll.MyllParser;

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
//	public partial class Visitor
//		: MyllParserBaseVisitor<object>
	{
		static readonly Dictionary<int, Qualifier>
			ToQual = new Dictionary<int, Qualifier> {
				{ MyllParser.CONST,		Qualifier.Const		},
				{ MyllParser.MUTABLE,	Qualifier.Mutable	},
				{ MyllParser.VOLATILE,	Qualifier.Volatile	},
				{ MyllParser.STABLE,	Qualifier.Stable	},
			};

		static readonly Dictionary<int, Pointer.Kind>
			ToPtr = new Dictionary<int, Pointer.Kind> {
				{ MyllParser.AT_BANG,	Pointer.Kind.Unique	},
				{ MyllParser.AT_PLUS,	Pointer.Kind.Shared	},
				{ MyllParser.AT_QUEST,	Pointer.Kind.Weak	},
				{ MyllParser.STAR,		Pointer.Kind.RawPtr	},
				{ MyllParser.PTR_TO_ARY,Pointer.Kind.PtrToAry},
				{ MyllParser.AMP,		Pointer.Kind.LVRef	},
				{ MyllParser.DBL_AMP,	Pointer.Kind.RVRef	},
				{ MyllParser.AT_LBRACK,	Pointer.Kind.Vector	},
				{ MyllParser.LBRACK,	Pointer.Kind.Array	},
			};

		private static readonly Dictionary<int, int>
			ToSize = new Dictionary<int, int> {
				{ MyllParser.FLOAT, 4 }, // TODO: native best float
				{ MyllParser.DOUBLE, 8 },
				{ MyllParser.F80, 10 },
				{ MyllParser.F64, 8 },
				{ MyllParser.F32, 4 },
				{ MyllParser.F16, 2 },
				{ MyllParser.BYTE, 1 },
				{ MyllParser.B64, 8 },
				{ MyllParser.B32, 4 },
				{ MyllParser.B16, 2 },
				{ MyllParser.B8, 1 },
				{ MyllParser.INT, 4 },   // TODO: native best int
				{ MyllParser.ISIZE, 8 }, // TODO: native ptr/container sized int / offset
				{ MyllParser.I64, 8 },
				{ MyllParser.I32, 4 },
				{ MyllParser.I16, 2 },
				{ MyllParser.I8, 1 },
				{ MyllParser.UINT, 4 },  // TODO: native best int
				{ MyllParser.USIZE, 8 }, // TODO: native ptr/container sized int
				{ MyllParser.U64, 8 },
				{ MyllParser.U32, 4 },
				{ MyllParser.U16, 2 },
				{ MyllParser.U8, 1 },
			};

		public new Typespec VisitTypespec( TypespecContext c )
		{
			if( c == null )
				return null;

			Typespec ret;
			if( c.typespecBasic()       != null ) ret = VisitTypespecBasic( c.typespecBasic() );
			else if( c.typespecFunc()   != null ) ret = VisitTypespecFunc( c.typespecFunc() );
			else if( c.typespecNested() != null ) ret = VisitTypespecNested( c.typespecNested() );
			else throw new Exception( "unknown typespec" );
			ret.qual = c.qual().Visit();
			ret.ptrs = c.typePtr().Select( VisitTypePtr ).ToList();
			return ret;
		}

		public new Pointer VisitTypePtr( TypePtrContext c )
		{
			Pointer ret;
			if( c.ptr != null ) {
				ret      = new Pointer();
				ret.kind = ToPtr[c.ptr.Type];
			}
			else if( c.ary != null ) {
				ret = new Array {
					expr = c.expr().Visit()
				};
				ret.kind = ToPtr[c.ary.Type];
			}
			else throw new Exception( "unknown ptr type" );

			ret.qual = c.qual().Visit();
			return ret;
		}

		public new TypespecBasic VisitTypespecBasic( TypespecBasicContext c )
		{
			TypespecBasic ret = new TypespecBasic {
				align = -1,
				size  = -1
			};
			var cc = c.GetChild<ParserRuleContext>( 0 );
			switch( cc.RuleIndex ) {
				case RULE_specialType: {
					var t = c.specialType().v.Type;
					switch( t ) {
						case MyllParser.AUTO:
							ret.kind = TypespecBasic.Kind.Auto;
							break;
						case MyllParser.VOID:
							ret.kind = TypespecBasic.Kind.Void;
							ret.size = -2;
							break;
						case MyllParser.BOOL:
							ret.kind = TypespecBasic.Kind.Bool;
							ret.size = 1;
							break;
						default: throw new Exception( "unknown typespec" );
					}
					break;
				}
				case RULE_charType: {
					var tc = c.charType();
					if( tc.STRING() != null ) {
						ret.kind = TypespecBasic.Kind.Char;
						ret.size = (tc.CHAR() != null) ? 1 : 4;
					}
					else {
						ret.kind = TypespecBasic.Kind.String;
					}
					break;
				}
				case RULE_floatingType: {
					var t = c.floatingType().v.Type;
					ret.kind = TypespecBasic.Kind.Float;
					ret.size = ToSize[t];
					break;
				}
				case RULE_binaryType: {
					var t = c.binaryType().v.Type;
					ret.kind = TypespecBasic.Kind.Binary;
					ret.size = ToSize[t];
					break;
				}
				case RULE_signedIntType: {
					var t = c.signedIntType().v.Type;
					ret.kind = TypespecBasic.Kind.Integer;
					ret.size = ToSize[t];
					break;
				}
				case RULE_unsignIntType: {
					var t = c.unsignIntType().v.Type;
					ret.kind = TypespecBasic.Kind.Unsigned;
					ret.size = ToSize[t];
					break;
				}
				default:
					throw new Exception( "unknown typespec" );
			}
			return ret;
		}

		public new TypespecFunc VisitTypespecFunc( TypespecFuncContext c )
		{
			TypespecFunc ret = new TypespecFunc {
				templateArgs = VisitTplArgs( c.tplArgs() ),
				paras        = VisitFuncTypeDef( c.funcTypeDef() ),
				retType      = VisitTypespec( c.typespec() ),
			};
			return ret;
		}

		public new TypespecNested VisitTypespecNested( TypespecNestedContext c )
		{
			TypespecNested ret = new TypespecNested {
				identifiers = c.idTplArgs().Select( VisitIdTplArgs ).ToList()
			};
			return ret;
		}

		public new List<TypespecNested> VisitTypespecsNested( TypespecsNestedContext c )
			=> c?.typespecNested().Select( VisitTypespecNested ).ToList()
			   ?? new List<TypespecNested>();
	}
}

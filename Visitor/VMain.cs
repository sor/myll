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
using Parser = Myll.MyllParser;

using static Myll.MyllParser;	// sadly pulls in all constants

namespace Myll
{
	public partial class ExtendedVisitor<Result>
		: MyllParserBaseVisitor<Result>
	{
		public static Stack<Scope> HierarchyStack = new Stack<Scope>();

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public new string VisitId( IdContext c )
			=> c.GetText();
	}
}

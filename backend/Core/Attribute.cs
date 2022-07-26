namespace Myll.Core
{
	public class Attribute
	{
		/*
		public enum Attribute
		{
			Auto_Begin,
			Auto,
			AutoOn,
			AutoOff,
			AutoExplicit,	// if explicitly set to auto
			Auto_End,

			Preselection_Begin,	// for surrounding areas, ranging from never to always
			Disallow,
			Discourage,
			Manual,		// Needs to be manually set for every applicable child
			Basic,		// Basic Support, for dispatch this means adding a virtual dtor
			BasicAuto,	// Basic Support with auto deduction for overridden methods
			Encourage,
			Enforce,
			Preselection_End,

			Yes_Begin,
			Yes,
			Pure,
			Throw,
			Virtual,
			Override,
			Final,
			Yes_End,

			No_Begin,
			No,
			ImPure,
			NoThrow,
			NonVirtual,
			No_End,

			MultiState_Begin,
			Private,
			Protected,
			Public,
			MultiState_End,
		}
		*/

		// TODO merge all of them into one enum?
		// or split the category parts off?
		public enum Dispatch : byte {
			Auto,

			Category_Begin,
			Forbid,     // all child func NonVirtual (unchangable)
			Discourage, // all child func NonVirtual
			Manual,     // all children need explicit specification
			Encourage,  // all child func Virtual
			Enforce,    // all child func Virtual (unchangable)
			Interface,  // all child func Abstract
			Basic,      // dtor Virtual
			Abstract,	// dtor PureVirtual but implemented (wtf)
			Category_End,

			NonVirtual, // Static dispatch, as opposed to Dynamic (Virtual)

			Dynamic_Begin,
			//Abstract,
			Virtual,
			Override,
			Final,
			Dynamic_End,
		}

		public enum Implementation : byte {
			Auto,
			Provided, // {...}
			Abstract, // = 0
			Default,  // = default
			Forbid,   // = delete OR Disable
		}

		// static storage duration and internal linkage
		public enum Storage : byte {
			Auto,

			Category_Begin,
			AllowBoth,
			OnlyStatic,
			OnlyStatelessStatic,
			OnlyInstance,
			Category_End,

			Static,   // Shared, Persistent, Permanent, Always, Continous
			Dynamic,  // same as Instance?
			Instance, // for func, var
			Thread,   // thread(+static) = internal, thread+extern = external
		}

		public enum Linkage : byte {
			Auto,
			None,     // visible only in its scope
			Internal, // hides between different TU, Hidden, (Static), prefer Module
			External, // visible everywhere, Visible, Global
			Module,   // visible inside the same module, same as Internal for merged compilation
		}

		public enum Purity : byte {
			Auto,
			Impure,
			Const,     // const this
			Pure,      // const this and global
			Constexpr, // same as Pure?
		}
	}
}

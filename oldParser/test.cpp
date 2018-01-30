namespace JanSordid
{
	class stack
	{
		prop  int	count = 0;
		field int	alloc;
		field int*	store = new int[alloc];

		ctor()
		{
			alloc = 4;
		}

		meth int top()
		{
			if( count > 0 )
				return store[count-1];
			else
				return -1;
		}

		// implicit void
		/*[[mutable]]*/
		meth push( /*sink*/ int o )
		{
			if( alloc <= count )
				enlarge( 2 );

			store[count] = o;
			count++;
		}

		/*[[mutable]]*/
		meth enlarge( int factor ) -> void
		{
			var int* new_store = new int[ alloc * factor ];
			alloc times
				new_store[#1] = store[#1];

			delete store;
			store = new_store;
		}
	}

	func int main()
	{
		var stack s;
		cout << s.count << endl;
		a?b:c?d:e;
	}
}
/*
	enum emu {	// EMPTY, NONE, ZERO, DEFAULT
		bird = 1,
		blubb,
	}

	func main( int argc, char ** argv )
	  -> int
	{
		var double dd;
		blah << ""Knuck"" << endl;
		cin >> a;
		return ""Ha/*llo"";
		if( 1 )
		{
			a + 1;
			a + b+c;
		}
	
		return 0;
	}*/
	/*catch( exception & e )
	{
		cout << ""This would catch everything inside main"";
	}*/
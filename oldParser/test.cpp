namespace JanSordid::Core
{
	class list<T> {
		meth int test() {}
	}
	/*class list<int> {
		//overload
		meth int test() {}
	}*/

	class stack
	{
		type int T

		prop  int		count = 0;
		field int		alloced;
		field int*		store		= new int[ alloc ];
		field int*		new_store2	= new int[ alloc * factor ];
		field blubb*	b;

	//public:
		ctor()
		{
			alloced = count = 4 + 12;
			count++;
		}

		meth int top()
		{
			if( count > 0 )
				return store[count-1];
			else {
				return -1;
			}
		}

		// implicit void?, not a good idea...
		// implicit auto?, would be void because no return, better than implicit auto
		/*[[mutable]]*/
		meth push( /*copy*/ int o )
		{
			if( alloced <= count )
				enlarge( 2 );

			store[count] = o;
			count++;
		}

		// implicit void?, not a good idea...
		/*[[mutable]]*/
		meth emplace( /*sink*/ int o )
		{
			if( alloced <= count )
				enlarge( 2 );

			store[count] = move( o );
			count++;
		}

		/*[[mutable]]*/
		meth enlarge( int factor ) -> void
		{
			var int* new_store = new int[ alloced * factor ]; // fails
			alloc times o {
				new_store[#0] = store[#0];
			}

			delete store;
			store = new_store;
		}

		//[pure binary]
		operator "==" -> bool
		{
			const int count = self.count;

			if( count != other.count )
				return false;

			for( var int i = 0; i < count; ++i )
				if( self.store[i] != other.store[i] )
					return false;
		
			return true;
		}
	}

	//class blubb{}

	func int main()
	{
		var stack s;
		cout << s.count << endl;
		//a?b:c?d:e;
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

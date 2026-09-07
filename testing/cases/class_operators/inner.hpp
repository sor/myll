#pragma once

namespace BoxLib {
	class Inner {
	public:
		int value() const { return 7; }
	};

	class Box {
	public:
		Inner inner;

		Box() : inner() {}

		Inner & operator*() { return inner; }
		Inner * operator->() { return &inner; }
	};

	class Widget {
	public:
		int v;

		Widget() : v( 0 ) {}
		Widget( int value ) : v( value ) {}

		int readVal() const { return v; }
	};
}

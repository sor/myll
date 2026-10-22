#pragma once

// The real C++ header somelib.decl.myll mirrors, including the existing
// specialization.
namespace Somelib {
	template<typename T>
	class Box {
	public:
		Box() : _v{} {}
		T _v;
		T Get() const { return _v; }
	};

	template<>
	class Box<int> {
	public:
		Box() : _v{0}, _extra{0} {}
		int _v;
		int _extra;
		int Get() const { return _v + _extra; }
	};
}
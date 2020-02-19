#pragma once

class A;

void do_a(A* a);

class A
{
	class B;

	class C
	{
		void do_b(B* b);
	};

	class B
	{
	};
};


#include "template_typename.hpp"
int checkBase()
{
    BaseDerived<int> d{};
    d.value = 11;
    d.extra = 12;
    return (d.value == 11 && d.extra == 12) ? 0 : 1;
}
int checkComplex()
{
    ComplexOuter<int>::Item<float> a = makeComplex<int, float>( 1, 3.14f );
    ComplexOuter<int>::Item<float> b = duplicateComplex<int, float>( a );
    return (b.key == 1 && b.value > 3.13f && b.value < 3.15f) ? 0 : 1;
}
int checkDependentInner()
{
    DependentOuter<int>::Inner x = makeDependent<int>( 99 );
    return x.value == 99 ? 0 : 1;
}
int main()
{
    int result = checkReturn();
    if( result != 0 ) {
        return result;
    }
    result = checkParam();
    if( result != 0 ) {
        return result;
    }
    result = checkVar();
    if( result != 0 ) {
        return result;
    }
    result = checkBase();
    if( result != 0 ) {
        return result;
    }
    result = checkNestedTpl();
    if( result != 0 ) {
        return result;
    }
    result = checkComplex();
    if( result != 0 ) {
        return result;
    }
    result = checkDependentInner();
    if( result != 0 ) {
        return result;
    }
    return 0;
}
int checkNestedTpl()
{
    NestedOuter<int>::Inner<float> r = testNestedTpl<int, float>( 1, 2.5f );
    return (r.a == 1 && r.b > 2.4f && r.b < 2.6f) ? 0 : 1;
}
int checkParam()
{
    ParamOuter<int>::Inner x{};
    x.value = 7;
    return testParam<int>( x, 7 );
}
int checkReturn()
{
    ReturnOuter<int>::Inner r = testReturn<int>( 42 );
    return r.value == 42 ? 0 : 1;
}
int checkVar()
{
    return testVar<int>( 5 );
}

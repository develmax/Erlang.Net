namespace Erlang;

internal interface ITermMapOrderingLayout : ITermMapComparisonLayout
{
    int CompareHashKeys(Term left, Term right);

    NativeMapEqualityPlan505? ReadExactEqualityPlan(MapTerm left, MapTerm right);
}

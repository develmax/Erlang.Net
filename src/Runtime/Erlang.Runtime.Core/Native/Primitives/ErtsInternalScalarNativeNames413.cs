namespace Erlang;

internal static class ErtsInternalScalarNativeNames413
{
    internal const string Module = "erts_internal";
    internal const string BinaryInteger = "binary_to_integer";
    internal const string ListInteger = "list_to_integer";
    internal const string TimeUnit = "time_unit";
    internal const string PerformanceUnit = "perf_counter_unit";
    internal const string BadArgument = "badarg";
    internal const string Big = "big";
    internal const string NoInteger = "no_integer";
    internal const string NotList = "not_a_list";
    internal const string Foreign = "Original internal scalar primitive requires its owning runtime";
    internal const string Conflict = "Original internal scalar primitive binding conflicts with the current owner";
    internal static readonly (string Function, int Arity)[] Signatures =
    [
        (BinaryInteger, 2),
        (ListInteger, 2),
        (TimeUnit, 0),
        (PerformanceUnit, 0)
    ];
}

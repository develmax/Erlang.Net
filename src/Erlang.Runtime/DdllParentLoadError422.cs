namespace Erlang;

internal static class DdllParentLoadError422
{
    private static readonly string[] Names = ["no_error", "no_driver_init", "driver_init_failed",
        "bad_driver_name", "driver_name_to_long", "driver_incorrect_version", "no_ddll_available",
        "unspecified_error", "symbol_not_found"];

    internal static Term Value(int code)
        => code <= 0 && code >= -(Names.Length - 1) ? Term.A(Names[-code])
            : Term.Tuple(Term.A(DdllErrorNames416.OpenError), Term.I(code));
}

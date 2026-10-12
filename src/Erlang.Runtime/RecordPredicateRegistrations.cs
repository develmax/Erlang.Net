namespace Erlang;

internal static class RecordPredicateRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            RecordPredicateNames.IsRecord,
            2,
            (_, arguments) =>
            {
                if (arguments[1] is not Atom tag)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return ValueTask.FromResult<Term>(CoreModules.Boolean(arguments[0] is TupleTerm { Items.Count: > 0 } tuple && tuple.Items[0].Equals(tag)));
            }
        );
        registry.Register(
            ErlangModuleNames.Module,
            RecordPredicateNames.IsRecord,
            3,
            (_, arguments) =>
            {
                if (arguments[1] is not Atom tag)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (arguments[2] is Atom)
                    return ValueTask.FromResult<Term>(CoreModules.Boolean(false));
                if (arguments[2] is not Integer size || size.Value < RecordPredicateParameters.MinimumSmallInteger || size.Value > RecordPredicateParameters.MaximumSmallInteger)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                return ValueTask.FromResult<Term>(CoreModules.Boolean(arguments[0] is TupleTerm { Items.Count: > 0 } tuple && tuple.Items.Count == size.Value && tuple.Items[0].Equals(tag)));
            }
        );
    }
}

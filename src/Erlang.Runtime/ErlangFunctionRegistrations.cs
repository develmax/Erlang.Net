namespace Erlang;

internal static class ErlangFunctionRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        void Add(string name, ErlangFunction body) => registry.Register(
            ErlangModuleNames.Module,
            name,
            3,
            body
        );
        Add(
            ErlangFunctionBifNames.Exported,
            (context, arguments) =>
        {
            var (module, function, arity) = Query(arguments);

            return ValueTask.FromResult<Term>(Term.A(context.Runtime.Modules.HasExport(module.Name, function.Name, arity) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
        );
        Add(
            ErlangFunctionBifNames.Builtin,
            (_, arguments) =>
        {
            var (module, function, arity) = Query(arguments);

            return ValueTask.FromResult<Term>(Term.A(NativeBuiltinCatalog.Contains(module.Name, function.Name, arity) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
        );
        Add(
            ErlangFunctionBifNames.Make,
            (context, arguments) =>
        {
            if (arguments[0] is not Atom module || arguments[1] is not Atom function || arguments[2] is not Integer arity || arity.Value < 0 || arity.Value > ErlangFunctionParameters.MaximumFunctionArity)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            int count = (int)arity.Value;
            var identity = new ExternalFunctionIdentity(module, function, count);
            var result = context.Runtime.Functions.External(context.Runtime, identity);

            return ValueTask.FromResult<Term>(result);
        }
        );
    }

    private static (Atom Module, Atom Function, int Arity) Query(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Atom module || arguments[1] is not Atom function || arguments[2] is not Integer arity || arity.Value < ErlangFunctionParameters.MinimumQueryArity || arity.Value > ErlangFunctionParameters.MaximumQueryArity)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (module, function, unchecked((int)(long)arity.Value));
    }
}

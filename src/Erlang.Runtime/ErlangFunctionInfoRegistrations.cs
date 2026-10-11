namespace Erlang;

internal static class ErlangFunctionInfoRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ErlangFunctionInfoNames.Info,
            1,
            (context, arguments) =>
        {
            var function = Require(arguments[0]);
            Term result = Nil.Value;
            foreach (string key in ErlangFunctionInfoNames.Keys)
            {
                Term value = Value(function, key, context);
                if (value is not Atom { Name: ErlangFunctionInfoNames.Undefined })
                    result = new Cons(Term.Tuple(Term.A(key), value), result);
            }

            return ValueTask.FromResult(result);
        }
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangFunctionInfoNames.Info,
            2,
            (context, arguments) =>
        {
            var function = Require(arguments[0]);
            if (arguments[1] is not Atom key)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return ValueTask.FromResult<Term>(Term.Tuple(key, Value(function, key.Name, context)));
        }
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangFunctionInfoNames.Mfa,
            1,
            (context, arguments) =>
        {
            var function = Require(arguments[0]);

            return ValueTask.FromResult<Term>(Term.Tuple(
                Value(function, ErlangFunctionInfoNames.Module, context),
                Value(function, ErlangFunctionInfoNames.Name, context),
                Term.I(function.Arity)
            ));
        }
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangFunctionInfoNames.ToList,
            1,
            (_, arguments) => ValueTask.FromResult(Term.String(FunctionText.Format(Require(arguments[0]))))
        );
    }

    private static FunctionTerm Require(Term value) => value is FunctionTerm function && (function.ExternalIdentity is not null || function.LocalIdentity is not null) ? function : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static Term Value(FunctionTerm function, string key, ProcessContext context)
    {
        var local = function.LocalIdentity;
        var external = function.ExternalIdentity;
        Term undefined = Term.A(ErlangFunctionInfoNames.Undefined);

        return key switch
        {
            ErlangFunctionInfoNames.Type => Term.A(local is null ? ErlangFunctionInfoNames.External : ErlangFunctionInfoNames.Local),
            ErlangFunctionInfoNames.Environment => Cons.From(function.Environment),
            ErlangFunctionInfoNames.Arity => Term.I(function.Arity),
            ErlangFunctionInfoNames.Module => local?.Module ?? external!.Module,
            ErlangFunctionInfoNames.Name => local?.Name ?? external!.Function,
            ErlangFunctionInfoNames.Pid => local is null ? undefined : context.Runtime.InitializationPid,
            ErlangFunctionInfoNames.References => local is null ? undefined : Term.I(1),
            ErlangFunctionInfoNames.Index or ErlangFunctionInfoNames.NewIndex => local is null ? undefined : Term.I(local.Index),
            ErlangFunctionInfoNames.OldUnique => local is null ? undefined : Term.I(local.OldUnique),
            ErlangFunctionInfoNames.NewUnique => local is null ? undefined : local.Unique,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };
    }
}

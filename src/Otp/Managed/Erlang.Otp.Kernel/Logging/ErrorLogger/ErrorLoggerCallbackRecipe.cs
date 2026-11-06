namespace Erlang;

internal sealed record ErrorLoggerCallbackRecipe(
    RuntimeFunctionEntries Owner,
    LocalFunctionIdentity Identity,
    bool Formatter
) : IRuntimeStoredFunctionRecipe
{
    internal static FunctionTerm Create(ProcessContext context, bool formatter)
    {
        var identity = new LocalFunctionIdentity(
            Term.A(ErrorLoggerModule.Name),
            Term.A(formatter ? ErrorLoggerCallbackNames.ReportToFormat : ErrorLoggerCallbackNames.TtyStopFunction),
            formatter ? 0 : 1,
            0,
            new BitString(typeof(ErrorLoggerModule).Assembly.ManifestModule.ModuleVersionId.ToByteArray())
        );
        RuntimeFunctionEntries owner = context.Runtime.Functions;
        int arity = formatter ? 1 : 2;
        identity = owner.Register(
            identity,
            arity,
            0,
            environment => new ErrorLoggerCallbackRecipe(owner, identity, formatter).Rebind(environment)
        );

        return new ErrorLoggerCallbackRecipe(owner, identity, formatter).Rebind([]);
    }

    public FunctionTerm Rebind(IReadOnlyList<Term> environment)
    {
        if (environment.Count != 0)
        {
            throw new InvalidOperationException(ErrorLoggerCallbackNames.ErrorLoggerCallbacksHaveNoFreeVariables);
        }
        var function = new FunctionTerm(
            Identity,
            Formatter ? 1 : 2,
            environment,
            (execution, arguments) => Formatter
                ? ErrorLoggerModule.ReportToFormat((ProcessContext)execution, arguments[0])
                : ValueTask.FromResult<Term>(Term.A(ErrorLoggerCallbackNames.Stop))
        );
        Owner.RegisterStoredSource(function, this);

        return function;
    }
}

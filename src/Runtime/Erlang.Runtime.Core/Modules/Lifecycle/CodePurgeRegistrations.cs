namespace Erlang;

internal static class CodePurgeRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        CodePurgeNames.Function,
        1,
        Purge
    );

    private static async ValueTask<Term> Purge(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Atom module || !await context.Runtime.PurgeModule(context, module.Name))
            throw new ErlangException(ErlangErrorReasons.BadArgument)
            {
                ExplicitStackTrace = true,
                StackTraceTerm = context.CaptureErrorStack(
                    Cons.From(arguments),
                    new MapTerm([new(Term.A(CodePurgeNames.ErrorModuleKey), Term.A(CodePurgeNames.ErrorFormatter))]),
                    CodePurgeNames.Function
                )
            };

        return Term.A(ErlangBooleanAtoms.True);
    }
}

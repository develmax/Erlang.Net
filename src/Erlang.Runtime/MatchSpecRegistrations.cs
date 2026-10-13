namespace Erlang;

public static class MatchSpecRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        MatchSpecNames.Test,
        3,
        Test
    );

    private static async ValueTask<Term> Test(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        MatchSpecDialect dialect = arguments[2] switch
        {
            Atom { Name: MatchSpecNames.Table } => MatchSpecDialect.Table,
            Atom { Name: MatchSpecNames.Trace } => MatchSpecDialect.Trace,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };
        if (dialect == MatchSpecDialect.Trace && arguments[0] is not Cons && arguments[0] is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        MatchSpecCompilation compilation = MatchSpecCompiler.Compile(arguments[1], dialect);
        Term diagnostics = Cons.From(compilation.Diagnostics.Select(message => Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.String(message))));
        if (compilation.Program is null)
            return Term.Tuple(Term.A(ErlangExceptionClasses.Error), diagnostics);
        MatchSpecResult result = await compilation.Program.RunAsync(context, arguments[0]);
        var flags = new List<Term>();
        if (result.ReturnTrace)
            flags.Add(Term.A(MatchSpecNames.ReturnTrace));
        if (result.ExceptionTrace)
            flags.Add(Term.A(MatchSpecNames.ExceptionTrace));

        return Term.Tuple(
            Term.A(MatchSpecNames.Ok),
            result.Value,
            Cons.From(flags),
            diagnostics
        );
    }
}

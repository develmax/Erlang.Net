namespace Erlang.Tests;

public static class StandardStreamHostChecks
{
    public static async Task Run()
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var ordinary = new StringWriter();
        using var streams = new RuntimeStandardStreams(output, error);
        await using var runtime = new ProcessRuntime(
            ProcessNodeDefaults.Local,
            ordinary,
            StandardStreamHostParameters.ProcessCapacity,
            streams
        );
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                await Call(context, Term.List(Term.I(65), Term.I(128512), Term.I(0)));
                await Call(context, Term.A(ErlangStandardStreamNames.StandardOutput), new BitString([255, 0, 195, 40]));
                await Call(context, Term.A(ErlangStandardStreamNames.StandardError), Nil.Value);
                if (!error.ToArray().SequenceEqual(new byte[] { 65, 240, 159, 152, 128, 0 }) ||
                    !output.ToArray().SequenceEqual(new byte[] { 255, 0, 195, 40 }) || ordinary.ToString().Length != 0)
                    throw new InvalidOperationException(StandardStreamHostNames.Broken);
                foreach (Term invalid in new Term[]
                {
                    Term.A(ErlangBooleanAtoms.True),
                    Term.I(0),
                    new BitString([128], 1),
                    Term.List(Term.I(55296)),
                    Term.List(Term.I(1114112)),
                    Term.List(Term.I(-1)),
                    Term.List(Term.List(Term.I(65))),
                    new Cons(Term.I(65), Term.A(ErlangBooleanAtoms.False))
                })
                    await Rejected(context, [invalid]);
                await Rejected(context, [Nil.Value, Nil.Value]);
                if (error.Length != 6 || output.Length != 4)
                    throw new InvalidOperationException(StandardStreamHostNames.Broken);
                if (OperatingSystem.IsWindows())
                    await Rejected(context, [Term.A(ErlangStandardStreamNames.StandardInput), Nil.Value]);
                output.Dispose();
                await Rejected(context, [Term.A(ErlangStandardStreamNames.StandardOutput), new BitString([65])]);

                return Nil.Value;
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
        });
        await process.Completion.WaitAsync(StandardStreamHostParameters.Deadline);
        if (failure is not null)
            throw failure;
        await runtime.DisposeAsync();
        if (!error.CanWrite)
            throw new InvalidOperationException(StandardStreamHostNames.Broken);
    }

    private static async ValueTask Call(ProcessContext context, params Term[] arguments)
    {
        Term result = await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            ErlangStandardStreamNames.DisplayString,
            arguments
        );
        if (!result.Equals(Term.A(ErlangBooleanAtoms.True)))
            throw new InvalidOperationException(StandardStreamHostNames.Broken);
    }

    private static async Task Rejected(ProcessContext context, Term[] arguments)
    {
        try
        {
            await Call(context, arguments);
            throw new InvalidOperationException(StandardStreamHostNames.Broken);
        }
        catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            var frame = (TupleTerm)Cons.Items(exception.StackTraceTerm).First();
            if (!frame.Items[0].Equals(Term.A(ErlangModuleNames.Module)) ||
                !frame.Items[1].Equals(Term.A(ErlangStandardStreamNames.DisplayString)) || !frame.Items[2].Equals(Cons.From(arguments)))
                throw new InvalidOperationException(StandardStreamHostNames.Broken);
            var location = (TupleTerm)Cons.Items(frame.Items[3]).First();
            var info = (MapTerm)location.Items[1];
            if (!info.Get(Term.A(StandardStreamHostNames.ModuleKey)).Equals(Term.A(StandardStreamHostNames.Formatter)))
                throw new InvalidOperationException(StandardStreamHostNames.Broken);
        }
    }
}

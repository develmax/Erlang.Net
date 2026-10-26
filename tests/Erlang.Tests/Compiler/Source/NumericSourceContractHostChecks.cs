using Erlang.Differential;

namespace Erlang.Tests;

internal static class NumericSourceContractHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(NumericSourceContractHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(NumericSourceContractHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), NumericSourceContractHostNames.Fixture);
        await using var runtime = new ProcessRuntime();
        artifact.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term value = await actor.InvokeAsync(context => runtime.Modules.Call(context, artifact.Definition.Name, NumericSourceContractHostNames.Entry).AsTask());
        Term[] rows = Cons.Items(value).ToArray();
        Require(rows.Length == NumericSourceContractHostNames.Cases);
        Term info = Term.List(Term.Tuple(
            Term.A(NumericSourceContractHostNames.ErrorInfo),
            new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(NumericSourceContractHostNames.ModuleKey)] = Term.A(NumericSourceContractHostNames.Formatter)
            })
        ));
        info = IntegerParseError493Metadata.WithLocation(info);
        foreach (Term item in rows)
        {
            Require(item is TupleTerm { Items.Count: 5 });
            var row = (TupleTerm)item;
            Require(row.Items[2].Equals(Term.A(ErlangExceptionClasses.Error)) && row.Items[3].Equals(Term.A(ErlangErrorReasons.BadArgument)));
            Term[] frames = Cons.Items(row.Items[4]).ToArray();
            Require(frames.Length >= 2 && frames[0] is TupleTerm { Items.Count: 4 } && frames[1] is TupleTerm { Items.Count: 4 });
            var first = (TupleTerm)frames[0];
            Require(first.Items[0].Equals(Term.A(NumericSourceContractHostNames.Erlang)) && first.Items[1].Equals(row.Items[0]) && first.Items[2].Equals(row.Items[1]) && first.Items[3].Equals(info));
            var caller = (TupleTerm)frames[1];
            Require(caller.Items[0].Equals(Term.A(NumericSourceContractHostNames.Module)) && caller.Items[1].Equals(Term.A(NumericSourceContractHostNames.Probe)) && caller.Items[2].Equals(Term.I(2)));
            Require(caller.Items[3].Equals(Term.List(
                Term.Tuple(Term.A(NumericSourceContractHostNames.File), Term.String(NumericSourceContractHostNames.Fixture)),
                Term.Tuple(Term.A(NumericSourceContractHostNames.Line), Term.I(NumericSourceContractHostNames.ProbeLine))
            )));
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NumericSourceContractHostNames.Failure);
    }
}

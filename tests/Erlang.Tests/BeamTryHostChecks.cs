using Erlang;

namespace Erlang.Tests;

internal static class BeamTryHostChecks
{
    internal static async Task Run(string name)
    {
        TupleTerm expected = BeamTryHostAssets.Expected(name);
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage image = BeamTryHostAssets.Select(runtime);
        var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(async owner =>
            {
                BeamTryHostAssets.Verify(runtime, image);
                Term cases = await runtime.Modules.Call(owner, BeamTryHostNames.Module, BeamTryHostNames.Cases);
                Term[] names = Cons.Items(cases).ToArray();
                if (names.Length != BeamTryHostNames.CaseCount || !names.SequenceEqual(BeamTryHostCaseNames.Ordered.Select(Term.A)))
                    throw new InvalidDataException(BeamTryHostNames.EvidenceFailure);
                BeamTryHostAssets.Verify(runtime, image);
                ProcessHandle child = runtime.Spawn(context => runtime.Modules.Call(
                    context,
                    BeamTryHostNames.Module,
                    BeamTryHostNames.Capture,
                    owner.Self,
                    Term.A(name)
                ), owner);
                Term? report = await owner.ReceiveAsync<Term>(
                    message =>
                    message is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(expected.Items[0]) ? tuple : null,
                    BeamTryHostNames.CaseDeadline
                );
                if (report is not TupleTerm)
                    throw new InvalidOperationException(BeamTryHostNames.MissingReport);
                Term exit = await child.Completion.WaitAsync(BeamTryHostNames.CaseDeadline);
                if (!exit.Equals(Term.A(BeamTryHostNames.Normal)))
                    throw new InvalidOperationException(BeamTryHostNames.ExitFailure);
                if (!report.Equals(expected))
                    throw new InvalidOperationException(BeamTryHostNames.OutcomeFailure);

                return true;
            });
        }
        finally
        {
            await actor.DisposeAsync();
        }
        if (!(await actor.Process.Completion).Equals(Term.A(BeamTryHostNames.Normal)))
            throw new InvalidOperationException(BeamTryHostNames.ExitFailure);
    }
}

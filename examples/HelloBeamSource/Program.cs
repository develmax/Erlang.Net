using Erlang;
using HelloBeamSource;

try
{
    await using var runtime = new ProcessRuntime();
    BeamSourceExampleHost.Register(runtime);
    BeamSourceExampleAssets.Load(runtime);
    int[] values = [3, 1, 2];
    Term snapshot = Term.List(values.Select(value => Term.I(value)).ToArray());
    values[0] = BeamSourceExampleNames.MutationSentinel;
    var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            result.TrySetResult(await runtime.Modules.Call(
                context,
                BeamSourceExampleNames.Driver,
                BeamSourceExampleNames.Run,
                snapshot
            ));
        }
        catch (Exception error)
        {
            result.TrySetException(error);
        }

        return Nil.Value;
    });
    Term actual = await result.Task;
    Term expected = Term.Tuple(
        Term.Tuple(
            Term.A(BeamSourceExampleNames.Ok),
            Term.I(BeamSourceExampleNames.FixtureValue),
            Term.List(Term.A(BeamSourceExampleNames.Alpha), Term.A(BeamSourceExampleNames.Beta))
        ),
        Term.I(BeamSourceExampleNames.FixtureValue),
        Term.I(BeamSourceExampleNames.ExpectedSum)
    );
    if (!actual.Equals(expected) || !(await worker.Completion).Equals(Term.A(BeamSourceExampleNames.Normal)))
        throw new InvalidOperationException(BeamSourceExampleNames.Result);
    Console.WriteLine(BeamSourceExampleNames.Success);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}

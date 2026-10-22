using Erlang;
using HelloBeamIncludes;

try
{
    await using var runtime = new ProcessRuntime();
    BeamIncludesExampleAssets.Load(runtime);
    var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            result.TrySetResult(await runtime.Modules.Call(
                context,
                BeamIncludesExampleNames.Module,
                BeamIncludesExampleNames.HelloFunction
            ));
        }
        catch (Exception error)
        {
            result.TrySetException(error);
        }

        return Nil.Value;
    });
    Term reason = await worker.Completion;
    if (!result.Task.IsCompleted)
        throw new InvalidOperationException(BeamIncludesExampleNames.IncompleteActor);
    Term actual = await result.Task;
    Term expected = Term.Tuple(
        Term.A(BeamIncludesExampleNames.GreetingAtom),
        Term.Tuple(Term.A(BeamIncludesExampleNames.PayloadAtom), Term.I(BeamIncludesExampleNames.IncludedValue)),
        Term.A(BeamIncludesExampleNames.FlavorAtom)
    );
    if (!actual.Equals(expected) || !reason.Equals(Term.A(BeamIncludesExampleNames.NormalExit)))
        throw new InvalidOperationException(BeamIncludesExampleNames.InvalidResult);
    Console.WriteLine(BeamIncludesExampleNames.Success);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}

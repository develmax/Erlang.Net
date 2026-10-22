using Erlang;
using HelloBeam;

try
{
    byte[] constantBytes = BeamExampleAssets.Read(BeamExampleNames.Constant, BeamExampleNames.ConstantSha);
    byte[] addBytes = BeamExampleAssets.Read(BeamExampleNames.Add, BeamExampleNames.AddSha);
    await using var runtime = new ProcessRuntime();
    PreparedBeamImage[] images =
    [
        runtime.Modules.PrepareBeam(constantBytes),
        runtime.Modules.PrepareBeam(addBytes)
    ];
    if (runtime.Modules.PublishPreparedBeam(images).Status != BeamPublicationStatus.Published)
        throw new InvalidOperationException(BeamExampleNames.PublicationFailure);
    foreach (PreparedBeamImage image in images)
        if (!ReferenceEquals(runtime.Modules.SourceOwner(image.Name, BeamExampleNames.Run, 0), image))
            throw new InvalidOperationException(BeamExampleNames.OwnerFailure);
    var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    ProcessHandle worker = runtime.Spawn(async context =>
    {
        try
        {
            Term constant = await runtime.Modules.Call(context, BeamExampleNames.Constant, BeamExampleNames.Run);
            Term expected = Term.Tuple(
                Term.A(BeamExampleNames.Ok),
                Term.I(42),
                Term.List(Term.A(BeamExampleNames.Alpha), Term.A(BeamExampleNames.Beta))
            );
            Term result = await runtime.Modules.Call(context, BeamExampleNames.Add, BeamExampleNames.Run);
            Term direct = await runtime.Modules.Call(
                context,
                BeamExampleNames.Add,
                BeamExampleNames.AddFunction,
                Term.I(17),
                Term.I(25)
            );
            if (!constant.Equals(expected) || !result.Equals(Term.I(42)) || !direct.Equals(Term.I(42)))
                throw new InvalidOperationException(BeamExampleNames.ResultFailure);
            completed.TrySetResult();
        }
        catch (Exception error)
        {
            completed.TrySetException(error);
            throw;
        }

        return Nil.Value;
    });
    Term reason = await worker.Completion;
    if (!completed.Task.IsCompleted)
        throw new InvalidOperationException(BeamExampleNames.ExitFailure);
    await completed.Task;
    if (!reason.Equals(Term.A(BeamExampleNames.Normal)))
        throw new InvalidOperationException(BeamExampleNames.ExitFailure);
    Console.WriteLine(BeamExampleNames.Success);

    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);

    return 1;
}

using Erlang;

internal static class NifPlatformImageChecks
{
    internal static async Task Run(string beam, string library, string output)
    {
        const string module = "nif_platform_fixture";
        await using var runtime = new ProcessRuntime();
        await using var prepared = runtime.Modules.PrepareBeam(
            File.ReadAllBytes(beam),
            new BeamDependencySelection([module])
        );
        await runtime.InitializeModulesAsync(registry =>
        {
            if (registry.PublishPreparedBeam(prepared).Status != BeamPublicationStatus.Published)
                throw new InvalidOperationException("NIF fixture BEAM publication failed");
        });
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                string stem = Path.GetFullPath(library)[..^Path.GetExtension(library).Length];
                Term loaded = await runtime.Modules.Call(context, module, "load", Term.String(stem));
                if (!loaded.Equals(Term.A("ok")))
                    throw new InvalidOperationException("Native image load returned " + loaded);
                Term sum = await runtime.Modules.Call(context, module, "sum", new Integer(19), new Integer(23));
                Term tuple = await runtime.Modules.Call(context, module, "tuple");
                Term generation = await runtime.Modules.Call(context, module, "generation");
                if (!sum.Equals(new Integer(42)) || !generation.Equals(new Integer(1))
                    || tuple is not TupleTerm items || items.Items.Count != 17)
                    throw new InvalidOperationException("Native fixture values differ from OTP");
                for (int index = 0; index < items.Items.Count; index++)
                    if (!items.Items[index].Equals(new Integer(index + 1)))
                        throw new InvalidOperationException("Native varargs tuple element order differs");
                result.TrySetResult(Term.Tuple(sum, tuple, generation));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
            }

            return Nil.Value;
        });
        Term observed = await result.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await actor.Completion.WaitAsync(TimeSpan.FromSeconds(30));
        await File.WriteAllBytesAsync(output, ExternalTermFormat.Encode(observed));
    }
}

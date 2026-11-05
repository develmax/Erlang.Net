using System.Text.Json;
using System.Runtime.InteropServices;
using Erlang;

internal static class NifResourceImageChecks
{
    internal static async Task Run(string beam, string library, string output)
    {
        const string module = "nif_resource_fixture";
        await using var runtime = new ProcessRuntime();
        await using var prepared = runtime.Modules.PrepareBeam(File.ReadAllBytes(beam), new BeamDependencySelection([module]));
        await runtime.InitializeModulesAsync(registry =>
        {
            NativeResourceStorageChecks.Require(registry.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        });
        var result = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                string stem = Path.GetFullPath(library)[..^Path.GetExtension(library).Length];
                NativeResourceStorageChecks.Require((await runtime.Modules.Call(
                    context,
                    module,
                    "load",
                    Term.String(stem)
                )).Equals(Term.A("ok")));
                await runtime.Modules.Call(context, module, "native_only");
                await Wait(
                    runtime,
                    context,
                    1,
                    0
                );
                var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
                var worker = runtime.Spawn(async child =>
                {
                    try
                    {
                        observed.TrySetResult(await runtime.Modules.Call(child, module, "run"));
                    }
                    catch (Exception error)
                    {
                        observed.TrySetException(error);
                    }

                    return Nil.Value;
                });
                Term values = await observed.Task.WaitAsync(TimeSpan.FromSeconds(30));
                await worker.Completion.WaitAsync(TimeSpan.FromSeconds(30));
                Term before = await Wait(
                    runtime,
                    context,
                    1,
                    1
                );
                NativeResourceStorageChecks.Require((await runtime.Modules.Call(context, module, "release")).Equals(Term.A("ok")));
                Term after = await Wait(
                    runtime,
                    context,
                    2,
                    0
                );
                result.TrySetResult(ExternalTermFormat.Encode(Term.Tuple(values, before, after)));
            }
            catch (Exception error)
            {
                result.TrySetException(error);
            }

            return Nil.Value;
        });
        byte[] bytes = await result.Task.WaitAsync(TimeSpan.FromSeconds(90));
        await actor.Completion.WaitAsync(TimeSpan.FromSeconds(30));
        await File.WriteAllBytesAsync(output, bytes);
        Console.WriteLine(JsonSerializer.Serialize(new { Passed = true, Architecture = RuntimeInformation.ProcessArchitecture.ToString(), PointerBytes = IntPtr.Size, NativeResourceNifExecuted = true, ResourceBinary = true, KeepRelease = true, Destructors = 2 }));
    }

    private static async Task<Term> Wait(
        ProcessRuntime runtime,
        ProcessContext context,
        int count,
        int held
    )
    {
        Term expected = Term.Tuple(new Integer(count), new Integer(held));
        for (int attempt = 0; attempt < 1000; attempt++)
        {
            Term value = await runtime.Modules.Call(context, "nif_resource_fixture", "stats");
            if (value.Equals(expected)) return value;
            await Task.Delay(5);
        }
        throw new InvalidOperationException(NifResourceValidationDiagnostics.Layout);
    }
}

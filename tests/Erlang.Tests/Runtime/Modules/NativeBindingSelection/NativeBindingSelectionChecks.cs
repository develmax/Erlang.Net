using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class NativeBindingSelectionChecks
{
    internal static async Task Run()
    {
        var reports = new List<object>();
        foreach (bool original in new[] { false, true })
        {
            using Stream resource = typeof(NativeBindingSelectionChecks).Assembly.GetManifestResourceStream(NativeBindingSelectionNames.Resource)!;
            using var bytes = new MemoryStream();
            resource.CopyTo(bytes);
            if (Convert.ToHexString(SHA256.HashData(bytes.ToArray())) != NativeBindingSelectionNames.OriginalImageSha256)
                throw new InvalidDataException(NativeBindingSelectionNames.Failure);
            await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
                original ? RuntimeModuleSelection.Beam(NativeBindingSelectionNames.Lists, bytes.ToArray())
                    : RuntimeModuleSelection.Managed(NativeBindingSelectionNames.Lists)
            ]));
            if (runtime.LibrarySelection.Modules.Single(module => module.Module == NativeBindingSelectionNames.Lists).Backend
                != (original ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed)
                || original && runtime.Modules.SourceOwner(NativeBindingSelectionNames.Lists, NativeBindingSelectionNames.Reverse,
                    NativeBindingSelectionNames.ReverseArity) is not PreparedBeamImage)
                throw new InvalidDataException(NativeBindingSelectionNames.Failure);
            MethodInfo call = typeof(ModuleRegistry).GetMethod(NativeBindingSelectionNames.NativeCall, BindingFlags.Instance | BindingFlags.NonPublic)!;
            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle actor = runtime.Spawn(async context =>
            {
                try
                {
                    var probe = new NativeBindingScopeProbe();
                    Func<ProcessContext, IDisposable> selected = _ =>
                    {
                        runtime.Modules.DeleteModule(NativeBindingSelectionNames.Lists);
                        runtime.Modules.Register(
                            NativeBindingSelectionNames.Lists,
                            NativeBindingSelectionNames.Reverse,
                            NativeBindingSelectionNames.ReverseArity,
                            (_, _) => ValueTask.FromResult<Term>(Term.A(NativeBindingSelectionNames.Replacement))
                        );

                        return probe.Enter();
                    };
                    bool nativeFailed = false;
                    try
                    {
                        await (ValueTask<Term>)call.Invoke(
                            runtime.Modules,
                            [context, NativeBindingSelectionNames.Lists, NativeBindingSelectionNames.Reverse, new Term[] { Term.A(NativeBindingSelectionNames.InvalidList), Nil.Value }, selected]
                        )!;
                    }
                    catch (ErlangException failure) when (failure.Reason.Equals(Term.A(NativeBindingSelectionNames.BadArgument)))
                    {
                        nativeFailed = true;
                    }
                    Term replacement = await (ValueTask<Term>)call.Invoke(
                        runtime.Modules,
                        [context, NativeBindingSelectionNames.Lists, NativeBindingSelectionNames.Reverse, new Term[] { Term.A(NativeBindingSelectionNames.InvalidList), Nil.Value }, selected]
                    )!;
                    if (!nativeFailed || !replacement.Equals(Term.A(NativeBindingSelectionNames.Replacement)) || probe.Entered != 1 || probe.Disposed != 1)
                        throw new InvalidDataException(NativeBindingSelectionNames.Failure);
                    reports.Add(new { Original = original, NativeBodySnapshotRetained = nativeFailed, probe.Entered, probe.Disposed, ReplacementDoesNotEnterScope = true });
                    completed.TrySetResult();
                }
                catch (Exception error)
                {
                    completed.TrySetException(error);
                }

                return Nil.Value;
            });
            await completed.Task.WaitAsync(NativeBindingSelectionNames.Deadline);
            await actor.Completion.WaitAsync(NativeBindingSelectionNames.Deadline);
        }
        reports.AddRange(await NativeBindingScopeCases.Run());
        Directory.CreateDirectory(Path.GetDirectoryName(NativeBindingSelectionNames.Output)!);
        await File.WriteAllTextAsync(NativeBindingSelectionNames.Output, JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(reports));
    }

    internal static async Task Race()
    {
        await NativeBindingRaceChecks.Run(NativeBindingSelectionNames.Control);
        await NativeBindingRaceChecks.Run(NativeBindingSelectionNames.After);
    }
}

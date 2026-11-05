namespace Erlang.Tests;

internal static class BulkMapSourceChecks
{
    internal static async Task Run()
    {
        using Stream input = typeof(BulkMapSourceChecks).Assembly.GetManifestResourceStream(BulkMapSourceNames.Resource)!;
        using var bytes = new MemoryStream();
        await input.CopyToAsync(bytes);
        await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed("ordsets"),
            RuntimeModuleSelection.Managed("lists")
        ]));
        Term fixture = runtime.ImportExternalTerm(bytes.ToArray()).Term;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                foreach (TupleTerm row in Cons.Items(fixture).Cast<TupleTerm>())
                {
                    string name = ((Atom)row.Items[0]).Name;
                    if (name is not (BulkMapSourceNames.Mixed or BulkMapSourceNames.Identical or BulkMapSourceNames.Interleaved))
                        continue;

                    Term pairs = row.Items[1];
                    if (Cons.Items(pairs).Count() != BulkMapSourceNames.InputCount)
                        throw new InvalidDataException(BulkMapSourceNames.Failure);

                    var map = (MapTerm)await runtime.Modules.Call(
                        context,
                        "maps",
                        "from_list",
                        pairs
                    );
                    int count = map.Entries.Count;
                    bool valid = name switch
                    {
                        BulkMapSourceNames.Mixed => count == BulkMapSourceNames.InputCount,
                        BulkMapSourceNames.Identical => count == 1,
                        _ => count >= BulkMapSourceNames.MinimumInterleavedCount && count <= BulkMapSourceNames.InputCount
                    };
                    if (!valid)
                        throw new InvalidDataException(BulkMapSourceNames.Failure);

                    Term observed = await runtime.Modules.Call(
                        context,
                        "maps",
                        "to_list",
                        map
                    );
                    if (Cons.Items(observed).Count() != count)
                        throw new InvalidDataException(BulkMapSourceNames.Failure);
                }
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(BulkMapSourceNames.WaitSeconds));
        await actor.Completion.WaitAsync(TimeSpan.FromSeconds(BulkMapSourceNames.WaitSeconds));
    }
}

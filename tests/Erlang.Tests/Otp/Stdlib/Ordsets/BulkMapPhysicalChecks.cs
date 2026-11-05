using System.Reflection;

namespace Erlang.Tests;

internal static class BulkMapPhysicalChecks
{
    internal static async Task Run()
    {
        using Stream input = typeof(BulkMapPhysicalChecks).Assembly.GetManifestResourceStream(BulkMapSourceNames.Resource)!;
        using var bytes = new MemoryStream();
        await input.CopyToAsync(bytes);
        await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            RuntimeModuleSelection.Managed("ordsets"), RuntimeModuleSelection.Managed("lists")
        ]));
        Term fixture = runtime.ImportExternalTerm(bytes.ToArray()).Term;
        TupleTerm row = Cons.Items(fixture).Cast<TupleTerm>().Single(item =>
            item.Items[0] is Atom { Name: BulkMapPhysicalNames.InputCase });
        Term[] keys = Cons.Items(row.Items[1]).Cast<TupleTerm>().Select(item => item.Items[0]).ToArray();
        KeyValuePair<Term, Term>[] physical = [new(keys[0], Term.I(0)), new(keys[1], Term.I(1)), new(keys[0], Term.I(2))];
        // Exercise the actual source builder's private publication boundary.
        // There is no replacement constructor, hash provider or lookup model.
        var factory = typeof(MapTerm).GetMethod(BulkMapPhysicalNames.Factory, BindingFlags.Static | BindingFlags.NonPublic)!;
        var map = (MapTerm)factory.Invoke(null, [physical])!;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                RequireValues(MapOperations.ToList(map, runtime));
                RequireValues(await runtime.Modules.Call(
                    context,
                    "maps",
                    "to_list",
                    map
                ));
                runtime.Send(context.Self, map);
                var copied = await context.ReceiveAsync(value => value as MapTerm, TimeSpan.Zero);
                if (copied is null)
                    throw new InvalidDataException(BulkMapPhysicalNames.Failure);
                RequireValues(MapOperations.ToList(copied, runtime));
                RequireValues(await runtime.Modules.Call(
                    context,
                    "maps",
                    "to_list",
                    copied
                ));
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

    private static void RequireValues(Term list)
    {
        Term[] values = Cons.Items(list).Cast<TupleTerm>().Select(pair => pair.Items[1]).ToArray();
        if (values.Length != BulkMapPhysicalNames.PhysicalCount
            || !values.Select((value, index) => value.Equals(Term.I(index))).All(equal => equal))
            throw new InvalidDataException(BulkMapPhysicalNames.Failure);
    }
}

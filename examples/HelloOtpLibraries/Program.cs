using System.Text.Json;
using Erlang;
using static HelloOtpLibraries.OtpExampleNames;

RuntimeLibrarySelection selection = RuntimeLibraryManifest.Load(Path.Combine(AppContext.BaseDirectory, RuntimeLibraryManifest.FileName));
await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection);
foreach (RuntimeLibraryModuleSnapshot module in runtime.LibrarySelection.Modules)
{
    if (module.Backend != RuntimeModuleBackend.Beam || runtime.Modules.HasOldCode(module.Module)
        || !ReferenceEquals(module.Image, runtime.Modules.CurrentCodeImage(module.Module))
        || module.Exports.Any(export => runtime.Modules.SourceOwner(module.Module, export.Name, export.Arity) is not PreparedBeamImage))
        throw new InvalidOperationException(Failure);
}
var completed = new TaskCompletionSource<Dictionary<string, string?>>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle actor = runtime.Spawn(async context =>
{
    try
    {
        var rows = new Dictionary<string, string?>();
        Term numbers = Term.List(Term.I(2), Term.I(1), Term.I(2));
        Term pairs = Term.List(Term.Tuple(Term.I(1), Term.A(FirstValue)), Term.Tuple(Term.I(2), Term.A(SecondValue)));

        async ValueTask<Term> Call(string module, string function, params Term[] arguments)
            => await runtime.Modules.Call(
                context,
                module,
                function,
                arguments
            );
        rows[Lists] = (await Call(Lists, ListSort, numbers)).ToString();
        rows[Maps] = (await Call(Maps, MapToList, new MapTerm(new Dictionary<Term, Term>
        {
            [Term.I(1)] = Term.A(FirstValue),
            [Term.I(2)] = Term.A(SecondValue)
        }))).ToString();
        rows[Properties] = (await Call(
            Properties,
            PropertyValue,
            Term.A(ReplyKey),
            Term.List(Term.Tuple(Term.A(ReplyKey), Term.A(Reply)))
        )).ToString();
        rows[Binary] = (await Call(Binary, EncodeHex, new BitString([0x48, 0x69]))).ToString();
        rows[Queue] = (await Call(Queue, QueueToList, await Call(Queue, QueueFromList, numbers))).ToString();
        rows[OrderedSets] = (await Call(OrderedSets, OrderedSetFromList, numbers)).ToString();
        rows[Sets] = (await Call(Sets, SetToList, await Call(Sets, SetFromList, numbers))).ToString();
        rows[Dictionary] = (await Call(Dictionary, DictionaryToList, await Call(Dictionary, DictionaryFromList, pairs))).ToString();
        rows[OrderedDictionary] = (await Call(OrderedDictionary, OrderedDictionaryFromList, pairs)).ToString();
        rows[BalancedSets] = (await Call(BalancedSets, BalancedSetToList, await Call(BalancedSets, BalancedSetFromList, numbers))).ToString();
        rows[BalancedTrees] = (await Call(BalancedTrees, BalancedTreeToList, await Call(BalancedTrees, BalancedTreeFromDictionary, pairs))).ToString();
        completed.TrySetResult(rows);
    }
    catch (Exception error)
    {
        completed.TrySetException(error);
    }

    return Nil.Value;
});
Dictionary<string, string?> results = await completed.Task.WaitAsync(Deadline);
if (!(await actor.Completion.WaitAsync(Deadline)).Equals(Term.A(Normal)))
    throw new InvalidOperationException(Failure);
Console.WriteLine(JsonSerializer.Serialize(
    new
    {
        Backends = runtime.LibrarySelection.Modules.Select(module => new
        {
            module.Module,
            Backend = module.Backend.ToString(),
            module.InputSha256,
            PublicExports = module.Exports.Count - ModuleInfoExports,
            NativePrimitives = module.NativePrimitives.Count
        }),
        Results = results
    },
    RuntimeLibraryManifest.SerializationOptions
));

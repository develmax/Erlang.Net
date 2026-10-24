using Erlang;
using Erlang.Otp;
using static HelloOtpLibraryReference.LibraryReferenceNames;

RuntimeModuleBackend lists = args.Length > 0 ? Enum.Parse<RuntimeModuleBackend>(args[0]) : RuntimeModuleBackend.Managed;
RuntimeModuleBackend queue = args.Length > 1 ? Enum.Parse<RuntimeModuleBackend>(args[1]) : RuntimeModuleBackend.Beam;
var selection = new RuntimeLibrarySelection([
    OtpStdlib.Select(Lists, lists),
    OtpStdlib.Select(Queue, queue)
]);
await using var runtime = await ProcessRuntime.CreateAsync(selection);
var completed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle actor = runtime.Spawn(async context =>
{
    try
    {
        Term sorted = await runtime.Modules.Call(
            context,
            Lists,
            Sort,
            Term.List(Term.I(3), Term.I(1), Term.I(2))
        );
        Term queued = await runtime.Modules.Call(
            context,
            Queue,
            FromList,
            sorted
        );
        Term result = await runtime.Modules.Call(
            context,
            Queue,
            ToList,
            queued
        );
        completed.TrySetResult(result);
    }
    catch (Exception failure)
    {
        completed.TrySetException(failure);
    }

    return Nil.Value;
});
Term outcome = await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
await actor.Completion.WaitAsync(TimeSpan.FromSeconds(30));
if (!outcome.Equals(Term.List(Term.I(1), Term.I(2), Term.I(3))))
    throw new InvalidOperationException(Failure);
foreach (RuntimeLibraryModuleSnapshot module in runtime.LibrarySelection.Modules)
{
    bool original = runtime.Modules.SourceOwner(module.Module, module.Module == Lists ? Sort : FromList, 1) is PreparedBeamImage;
    if (original != (module.Backend == RuntimeModuleBackend.Beam))
        throw new InvalidOperationException(Failure);
    Console.WriteLine($"{module.Module}: {module.Backend}");
}
Console.WriteLine(outcome);

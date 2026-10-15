using Erlang;
using NifPrototype;

await using var runtime = new ProcessRuntime();
runtime.Modules.PublishModule(WindowsNifFixtureLibrary.ModuleName);
ModuleCodeImage image = runtime.Modules.CurrentCodeImage(WindowsNifFixtureLibrary.ModuleName)!;
var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
runtime.Spawn(context =>
{
    try
    {
        NifCoreChecks.Run(context, image, args);
        completion.SetResult();
    }
    catch (Exception exception)
    {
        completion.SetException(exception);
    }

    return ValueTask.FromResult<Term>(Term.I(0));
});
await completion.Task.WaitAsync(TimeSpan.FromSeconds(30));
Console.WriteLine(args.Length == 2 ? "NIF internal and native fixture checks passed." : "NIF internal checks passed; native fixture NOT executed.");

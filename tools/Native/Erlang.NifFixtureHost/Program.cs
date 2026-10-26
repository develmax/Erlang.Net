using Erlang;
using Erlang.NifFixtureHost;

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
await completion.Task.WaitAsync(NifFixtureNames.Deadline);
Console.WriteLine(args.Length == NifFixtureNames.NativeArgumentCount ? NifFixtureNames.NativeSuccess : NifFixtureNames.InternalSuccess);

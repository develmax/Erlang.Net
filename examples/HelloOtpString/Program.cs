using System.Text.Json;
using Erlang;
using static HelloOtpString.StringExampleNames;

RuntimeLibrarySelection selection = RuntimeLibraryManifest.Load(Path.Combine(AppContext.BaseDirectory, RuntimeLibraryManifest.FileName));
await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
RuntimeLibraryModuleSnapshot receipt = runtime.LibrarySelection.Modules.Single(module => module.Module == Module);
var completed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle worker = runtime.Spawn(async context =>
{
    try
    {
        Term upper = await runtime.Modules.Call(
            context,
            Module,
            Uppercase,
            Term.String("Straße")
        );
        Term split = await runtime.Modules.Call(
            context,
            Module,
            Split,
            new BitString("a..b..c"u8.ToArray()),
            Term.String(".."),
            Term.A(All)
        );
        completed.TrySetResult(Term.Tuple(upper, split));
    }
    catch (Exception error)
    {
        completed.TrySetException(error);
    }

    return Nil.Value;
});
Term result = await completed.Task.WaitAsync(Deadline);
if (!(await worker.Completion.WaitAsync(Deadline)).Equals(Term.A(Normal)))
{
    throw new InvalidOperationException(Failure);
}
Console.WriteLine(JsonSerializer.Serialize(
    new
    {
        Backend = receipt.Backend.ToString(),
        receipt.InputSha256,
        receipt.ManagedAssemblies,
        Result = result.ToString()
    },
    new JsonSerializerOptions { WriteIndented = true }
));

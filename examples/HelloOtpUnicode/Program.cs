using System.Text.Json;
using Erlang;
using static HelloOtpUnicode.UnicodeExampleNames;

RuntimeLibrarySelection selection = RuntimeLibraryManifest.Load(Path.Combine(AppContext.BaseDirectory, RuntimeLibraryManifest.FileName));
await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
RuntimeLibraryModuleSnapshot receipt = runtime.LibrarySelection.Modules.Single(module => module.Module == Module);
var completed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle worker = runtime.Spawn(async context =>
{
    try
    {
        Term encoded = await runtime.Modules.Call(
            context,
            Module,
            Binary,
            Term.String("Привет 👋"),
            Term.A(Utf8),
            Term.Tuple(Term.A(Utf16), Term.A(Little))
        );
        Term normalized = await runtime.Modules.Call(
            context,
            Module,
            Normalize,
            Term.List(Term.I(65), Term.I(778))
        );
        completed.TrySetResult(Term.Tuple(encoded, normalized));
    }
    catch (Exception error)
    {
        completed.TrySetException(error);
    }

    return Nil.Value;
});
Term result = await completed.Task.WaitAsync(Deadline);
if (!(await worker.Completion.WaitAsync(Deadline)).Equals(Term.A("normal")))
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

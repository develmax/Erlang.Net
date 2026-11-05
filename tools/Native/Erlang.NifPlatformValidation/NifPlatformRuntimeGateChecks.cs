using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

internal static class NifPlatformRuntimeGateChecks
{
    internal static async Task Run()
    {
        if (IntPtr.Size != sizeof(uint))
            throw new InvalidOperationException(NifPlatformValidationDiagnostics.Expected32);
        try
        {
            await using var runtime = new ProcessRuntime();
        }
        catch (NotSupportedException error) when (error.Message == CellStorage526Names.WordSize)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                PointerBytes = IntPtr.Size,
                RuntimeStarted = false,
                NativeNifExecuted = false,
                KnownGateObserved = true,
                Reason = error.Message
            }));

            return;
        }
        throw new InvalidOperationException(NifPlatformValidationDiagnostics.UnexpectedRuntime);
    }
}

using System.Text;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class NifLinuxCallbackTrace
{
    internal static void Mark(string destination, string stage, string? module = null, string? function = null, Exception? failure = null)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            Utc = DateTimeOffset.UtcNow,
            Process = Environment.ProcessId,
            Thread = Environment.CurrentManagedThreadId,
            Stage = stage,
            Module = module,
            Function = function,
            Exception = failure?.ToString(),
            DiagnosticOnly = true
        }) + "\n");
        using var file = new FileStream(Path.Combine(destination, NifLinuxCallbackTraceNames.Journal), FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        file.Write(bytes);
        file.Flush(flushToDisk: true);
    }
}

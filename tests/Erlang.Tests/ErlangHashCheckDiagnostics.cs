namespace Erlang.Tests;

internal static class ErlangHashCheckDiagnostics
{
    public const string BinaryCopied = "Portable hash allocated a copy of the binary payload";
    public const string DeepTraversal = "Portable hash did not complete a deep structure";
    public const string AsynchronousMeasurement = "Portable hash allocation measurement requires synchronous completion";

    public static string ThreadChanged(int starting, int ending) => $"Portable hash allocation measurement changed thread from {starting} to {ending}";

    public static string AllocationExceeded(long bytes, string resultType, int thread) => $"Portable hash allocation budget exceeded: {bytes} bytes, budget {ErlangHashCheckParameters.AllocationBudgetBytes}, result {resultType}, thread {thread}, synchronous completion";
}

namespace HashAllocationDiagnostic;

internal static class DiagnosticParameters
{
    public const int BinaryBytes = 4 * 1024 * 1024;
    public const int Samples = 16;
    public const long FullRange = 4294967296;
    public const int OriginalBudgetBytes = 4096;
    public const string Module = "erlang";
    public const string Function = "phash2";
    public const string AlgorithmType = "Erlang.ErlangNativeHash";
    public const string AlgorithmMethod = "Portable2";
    public const string SourceHead = "5bec498c7774ef324ba60389d9ff6edf110b1836";
}

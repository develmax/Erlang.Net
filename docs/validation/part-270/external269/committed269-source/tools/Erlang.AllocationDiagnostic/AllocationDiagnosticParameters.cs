namespace AllocationDiagnostic;

internal static class AllocationDiagnosticParameters
{
    internal const int FunctionArity = 2;
    internal const int CategoryCount = 5;
    internal const int ExpectedRows = 1 + CategoryCount * Samples;
    internal const int BinaryBytes = 4 * 1024 * 1024;
    internal const long FullRange = 4294967296;
    internal const int Samples = 16;
    internal const int BudgetBytes = 4096;
}

namespace Erlang.Tests;

public static class ProcessTableHostDiagnostics
{
    public const string Capacity = "Process table must enforce capacity before scheduling excess work";
    public const string Reuse = "Released slots must accept a new distinct process identity";
    public const string Stale = "A stale process identifier must not address a reused slot";
    public const string Walk = "Batched process iteration must include each actual live process exactly once";
    public const string Termination = "Capacity checks must finish normally without leaking owned processes";
}

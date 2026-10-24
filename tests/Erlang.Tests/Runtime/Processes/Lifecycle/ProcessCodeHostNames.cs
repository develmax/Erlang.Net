namespace Erlang.Tests;

public static class ProcessCodeHostNames
{
    public const string Test = "runtime/process-code-queued-death-and-heap-function-boundaries";
    public const string Source = "process_code_heap_source";
    public const string Callback = "process_code_host_callback";
    public const string Block = "block";
    public const string Held = "held";
    public const string Kept = "kept";
    public const string Make = "make";
    public const string Request = "request";
    public const string Synchronous = "synchronous";
    public const string Completed = "completed";
    public const string Broken = "Queued process-code reply,callback ownership or heap-function boundary differs";
}

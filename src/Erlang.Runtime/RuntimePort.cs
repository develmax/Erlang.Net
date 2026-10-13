using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimePort(
    PortTerm identity,
    Pid connected,
    PortChildProcess process,
    PortOpenOptions options
)
{
    public PortTerm Identity { get; } = identity;
    public Pid Connected { get; set; } = connected;
    public PortChildProcess Process { get; } = process;
    public Stream Input { get; } = process.Input;
    public Stream Output { get; } = process.Output;
    public int OsPid { get; } = process.Id;
    public PortOpenOptions Options { get; } = options;
    public PortLineBuffer? Lines { get; } = options.LineBytes == 0 ? null : new(options.LineBytes);
    public HashSet<Pid> Links { get; } = new() { connected };
    public Term Data { get; set; } = Term.A(ProcessRegistryAtoms.Undefined);
    public long InputBytes;
    public long OutputBytes;
    public bool Closed;
    public Task Writes { get; set; } = Task.CompletedTask;
    public ulong QueuedBytes { get; set; }
    public bool Busy { get; set; }
    public ulong MessageBytes { get; set; }
    public bool MessagesBusy { get; set; }
    public TaskCompletionSource MessageWritable { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Writable { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public CancellationTokenSource Stop { get; } = new();
    public Task? Pump { get; set; }
    public Task Commands { get; set; } = Task.CompletedTask;
}

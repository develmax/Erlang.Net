using System.Diagnostics;

namespace Erlang;

internal sealed class RuntimePort(
    PortTerm identity,
    Pid connected,
    Process process,
    PortOpenOptions options
)
{
    public PortTerm Identity { get; } = identity;
    public Pid Connected { get; set; } = connected;
    public Process Process { get; } = process;
    public Stream Input { get; } = process.StandardInput.BaseStream;
    public Stream Output { get; } = process.StandardOutput.BaseStream;
    public int OsPid { get; } = process.Id;
    public PortOpenOptions Options { get; } = options;
    public PortLineBuffer? Lines { get; } = options.LineBytes == 0 ? null : new(options.LineBytes);
    public HashSet<Pid> Links { get; } = new() { connected };
    public Term Data { get; set; } = Term.A(ProcessRegistryAtoms.Undefined);
    public long InputBytes;
    public long OutputBytes;
    public bool Closed;
    public SemaphoreSlim WriteGate { get; } = new(1, 1);
    public CancellationTokenSource Stop { get; } = new();
    public Task? Pump { get; set; }
    public Task Commands { get; set; } = Task.CompletedTask;
}

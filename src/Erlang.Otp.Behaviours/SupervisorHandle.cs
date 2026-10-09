using System.Diagnostics;

namespace Erlang.Otp;

public sealed class SupervisorHandle
{
    private (Term Id, Pid Pid)[] children = [];
    public ProcessHandle Process { get; internal set; } = null!;
    public IReadOnlyList<(Term Id, Pid Pid)> Children => Array.AsReadOnly(Volatile.Read(ref children));
    internal void Publish(IEnumerable<(Term Id, Pid Pid)> current) => Volatile.Write(ref children, current.ToArray());
}

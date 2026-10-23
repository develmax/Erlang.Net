using System.Diagnostics;

namespace Erlang.Otp;

public sealed class SupervisorHandle
{
    private (Term Id, Pid Pid)[] children = [];
    private Term[] restarting = [];
    public ProcessHandle Process { get; internal set; } = null!;
    public IReadOnlyList<ChildSpec> Specifications { get; internal set; } = [];
    public IReadOnlyList<(Term Id, Pid Pid)> Children => Array.AsReadOnly(Volatile.Read(ref children));

    internal void Publish(IEnumerable<(Term Id, Pid Pid)> current) => Volatile.Write(ref children, current.ToArray());

    internal bool IsRestarting(Term id) => Volatile.Read(ref restarting).Contains(id);

    internal void PublishRestarting(IEnumerable<Term> current) => Volatile.Write(ref restarting, current.ToArray());
}

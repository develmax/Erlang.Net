using System.Numerics;
namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool ReadDebugSame420(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DebugSame420Names.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (arguments.Count != 2) throw new ArgumentException(DebugSame420Names.WrongArguments);
            Term first = arguments[0], second = arguments[1];
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            if (ReferenceEquals(first, second)) return true;
            if (first is TupleTerm { HasGlobalEmptyStorage428: true }
                && second is TupleTerm { HasGlobalEmptyStorage428: true }) return true;
            if (first is Atom atom && second is Atom other) return string.Equals(atom.Name, other.Name, StringComparison.Ordinal);
            if (first is Nil && second is Nil) return true;
            if (first is Integer left && second is Integer right)
            {
                BigInteger bound = DebugSame420Names.SmallMagnitude;

                return left.Value >= -bound && left.Value < bound && right.Value >= -bound && right.Value < bound && left.Value == right.Value;
            }
            if (first is Pid pid && second is Pid otherPid)
                return IsCurrentLocal(pid) && IsCurrentLocal(otherPid) && pid.Id == otherPid.Id;
            if (first is PortTerm port && second is PortTerm otherPort)
            {
                LocalNodeSnapshot current = LocalIdentity.Snapshot;

                bool Local(PortTerm candidate) => candidate.LocalIdentity is { } identity ? ReferenceEquals(identity, LocalIdentity) : candidate.WireIdentity == current;

                return Local(port) && Local(otherPort) && port.Id == otherPort.Id;
            }

            // Every other actual representation is boxed. Semantic/exact equality,
            // visible bytes or equal FUN/reference metadata cannot merge two boxes.
            return false;
        }
    }
}

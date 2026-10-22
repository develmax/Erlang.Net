namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term ReadDebugCode427(
        ProcessContext context,
        DebugCodeProfile427 profile,
        string function,
        IReadOnlyList<Term> arguments
    )
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DebugCodeNames427.Foreign);
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            if (!ReferenceEquals(profile, BeamRegisterInterpreter.ManagedDebugCodeProfile427))
                throw new InvalidOperationException(DebugCodeNames427.Profile);
            if (arguments.Count != (function == DebugCodeNames427.Disassemble ? 1 : 0)) throw new ArgumentException(DebugCodeNames427.Arity);

            // Exact source no-threaded branch: disassemble ignores every value,
            // including invalid/native-address-looking values. Do not perform
            // the threaded branch's MFA or address validation on this backend.

            return function == DebugCodeNames427.Disassemble ? Term.A(ErlangBooleanAtoms.False) : Term.I(0);
        }
    }
}

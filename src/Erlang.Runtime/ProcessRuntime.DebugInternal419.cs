namespace Erlang;

public sealed partial class ProcessRuntime
{
    private bool internalStateEnabled419;

    internal void RequireDebugInternalActor419(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(DebugInternalNames419.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }

    private void RequireDebugInternalEnabled419(ProcessContext context)
    {
        RequireDebugInternalActor419(context);
        if (!internalStateEnabled419) throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
    }

    internal Term ReadDebugInternal419(ProcessContext context, Term query)
    {
        lock (gate)
        {
            RequireDebugInternalEnabled419(context);
            if (query is Atom name)
            {
                switch (name.Name)
                {
                    case DebugInternalNames419.Available: return Term.A(ErlangBooleanAtoms.True);
                    case DebugUniqueNames436.Minimum:
                    case DebugUniqueNames436.MonotonicMinimum: return Term.I(ErlangClockParameters.SmallIntegerMinimum);
                    case DebugUniqueNames436.MonotonicState: return UniqueMonotonic436.State;
                    case DebugInternalNames419.Reds: return Term.I(context.DebugRemainingReductions419);
                    case DebugInternalNames419.Processes: return processSlots.DebugList419();
                    case DebugInternalNames419.NextPid: return new Integer(new System.Numerics.BigInteger(unchecked((ulong)(nextPid + 1))));
                    case DebugInternalNames419.NextPort: return Ports.DebugNextIdentity419;
                    case DebugInternalNames419.Debugger: return CoreModules.Boolean(!HasDisabledDebugger413);
                    case DebugInternalNames419.ForceFragments:
                    case DebugInternalNames419.CollisionBonanza: return Term.A(ErlangBooleanAtoms.False);
                    case DebugInternalNames419.LockGraph: return Term.A(DebugInternalNames419.NotSupported);
                }
                if (DebugInternalKeys419.GetAtoms.Contains(name.Name)) throw new NotSupportedException(DebugInternalNames419.Unimplemented);
            }
            else if (query is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom key)
            {
                if (key.Name == DebugInternalNames419.ProcessStatus)
                {
                    if (tuple.Items[1] is not Pid pid || !ReferenceEquals(pid.LocalIdentity, LocalIdentity)) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    if (!processes.TryGetValue(pid, out var process) || process.Terminated) return Term.A(ProcessRegistryAtoms.Undefined);

                    return Term.A(process.SuspensionRequested ? ProcessInfoStatusNames.Suspended : process.WaitingForMessage ? ProcessInfoStatusNames.Waiting : !process.ExecutionStarted || process.Yielding ? ProcessInfoStatusNames.Runnable : ProcessInfoStatusNames.Running);
                }
                if (key.Name == DebugInternalNames419.InternalHash)
                {
                    using var scope = MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);

                    return new Integer(new System.Numerics.BigInteger(scope.Hash(tuple.Items[1])));
                }
                if (DebugInternalKeys419.GetTuple2.Contains(key.Name)) throw new NotSupportedException(DebugInternalNames419.Unimplemented);
            }
            else if (query is TupleTerm { Items.Count: 3 } triple && triple.Items[0] is Atom tripleKey && DebugInternalKeys419.GetTuple3.Contains(tripleKey.Name)) throw new NotSupportedException(DebugInternalNames419.Unimplemented);
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    internal Term WriteDebugInternal419(ProcessContext context, Term key, Term value)
    {
        bool warn = false;
        bool previous = false;
        lock (gate)
        {
            RequireDebugInternalActor419(context);
            if (key is Atom { Name: DebugInternalNames419.Available } && value is Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } enabled)
            {
                previous = internalStateEnabled419;
                internalStateEnabled419 = enabled.Name == ErlangBooleanAtoms.True;
                warn = internalStateEnabled419;
            }
            else
            {
                RequireDebugInternalEnabled419(context);
                // The only available_internal_state handler is the boolean special case
                // before the enable gate; other values fall through to native BADARG.
                if (key is Atom { Name: DebugInternalNames419.Available }) throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (key is Atom { Name: DebugUniqueNames436.MonotonicState })
                    return CoreModules.Boolean(UniqueMonotonic436.Set(value));
                if (key is Atom { Name: DebugInternalNames419.Reds })
                {
                    if (value is not Integer integer || integer.Value < long.MinValue || integer.Value > long.MaxValue) throw new ErlangException(ErlangErrorReasons.BadArgument);
                    if (integer.Value >= 0 && integer.Value <= DebugInternalNames419.NativeContextReductions) context.DebugSetRemainingReductions419((int)integer.Value);

                    return Term.A(ErlangBooleanAtoms.True);
                }
                if (key is Atom atom && DebugInternalKeys419.SetAtoms.Contains(atom.Name)) throw new NotSupportedException(DebugInternalNames419.Unimplemented);
                if (key is TupleTerm tuple && tuple.Items.Count >= 1 && tuple.Items[0] is Atom head && DebugInternalKeys419.SetTuples.Contains(head.Name)) throw new NotSupportedException(DebugInternalNames419.Unimplemented);
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        // Every successful enable, including true->true, emits the source warning.
        // No await/task retains the caller context after this synchronous invocation.
        if (warn) SendDebugInternalWarning419(context);

        return CoreModules.Boolean(previous);
    }
}

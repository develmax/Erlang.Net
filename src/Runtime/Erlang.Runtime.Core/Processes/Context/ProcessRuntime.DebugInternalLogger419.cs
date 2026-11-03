using System.Text;
namespace Erlang;

public sealed partial class ProcessRuntime
{
    private void SendDebugInternalWarning419(ProcessContext context)
    {
        string message = DebugInternalNames419.ProcessPrefix + DebugInternalTermFormatting420.Text(this, context.Self) + DebugInternalNames419.Space;
        if (IsDistributionAlive) message += DebugInternalNames419.NodePrefix + DebugInternalTermFormatting420.Text(this, NodeAtom) + DebugInternalNames419.Space;
        message += DebugInternalNames419.EnableMessage;
        Term level = WarningMapNative.Value(WarningMapping);
        string tag = WarningMapping switch
        {
            RuntimeWarningMapping.Info => DebugInternalNames419.InfoMessage,
            RuntimeWarningMapping.Warning => DebugInternalNames419.WarningMessage,
            _ => DebugInternalNames419.Error
        };
        Term text = Cons.From(Encoding.UTF8.GetBytes(message).Select(value => (Term)Term.I(value)));
        var origin = new MapTerm([new(Term.A(DebugInternalNames419.Emulator), Term.A(ErlangBooleanAtoms.True)), new(Term.A(DebugInternalNames419.Tag), Term.A(tag))]);
        var metadata = new MapTerm([new(Term.A(DebugInternalNames419.ErrorLogger),origin),new(Term.A(DebugInternalNames419.GroupLeader),context.GroupLeader),new(Term.A(DebugInternalNames419.Pid),context.Self),new(
            Term.A(DebugInternalNames419.Time),
            new Integer(ErlangClockState.SystemNanoseconds/(ErlangClockParameters.NanosecondsPerSecond/ErlangClockParameters.MicrosecondsPerSecond))
        )]);
        Term notice = Term.Tuple(
            Term.A(DebugInternalNames419.Log),
            level,
            Term.String(DebugInternalNames419.Format),
            Term.List(text),
            metadata
        );
        bool fallback = false;
        lock (gate)
        {
            // A warning is a VM system message; it must not inherit a caller seq token.
            // No retained background task or captured ProcessContext is created.
            if (disposed || IsHaltRequested) return;
            Term target = systemLogger;
            bool named = target is Atom { Name: RuntimeSystemLoggerNames.Logger };
            ProcessContext? receiver = null;
            if (target is Atom { Name: RuntimeSystemLoggerNames.Undefined }) return;
            if (target is Pid pid && (!processes.TryGetValue(pid, out receiver) || !IsOwnedLiveNodeProcess(receiver)))
            {
                systemLogger = RuntimeSystemLoggerAtoms.Logger;
                named = true;
            }
            if (named)
            {
                receiver = names.GetValueOrDefault(RuntimeSystemLoggerNames.Logger) is Pid registered ? processes.GetValueOrDefault(registered) : null;
                if (receiver is not null && !IsOwnedLiveNodeProcess(receiver)) receiver = null;
                if (receiver is not null && receiver.Self.Equals(context.Self)) return;
            }
            if (receiver is null) fallback = true;
            else receiver.Mailbox.SendTraced(
                notice,
                null,
                preserve: true,
                from: Term.A(DebugInternalNames419.System)
            );
        }
        if (fallback)
        {
            byte[] bytes = [.. Encoding.UTF8.GetBytes(DebugInternalNames419.NoLogger), .. DebugInternalTermFormatting420.Bytes(this, notice).Span, .. Encoding.UTF8.GetBytes(DebugInternalNames419.Newline)];
            StandardStreams.Write(
                ErlangStandardStreamNames.StandardError,
                BitString.FromOwnedBytes(OperatingSystem.IsWindows() ? NativePrintfOutput.Encode(bytes) : bytes)
            );
        }
    }
}

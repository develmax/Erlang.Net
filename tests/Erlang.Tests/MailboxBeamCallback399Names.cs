using System.Reflection;
using Erlang;

namespace Erlang.Tests;

internal static class MailboxBeamCallback399Names
{
    internal const string ReceiveMethod = "ReceiveBeamAsync";
    internal const string DeferredGateHeldProperty = "DeferredGateHeld";
    internal const string InvalidTimeoutAtom = "wrong_timeout";
    internal const string StopMethod = "StopForHalt";
    internal const string ReserveMarkerMethod = "ReserveReceiveMarker";
    internal const string BindMarkerMethod = "BindReceiveMarker";
    internal const string UseMarkerMethod = "UseReceiveMarker";
    internal const string DeferredAdmissionType = "Erlang.DeferredMailboxAdmission";
    internal const string SendDeferredMethod = "SendDeferred";
    internal const string ClaimDeferredMethod = "ClaimDeferred";
    internal const string PrepareProperty = "Prepare";
    internal const string CompleteProperty = "Complete";
    internal const string HasDeferredPendingProperty = "HasDeferredPending";
    internal const string BoundedError = "callback_bounded";
    internal const string CallbackError = "callback_error";
    internal const string FirstMessageAtom = "callback_a";
    internal const string SecondMessageAtom = "callback_b";
    internal const string ChosenResultAtom = "callback_c";
    internal const string PriorityMessageAtom = "callback_priority";
    internal const int Attempts = 8;
    internal const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static readonly Term A = Term.A(FirstMessageAtom);
    internal static readonly Term B = Term.A(SecondMessageAtom);
    internal static readonly Term C = Term.A(ChosenResultAtom);
    internal static readonly Term Priority = Term.A(PriorityMessageAtom);
    internal static readonly TimeSpan CallbackDeadline = TimeSpan.FromSeconds(5);
    internal static readonly TimeSpan ProcessDeadline = TimeSpan.FromSeconds(10);
}

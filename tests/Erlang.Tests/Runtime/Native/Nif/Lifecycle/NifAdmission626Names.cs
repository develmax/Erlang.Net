using System.Reflection;

namespace Erlang.Tests;

internal static class NifAdmission626Names
{
    internal const string GateExit = "runtime/nif-admission626/real-unclaimed-gate-actor-exit-and-host-failure";
    internal const string Scoped = "runtime/nif-admission626/claimed-physical-scope-survives-cancellation-and-exit";
    internal const string BarrierExit = "runtime/nif-admission626/real-unclaimed-barrier-actor-exit-and-host-failure";
    internal const string BarrierPending = "runtime/nif-admission626/real-pending-barrier-cancellation-and-cleanup";
    internal const string GateType = "Erlang.NifNativeLifecycleGate";
    internal const string TurnType = "Erlang.RuntimeActorExecutionTurn";
    internal const string PermitType = "Erlang.NifNativeLifecyclePermit616";
    internal const string Acquire = "AcquireAsync616";
    internal const string AcquireBarrier = "AcquireNifBarrier616";
    internal const string Ready = "ready";
    internal const string Gate = "gate";
    internal const string Waiting = "WaitingAdmissions616";
    internal const string CurrentActor = "CurrentNifAdmission616";
    internal const string EnterScope = "Enter616";
    internal const string ScopeWriter = "StoreActualNifScope626";
    internal const string State = "State616";
    internal const string EnteredState = "Entered";
    internal const string ClaimedState = "Granted";
    internal const string Kill = "kill";
    internal const string Broken = "Actual NIF admission custody/delivery invariant failed.";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags Statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);
}

namespace Erlang.Tests;

internal static class BeamLazyRootSlots825Names
{
    internal const string Case = "runtime/beam825/lazy-mirror-pairs-and-rollback";
    internal const string Failure = "Lazy BEAM mirror slots changed real custody or transactional rollback.";
    internal const string Module = "beam_lazy_pairs825";
    internal const string Probe = "beam_lazy_probe825";
    internal const string Run = "run";
    internal const string Check = "check";
    internal const string Source = "-module(beam_lazy_pairs825). -export([run/0]). run() -> beam_lazy_probe825:check().";
    internal const string StateType = "Erlang.BeamRegisterState";
    internal const string WordType = "Erlang.ProcessWordExecution504";
    internal const string SlotType = "Erlang.BeamRegisterSlot";
    internal const string LocalType = "Erlang.BeamLocalFrame";
    internal const string Heap = "TermHeap506";
    internal const string NativeArena = "OwnedWords504";
    internal const string Authority = "OwnedExecutionEnabled683";
    internal const string Roots = "roots507";
    internal const string Frame = "frame";
    internal const string Frames = "frames";
    internal const string Issued = "IssuedRootSlotIds507";
    internal const string Read = "Read";
    internal const string Write = "Write";
    internal const string Prepare = "Prepare";
    internal const string FromTerm = "FromTerm";
    internal const string ReadTerm = "ReadTerm";
    internal const string Rollback = "Rollback";
    internal const string Retain = "Retain";
    internal const string Enter = "Enter";
    internal const string Collect = "Collect";
    internal const string Allocate = "Allocate";
    internal const string Peek = "Peek";
    internal const string WriteY = "WriteY";
    internal const string ReadY = "ReadY";
    internal const string Trim = "Trim";
    internal const string Clone = "Clone";
    internal const string Clear = "ClearXExcept";
    internal const string Live = "RequireLive507";
    internal const int LastX = 1023;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}

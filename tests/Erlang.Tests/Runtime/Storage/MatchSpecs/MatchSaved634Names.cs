using System.Reflection;

namespace Erlang.Tests;

internal static class MatchSaved634Names
{
    internal const string ReceiptFile = "receipt.json";
    internal const string ProtocolSchema = "match634/constant-table-v1";
    internal const string OriginalPin = "ad05823719d77c8faee87348ea39513d4e2f99c5";
    internal const string OriginalObserverSha256 = "C6F7293D405F2127C002F90956C2049688F3A166A67FA46E618080FD2C1CA3F2";
    internal const int ProtocolEntries = 10;
    internal const string Compiler = "Erlang.MatchSavedCompiler634";
    internal const string ProgramType = "Erlang.MatchSavedProgram634";
    internal const string FragmentType = "Erlang.MatchSavedFragment604";
    internal const string Compile = "TryCompile634";
    internal const string Run = "Run634";
    internal const string Acquire = "Acquire634";
    internal const string Copy = "CopyResult634";
    internal const string Text = "TextAddress634";
    internal const string Fragment = "FragmentAddress634";
    internal const string Close = "Close634";
    internal const string Freed = "PhysicalFreed634";
    internal const string Allocations = "NativeAllocationCount634";
    internal const string StopAdmission = "SavedMatchAdmissionClosed634";
    internal const string CaptureFragment = "TryCapture604";
    internal const string ReadFragment = "Acquire604";
    internal const string CopyFragment = "Materialize604";
    internal const string CloseFragment = "Close604";
    internal const string Atom = "saved_constant634";
    internal const string Failure = "Internal saved program634 component invariant failed.";
    internal const string ProtocolCase = "runtime/match634/internal-native-constant-original-protocol";
    internal const string MutationCase = "runtime/match634/native-operand-reader-and-detached-result";
    internal const string HeaderCase = "runtime/match634/native-fragment-links-offheap-and-extent-refusal";
    internal const string SourceCase = "runtime/match634/source-ast-collectable-after-transfer";
    internal const string RollbackCase = "runtime/match634/constructor-rollback-and-cancelled-copy";
    internal const string DisposeCase = "runtime/match634/real-vm-dispose-waits-reader";
    internal const string HaltCase = "runtime/match634/real-vm-halt-quiescence-waits-reader";
    internal const string Output = "artifacts/match634-components";
    internal const string OutcomeFile = "observed.etf";
    internal const string GuidFormat = "N";
    internal const int First = 123;
    internal const int Second = 567;
    internal const int NextOffsetBytes = 0;
    internal const int OffHeapOffsetBytes = 8;
    internal const int UsedWordsOffsetBytes = 32;
    internal const int RootOffsetBytes = 16;
    internal const int SmallShift = 4;
    internal const int SmallTag = 15;
    internal const int PollMilliseconds = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}

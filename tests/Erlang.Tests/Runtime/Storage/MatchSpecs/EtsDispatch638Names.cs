using System.Reflection;

namespace Erlang.Tests;

internal static class EtsDispatch638Names
{
    internal const string CompilerType = "Erlang.MatchSavedCompiler634";
    internal const string PayloadType = "Erlang.EtsCompiledProgram419";
    internal const string Compile = "TryCompile634";
    internal const string Publish = "PublishMatchSpec419";
    internal const string Close = "Close634";
    internal const string Freed = "PhysicalFreed634";
    internal const string Failure = "Real compiled dispatch638 reader/continuation ownership failed.";
    internal const string RunCase = "runtime/dispatch638/registered-run-budget-reader-close";
    internal const string HashCase = "runtime/dispatch638/registered-hash-cursor-budget-reader-close";
    internal const string TreeCase = "runtime/dispatch638/registered-tree-cursor-budget-reader-close";
    internal const string RunCancelCase = "runtime/dispatch638/registered-run-vm-close-cancellation";
    internal const string TreeCancelCase = "runtime/dispatch638/registered-tree-cursor-vm-close-cancellation";
    internal const string LegacyCase = "runtime/dispatch638/default-public-compile-preserves-map-and-empty-tuple";
    internal const string ForeignCase = "runtime/dispatch638/foreign-native-payload-publication-refused";
    internal const int Value = 123;
    internal const int RunRows = 4001;
    internal const int CursorRows = 1001;
    internal const int PollMilliseconds = 1;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
}

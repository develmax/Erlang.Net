namespace Erlang.Tests;

internal static class ReceiveControl483Names
{
    internal const string SubjectTest = "compiler/receive483/uninitialized-binary-subject-refused";
    internal const string ArityTest = "compiler/receive483/uninitialized-function-arity-refused";
    internal const string KindTest = "compiler/receive483/unsupported-function-arity-role-refused";
    internal const string BranchTest = "compiler/receive483/foreign-receive-branch-refused";
    internal const string FrameDiagnostic = "The BEAM frame or Y-register ownership is unsupported";
    internal const string MatcherDiagnostic = "Unsupported decoded BEAM receive matcher";
    internal const string RoleDiagnostic = "Invalid operand role in bounded BEAM linker";
    internal const byte Label = 1;
    internal const byte Allocate = 12;
    internal const byte Deallocate = 18;
    internal const byte Return = 19;
    internal const byte Remove = 21;
    internal const byte Scan = 23;
    internal const byte Reject = 24;
    internal const byte Wait = 26;
    internal const byte TimeoutOpcode = 22;
    internal const byte Move = 64;
    internal const byte Binary = 53;
    internal const byte Function = 115;
}

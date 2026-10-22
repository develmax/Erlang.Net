namespace Erlang.Tests;

internal static class BeamClosureProtocolNames
{
    internal const string Prefix = "runtime/beam/image388-cfg/";
    internal const string AllRaise = Prefix + "all-raise-info-default";
    internal const string OneReturns = Prefix + "one-return-refused";
    internal const string DefaultReturns = Prefix + "default-return-refused";
    internal const string EntryCycle = Prefix + "entry-label-cycle-refused";
    internal const string LocalCycle = Prefix + "local-recursion-refused";
    internal const string CaughtReturn = Prefix + "callee-catches-and-returns-refused";
    internal const string SharedDag = Prefix + "shared-raising-tail-dag";
    internal const string YCommit = Prefix + "ordinary-y-unconditional-commit";
    internal const string YReject = Prefix + "y-write-before-rejection-refused";
    internal const string HandlerOverwrite = Prefix + "committed-handler-overwrite-refused";
    internal const string Failure = "Bounded no-return CFG or committed receive Y invariant failed";
    internal const string ErrorImport = "error";
    internal const int RunInfo = 1;
    internal const int RunEntry = 2;
    internal const int RunHandler = 3;
    internal const int MiddleInfo = 4;
    internal const int MiddleEntry = 5;
    internal const int FirstArm = 6;
    internal const int SecondArm = 7;
    internal const int SharedArm = 8;
    internal const int FunctionCount = 2;
    internal const int LabelCount = 9;
    internal const int RunAtom = 2;
    internal const int MiddleAtom = 3;
    internal const int RetainedAtom = 7;
    internal const int ReturnedAtom = 8;
    internal const byte Call = 4;
    internal const byte CallOnly = 6;
    internal const byte ExternalCall = 7;
    internal const byte Select = 59;
    internal const byte ExternalTail = 78;
}

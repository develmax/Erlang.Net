namespace Erlang.Tests;

internal static class BeamWritableReceive411Names
{
    internal const byte Label = 1, Allocate = 12, Deallocate = 18, Return = 19;
    internal const byte Remove = 21, Loop = 23, LoopEnd = 24, Wait = 25, EqualOpcode = 43, Move = 64, Initialize = 133;
    internal const string AfterTest = "runtime/beam/writable-receive411/initialize-after-consume";
    internal const string BeforeTest = "runtime/beam/writable-receive411/initialize-before-scan-retained-alias";
    internal const string RefuseTest = "runtime/beam/writable-receive411/matcher-init-and-opaque-remain-refused";
    internal const string InitializeMethod = "InitializeWritable";
    internal const string CloneMethod = "Clone";
}

namespace Erlang;

internal static class RuntimeWorkerCreationNames
{
    internal const string Normal = "normal";
    internal const string DirtyCpu = "dirty-cpu";
    internal const string DirtyIo = "dirty-io";
    internal const string BeforeDispatcher = "before-dispatcher";
    internal const string BeforeAtoms = "before-atoms";
    internal const string AfterStreams = "after-streams";
    internal const string BeforeInitializer = "before-initializer";
    internal const string BeforeClrRegistration = "before-clr-registration";
}

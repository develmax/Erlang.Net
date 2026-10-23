namespace Erlang;

internal static partial class BeamRegisterInterpreter
{
    // This code owner executes decoded instructions through its managed register
    // dispatch. It owns no ERTS threaded process_main/opcode native address
    // range. CLR JIT machine code is a different resource and is not measured.
    // A future native-threaded implementation must provide its own profile;
    // NativeAOT/unknown method bodies are refused, not relabeled as this one.
    private static readonly Lazy<DebugCodeProfile427?> debugCode427 = new(() =>
        DebugCodeProfile427.CaptureManagedRegisterOwner(typeof(BeamRegisterInterpreter), nameof(ExecuteCore), typeof(BeamRegisterState)));

    internal static DebugCodeProfile427? ManagedDebugCodeProfile427 => debugCode427.Value;
}

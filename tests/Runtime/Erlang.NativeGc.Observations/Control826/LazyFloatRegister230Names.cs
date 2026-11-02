using System.Reflection;

namespace Erlang.Tests;

internal static class LazyFloatRegister230Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string StateType = "Erlang.BeamRegisterState";
    internal const string OperandType = "Erlang.BeamOperand";
    internal const string FloatKind = "float-register";
    internal const string Floating = "floating";
    internal const string Read = "ReadFloat";
    internal const string Write = "WriteFloat";
    internal const string Clear = "ClearFloats";
    internal const string CloneMethod = "Clone";
    internal const string Heap = "TermHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string RootFrames = "RootFrames";
    internal const string FrameError = "The BEAM frame or Y-register ownership is unsupported";
    internal const string BadArithmetic = "badarith";
    internal const string Failure = "Lazy floating registers or their original error/clone semantics differ.";
    internal const int RegisterCount = 1024;
    internal const int LastRegister = RegisterCount - 1;
    internal const int OverflowShift = 64;
}

using System.Runtime.InteropServices;

namespace Erlang;

// x64 release, DMC_DEBUG/DEBUG off; erl_db_util.h match_prog text begins at56.
[StructLayout(LayoutKind.Sequential)]
internal struct MatchProgramHeader600
{
    internal nint TermSave;
    internal int Bindings;
    internal nint SavedProgramBuffer;
    internal ulong SavedProgram;
    internal nuint HeapSize;
    internal nuint StackOffset;
    internal nint TraceSession;
}

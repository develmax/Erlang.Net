using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal struct PrimFilePosixInfo
{
    internal long Type;
    internal long Access;
    internal long Size;
    internal long ATime;
    internal long MTime;
    internal long CTime;
    internal long Mode;
    internal long Links;
    internal long Dev;
    internal long RDev;
    internal long Inode;
    internal long Uid;
    internal long Gid;
}

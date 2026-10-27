using System.Runtime.InteropServices;

namespace Erlang;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NifNativeBridgeTable
{
    internal uint Version;
    internal uint Size;
    internal delegate* unmanaged[Cdecl]<nint, nint> PrivateData;
    internal delegate* unmanaged[Cdecl]<nuint, nint> Allocate;
    internal delegate* unmanaged[Cdecl]<nint, void> Free;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int*, int> GetInt;
    internal delegate* unmanaged[Cdecl]<nint, nuint> BadArgument;
    internal delegate* unmanaged[Cdecl]<nint, int, nuint> MakeInt;
    internal delegate* unmanaged[Cdecl]<nint, byte*, int, nuint> MakeString;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsException;
    internal delegate* unmanaged[Cdecl]<nint, nuint*, int> HasPendingException;
    internal delegate* unmanaged[Cdecl]<nint, nuint, nuint> RaiseException;
    internal delegate* unmanaged[Cdecl]<nint, uint, nint, nuint> MakeTuple;
    internal delegate* unmanaged[Cdecl]<nint, uint, nint, nuint> MakeList;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsAtom;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsBinary;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsReference;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsFunction;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsPid;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsPort;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsEmptyList;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsList;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsTuple;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsNumber;
    internal delegate* unmanaged[Cdecl]<nint, nuint, int> IsMap;
    internal delegate* unmanaged[Cdecl]<nint, nuint, nuint*, nuint*, int> GetListCell;
    internal delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint> MakeTupleFromArray;
    internal delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint> MakeListFromArray;
    internal delegate* unmanaged[Cdecl]<nint, nuint, ulong*, int> GetUnsignedLong;
    internal delegate* unmanaged[Cdecl]<nint, nuint, double*, int> GetDouble;
    internal delegate* unmanaged[Cdecl]<nint, nuint, uint*, int> GetUnsignedInt;
    internal delegate* unmanaged[Cdecl]<nint, nuint, long*, int> GetLong;
    internal delegate* unmanaged[Cdecl]<nint, nuint, long*, int> GetInt64;
    internal delegate* unmanaged[Cdecl]<nint, nuint, ulong*, int> GetUInt64;
    internal delegate* unmanaged[Cdecl]<nint, ulong, nuint> MakeUnsignedLong;
    internal delegate* unmanaged[Cdecl]<nint, double, nuint> MakeDouble;
    internal delegate* unmanaged[Cdecl]<nint, uint, nuint> MakeUnsignedInt;
    internal delegate* unmanaged[Cdecl]<nint, long, nuint> MakeLong;
    internal delegate* unmanaged[Cdecl]<nint, long, nuint> MakeInt64;
    internal delegate* unmanaged[Cdecl]<nint, ulong, nuint> MakeUInt64;
    internal delegate* unmanaged[Cdecl]<nint, nuint, nuint*, int> GetMapSize;
    internal delegate* unmanaged[Cdecl]<nint, nuint> MakeReference;
    internal delegate* unmanaged[Cdecl]<nint> AllocateEnvironment;
    internal delegate* unmanaged[Cdecl]<nint, void> FreeEnvironment;
    internal delegate* unmanaged[Cdecl]<nint, void> ClearEnvironment;
    internal delegate* unmanaged[Cdecl]<nint, nuint, nuint> CopyTerm;
    internal delegate* unmanaged[Cdecl]<int> ThreadType521;
}

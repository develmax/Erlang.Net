using System.Runtime.InteropServices;

namespace Erlang;

internal sealed unsafe class NifNativeWindowsCallbacks : IDisposable
{
    private nint pointer;
    private NifNativeEnvironmentAllocator? environmentAllocator;
    internal nint Pointer => pointer;

    internal NifNativeWindowsCallbacks() : this(false)
    {
    }

    internal NifNativeWindowsCallbacks(bool reallocationEnabled)
    {
        if (IntPtr.Size != NifNativeAbi.PointerBytes)
            throw new PlatformNotSupportedException(NifNativeDiagnostics.CallbackPlatform);
        pointer = (nint)NativeMemory.AllocZeroed((nuint)((NifNativeWindowsCallbackLayout.FunctionCount + NifNativeWindowsCallbackLayout.TailPointers) * IntPtr.Size));
        if (pointer == 0)
            throw new OutOfMemoryException();
        var slots = (nint*)pointer;
        slots[NifNativeWindowsCallbackLayout.MakeNewMap] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint>)&NifNativeMapCreation501Callbacks.Make;
        slots[NifNativeWindowsCallbackLayout.GetMapValue] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)&NifNativeMapValue503Callbacks.Get;
        slots[NifNativeWindowsCallbackLayout.MakeMapPut] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint, nuint*, int>)&NifNativeMapMutation504Callbacks.Put;
        slots[NifNativeWindowsCallbackLayout.MakeMapUpdate] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint, nuint*, int>)&NifNativeMapMutation504Callbacks.Update;
        slots[NifNativeWindowsCallbackLayout.MakeMapRemove] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint*, int>)&NifNativeMapMutation504Callbacks.Remove;
        slots[NifNativeWindowsCallbackLayout.MakeMapFromArrays] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint*, nuint*, nuint, nuint*, int>)&NifNativeMapArrays504Callbacks.Create;
        slots[NifNativeWindowsCallbackLayout.CreateMapIterator] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, NifNativeMapIterator504*, int, int>)&NifNativeMapIterator504Callbacks.Create;
        slots[NifNativeWindowsCallbackLayout.DestroyMapIterator] = (nint)(delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, void>)&NifNativeMapIterator504Callbacks.Destroy;
        slots[NifNativeWindowsCallbackLayout.IsMapIteratorHead] = (nint)(delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, int>)&NifNativeMapIterator504Callbacks.IsHead;
        slots[NifNativeWindowsCallbackLayout.IsMapIteratorTail] = (nint)(delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, int>)&NifNativeMapIterator504Callbacks.IsTail;
        slots[NifNativeWindowsCallbackLayout.NextMapIterator] = (nint)(delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, int>)&NifNativeMapIterator504Callbacks.Next;
        slots[NifNativeWindowsCallbackLayout.PreviousMapIterator] = (nint)(delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, int>)&NifNativeMapIterator504Callbacks.Previous;
        slots[NifNativeWindowsCallbackLayout.GetMapIteratorPair] = (nint)(delegate* unmanaged[Cdecl]<nint, NifNativeMapIterator504*, nuint*, nuint*, int>)&NifNativeMapIterator504Callbacks.GetPair;
        slots[NifNativeWindowsCallbackLayout.GetMapSize] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint*, int>)&NifNativeMapSizeCallbacks.Size;
        slots[NifNativeWindowsCallbackLayout.IsIdentical] = (nint)(delegate* unmanaged[Cdecl]<nuint, nuint, int>)&NifNativeComparisonCallbacks.Identical;
        slots[NifNativeWindowsCallbackLayout.Compare] = (nint)(delegate* unmanaged[Cdecl]<nuint, nuint, int>)&NifNativeComparisonCallbacks.Compare;
        slots[NifNativeWindowsCallbackLayout.MakeReference] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint>)&NifNativeReferenceCallbacks.Make;
        slots[NifNativeWindowsCallbackLayout.InspectBinary] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, NifNativeBinaryDescriptor*, int>)&NifNativeBinaryCallbacks.Inspect;
        slots[NifNativeWindowsCallbackLayout.AllocateBinary] = (nint)(delegate* unmanaged[Cdecl]<nuint, NifNativeBinaryDescriptor*, int>)&NifNativeBinaryCallbacks.Allocate;
        slots[NifNativeWindowsCallbackLayout.ReallocateBinary] = (nint)(delegate* unmanaged[Cdecl]<NifNativeBinaryDescriptor*, nuint, int>)&NifNativeBinaryCallbacks.Reallocate;
        slots[NifNativeWindowsCallbackLayout.ReleaseBinary] = (nint)(delegate* unmanaged[Cdecl]<NifNativeBinaryDescriptor*, void>)&NifNativeBinaryCallbacks.Release;
        slots[NifNativeWindowsCallbackLayout.MakeBinary] = (nint)(delegate* unmanaged[Cdecl]<nint, NifNativeBinaryDescriptor*, nuint>)&NifNativeBinaryCallbacks.Make;
        slots[NifNativeWindowsCallbackLayout.InspectIolistAsBinary] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, NifNativeBinaryDescriptor*, int>)&NifNativeBinaryCallbacks.InspectIolist;
        slots[NifNativeWindowsCallbackLayout.MakeSubBinary] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint, nuint, nuint>)&NifNativeBinaryCallbacks.MakeSub;
        slots[NifNativeWindowsCallbackLayout.MakeNewBinary] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint*, byte*>)&NifNativeBinaryCallbacks.MakeNew;
        slots[NifNativeWindowsCallbackLayout.GetTuple] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int*, nuint**, int>)&NifNativeSequenceCallbacks.GetTuple;
        slots[NifNativeWindowsCallbackLayout.MakeAtom] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nuint>)&NifNativeAtomCallbacks.Make;
        slots[NifNativeWindowsCallbackLayout.MakeExistingAtom] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nuint*, int, int>)&NifNativeAtomCallbacks.Existing;
        slots[NifNativeWindowsCallbackLayout.GetString] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)&NifNativeStringCallbacks.Get;
        slots[NifNativeWindowsCallbackLayout.GetAtom] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, byte*, uint, int, int>)&NifNativeAtomCallbacks.Get;
        slots[NifNativeWindowsCallbackLayout.GetAtomLength] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, uint*, int, int>)&NifNativeAtomCallbacks.Length;
        slots[NifNativeWindowsCallbackLayout.GetListLength] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, uint*, int>)&NifNativeSequenceCallbacks.GetListLength;
        slots[NifNativeWindowsCallbackLayout.MakeAtomLength] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nuint, nuint>)&NifNativeAtomCallbacks.MakeLength;
        slots[NifNativeWindowsCallbackLayout.MakeExistingAtomLength] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nuint, nuint*, int, int>)&NifNativeAtomCallbacks.ExistingLength;
        slots[NifNativeWindowsCallbackLayout.MakeStringLength] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, nuint, int, nuint>)&NifNativeStringCallbacks.MakeLength;
        slots[NifNativeWindowsCallbackLayout.GetUnsignedLong] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, uint*, int>)&NifNativeNumericCallbacks.GetUnsignedLong;
        slots[NifNativeWindowsCallbackLayout.GetDouble] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, double*, int>)&NifNativeNumericCallbacks.GetDouble;
        slots[NifNativeWindowsCallbackLayout.MakeUnsignedLong] = (nint)(delegate* unmanaged[Cdecl]<nint, uint, nuint>)&NifNativeNumericCallbacks.MakeUnsignedLong;
        slots[NifNativeWindowsCallbackLayout.MakeDouble] = (nint)(delegate* unmanaged[Cdecl]<nint, double, nuint>)&NifNativeNumericCallbacks.MakeDouble;
        slots[NifNativeWindowsCallbackLayout.GetUnsignedInt] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, uint*, int>)&NifNativeNumericCallbacks.GetUnsignedInt;
        slots[NifNativeWindowsCallbackLayout.GetLong] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)&NifNativeNumericCallbacks.GetLong;
        slots[NifNativeWindowsCallbackLayout.MakeUnsignedInt] = (nint)(delegate* unmanaged[Cdecl]<nint, uint, nuint>)&NifNativeNumericCallbacks.MakeUnsignedInt;
        slots[NifNativeWindowsCallbackLayout.MakeLong] = (nint)(delegate* unmanaged[Cdecl]<nint, int, nuint>)&NifNativeNumericCallbacks.MakeLong;
        slots[NifNativeWindowsCallbackLayout.GetInt64] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, long*, int>)&NifNativeNumericCallbacks.GetInt64;
        slots[NifNativeWindowsCallbackLayout.GetUInt64] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, ulong*, int>)&NifNativeNumericCallbacks.GetUInt64;
        slots[NifNativeWindowsCallbackLayout.MakeInt64] = (nint)(delegate* unmanaged[Cdecl]<nint, long, nuint>)&NifNativeNumericCallbacks.MakeInt64;
        slots[NifNativeWindowsCallbackLayout.MakeUInt64] = (nint)(delegate* unmanaged[Cdecl]<nint, ulong, nuint>)&NifNativeNumericCallbacks.MakeUInt64;

        slots[NifNativeWindowsCallbackLayout.IsAtom] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsAtom;
        slots[NifNativeWindowsCallbackLayout.IsBinary] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsBinary;
        slots[NifNativeWindowsCallbackLayout.IsReference] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsReference;
        slots[NifNativeWindowsCallbackLayout.IsFunction] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsFunction;
        slots[NifNativeWindowsCallbackLayout.IsPid] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsPid;
        slots[NifNativeWindowsCallbackLayout.IsPort] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsPort;
        slots[NifNativeWindowsCallbackLayout.IsEmptyList] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsEmptyList;
        slots[NifNativeWindowsCallbackLayout.IsList] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsList;
        slots[NifNativeWindowsCallbackLayout.IsTuple] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsTuple;
        slots[NifNativeWindowsCallbackLayout.IsNumber] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsNumber;
        slots[NifNativeWindowsCallbackLayout.IsMap] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativeTypeCallbacks.IsMap;
        slots[NifNativeWindowsCallbackLayout.GetListCell] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint*, nuint*, int>)&NifNativeListCellCallbacks.Get;
        if (reallocationEnabled)
            slots[NifNativeWindowsCallbackLayout.Reallocate] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint>)&NifNativePrimitiveCallbacks.Reallocate;
        slots[NifNativeWindowsCallbackLayout.MakeTupleFromArray] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)&NifNativeArrayCallbacks.MakeTuple;
        slots[NifNativeWindowsCallbackLayout.MakeListFromArray] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint*, uint, nuint>)&NifNativeArrayCallbacks.MakeList;
        slots[NifNativeWindowsCallbackLayout.PrivateData] = (nint)(delegate* unmanaged[Cdecl]<nint, nint>)&NifNativePrimitiveCallbacks.PrivateData;
        slots[NifNativeWindowsCallbackLayout.Allocate] = (nint)(delegate* unmanaged[Cdecl]<nuint, nint>)&NifNativePrimitiveCallbacks.Allocate;
        slots[NifNativeWindowsCallbackLayout.Free] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativePrimitiveCallbacks.Free;
        slots[NifNativeWindowsCallbackLayout.GetInt] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)&NifNativePrimitiveCallbacks.GetInt;
        slots[NifNativeWindowsCallbackLayout.BadArgument] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint>)&NifNativePrimitiveCallbacks.BadArgument;
        slots[NifNativeWindowsCallbackLayout.MakeInt] = (nint)(delegate* unmanaged[Cdecl]<nint, int, nuint>)&NifNativePrimitiveCallbacks.MakeInt;
        slots[NifNativeWindowsCallbackLayout.IsException] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, int>)&NifNativePrimitiveCallbacks.IsException;
        slots[NifNativeWindowsCallbackLayout.HasPendingException] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint*, int>)&NifNativePrimitiveCallbacks.HasPendingException;
        slots[NifNativeWindowsCallbackLayout.RaiseException] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint>)&NifNativePrimitiveCallbacks.RaiseException;
        slots[NifNativeWindowsCallbackLayout.MakeString] = (nint)(delegate* unmanaged[Cdecl]<nint, byte*, int, nuint>)&NifNativePrimitiveCallbacks.MakeString;
    }

    internal void BindIndependentEnvironments(ProcessRuntime runtime, ModuleCodeImage image)
    {
        environmentAllocator = () =>
        {
            try
            {
                return new NifNativeCallEnvironment(runtime, image).Pointer;
            }
            catch (Exception failure)
            {
                NifNativeCallbackScope.Fail(failure);

                return 0;
            }
        };
        var slots = (nint*)pointer;
        slots[NifNativeEnvironmentLayout.Allocate] = Marshal.GetFunctionPointerForDelegate(environmentAllocator);
        slots[NifNativeEnvironmentLayout.Free] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativeEnvironmentCallbacks.Free;
        slots[NifNativeEnvironmentLayout.Clear] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&NifNativeEnvironmentCallbacks.Clear;
        slots[NifNativeEnvironmentLayout.Copy] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nuint>)&NifNativeEnvironmentCallbacks.Copy;
    }

    public void Dispose()
    {
        nint previous = Interlocked.Exchange(ref pointer, 0);
        NativeMemory.Free((void*)previous);
    }
}

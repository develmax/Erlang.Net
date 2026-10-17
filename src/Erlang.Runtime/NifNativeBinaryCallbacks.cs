using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeBinaryCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Inspect(nint env, nuint value, NifNativeBinaryDescriptor* output)
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(value, env);

            return InspectCore(owner, owner.GetValue(value), output);
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static int InspectCore(NifNativeCallEnvironment owner, NifNativeTermValue value, NifNativeBinaryDescriptor* output)
    {
        if (value.Pending is NifNativeBinaryNode pending)
        {
            RequireDescriptor(output);
            owner.RetainBinaryStorage(pending.Storage);
            Write(
                output,
                pending.Length,
                pending.Data,
                0
            );

            return 1;
        }
        if (value.StaticTerm is not BitString { IsBinary: true } binary)
            return 0;
        RequireDescriptor(output);
        var storage = owner.InspectBinaryStorage(binary);
        Write(
            output,
            storage.Size,
            storage.Pointer,
            0
        );

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int InspectIolist(nint env, nuint value, NifNativeBinaryDescriptor* output)
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(value, env);
            var input = owner.GetValue(value);
            if (input.Pending is NifNativeBinaryNode || input.StaticTerm is BitString)
                return InspectCore(owner, input, output);
            if (input.IsNil)
            {
                RequireDescriptor(output);
                Write(
                    output,
                    0,
                    (byte*)&output->Data,
                    0
                );

                return 1;
            }
            var bytes = new ArrayBufferWriter<byte>();
            if (!FlattenIolist(NifNativeTermGraph.Resolve(input), bytes))
                return 0;
            RequireDescriptor(output);
            var storage = owner.CreateBinaryStorage((nuint)bytes.WrittenCount);
            bytes.WrittenSpan.CopyTo(new Span<byte>(storage.Pointer, bytes.WrittenCount));
            Write(
                output,
                storage.Size,
                storage.Pointer,
                0
            );

            return 1;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static bool FlattenIolist(Term input, ArrayBufferWriter<byte> output)
    {
        var stack = new Stack<(Term Term, bool ByteAllowed)>();
        stack.Push((input, false));
        while (stack.TryPop(out var entry))
        {
            switch (entry.Term)
            {
                case Nil:
                    break;
                case Cons cell:
                    stack.Push((cell.Tail, false));
                    stack.Push((cell.Head, true));
                    break;
                case BitString { IsBinary: true } binary:
                    output.Write(binary.ByteSpan);
                    break;
                case Integer integer when entry.ByteAllowed && integer.Value >= byte.MinValue && integer.Value <= byte.MaxValue:
                    output.GetSpan(1)[0] = (byte)integer.Value;
                    output.Advance(1);
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Allocate(nuint size, NifNativeBinaryDescriptor* output)
    {
        try
        {
            RequireDescriptor(output);
            if (!NifNativeBinaryRegistry.TryAllocate(size, out nint reference, out var storage))
                return 0;
            Write(
                output,
                size,
                storage!.Pointer,
                reference
            );

            return 1;
        }
        catch (OutOfMemoryException)
        {
            return 0;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Reallocate(NifNativeBinaryDescriptor* binary, nuint size)
    {
        try
        {
            RequireDescriptor(binary);
            if (binary->Reference != 0)
            {
                var storage = NifNativeBinaryRegistry.Get(binary);
                if (!storage.TryReallocate(size))
                    return 0;
                Write(
                    binary,
                    size,
                    storage.Pointer,
                    binary->Reference
                );

                return 1;
            }
            nuint copy = size < binary->Size ? size : binary->Size;
            if (copy != 0 && binary->Data == null)
                throw new InvalidOperationException(NifNativeBinaryNames.NullData);
            if (!NifNativeBinaryRegistry.TryAllocate(size, out nint reference, out var replacement))
                return 0;
            try
            {
                if (copy != 0)
                    NativeMemory.Copy(binary->Data, replacement!.Pointer, copy);
                Write(
                    binary,
                    size,
                    replacement!.Pointer,
                    reference
                );
            }
            catch
            {
                NifNativeBinaryRegistry.Release(reference);
                throw;
            }

            return 1;
        }
        catch (OutOfMemoryException)
        {
            return 0;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Release(NifNativeBinaryDescriptor* binary)
    {
        try
        {
            RequireDescriptor(binary);
            if (binary->Reference != 0)
                NifNativeBinaryRegistry.Release(binary->Reference);
            // Release builds of OTP leave descriptor fields untouched. They are no
            // longer an owned allocation and must not be used as one after release.
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Make(nint env, NifNativeBinaryDescriptor* binary)
    {
        try
        {
            RequireDescriptor(binary);
            var owner = NifNativeCallEnvironment.Resolve(env);
            if (binary->Reference != 0)
                _ = NifNativeBinaryRegistry.Get(binary);
            if (binary->Size > NifNativeBinaryNames.MaximumTermBytes)
                throw new NotSupportedException(NifNativeBinaryNames.TermSizeBoundary);
            if (binary->Size != 0 && binary->Data == null)
                throw new InvalidOperationException(NifNativeBinaryNames.NullData);
            // make_binary ends mutability. Keep the original allocation readable
            // through this call, while the immutable guest term owns a safe copy.
            nuint result = owner.Add(new BitString(new ReadOnlySpan<byte>(binary->Data, (int)binary->Size)));
            if (binary->Reference != 0)
                NifNativeBinaryRegistry.Transfer(binary, owner);

            return result;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static byte* MakeNew(nint env, nuint size, nuint* term)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);
            if (term == null)
                throw new InvalidOperationException(NifNativeBinaryNames.NullTermOutput);
            if (size > NifNativeBinaryNames.MaximumTermBytes)
                throw new NotSupportedException(NifNativeBinaryNames.TermSizeBoundary);
            var storage = owner.CreateBinaryStorage(size);
            nuint handle = owner.AddValue(new NifNativeTermValue(new NifNativeBinaryNode(storage, 0, size)));
            *term = handle;

            return storage.Pointer;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return null;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeSub(
        nint env,
        nuint input,
        nuint position,
        nuint size
    )
    {
        try
        {
            var source = NifNativeCallEnvironment.ResolveTermOwner(input, env).GetValue(input);
            var target = NifNativeCallEnvironment.Resolve(env);
            if (source.Pending is NifNativeBinaryNode binary)
            {
                RequireRange(position, size, binary.Length);
                // Pinned erts_build_sub_bitstring copies heap-sized slices now;
                // larger views retain the original data and observe later writes.
                if (size <= NifNativeBinaryNames.SmallSubBinaryBytes)
                    return target.Add(new BitString(new ReadOnlySpan<byte>(binary.Data + position, (int)size)));

                return target.AddValue(new NifNativeTermValue(new NifNativeBinaryNode(binary.Storage, binary.Offset + position, size)));
            }
            if (source.StaticTerm is not BitString bits)
                throw new InvalidOperationException(NifNativeBinaryNames.SubBinaryRange);
            nuint wholeBytes = (nuint)(bits.BitLength / 8);
            RequireRange(position, size, wholeBytes);

            return target.Add(new BitString(bits.ByteSpan.Slice((int)position, (int)size)));
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }

    private static void RequireRange(nuint position, nuint size, nuint wholeBytes)
    {
        if (position > wholeBytes || size > wholeBytes - position)
            throw new InvalidOperationException(NifNativeBinaryNames.SubBinaryRange);
    }

    private static void RequireDescriptor(NifNativeBinaryDescriptor* binary)
    {
        if (binary == null)
            throw new InvalidOperationException(NifNativeBinaryNames.NullDescriptor);
    }

    private static void Write(
        NifNativeBinaryDescriptor* binary,
        nuint size,
        byte* data,
        nint reference
    )
    {
        binary->Size = size;
        binary->Data = data;
        binary->Reference = reference;
    }
}

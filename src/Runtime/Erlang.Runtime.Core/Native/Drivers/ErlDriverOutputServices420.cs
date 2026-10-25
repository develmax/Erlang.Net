using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ErlDriverOutputServices420
{
    private readonly Func<nint, ErlDriverPortAdmission420> acquire;
    private readonly Func<ErlDriverPort420, ErlDriverOutput420, int> deliver;
    private readonly ErlDriverPrimitiveAccess420 memory;
    private readonly ErlDriverAbiLayout420 layout;
    private readonly ErlDriverServiceFault420 fault;

    internal ErlDriverOutputServices420(
        Func<nint, ErlDriverPortAdmission420> acquire,
        Func<ErlDriverPort420, ErlDriverOutput420, int> deliver,
        ErlDriverPrimitiveAccess420 memory,
        ErlDriverAbiLayout420 layout,
        ErlDriverServiceFault420 fault
    )
    {
        this.acquire = acquire;
        this.deliver = deliver;
        this.memory = memory;
        this.layout = layout;
        this.fault = fault;
        Callbacks = new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            [nameof(ErlDriverCallbackTypes420.driver_output)] = new ErlDriverCallbackTypes420.driver_output(Output),
            [nameof(ErlDriverCallbackTypes420.driver_output2)] = new ErlDriverCallbackTypes420.driver_output2(Output2),
            [nameof(ErlDriverCallbackTypes420.driver_output_binary)] = new ErlDriverCallbackTypes420.driver_output_binary(OutputBinary),
            [nameof(ErlDriverCallbackTypes420.driver_outputv)] = new ErlDriverCallbackTypes420.driver_outputv(OutputVector)
        };
    }

    internal IReadOnlyDictionary<string, Delegate> Callbacks { get; }

    private static byte[] Copy(nint source, nuint size)
    {
        if (source == nint.Zero && size != 0) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        var bytes = new byte[checked((int)size)];
        if (bytes.Length != 0) Marshal.Copy(
            source,
            bytes,
            0,
            bytes.Length
        );

        return bytes;
    }

    private int Emit(nint port, Func<ErlDriverOutput420> output)
    {
        try
        {
            var admission = acquire(port);
            using var lease = admission.Lease;
            if (admission.Status <= 0 || lease is null) return admission.Status;

            return deliver(lease.Port, output());
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }

    private int Output(nint port, nint bytes, nuint size) => Output2(
        port,
        nint.Zero,
        0,
        bytes,
        size
    );

    private int Output2(
        nint port,
        nint header,
        nuint headerSize,
        nint bytes,
        nuint size
    )
        => Emit(port, () => new(
            ErlDriverOutputKind420.Bytes,
            Copy(header, headerSize),
            Copy(bytes, size),
            BodyPresent: bytes != nint.Zero
        ));

    private int OutputBinary(
        nint port,
        nint header,
        nuint headerSize,
        nint binary,
        nuint offset,
        nuint size
    )
        => Emit(
            port,
            () =>
        {
            memory.ValidateSlice(binary, offset, size);
            memory.RetainBinary(binary);
            try
            {
                return new(
                    ErlDriverOutputKind420.Binary,
                    Copy(header, headerSize),
                    Copy(checked(memory.BinaryBytes(binary) + checked((nint)offset)), size)
                );
            }
            finally
            {
                memory.ReleaseBinary(binary);
            }
        }
        );

    private byte[][] VectorSegments(nint vector, nuint skip)
    {
        if (vector == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        int count = Marshal.ReadInt32(vector, layout.Field(ErlDriverMemoryNames420.IoVecCount));
        nuint size = unchecked((nuint)Marshal.ReadInt64(vector, layout.Field(ErlDriverMemoryNames420.IoVecSize)));
        nint vectors = Marshal.ReadIntPtr(vector, layout.Field(ErlDriverMemoryNames420.IoVecVectors));
        if (count < 0 || skip >= size || vectors == nint.Zero)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
        int stride = layout.Field(ErlDriverMemoryNames420.SystemVectorBytes);
        var result = new List<byte[]>(count);
        nuint seen = 0;
        bool stripping = true;
        // The pinned do/while consumes one leading empty entry even at skip=0;
        // after skip reaches zero subsequent empty entries remain observable.
        for (int index = 0; index < count; index++)
        {
            nint entry = checked(vectors + checked(index * stride));
            nuint length = unchecked((uint)Marshal.ReadInt32(entry, layout.Field(ErlDriverMemoryNames420.SystemVectorLength)));
            nint source = Marshal.ReadIntPtr(entry, layout.Field(ErlDriverMemoryNames420.SystemVectorAddress));
            seen = checked(seen + length);
            nuint drop = 0;
            if (stripping)
            {
                if (length <= skip)
                {
                    skip -= length;
                    stripping = skip != 0;
                    continue;
                }
                drop = skip;
                // The pinned callback advances the first partially skipped
                // iov in place; native code may inspect it after return.
                Marshal.WriteIntPtr(entry, layout.Field(ErlDriverMemoryNames420.SystemVectorAddress), checked(source + checked((nint)drop)));
                Marshal.WriteInt32(entry, layout.Field(ErlDriverMemoryNames420.SystemVectorLength), unchecked((int)(uint)(length - drop)));
                skip = 0;
                stripping = false;
            }
            result.Add(Copy(checked(source + checked((nint)drop)), length - drop));
        }
        if (seen != size || skip != 0 || result.Count == 0)
            throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);

        return result.ToArray();
    }

    private int OutputVector(
        nint port,
        nint header,
        nuint headerSize,
        nint vector,
        nuint skip
    )
    {
        try
        {
            if (vector == nint.Zero) throw new InvalidDataException(ErlDriverHostNames420.InvalidFrame);
            nuint size = unchecked((nuint)Marshal.ReadInt64(vector, layout.Field(ErlDriverMemoryNames420.IoVecSize)));
            // Pinned empty/skipped vector goes through ordinary output2, which
            // matters for line buffering even when a nonempty header remains.
            if (size <= skip) return Output2(
                port,
                header,
                headerSize,
                nint.Zero,
                0
            );

            return Emit(port, () => new(
                ErlDriverOutputKind420.Vector,
                Copy(header, headerSize),
                Array.Empty<byte>(),
                VectorSegments(vector, skip)
            ));
        }
        catch (Exception error)
        {
            fault.Record(error);

            return -1;
        }
    }
}

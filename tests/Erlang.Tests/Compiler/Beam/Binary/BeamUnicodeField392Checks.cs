using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamUnicodeField392Checks
{
    private const string Fields = "BeamUnicodeFields";
    private const string Get = "Get";
    private const string Encode = "Encode";
    private const string Utf16Name = "bs_get_utf16";
    private const string Utf32Name = "bs_get_utf32";
    private const byte Utf16Opcode = 140;
    private const byte Utf32Opcode = 142;
    private const int Little = 2;
    private const int Native = 16;
    private const int ConflictingEndian = 18;

    private static object X(int value) => Op(BeamBinaryHostNames.X, value);

    private static Array Operands(int flags = 0)
        => Ops(
            Op(BeamBinaryHostNames.Label, 3),
            X(0),
            Op(BeamBinaryHostNames.Unsigned, 2),
            Op(BeamBinaryHostNames.Unsigned, flags),
            X(1)
        );

    private static object State(BitString source, out object context)
    {
        object state = New(BeamBinaryHostNames.RegisterState);
        context = New(BeamBinaryHostNames.MatchContext, source);
        Call(
            state,
            BeamBinaryHostNames.WriteContext,
            X(0),
            context
        );
        Call(
            state,
            BeamBinaryHostNames.Write,
            1,
            Term.I(37)
        );

        return state;
    }

    internal static void NativeWritable492(ProcessContext context, object heap, IDisposable frame)
    {
        using var invocation = (IDisposable)Call(
            context,
            BeamUnicodeField392Names.Invocation,
            (IReadOnlyList<Term>)Array.Empty<Term>(),
            false
        );
        byte[] initial = Enumerable.Repeat((byte)0, BeamUnicodeField392Names.WriterInitialBytes).ToArray();
        initial[1] = 0x41;
        var empty = (BitString)Call(typeof(BitString), BeamUnicodeField392Names.InitializeWriter, BeamUnicodeField392Names.WriterCapacity);
        var portable = (BitString)Call(
            typeof(BitString),
            BeamUnicodeField392Names.AppendWriter,
            empty,
            new BitString(initial)
        );
        int firstSlot = (int)Call(frame, BeamUnicodeField392Names.WriterReserve);
        int nextSlot = (int)Call(frame, BeamUnicodeField392Names.WriterReserve);
        BitString? first = null, next = null;
        try
        {
            first = (BitString)Call(
                frame,
                BeamUnicodeField392Names.WriterAdopt,
                firstSlot,
                portable
            );
            object native = Property(Property(first, BeamUnicodeField392Names.WriterBacking), BeamUnicodeField392Names.WriterNative);
            Require(native.GetType().Name == BeamUnicodeField392Names.NativeWriterType);
            object binding = Property(native, BeamUnicodeField392Names.WriterBinding);
            Require((bool)Property(native, BeamUnicodeField392Names.WriterState) && binding is not null);
            AssertWriterFallback492(first, native);
            AssertScalarAt492(first, 0, 0x41);
            Require((bool)Property(native, BeamUnicodeField392Names.WriterState));
            byte[] complete = [.. initial, 0, 0x42];
            next = (BitString)Call(
                heap,
                BeamUnicodeField392Names.NativeAppendWriter,
                invocation,
                first,
                complete,
                complete.Length * 8,
                context.Cancellation
            );
            if (next is null) throw new InvalidOperationException();
            _ = Call(
                frame,
                BeamUnicodeField392Names.WriterSetLocal,
                nextSlot,
                next
            );
            Require(ReferenceEquals(native, Property(Property(next, BeamUnicodeField392Names.WriterBacking), BeamUnicodeField392Names.WriterNative)));
            Require(ReferenceEquals(binding, Property(native, BeamUnicodeField392Names.WriterBinding)));
            Require(first.BitLength == initial.Length * 8 && next.BitLength == complete.Length * 8);
            AssertWriterFallback492(next, native);
            AssertScalarAt492(next, initial.Length * 8, 0x42);
            AssertScalarAt492(first, 0, 0x41);
            Require((bool)Property(native, BeamUnicodeField392Names.WriterState));
        }
        finally
        {
            _ = Call(frame, BeamUnicodeField392Names.WriterClear, nextSlot);
            _ = Call(frame, BeamUnicodeField392Names.WriterClear, firstSlot);
            invocation.Dispose();
        }
        _ = Call(heap, BeamUnicodeField392Names.WriterCollect);
        Refuses<ObjectDisposedException>(() => AssertScalarAt492(first!, 0, 0x41));
        Refuses<ObjectDisposedException>(() => AssertScalarAt492(next!, 0, 0x41));
    }

    private static void AssertWriterFallback492(BitString source, object native)
    {
        object storage = Property(source, BeamUnicodeField392Names.WriterStorage);
        object cells = Property(Property(storage, BeamUnicodeField392Names.WriterSlab), BeamUnicodeField392Names.WriterCells);
        int offset = (int)Property(storage, BeamUnicodeField392Names.WriterOffset);
        nuint flags = (nuint)Call(cells, BeamUnicodeField392Names.ReadNumeric, offset + BeamUnicodeField392Names.WriterBaseWord);
        Require((flags & BeamUnicodeField392Names.WriterFlagMask) == BeamUnicodeField392Names.CurrentWriter);
        Require((bool)Property(native, BeamUnicodeField392Names.WriterState));
        System.Reflection.MethodInfo stable = Type(Fields).GetMethod(
            BeamUnicodeField392Names.StableWord,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        )!;
        object?[] arguments = [source, 0, 2, false, uint.MaxValue];
        Require(!(bool)stable.Invoke(null, arguments)! && (uint)arguments[4]! == 0);
    }

    private static void AssertScalarAt492(BitString source, int position, int scalar)
    {
        object state = State(source, out object context);
        SetPosition(context, position);
        Require((bool)Call(
            Type(Fields),
            Get,
            state,
            Operands(),
            false
        ));
        Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == scalar);
        Require((int)Property(context, BeamBinaryHostNames.Position) == position + 16);
    }

    internal static void CompareSourceReader492(BitString actual, BitString expected)
    {
        foreach (bool utf32 in new[] { false, true })
            foreach (bool little in new[] { false, true })
                foreach (int position in new[] { 0, 1, 3, 8 })
                {
                    object state = State(actual, out object context);
                    object reference = State(expected, out object referenceContext);
                    SetPosition(context, position);
                    SetPosition(referenceContext, position);
                    bool result = (bool)Call(
                        Type(Fields),
                        Get,
                        state,
                        Operands(little ? Little : 0),
                        utf32
                    );
                    bool referenceResult = (bool)Call(
                        Type(Fields),
                        Get,
                        reference,
                        Operands(little ? Little : 0),
                        utf32
                    );
                    Require(result == referenceResult);
                    Require((int)Property(context, BeamBinaryHostNames.Position) == (int)Property(referenceContext, BeamBinaryHostNames.Position));
                    Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == ((Integer)Call(reference, BeamBinaryHostNames.Read, 1)).Value);
                }
    }

    private static void CheckBoundedWord492()
    {
        byte[] raw = [0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0];
        System.Reflection.MethodInfo reader = typeof(BitString).GetMethod(
            BeamUnicodeField392Names.BoundedWord,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        )!;
        for (int offset = 0; offset < 8; offset++)
            for (int position = 0; position < 8; position++)
                foreach (int count in new[] { 2, 4 })
                    foreach (bool little in new[] { false, true })
                    {
                        BitString view = (BitString)Call(
                            typeof(BitStringComparison139Checks),
                            BeamUnicodeField392Names.PortableView,
                            raw,
                            offset,
                            raw.Length * 8 - offset
                        );
                        object?[] arguments = [position, count, little, 0u];
                        bool accepted = (bool)reader.Invoke(view, arguments)!;
                        Require(accepted == ((offset + position) % 8 == 0));
                        if (!accepted)
                        {
                            Require((uint)arguments[3]! == 0);
                            continue;
                        }
                        ReadOnlySpan<byte> bytes = raw.AsSpan((offset + position) / 8, count);
                        uint expected = count == 2
                            ? little ? System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(bytes)
                                : System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(bytes)
                            : little ? System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes)
                                : System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes);
                        Require((uint)arguments[3]! == expected);
                    }
        foreach ((int position, int count) in new[]
        {
            (-1, 2), (int.MaxValue, 4), (0, -1), (0, 0), (0, 1), (0, 3), (0, 5), (49, 2), (33, 4)
        })
        {
            object?[] arguments = [position, count, false, uint.MaxValue];
            Require(!(bool)reader.Invoke(new BitString(raw), arguments)!);
            Require((uint)arguments[3]! == 0);
        }
    }

    private static void CheckAllAlignments492()
    {
        foreach (int scalar in new[] { 0, 0x41, 0xd7ff, 0xe000, 0xffff, 0x10000, 0x1f600, 0x10ffff })
            foreach (bool utf32 in new[] { false, true })
                foreach (bool little in new[] { false, true })
                {
                    byte[] encoded = ((BitString)Call(
                        Type(Fields),
                        Encode,
                        Term.I(scalar),
                        utf32,
                        little
                    )).ToArray();
                    for (int prefix = 0; prefix < 8; prefix++)
                        for (int position = 0; position < 8; position++)
                        {
                            int start = prefix + position;
                            byte[] raw = new byte[(start + encoded.Length * 8 + 7) / 8];
                            for (int bit = 0; bit < encoded.Length * 8; bit++)
                                raw[(start + bit) / 8] |= (byte)(((encoded[bit / 8] >> (7 - bit % 8)) & 1) << (7 - (start + bit) % 8));
                            BitString view = (BitString)Call(
                                typeof(BitStringComparison139Checks),
                                BeamUnicodeField392Names.PortableView,
                                raw,
                                prefix,
                                position + encoded.Length * 8
                            );
                            object state = State(view, out object context);
                            SetPosition(context, position);
                            Require((bool)Call(
                                Type(Fields),
                                Get,
                                state,
                                Operands(little ? Little : 0),
                                utf32
                            ));
                            Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == scalar);
                            Require((int)Property(context, BeamBinaryHostNames.Position) == position + encoded.Length * 8);
                            // Each absolute alignment is sufficient for exhaustive truncation;
                            // relative-position combinations above retain full valid-value coverage.
                            if (position != 0) continue;
                            // Every truncated prefix must preserve both match position and destination.
                            for (int kept = 0; kept < encoded.Length * 8; kept++)
                            {
                                BitString truncated = (BitString)Call(
                                    typeof(BitStringComparison139Checks),
                                    BeamUnicodeField392Names.PortableView,
                                    raw,
                                    prefix,
                                    position + kept
                                );
                                object failed = State(truncated, out object failedContext);
                                SetPosition(failedContext, position);
                                Require(!(bool)Call(
                                    Type(Fields),
                                    Get,
                                    failed,
                                    Operands(little ? Little : 0),
                                    utf32
                                ));
                                Require((int)Property(failedContext, BeamBinaryHostNames.Position) == position);
                                Require(((Integer)Call(failed, BeamBinaryHostNames.Read, 1)).Value == 37);
                            }
                        }
                }
    }

    internal static Task RoundTrip()
    {
        CheckBoundedWord492();
        CheckAllAlignments492();
        foreach (int scalar in new[] { 0, 0x41, 0xd7ff, 0xe000, 0xffff, 0x10000, 0x1f600, 0x10ffff })
            foreach (bool utf32 in new[] { false, true })
                foreach (bool little in new[] { false, true })
                {
                    BitString encoded = (BitString)Call(
                        Type(Fields),
                        Encode,
                        Term.I(scalar),
                        utf32,
                        little
                    );
                    object state = State(encoded, out object context);
                    Require((bool)Call(
                        Type(Fields),
                        Get,
                        state,
                        Operands(little ? Little : 0),
                        utf32
                    ));
                    Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == scalar);
                    Require((int)Property(context, BeamBinaryHostNames.Position) == encoded.BitLength);
                    Refuses<InvalidDataException>(() => Call(state, BeamBinaryHostNames.Read, 0));
                }

        return Task.CompletedTask;
    }

    internal static Task IndependentBytesAndOffset()
    {
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            false,
            false
        )).Equals(new BitString([0xd8, 0x3d, 0xde, 0x00])));
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            false,
            true
        )).Equals(new BitString([0x3d, 0xd8, 0x00, 0xde])));
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            true,
            false
        )).Equals(new BitString([0x00, 0x01, 0xf6, 0x00])));
        Require(((BitString)Call(
            Type(Fields),
            Encode,
            Term.I(0x1f600),
            true,
            true
        )).Equals(new BitString([0x00, 0xf6, 0x01, 0x00])));
        byte[] source = new byte[5];
        byte[] value = [0xd8, 0x3d, 0xde, 0x00];
        for (int bit = 0; bit < 32; bit++)
            source[(bit + 3) / 8] |= (byte)(((value[bit / 8] >> (7 - bit % 8)) & 1) << (7 - (bit + 3) % 8));
        object state = State(new(source, 35), out object context);
        SetPosition(context, 3);
        Require((bool)Call(
            Type(Fields),
            Get,
            state,
            Operands(),
            false
        ));
        Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 0x1f600);
        Require((int)Property(context, BeamBinaryHostNames.Position) == 35);

        return Task.CompletedTask;
    }

    internal static Task InvalidAndFlags()
    {
        foreach ((byte[] source, bool utf32) in new (byte[], bool)[]
        {
            ([], false), ([0xd8], false), ([0xdc, 0], false), ([0xd8, 0, 0, 0x41], false),
            ([0, 0, 0], true), ([0, 0, 0xd8, 0], true), ([0, 0x11, 0, 0], true), ([0xff, 0xff, 0xff, 0xff], true)
        })
        {
            object state = State(new(source), out object context);
            Require(!(bool)Call(
                Type(Fields),
                Get,
                state,
                Operands(),
                utf32
            ));
            Require((int)Property(context, BeamBinaryHostNames.Position) == 0);
            Require(((Integer)Call(state, BeamBinaryHostNames.Read, 1)).Value == 37);
        }
        foreach (int invalid in new[] { -1, 0xd800, 0xdfff, 0x110000 })
            foreach (bool utf32 in new[] { false, true })
                Refuses<ErlangException>(() => Call(
                    Type(Fields),
                    Encode,
                    Term.I(invalid),
                    utf32,
                    false
                ));
        object code = Code(BeamBinaryHostBytes.Image());
        foreach (int flags in new[] { 0, Little, Native })
        {
            Validate(
                code,
                Utf16Name,
                Utf16Opcode,
                5,
                Operands(flags)
            );
            Validate(
                code,
                Utf32Name,
                Utf32Opcode,
                5,
                Operands(flags)
            );
        }
        foreach (int flags in new[] { 1, 3, 4, ConflictingEndian })
        {
            Refuses<NotSupportedException>(() => Validate(
                code,
                Utf16Name,
                Utf16Opcode,
                5,
                Operands(flags)
            ));
            Refuses<NotSupportedException>(() => Validate(
                code,
                Utf32Name,
                Utf32Opcode,
                5,
                Operands(flags)
            ));
        }

        return Task.CompletedTask;
    }
}

using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.BitStringComparison139Names;

namespace Erlang.Tests;

internal static class BitStringComparison139Checks
{
    private sealed record View(
        BitString Term,
        byte[] Source,
        int Offset,
        int Bits
    );

    internal static Task Boundaries()
    {
        var views = new List<View>();
        // Every short length/prefix, every possible bit alignment and three
        // independent data patterns. Internal portable views retain dirty pad
        // bits in real backing bytes instead of the public constructor's mask.
        for (int bits = 0; bits <= ShortMaximumBits; bits++)
            for (int offset = 0; offset < AlignmentCount; offset++)
                for (int pattern = 0; pattern < PatternCount; pattern++)
                {
                    byte[] data = Enumerable.Range(0, RawBackingBytes).Select(index => pattern switch
                    {
                        ZeroPattern => (byte)0,
                        OnesPattern => byte.MaxValue,
                        _ => (byte)(MixedPatternSeed ^ (index * MixedPatternStep))
                    }).ToArray();
                    views.Add(new(
                        PortableView(data, offset, bits),
                        data,
                        offset,
                        bits
                    ));
                }
        foreach (View left in views)
            foreach (View right in views)
                Check(TermOrder.Compare(left.Term, right.Term) == Reference(
                    left.Source,
                    left.Offset,
                    left.Bits,
                    right.Source,
                    right.Offset,
                    right.Bits
                ));

        byte[] source = Enumerable.Range(0, BoundaryBackingBytes).Select(index => (byte)(index * BoundaryPatternStep + BoundaryPatternSeed)).ToArray();
        foreach (int leftBits in BoundaryLengths)
            foreach (int rightBits in BoundaryLengths)
                for (int leftOffset = 0; leftOffset < AlignmentCount; leftOffset++)
                    for (int rightOffset = 0; rightOffset < AlignmentCount; rightOffset++)
                    {
                        var left = PortableView(source, leftOffset, leftBits);
                        var right = PortableView(source, rightOffset, rightBits);
                        int expected = Reference(
                            source,
                            leftOffset,
                            leftBits,
                            source,
                            rightOffset,
                            rightBits
                        );
                        Check(TermOrder.Compare(left, right) == expected && TermOrder.Compare(left, right, true) == expected);
                    }
        // Prefix length wins even when later valid bits share the last byte;
        // unused low padding cannot influence a partial common-prefix order.
        Check(TermOrder.Compare(PortableView(HighOne, AlignedOffset, OneBit), PortableView(AllHigh, AlignedOffset, TwoBits)) == Less);
        Check(TermOrder.Compare(PortableView(ThreeBitClean, AlignedOffset, ThreeBits), PortableView(ThreeBitDirty, AlignedOffset, ThreeBits)) == BitStringComparison139Names.Equal);
        byte[] equal = new byte[LargeBytes];
        byte[] later = equal.ToArray();
        later[^1] = byte.MaxValue;
        Check(TermOrder.Compare(new BitString(equal), new BitString(equal)) == BitStringComparison139Names.Equal);
        Check(TermOrder.Compare(new BitString(equal), new BitString(later)) == Less);
        Check(TermOrder.Compare(new BitString(later), new BitString(equal)) == Greater);

        return Task.CompletedTask;
    }

    internal static async Task OwnedAndModule()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var foreign = OwnedGuest506RuntimeFactory.Create();
        await runtime.InitializeModulesAsync(_ => { });
        await foreign.InitializeModulesAsync(_ => { });
        byte[] literalBytes = Enumerable.Range(0, LiteralBytes).Select(index => (byte)(index * LiteralPatternStep + LiteralPatternSeed)).ToArray();
        // Same genuine validated BEAM-literal preparation as ModuleBinary805.
        // No opaque registered host callback receives or exports the literal.
        await using PreparedBeamImage prepared = runtime.Modules.PrepareBeam(
            BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new BitString(literalBytes))));
        object code = Property(prepared, Code)!;
        var table = (IReadOnlyList<Term>)Property(Property(code, Structure)!, Literals)!;
        var literal = (BitString)table.Single();
        object storage = Property(literal, OwnedResource) ?? Property(literal, OwnedHeap)
            ?? throw new InvalidOperationException(ExpectedModule);
        object moduleHeap = Property(storage, Owner)!;
        object domain = Property(moduleHeap, Domain) ?? throw new InvalidOperationException(ExpectedModule);
        Task retirement = (Task)Property(domain, Retirement)!;
        Check(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(prepared.Name)!;
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var otherActor = new RuntimeNodeFoundationHostActor(foreign);
        IDisposable? moduleFrame = null, foreignFrame = null;
        object? moduleProcessHeap = null, foreignHeap = null;
        int moduleSlot = UnreservedSlot, foreignSlot = UnreservedSlot;
        bool codeOwnerClosed = false;
        try
        {
            await otherActor.Invoke(context =>
            {
                foreignHeap = Property(context, Heap)!;
                foreignFrame = (IDisposable)Call(foreignHeap, Enter)!;
                foreignSlot = (int)Call(foreignFrame, Reserve)!;

                return true;
            });
            await actor.Invoke(context =>
            {
                object heap = Property(context, Heap)!;
                using var frame = (IDisposable)Call(heap, Enter)!;
                int slot = (int)Call(frame, Reserve)!;
                var portable = new BitString(OwnedInput, OwnedBits);
                var owned = (BitString)Call(
                    frame,
                    Adopt,
                    slot,
                    portable
                )!;
                object actualHeapStorage = Property(owned, OwnedHeap)
                    ?? throw new InvalidOperationException(ExpectedHeap);
                Check(TermOrder.Compare(owned, portable) == BitStringComparison139Names.Equal && TermOrder.Compare(portable, owned) == BitStringComparison139Names.Equal);
                BeamUnicodeField392Checks.CompareSourceReader492(owned, portable);
                Check(TermOrder.Compare(owned, PortableBits(OwnedInput, AlignedOffset, OwnedPrefixBits)) == Greater);
                Expect<ObjectDisposedException>(() => Call(foreignHeap!, ReadHeapBits, actualHeapStorage));
                Call(frame, Clear, slot);
                Call(heap, Collect);
                Expect<ObjectDisposedException>(() => _ = TermOrder.Compare(owned, new BitString([])));
                Expect<ObjectDisposedException>(() => _ = TermOrder.Compare(new BitString([]), owned));
                Expect<ObjectDisposedException>(() => BeamUnicodeField392Checks.CompareSourceReader492(owned, portable));
                BeamUnicodeField392Checks.NativeWritable492(context, heap, frame);

                return true;
            });
            await actor.Invoke(context =>
            {
                using SourceCodeLease admitted = runtime.Modules.ReserveSourceCode(context, image);
                using IDisposable executing = admitted.Attach(prepared);
                moduleProcessHeap = Property(context, Heap)!;
                moduleFrame = (IDisposable)Call(moduleProcessHeap, Enter)!;
                moduleSlot = (int)Call(moduleFrame, Reserve)!;
                var actual = (BitString)Call(
                    moduleFrame,
                    Adopt,
                    moduleSlot,
                    literal
                )!;
                Check(ReferenceEquals(actual, literal));
                Check(ReferenceEquals(Property(Property(Property(actual, OwnedResource) ?? Property(actual, OwnedHeap)!, Owner)!, Domain), domain));
                Check(TermOrder.Compare(actual, new BitString(literalBytes)) == BitStringComparison139Names.Equal);
                Check(TermOrder.Compare(new BitString(literalBytes), actual, true) == BitStringComparison139Names.Equal);
                BeamUnicodeField392Checks.CompareSourceReader492(actual, new BitString(literalBytes));
                ExpectForeignRoot(() => Call(
                    foreignFrame!,
                    SetLocal,
                    foreignSlot,
                    actual
                ));

                return true;
            });
            Check(runtime.Modules.DeleteModule(prepared.Name).Equals(Term.A(ErlangBooleanAtoms.True)));
            await actor.InvokeAsync(async context =>
            {
                Check(await (ValueTask<bool>)Call(
                    runtime,
                    Purge,
                    context,
                    prepared.Name
                )!);
                codeOwnerClosed = true;
                Check(!retirement.IsCompleted);
                await AwaitQueuedReplyRelease(context);
                // Only the actual existing frame retains the literal after
                // real published code deletion/purge and source-lease disposal.
                Check(TermOrder.Compare(literal, new BitString(literalBytes)) == BitStringComparison139Names.Equal);
                Check(TermOrder.Compare(new BitString(literalBytes), literal) == BitStringComparison139Names.Equal);
                Call(moduleFrame!, Clear, moduleSlot);
                moduleFrame!.Dispose();
                moduleFrame = null;
                Call(
                    moduleProcessHeap!,
                    CollectGeneration,
                    true,
                    (ushort)MajorReductionCredit
                );

                return Nil.Value;
            });
            await retirement.WaitAsync(ResourceBinary577TestNames.Deadline);
            Check(retirement.IsCompleted);
            Expect<ObjectDisposedException>(() => _ = TermOrder.Compare(literal, new BitString([])));
            Expect<ObjectDisposedException>(() => _ = TermOrder.Compare(new BitString([]), literal));
            Expect<ObjectDisposedException>(() => BeamUnicodeField392Checks.CompareSourceReader492(literal, new BitString(literalBytes)));
        }
        finally
        {
            moduleFrame?.Dispose();
            foreignFrame?.Dispose();
            if (codeOwnerClosed) await retirement.WaitAsync(ResourceBinary577TestNames.Deadline);
        }
    }

    // Same actual queued-reply retirement barrier used by ModuleLiteral801.
    private static async Task AwaitQueuedReplyRelease(ProcessContext context)
    {
        object tracker = Property(context.Mailbox, MailboxRetirements)!;
        var pending = new List<Task>();
        lock (tracker.GetType().GetField(MailboxRetirementGate, Members)!.GetValue(tracker)!)
        {
            foreach (string list in new[] { ActiveRetirements, FailedRetirements })
                foreach (object work in (System.Collections.IEnumerable)tracker.GetType().GetField(list, Members)!.GetValue(tracker)!)
                    pending.Add((Task)Property(Property(work, RetirementCompletion)!, CompletionTask)!);
        }
        await Task.WhenAll(pending);
    }

    internal static BitString PortableBits(byte[] bytes, int offset, int bits)
    {
        var result = new byte[(bits + HighBit) / BitsPerByte];
        for (int index = 0; index < bits; index++)
            result[index / BitsPerByte] |= (byte)(((bytes[(offset + index) / BitsPerByte] >> (HighBit - (offset + index) % BitsPerByte)) & BitMask) << (HighBit - index % BitsPerByte));

        return new BitString(result, bits);
    }

    private static int Reference(
        byte[] left,
        int leftOffset,
        int leftBits,
        byte[] right,
        int rightOffset,
        int rightBits
    )
    {
        for (int index = 0; index < Math.Min(leftBits, rightBits); index++)
        {
            int a = (left[(leftOffset + index) / BitsPerByte] >> (HighBit - (leftOffset + index) % BitsPerByte)) & 1;
            int b = (right[(rightOffset + index) / BitsPerByte] >> (HighBit - (rightOffset + index) % BitsPerByte)) & 1;
            if (a != b) return a < b ? Less : Greater;
        }

        return leftBits < rightBits ? Less : leftBits > rightBits ? Greater : BitStringComparison139Names.Equal;
    }

    private static BitString PortableView(byte[] bytes, int offset, int bits)
    {
        Type backing = typeof(BitString).Assembly.GetType(BackingType, true)!;
        object owned = Activator.CreateInstance(
            backing,
            Members,
            null,
            [bytes.ToArray(), true],
            null
        )!;
        ConstructorInfo view = typeof(BitString).GetConstructor(
            Members,
            null,
            [backing, typeof(long), typeof(int), typeof(object), typeof(BitString)],
            null
        )!;

        return (BitString)view.Invoke([owned, (long)offset, bits, null, null]);
    }

    private static object? Property(object target, string name)
    {
        try
        {
            return target.GetType().GetProperty(name, Members)!.GetValue(target);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static object? Call(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void ExpectForeignRoot(Action body)
    {
        try
        {
            body();
        }
        catch (InvalidOperationException error) when (error.Message == ForeignRoot)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Expect<T>(Action body) where T : Exception
    {
        try
        {
            body();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(ExpectedException + typeof(T).Name);
    }

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Numerics;
using static Erlang.Tests.ModuleLiteral801Names;

namespace Erlang.Tests;

// A real validated/published BEAM image and its public source-code admission
// create the authority. Reflection observes storage and invokes the existing
// physical collector; it never constructs a domain/lease or installs an actor.
public static class ModuleLiteral801Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    public static async Task PhysicalCustody()
    {
        await using ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
        await runtime.InitializeModulesAsync(_ => { });
        Term portable = Term.Tuple(
            Term.A(LiteralAtom),
            Cons.From([Term.I(17), Term.I(23)]),
            new FloatTerm(1.25),
            new Integer(BigInteger.One << 80),
            new BitString(new byte[] { 17, 23 }),
            new MapTerm([new(Term.A(LiteralAtom), Term.I(17))])
        );
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(portable)));
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        ModuleCodeImage image = runtime.Modules.CurrentCodeImage(prepared.Name)!;
        object domain = Get(prepared, Domain);
        Term literal = ((IReadOnlyList<Term>)Get(domain, Literals))[0];
        object storage = Get(literal, Storage);
        Require(storage is not null && ReferenceEquals(Get(Get(storage, Owner), Domain), domain));
        Task retirement = (Task)Get(domain, Retirement);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        RuntimeNodeFoundationHostActor? other = null;
        IDisposable? first = null, parent = null, derived = null, foreign = null;
        Term? exported = null, localForeign = null;
        object? heap = null;
        int firstSlot = 0, parentSlot = 0;
        long baseline = 0;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                using SourceCodeLease admitted = await runtime.Modules.AcquireSourceCode(context, image);
                await context.ExecutionCheckpoint();
                using IDisposable executing = admitted.Attach(prepared);
                heap = Get(context, Heap);
                Call(
                    heap,
                    Collect,
                    true,
                    (ushort)0
                );
                baseline = ReadWords(heap);
                first = (IDisposable)Call(heap, Enter)!;
                firstSlot = (int)Call(first, Reserve)!;
                Term actual = (Term)Call(
                    first,
                    Adopt,
                    firstSlot,
                    literal
                )!;
                Require(ReferenceEquals(actual, literal));
                Require(ReadWords(heap) == baseline);
                exported = (Term)Call(
                    heap,
                    Export,
                    first,
                    literal
                )!;
                Require(exported.Equals(portable) && !ReferenceEquals(exported, literal));
                RequireDetached(exported);
                parent = (IDisposable)Call(heap, Enter)!;
                parentSlot = (int)Call(parent, Reserve)!;
                Term cell = (Term)Call(
                    parent,
                    CreateCons,
                    parentSlot,
                    literal,
                    Nil.Value
                )!;
                Require(ReferenceEquals(((Cons)cell).Head, literal));
                // Major establishes the real high-water boundary; the next
                // actual minor promotes the still-rooted Cons to OLD. The
                // arena is not a process allocation and receives no GC credit.
                Call(
                    heap,
                    Collect,
                    true,
                    (ushort)0
                );
                Call(
                    heap,
                    Collect,
                    false,
                    ushort.MaxValue
                );
                Require(ReadWords(heap) == baseline + 2);

                return Nil.Value;
            });
            runtime.Modules.DeleteModule(prepared.Name);
            await actor.InvokeAsync(async context =>
            {
                Require(await (ValueTask<bool>)Call(
                    runtime,
                    Purge,
                    context,
                    prepared.Name
                )!);
                Require(!retirement.IsCompleted);
                await AwaitQueuedReplyRelease(context);
                // After code-owner closure a real existing holder, not a new
                // image lookup, supplies this independent physical root slot.
                derived = (IDisposable)Call(heap!, Enter)!;
                Call(
                    derived,
                    SetLocal,
                    Call(derived, Reserve)!,
                    literal
                );
                Call(first!, Clear, firstSlot);
                first!.Dispose();
                first = null;
                Call(derived, Clear, 0);
                derived.Dispose();
                derived = null;
                Call(parent!, Clear, parentSlot);
                parent!.Dispose();
                parent = null;
                Call(
                    heap!,
                    Collect,
                    false,
                    ushort.MaxValue
                );
                Require(!retirement.IsCompleted);
                Require(ReadWords(heap!) == baseline + 2);
                Require(((TupleTerm)literal).Items.Count == 6);

                return Nil.Value;
            });
            // Another actual process owns no matching slot or executing image.
            // Equal runtime identity is insufficient to borrow a bare literal.
            // This command-channel helper does not pump mailbox control signals
            // while idle; it must not be a dormant purge-check participant.
            other = new RuntimeNodeFoundationHostActor(runtime);
            await other.InvokeAsync(context =>
            {
                object foreignHeap = Get(context, Heap);
                foreign = (IDisposable)Call(foreignHeap, Enter)!;
                int slot = (int)Call(foreign, Reserve)!;
                RejectForeign(() => Call(
                    foreign,
                    SetLocal,
                    slot,
                    literal
                ));
                localForeign = (Term)Call(
                    foreign,
                    Adopt,
                    slot,
                    Term.Tuple(Term.A(ForeignAtom))
                )!;

                return Task.FromResult<Term>(Nil.Value);
            });
            await using (ProcessRuntime foreignRuntime = OwnedGuest506RuntimeFactory.Create())
            {
                await foreignRuntime.InitializeModulesAsync(_ => { });
                await using var foreignActor = new RuntimeNodeFoundationHostActor(foreignRuntime);
                await foreignActor.InvokeAsync(context =>
                {
                    using var target = (IDisposable)Call(Get(context, Heap), Enter)!;
                    RejectForeign(() => Call(
                        target,
                        SetLocal,
                        Call(target, Reserve)!,
                        literal
                    ));

                    return Task.FromResult<Term>(Nil.Value);
                });
            }
            await actor.InvokeAsync(context =>
            {
                using var target = (IDisposable)Call(heap!, Enter)!;
                int slot = (int)Call(target, Reserve)!;
                RejectForeign(() => Call(
                    target,
                    SetLocal,
                    slot,
                    localForeign!
                ));
                Call(
                    heap!,
                    Collect,
                    true,
                    (ushort)0
                );
                Require(ReadWords(heap!) == baseline);

                return Task.FromResult<Term>(Nil.Value);
            });
            await retirement.WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
            Require(exported!.Equals(portable));
            RequireDetached(exported);
            bool stale = false;
            try
            {
                _ = ((TupleTerm)literal).Items[0];
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale);
        }
        finally
        {
            try
            {
                foreign?.Dispose();
                derived?.Dispose();
                parent?.Dispose();
                first?.Dispose();
                // Failed assertions still drop actual process block holders before
                // VM shutdown joins the arena; never wait on an intentionally held
                // old block in a fixture's finally path.
                if (heap is not null) Call(
                    heap,
                    Collect,
                    true,
                    (ushort)0
                );
            }
            finally
            {
                if (other is not null) await other.DisposeAsync();
            }
        }
        await TwoDomainPreferredFallback();
    }

    // Actual two independently admitted images; no fabricated arena/lease.
    public static async Task TwoDomainPreferredFallback()
    {
        await using ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
        await runtime.InitializeModulesAsync(_ => { });
        Term firstPortable = Term.Tuple(Term.A(LiteralAtom), Term.I(1));
        Term secondPortable = Term.Tuple(Term.A(LiteralAtom), Term.I(2));
        using PreparedBeamImage firstImage = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(firstPortable)));
        Require(runtime.Modules.PublishPreparedBeam(firstImage).Status == BeamPublicationStatus.Published);
        ModuleCodeImage firstCode = runtime.Modules.CurrentCodeImage(firstImage.Name)!;
        object firstDomain = Get(firstImage, Domain);
        Term firstLiteral = ((IReadOnlyList<Term>)Get(firstDomain, Literals))[0];
        Task firstRetirement = (Task)Get(firstDomain, Retirement);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        RuntimeNodeFoundationHostActor? other = null;
        IDisposable? first = null, second = null, parent = null;
        object? heap = null;
        Task? secondRetirement = null;
        Term? copied = null, certifiedParent = null, readonlyParent = null, readonlyExported = null, readonlyExpected = null;
        try
        {
            await actor.InvokeAsync(async context =>
            {
                using SourceCodeLease admitted = await runtime.Modules.AcquireSourceCode(context, firstCode);
                await context.ExecutionCheckpoint();
                using IDisposable executing = admitted.Attach(firstImage);
                heap = Get(context, Heap);
                first = (IDisposable)Call(heap, Enter)!;
                Require(ReferenceEquals(Call(
                    first,
                    Adopt,
                    Call(first, Reserve)!,
                    firstLiteral
                ), firstLiteral));

                return Nil.Value;
            });
            runtime.Modules.DeleteModule(firstImage.Name);
            await actor.InvokeAsync(async context =>
            {
                Require(await (ValueTask<bool>)Call(
                    runtime,
                    Purge,
                    context,
                    firstImage.Name
                )!);
                await AwaitQueuedReplyRelease(context);
                Require(!firstRetirement.IsCompleted);

                return Nil.Value;
            });
            using PreparedBeamImage secondImage = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(secondPortable)));
            Require(runtime.Modules.PublishPreparedBeam(secondImage).Status == BeamPublicationStatus.Published);
            ModuleCodeImage secondCode = runtime.Modules.CurrentCodeImage(secondImage.Name)!;
            object secondDomain = Get(secondImage, Domain);
            Require(!ReferenceEquals(firstDomain, secondDomain));
            Term secondLiteral = ((IReadOnlyList<Term>)Get(secondDomain, Literals))[0];
            secondRetirement = (Task)Get(secondDomain, Retirement);
            await actor.InvokeAsync(async context =>
            {
                using SourceCodeLease admitted = await runtime.Modules.AcquireSourceCode(context, secondCode);
                await context.ExecutionCheckpoint();
                using IDisposable executing = admitted.Attach(secondImage);
                second = (IDisposable)Call(heap!, Enter)!;
                Require(ReferenceEquals(Call(
                    second,
                    Adopt,
                    Call(second, Reserve)!,
                    secondLiteral
                ), secondLiteral));
                object preferred = ((System.Collections.IEnumerable)Get(first!, Authorities)).Cast<object>().Single();
                Require((bool)Call(preferred, Owns, Get(firstLiteral, Storage))!);
                Require(!(bool)Call(preferred, Owns, Get(secondLiteral, Storage))!);
                parent = (IDisposable)Call(heap!, Enter)!;
                int slot = (int)Call(parent, Reserve)!;
                // Actual H held => Capture801 cannot acquire the executing
                // second image lease. Preferred covers FIRST only; the second
                // frame must supply its independently retained fallback holder.
                lock (Get(heap!, HeapGate))
                    using ((IDisposable)Call(heap!, Admission, preferred)!)
                    {
                        TupleTerm actual = (TupleTerm)Call(
                            parent,
                            CreateTuple,
                            slot,
                            (object)new Term[] { firstLiteral, secondLiteral }
                        )!;
                        Require(ReferenceEquals(actual.Items[0], firstLiteral) && ReferenceEquals(actual.Items[1], secondLiteral));
                        certifiedParent = actual;
                        Require((bool)Get(Get(actual, Storage), LiteralOnly));
                        Term enclosing = (Term)Call(
                            parent,
                            CreateCons,
                            Call(parent, Reserve)!,
                            actual,
                            Nil.Value
                        )!;
                        Require((bool)Get(Get(enclosing, Storage), LiteralOnly));
                        Require(!(bool)Call(heap!, LiteralClassifier, Term.Tuple(firstLiteral))!);
                        Require(!(bool)Call(heap!, LiteralClassifier, new BitString(new byte[] { 1 }))!);
                        Require(!(bool)Call(heap!, LiteralClassifier, new MapTerm([]))!);
                    }
                // Exercise the actual outside-H fresh import route. Each pair
                // alternates the same TWO real literal domains; new physical
                // tuples are legitimate, unbounded metadata owner chains are not.
                int rollingSlot = (int)Call(parent, Reserve)!;
                Term previous = certifiedParent!;
                for (int index = 0; index < AlternatingCount; index++)
                {
                    Term selected = index % 2 == 0 ? firstLiteral : secondLiteral;
                    previous = (Term)Call(
                        parent,
                        Adopt,
                        rollingSlot,
                        Term.Tuple(previous, selected)
                    )!;
                    Require((bool)Get(Get(previous, Storage), LiteralOnly));
                    using var sidecar = (IDisposable)Call(heap!, RetainSidecar, previous)!;
                    Require(sidecar is not null);
                    RequireBoundedTwoDomainMetadata(sidecar!, firstDomain, secondDomain);
                    if (index == 0 || index == AlternatingCount - 1)
                        FlatLiteralCapture222Checks.Measure(heap!, previous, FlatLiteralCapture222Names.TwoDomainCase);
                }
                var readonlyBytes = new BitString(Enumerable.Repeat((byte)97, ReadonlyBytes).ToArray());
                BitString unaligned = readonlyBytes.SliceBits(ReadonlyOffset, ReadonlySliceBits);
                readonlyExpected = Term.Tuple(Term.Tuple(firstPortable, secondPortable), readonlyBytes, unaligned);
                readonlyParent = (Term)Call(
                    parent,
                    Adopt,
                    Call(parent, Reserve)!,
                    Term.Tuple(certifiedParent!, readonlyBytes, unaligned)
                )!;
                Require((bool)Get(Get(readonlyParent, Storage), ReadonlyCapture));
                Require(!(bool)Get(Get(readonlyParent, Storage), LiteralOnly));
                Require(((BitString)((TupleTerm)readonlyParent).Items[1]).BitLength == ReadonlyBytes * 8
                    && ((BitString)((TupleTerm)readonlyParent).Items[2]).BitLength == ReadonlySliceBits);
                Call(
                    heap!,
                    Collect,
                    true,
                    (ushort)0
                );
                Array binaryRows = (Array)Call(heap!, BinaryInfo822ControlNames.Capture)!;
                Require(binaryRows.Length == 2);
                foreach (object row in binaryRows)
                    Require((ulong)Get(row, BinaryInfo822ControlNames.OriginalSize) == ReadonlyBytes
                        && (long)Get(row, BinaryInfo822ControlNames.References) > 0);
                object registrySnapshot = Get(heap!, BinarySnapshot);
                Require(Convert.ToInt32(Get(registrySnapshot, DistinctBackings)) == 1
                    && Convert.ToInt64(Get(registrySnapshot, ReferenceBytes)) == ReadonlyBytes
                    && Convert.ToInt64(Get(registrySnapshot, BackingReferences)) > 0);
                TupleTerm reuseEnvelope = (TupleTerm)Call(
                    parent,
                    Adopt,
                    Call(parent, Reserve)!,
                    Term.Tuple(readonlyParent, Term.I(ReuseMarker))
                )!;
                Require(ReferenceEquals(reuseEnvelope.Items[0], readonlyParent));
                int copiedSlot = (int)Call(parent, Reserve)!;
                Call(
                    parent,
                    SetPrepared,
                    copiedSlot,
                    readonlyParent
                );
                TupleTerm actualCopy = (TupleTerm)Call(parent, ReadSlot, copiedSlot)!;
                Require(!ReferenceEquals(actualCopy, readonlyParent)
                    && !ReferenceEquals(actualCopy.Items[1], ((TupleTerm)readonlyParent).Items[1])
                    && !ReferenceEquals(actualCopy.Items[2], ((TupleTerm)readonlyParent).Items[2]));
                Require(ExternalTermFormat.Encode(actualCopy).AsSpan().SequenceEqual(ExternalTermFormat.Encode(readonlyParent)));
                Require(((Array)Call(heap!, BinaryInfo822ControlNames.Capture)!).Length == 4);
                var recordDto = new NativeRecordTerm(
                    Term.A(ReuseRecordModule),
                    Term.A(ReuseRecordName),
                    false,
                    new[] { new KeyValuePair<Atom, Term>(Term.A(ReuseRecordField), Term.I(ReuseMarker)) }
                );
                NativeRecordTerm actualRecord = (NativeRecordTerm)Call(
                    parent,
                    Adopt,
                    Call(parent, Reserve)!,
                    recordDto
                )!;
                TupleTerm recordEnvelope = (TupleTerm)Call(
                    parent,
                    Adopt,
                    Call(parent, Reserve)!,
                    Term.Tuple(actualRecord)
                )!;
                Require(ReferenceEquals(recordEnvelope.Items[0], actualRecord));
                int recordCopySlot = (int)Call(parent, Reserve)!;
                Call(
                    parent,
                    SetPrepared,
                    recordCopySlot,
                    actualRecord
                );
                Term recordCopy = (Term)Call(parent, ReadSlot, recordCopySlot)!;
                Require(!ReferenceEquals(recordCopy, actualRecord)
                    && ExternalTermFormat.Encode(recordCopy).AsSpan().SequenceEqual(ExternalTermFormat.Encode(actualRecord)));
                readonlyExported = (Term)Call(
                    heap!,
                    Export,
                    parent,
                    readonlyParent
                )!;
                RequireDetached(readonlyExported);
                Require(readonlyExported.Equals(readonlyExpected));
                copied = (Term)Call(
                    heap!,
                    Export,
                    parent,
                    Call(parent, ReadSlot, slot)!
                )!;
                RequireDetached(copied);
                Require(copied.Equals(Term.Tuple(firstPortable, secondPortable)));
                first!.Dispose();
                first = null;
                second!.Dispose();
                second = null;

                return Nil.Value;
            });
            runtime.Modules.DeleteModule(secondImage.Name);
            await actor.InvokeAsync(async context =>
            {
                Require(await (ValueTask<bool>)Call(
                    runtime,
                    Purge,
                    context,
                    secondImage.Name
                )!);
                await AwaitQueuedReplyRelease(context);
                Call(
                    heap!,
                    Collect,
                    true,
                    (ushort)0
                );
                Require(!firstRetirement.IsCompleted && !secondRetirement!.IsCompleted);
                Require(((TupleTerm)firstLiteral).Items[1].Equals(Term.I(1))
                    && ((TupleTerm)secondLiteral).Items[1].Equals(Term.I(2)));

                return Nil.Value;
            });
            other = new RuntimeNodeFoundationHostActor(runtime);
            await other.InvokeAsync(context =>
            {
                using var target = (IDisposable)Call(Get(context, Heap), Enter)!;
                int slot = (int)Call(target, Reserve)!;
                RejectForeign(() => Call(
                    target,
                    SetLocal,
                    slot,
                    firstLiteral
                ));
                RejectForeign(() => Call(
                    target,
                    SetLocal,
                    slot,
                    secondLiteral
                ));
                RejectForeign(() => Call(
                    target,
                    SetLocal,
                    slot,
                    certifiedParent!
                ));
                RejectForeign(() => Call(
                    target,
                    SetLocal,
                    slot,
                    readonlyParent!
                ));

                return Task.FromResult<Term>(Nil.Value);
            });
            await actor.InvokeAsync(context =>
            {
                parent!.Dispose();
                parent = null;
                Call(
                    heap!,
                    Collect,
                    true,
                    (ushort)0
                );
                Require(((Array)Call(heap!, BinaryInfo822ControlNames.Capture)!).Length == 0);

                return Task.FromResult<Term>(Nil.Value);
            });
            await Task.WhenAll(firstRetirement, secondRetirement!).WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
            Require(copied!.Equals(Term.Tuple(firstPortable, secondPortable)));
            RequireDetached(copied);
            Require(readonlyExported!.Equals(readonlyExpected));
            RequireDetached(readonlyExported);
        }
        finally
        {
            try
            {
                parent?.Dispose();
                second?.Dispose();
                first?.Dispose();
                if (heap is not null) Call(
                    heap,
                    Collect,
                    true,
                    (ushort)0
                );
            }
            finally
            {
                if (other is not null) await other.DisposeAsync();
            }
        }
    }

    private static void RequireBoundedTwoDomainMetadata(object sidecar, object firstDomain, object secondDomain)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var domains = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var leases = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var depths = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(object Node, bool Expanded)>();
        pending.Push((sidecar, false));
        while (pending.TryPop(out var entry))
        {
            if (entry.Expanded)
            {
                int depth = 1;
                foreach (object ancestor in (System.Collections.IEnumerable)entry.Node.GetType().GetField(ParentField, Members)!.GetValue(entry.Node)!)
                    depth = Math.Max(depth, checked(depths[ancestor] + 1));
                depths.Add(entry.Node, depth);
                continue;
            }
            if (!seen.Add(entry.Node)) continue;
            Require(Convert.ToInt32(entry.Node.GetType().GetField(ReferenceCount, Members)!.GetValue(entry.Node)) > 0);
            foreach (object lease in (System.Collections.IEnumerable)entry.Node.GetType().GetField(LeaseField, Members)!.GetValue(entry.Node)!)
            {
                Require(!(bool)Get(lease, Disposed));
                leases.Add(lease);
                domains.Add(Get(lease, LeaseDomain));
            }
            pending.Push((entry.Node, true));
            foreach (object ancestor in (System.Collections.IEnumerable)entry.Node.GetType().GetField(ParentField, Members)!.GetValue(entry.Node)!)
                pending.Push((ancestor, false));
        }
        Require(domains.Count == DomainCount && domains.Contains(firstDomain) && domains.Contains(secondDomain));
        // One aggregate, at most two mixed/terminal owners plus their oldest
        // actual ancestor. Allow the current direct-domain derived lease while
        // preserving a fixed bound independent of the64 physical constructors.
        Require(depths[sidecar] <= DomainCount + 2 && seen.Count <= DomainCount + 2
            && leases.Count <= DomainCount + 1);
    }

    private static async Task AwaitQueuedReplyRelease(ProcessContext context)
    {
        object tracker = Get(context.Mailbox, MailboxRetirements);
        var pending = new List<Task>();
        lock (tracker.GetType().GetField(MailboxRetirementGate, Members)!.GetValue(tracker)!)
        {
            foreach (string list in new[] { ActiveRetirements, FailedRetirements })
                foreach (object work in (System.Collections.IEnumerable)tracker.GetType().GetField(list, Members)!.GetValue(tracker)!)
                    pending.Add((Task)Get(Get(work, RetirementCompletion), CompletionTask));
        }
        await Task.WhenAll(pending);
    }

    private static object Get(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static long ReadWords(object heap) => Convert.ToInt64(Get(Get(heap, Words), LiveWords));

    private static void RequireDetached(Term root)
    {
        const BindingFlags staticMembers = BindingFlags.Static | BindingFlags.NonPublic;
        MethodInfo storage = typeof(ProcessRuntime).Assembly.GetType(HeapType, true)!.GetMethod(StorageMethod, staticMembers)!;
        MethodInfo children = typeof(ProcessRuntime).Assembly.GetType(NativeChildrenType, true)!.GetMethod(Children, staticMembers)!;
        var pending = new Stack<Term>();
        var seen = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out Term? current))
        {
            if (!seen.Add(current)) continue;
            Require(storage.Invoke(null, [current]) is null);
            foreach (Term child in (Term[])children.Invoke(null, [current])!) pending.Push(child);
        }
    }

    private static object? Call(object target, string name, params object[] arguments)
    {
        try
        {
            return target.GetType().GetMethods(Members).Single(method => method.Name == name
                && method.GetParameters().Length == arguments.Length).Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void RejectForeign(Action action)
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (InvalidOperationException error) when (error.Message == Foreign)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}

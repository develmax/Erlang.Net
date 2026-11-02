using System.Runtime.CompilerServices;
using static Erlang.Tests.BeamReceiveCustody557Access;
using static Erlang.Tests.BeamReceiveCustody557Names;

namespace Erlang.Tests;

internal static class BeamReceiveCustody557Checks
{
    internal static async Task CloseOrder()
    {
        foreach (bool native in new[] { false, true })
            foreach (bool y in new[] { false, true })
                foreach (bool sourceFirst in new[] { false, true })
                    await WithState(
                        native,
                        (
                            context,
                            source,
                            words,
                            registry,
                            heap
                        ) =>
                    {
                        BitString binary = Seed(
                            source,
                            y,
                            0,
                            17
                        );
                        Term original = Read(source, y, 0);
                        object child = Clone(context, source);
                        try
                        {
                            if (sourceFirst)
                            {
                                ((IDisposable)source).Dispose();
                                Collect(heap);
                                BeamReceiveCustody557Access.Check(ReferenceEquals(Read(child, y, 0), original));
                            }
                            else
                            {
                                ((IDisposable)child).Dispose();
                                Collect(heap);
                                BeamReceiveCustody557Access.Check(ReferenceEquals(Read(source, y, 0), original));
                            }
                            Bytes(binary, 17);
                        }
                        finally
                        {
                            ((IDisposable)child).Dispose();
                            ((IDisposable)source).Dispose();
                        }
                        Collect(heap);
                        Retired(binary);
                        GC.KeepAlive(original);

                        return Task.CompletedTask;
                    }
                    );
    }

    internal static async Task GenerationsAndAba()
    {
        foreach (bool native in new[] { false, true })
            foreach (bool y in new[] { false, true })
                await WithState(
                    native,
                    (
                        context,
                        source,
                        words,
                        registry,
                        heap
                    ) =>
                {
                    BitString originalBinary = Seed(
                        source,
                        y,
                        0,
                        21
                    );
                    Term original = Read(source, y, 0);
                    object first = Clone(context, source), sibling = Clone(context, source);
                    object? last = null;
                    try
                    {
                        _ = Seed(
                            source,
                            y,
                            0,
                            22
                        );
                        _ = Seed(
                            source,
                            y,
                            0,
                            23
                        );
                        _ = Seed(
                            source,
                            y,
                            0,
                            24
                        );
                        Collect(heap);
                        BeamReceiveCustody557Access.Check(ReferenceEquals(Read(first, y, 0), original));
                        Bytes(originalBinary, 21);
                        Bytes(Binary(Read(source, y, 0)), 24);
                        ((IDisposable)source).Dispose();
                        last = Clone(context, first);
                        ((IDisposable)first).Dispose();
                        ((IDisposable)sibling).Dispose();
                        Collect(heap);
                        BeamReceiveCustody557Access.Check(ReferenceEquals(Read(last, y, 0), original));
                        Bytes(originalBinary, 21);
                    }
                    finally
                    {
                        ((IDisposable)source).Dispose();
                        ((IDisposable)first).Dispose();
                        ((IDisposable)sibling).Dispose();
                        if (last is not null) ((IDisposable)last).Dispose();
                    }
                    Collect(heap);
                    Retired(originalBinary);
                    GC.KeepAlive(original);

                    return Task.CompletedTask;
                }
                );
    }

    internal static async Task CandidateAndRollback()
    {
        foreach (bool native in new[] { false, true })
            await WithState(
                native,
                (
                    context,
                    state,
                    words,
                    registry,
                    heap
                ) =>
            {
                object destination = CreateXOperand();
                Call(
                    state,
                    BeamOwnedRoots507Names.CreateTuple,
                    destination,
                    Array.Empty<Term>()
                );
                Term empty = Read(state, false, 0);
                using var loan = (IDisposable)Clone(context, state);
                Call(
                    state,
                    BeamOwnedRoots507Names.CreateTuple,
                    destination,
                    Array.Empty<Term>()
                );
                Call(
                    state,
                    BeamOwnedRoots507Names.CreateTuple,
                    destination,
                    Array.Empty<Term>()
                );
                BeamReceiveCustody557Access.Check(ReferenceEquals(empty, Read(state, false, 0)));
                object roots = Field(state, BeamConstructedCommit548TestNames.RootField);
                _ = Call(roots, BeamConstructedCommit548TestNames.CandidateMethod, 0);
                BeamReceiveCustody557Access.Throws<KeyNotFoundException>(() => Call(
                    state,
                    BeamConstructedCommit548TestNames.CommitX,
                    0,
                    empty
                ));
                Term pending = (Term)Call(
                    roots,
                    TuplePreparation,
                    0,
                    Array.Empty<Term>()
                )!;
                BeamReceiveCustody557Access.Check(ReferenceEquals(pending, empty));
                BeamReceiveCustody557Access.Throws<InvalidOperationException>(() => Call(
                    roots,
                    BeamConstructedCommit548TestNames.ConsMethod,
                    1,
                    Term.I(7),
                    Nil.Value
                ));
                Call(
                    state,
                    BeamConstructedCommit548TestNames.CommitX,
                    0,
                    pending
                );
                BeamReceiveCustody557Access.Throws<KeyNotFoundException>(() => Call(
                    state,
                    BeamConstructedCommit548TestNames.CommitX,
                    0,
                    pending
                ));
                BeamReceiveCustody557Access.Check(ReferenceEquals(empty, Read(state, false, 0)));
                Collect(heap);

                return Task.CompletedTask;
            }
            );
        await WithState(
            true,
            async (
                context,
                state,
                words,
                registry,
                heap
            ) =>
        {
            BitString binary = Seed(
                state,
                true,
                0,
                31
            );
            Term original = Read(state, true, 0);
            object child = Clone(context, state);
            try
            {
                object local = ((System.Collections.IEnumerable)Field(state, BeamConstructedCommit548TestNames.FramesField)).Cast<object>().First();
                object roots = Field(local, BeamConstructedCommit548TestNames.RootField);
                object frame = Field(roots, BeamConstructedCommit548TestNames.FrameField);
                Collect(heap);
                long before = Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References);
                Term candidate = (Term)Call(
                    roots,
                    BeamConstructedCommit548TestNames.ConsMethod,
                    0,
                    new BitString(new byte[BackingBytes]),
                    Nil.Value
                )!;
                await ((IAsyncDisposable)words!).DisposeAsync();
                BeamReceiveCustody557Access.Throws<ObjectDisposedException>(() => Call(
                    local,
                    BeamConstructedCommit548TestNames.CommitY,
                    0,
                    candidate
                ));
                BeamReceiveCustody557Access.Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == before);
                int current = (int)Call(roots, BeamConstructedCommit548TestNames.CurrentMethod, 0)!;
                BeamReceiveCustody557Access.Check(ReferenceEquals(Call(frame, LinearConstruction514Names.Read, current), original));
                ((IDisposable)state).Dispose();
                Collect(heap);
                Bytes(binary, 31);
            }
            finally
            {
                ((IDisposable)child).Dispose();
                ((IDisposable)state).Dispose();
            }
            Collect(heap);
            Retired(binary);
            GC.KeepAlive(original);
        }
        );
    }

    internal static async Task RetentionAndBaseline()
    {
        foreach (bool native in new[] { false, true })
            await WithState(
                native,
                (
                    context,
                    state,
                    words,
                    registry,
                    heap
                ) =>
            {
                BitString binary = Seed(
                    state,
                    false,
                    0,
                    41
                );
                Term original = Read(state, false, 0);
                long before = Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.Retains);
                using (var legacy = (IDisposable)Call(state, BeamOwnedRoots507Names.Clone)!)
                {
                    BeamReceiveCustody557Access.Check(ReferenceEquals(Read(legacy, false, 0), original));
                    BeamReceiveCustody557Access.Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.Retains) > before);
                }
                before = Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.Retains);
                using (var child = (IDisposable)Clone(context, state))
                {
                    BeamReceiveCustody557Access.Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.Retains) == before);
                    Collect(heap);
                    BeamReceiveCustody557Access.Check(ReferenceEquals(Read(child, false, 0), original));
                    Bytes(binary, 41);
                }
                var detached = DetachedSource(context, words);
                try
                {
                    Collect(heap);
                    BeamStoredFunction406Access.RequireCollected(detached.State);
                    BeamStoredFunction406Access.RequireCollected(detached.Unrelated);
                    Bytes(Binary(Read(detached.Child, false, 0)), 51);
                }
                finally
                {
                    ((IDisposable)detached.Child).Dispose();
                }

                return Task.CompletedTask;
            }
            );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (object Child, WeakReference<object> State, WeakReference<object> Unrelated) DetachedSource(ProcessContext context, object? words)
    {
        object source = NewState(context, words);
        object? child = null;
        try
        {
            try
            {
                _ = Seed(
                    source,
                    false,
                    0,
                    51
                );
                Write(
                    source,
                    false,
                    1,
                    Term.Tuple(Term.I(557))
                );
                object unrelated = Read(source, false, 1);
                child = Clone(context, source);
                ClearX(child, 0);

                return (child, new(source), new(unrelated));
            }
            finally
            {
                // Successful handoff occurs only after source disposal returns.
                ((IDisposable)source).Dispose();
            }
        }
        catch (Exception primary)
        {
            // A clone is still our responsibility if ClearX, weak-probe
            // construction or source disposal prevents the return handoff.
            try
            {
                ((IDisposable?)child)?.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(primary, cleanup);
            }
            throw;
        }
    }

    private static object CreateXOperand()
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(BeamOwnedRoots507Names.OperandType, true)!,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new object?[] { BeamOwnedRoots507Names.X, System.Numerics.BigInteger.Zero, null },
            null
        )!;
}

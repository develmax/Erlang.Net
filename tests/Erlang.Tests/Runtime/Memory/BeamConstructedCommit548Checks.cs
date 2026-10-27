using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.BeamConstructedCommit548TestNames;

namespace Erlang.Tests;

internal static class BeamConstructedCommit548Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task PublicationMatrix()
    {
        var failures = new List<string>();
        foreach (bool native in new[] { false, true })
            foreach (bool y in new[] { false, true })
                foreach (bool tuple in new[] { false, true })
                {
                    await using var runtime = OwnedGuest506RuntimeFactory.Create();
                    await using var actor = new RuntimeNodeFoundationHostActor(runtime);
                    await actor.InvokeAsync(async context =>
                    {
                        object? words = native ? Construct(BeamOwnedRoots507Names.WordType, Property(context, BeamOwnedRoots507Names.NativeArena)) : null;
                        object state = Construct(BeamOwnedRoots507Names.StateType, words, context);
                        try
                        {
                            object heap = Property(context, LinearConstruction514Names.Heap), registry = Property(runtime, LinearConstruction514Names.Registry);
                            var binary = new BitString(new byte[BackingBytes]);
                            Call(
                                state,
                                BeamOwnedRoots507Names.Write,
                                0,
                                Term.Tuple(binary)
                            );
                            Term child = (Term)Call(state, BeamOwnedRoots507Names.Read, 0)!;
                            if (y) Call(state, BeamOwnedRoots507Names.Allocate, 1);
                            object destination = Construct(
                                BeamOwnedRoots507Names.OperandType,
                                y ? Y : BeamOwnedRoots507Names.X,
                                new BigInteger(y ? 0 : 1),
                                null
                            );
                            object before = Property(registry, LinearConstruction514Names.Traversal);
                            if (tuple) Call(
                                state,
                                BeamOwnedRoots507Names.CreateTuple,
                                destination,
                                new Term[] { child, child }
                            );
                            else Call(
                                state,
                                BeamOwnedRoots507Names.CreateCons,
                                destination,
                                child,
                                Nil.Value
                            );
                            object after = Property(registry, LinearConstruction514Names.Traversal);
                            long retains = Number(after, LinearConstruction514Names.Retains) - Number(before, LinearConstruction514Names.Retains);
                            long visits = Number(after, LinearConstruction514Names.Visits) - Number(before, LinearConstruction514Names.Visits);
                            string diagnostic = string.Format(
                                CultureInfo.InvariantCulture,
                                Observation,
                                native,
                                y,
                                tuple,
                                retains,
                                visits
                            );
                            Console.WriteLine(diagnostic);
                            if (retains != ExpectedRetains) failures.Add(diagnostic);
                            Term constructed = (Term)Call(state, y ? BeamOwnedRoots507Names.ReadY : BeamOwnedRoots507Names.Read, y ? 0 : 1)!;
                            if (tuple)
                            {
                                var value = (TupleTerm)constructed;
                                Check(ReferenceEquals(value.Items[0], child) && ReferenceEquals(value.Items[1], child));
                            }
                            else Check(ReferenceEquals(((Cons)constructed).Head, child));
                            Call(state, BeamOwnedRoots507Names.Clear, y ? Array.Empty<int>() : new[] { 1 });
                            Call(heap, LinearConstruction514Names.Collect);
                            Check(((BitString)((TupleTerm)child).Items[0]).ToArray().Length == BackingBytes);
                            Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == 1);
                            if (y) Call(state, BeamOwnedRoots507Names.Deallocate, 1);
                            Call(state, BeamOwnedRoots507Names.Clear, Array.Empty<int>());
                            Call(heap, LinearConstruction514Names.Collect);
                            Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == 0);
                        }
                        finally
                        {
                            ((IDisposable)state).Dispose();
                            if (words is not null) await ((IAsyncDisposable)words).DisposeAsync();
                        }

                        return true;
                    });
                }
        if (failures.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
    }

    internal static async Task ClosedPhysicalStoreRollsBack()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            object words = Construct(BeamOwnedRoots507Names.WordType, Property(context, BeamOwnedRoots507Names.NativeArena));
            object state = Construct(BeamOwnedRoots507Names.StateType, words, context);
            try
            {
                Call(state, BeamOwnedRoots507Names.Allocate, 1);
                Call(
                    state,
                    BeamOwnedRoots507Names.WriteY,
                    0,
                    Term.Tuple(new BitString(new byte[BackingBytes]))
                );
                Term old = (Term)Call(state, BeamOwnedRoots507Names.ReadY, 0)!;
                object local = ((System.Collections.IEnumerable)Field(state, FramesField)).Cast<object>().First();
                object roots = Field(local, RootField), frame = Field(roots, FrameField);
                object registry = Property(runtime, LinearConstruction514Names.Registry), heap = Property(context, LinearConstruction514Names.Heap);
                Call(heap, LinearConstruction514Names.Collect);
                long beforeWords = Number(Property(heap, BeamOwnedRoots507Names.Snapshot), BeamOwnedRoots507Names.Words);
                long beforeReferences = Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References);
                Term candidate = (Term)Call(
                    roots,
                    ConsMethod,
                    0,
                    new BitString(new byte[BackingBytes]),
                    Nil.Value
                )!;
                Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == beforeReferences + 1);
                // Exact finite failure boundary: actual candidate already exists;
                // close the real504 store before its private commit step.
                await ((IAsyncDisposable)words).DisposeAsync();
                bool rejected = false;
                try
                {
                    Call(
                        local,
                        CommitY,
                        0,
                        candidate
                    );
                }
                catch (ObjectDisposedException)
                {
                    rejected = true;
                }
                Check(rejected);
                Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == beforeReferences);
                int current = (int)Call(roots, CurrentMethod, 0)!;
                Check(ReferenceEquals(Call(frame, LinearConstruction514Names.Read, current), old));
                int pending = (int)Call(roots, CandidateMethod, 0)!;
                bool cleared = false;
                try
                {
                    _ = Call(frame, LinearConstruction514Names.Read, pending);
                }
                catch (KeyNotFoundException)
                {
                    cleared = true;
                }
                Check(cleared);
                Call(heap, LinearConstruction514Names.Collect);
                Check(Number(Property(heap, BeamOwnedRoots507Names.Snapshot), BeamOwnedRoots507Names.Words) == beforeWords);
                Call(state, BeamOwnedRoots507Names.Deallocate, 1);
                Call(heap, LinearConstruction514Names.Collect);
                Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == 0);
            }
            finally
            {
                ((IDisposable)state).Dispose();
                await ((IAsyncDisposable)words).DisposeAsync();
            }

            return true;
        });
    }

    internal static async Task StaleCandidateRejected()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            using var state = (IDisposable)Construct(BeamOwnedRoots507Names.StateType, null, context);
            object destination = Construct(
                BeamOwnedRoots507Names.OperandType,
                BeamOwnedRoots507Names.X,
                new BigInteger(0),
                null
            );
            Call(
                state,
                BeamOwnedRoots507Names.CreateCons,
                destination,
                Term.I(7),
                Nil.Value
            );
            Term current = (Term)Call(state, BeamOwnedRoots507Names.Read, 0)!;
            bool rejected = false;
            try
            {
                Call(
                    state,
                    CommitX,
                    0,
                    current
                );
            }
            catch (KeyNotFoundException)
            {
                rejected = true;
            }
            Check(rejected && ReferenceEquals(Call(state, BeamOwnedRoots507Names.Read, 0), current));
            object roots = Field(state, RootField);
            _ = Call(
                roots,
                ConsMethod,
                0,
                Term.I(8),
                Nil.Value
            );
            rejected = false;
            try
            {
                Call(
                    state,
                    CommitX,
                    0,
                    Term.List(Term.I(8))
                );
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            Check(rejected && ReferenceEquals(Call(state, BeamOwnedRoots507Names.Read, 0), current));
            Call(Property(context, LinearConstruction514Names.Heap), LinearConstruction514Names.Collect);
            Check(((Integer)((Cons)current).Head).Value == 7);
            Call(state, BeamOwnedRoots507Names.Clear, Array.Empty<int>());
            Call(Property(context, LinearConstruction514Names.Heap), LinearConstruction514Names.Collect);

            return true;
        });
    }

    private static object Construct(string name, params object?[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            Members,
            null,
            arguments,
            null
        )!;

    private static object? Call(object owner, string name, params object?[] arguments)
    {
        MethodInfo method = owner.GetType().GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == arguments.Length
            && candidate.GetParameters().Select((parameter, index) => arguments[index] is null || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(match => match));
        try
        {
            return method.Invoke(owner, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static object Property(object owner, string name) => owner.GetType().GetProperty(name, Members)!.GetValue(owner)!;

    private static object Field(object owner, string name) => owner.GetType().GetField(name, Members)!.GetValue(owner)!;

    private static long Number(object owner, string name) => (long)Property(owner, name);

    private static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}

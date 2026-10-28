using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.BeamListCurrentRoot550Names;

namespace Erlang.Tests;

internal static class BeamListCurrentRoot550Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Matrix()
    {
        var failures = new List<string>();
        foreach (bool native in new[] { false, true })
            foreach (bool sourceY in new[] { false, true })
                foreach (int alias in new[] { 0, 1, 2 })
                    await WithState(
                        native,
                        async (
                            context,
                            state,
                            registry,
                            heap
                        ) =>
                    {
                        object source = Operand(sourceY, 0);
                        object first = alias == 1 ? source : Operand(true, 1);
                        object second = alias == 2 ? source : Operand(true, 2);
                        Seed(state, sourceY);
                        var cell = (Cons)Read(state, sourceY, 0);
                        Term head = cell.Head, tail = cell.Tail;
                        object before = Property(registry, LinearConstruction514Names.Traversal);
                        Call(
                            state,
                            Pair,
                            Opcode,
                            source,
                            first,
                            second,
                            cell
                        );
                        object after = Property(registry, LinearConstruction514Names.Traversal);
                        long retains = Number(after, LinearConstruction514Names.Retains) - Number(before, LinearConstruction514Names.Retains);
                        long visits = Number(after, LinearConstruction514Names.Visits) - Number(before, LinearConstruction514Names.Visits);
                        string line = string.Format(
                            CultureInfo.InvariantCulture,
                            Observation,
                            native,
                            sourceY,
                            alias,
                            retains,
                            visits
                        );
                        Console.WriteLine(line);
                        if (retains != (alias == 1 ? FallbackRetains : OrdinaryRetains) || visits <= 0)
                            failures.Add(line);
                        Check(ReferenceEquals(ReadOperand(state, first), head));
                        Check(ReferenceEquals(ReadOperand(state, second), tail));
                        Call(heap, LinearConstruction514Names.Collect);
                        Check(((BitString)((TupleTerm)head).Items[0]).ToArray().Length == BackingBytes);
                        Check(((BitString)((TupleTerm)((Cons)tail).Head).Items[0]).ToArray().Length == BackingBytes);
                        await Task.CompletedTask;
                    }
                    );
        if (failures.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
    }

    internal static async Task Custody()
    {
        foreach (bool sourceY in new[] { false, true })
            await WithState(
                false,
                async (
                    context,
                    state,
                    registry,
                    heap
                ) =>
            {
                Seed(state, sourceY);
                object source = Operand(sourceY, 0), first = Operand(true, 1), second = Operand(true, 2);
                var cell = (Cons)Read(state, sourceY, 0);
                Term head = cell.Head, tail = cell.Tail;
                Check((bool)Call(
                    state,
                    Proof,
                    source,
                    first,
                    cell
                )!);
                // Exact current register remains published throughout this
                // source-preserving interleaving; force real heap collection.
                Call(
                    state,
                    BeamOwnedRoots507Names.Write,
                    first,
                    head
                );
                Call(heap, LinearConstruction514Names.Collect);
                Check(ReferenceEquals(cell.Tail, tail));
                Call(
                    state,
                    BeamOwnedRoots507Names.Write,
                    second,
                    tail
                );
                Call(heap, LinearConstruction514Names.Collect);
                Check(((BitString)((TupleTerm)head).Items[0]).ToArray().Length == BackingBytes);
                Check(!(bool)Call(
                    state,
                    Proof,
                    source,
                    source,
                    cell
                )!);
                Check(!(bool)Call(
                    state,
                    Proof,
                    source,
                    first,
                    new Cons(cell.Head, cell.Tail)
                )!);
                object typedSame = Construct(
                    BeamOwnedRoots507Names.OperandType,
                    sourceY ? Y : BeamOwnedRoots507Names.X,
                    BigInteger.Zero,
                    1
                );
                Check(!(bool)Call(
                    state,
                    Proof,
                    source,
                    typedSame,
                    cell
                )!);
                using (var plain = (IDisposable)Construct(BeamOwnedRoots507Names.StateType, null, null))
                    Check(!(bool)Call(
                        plain,
                        Proof,
                        Operand(false, 0),
                        first,
                        cell
                    )!);
                // Head alias keeps the old fallback, including its actual
                // temporary root across first-write/collection/second-write.
                using (var held = (IDisposable)Call(state, Protect, (object)new Term[] { cell })!)
                {
                    Call(
                        state,
                        BeamOwnedRoots507Names.Write,
                        source,
                        head
                    );
                    Call(heap, LinearConstruction514Names.Collect);
                    Check(ReferenceEquals(cell.Tail, tail));
                    Call(
                        state,
                        BeamOwnedRoots507Names.Write,
                        second,
                        tail
                    );
                }
                await Task.CompletedTask;
            }
            );
    }

    internal static async Task ClosedYStore()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            object words = Construct(BeamOwnedRoots507Names.WordType, Property(context, BeamOwnedRoots507Names.NativeArena));
            object state = Construct(BeamOwnedRoots507Names.StateType, words, context);
            try
            {
                Call(state, BeamOwnedRoots507Names.Allocate, 3);
                Seed(state, false);
                var cell = (Cons)Read(state, false, 0);
                object source = Operand(false, 0);
                object heap = Property(context, LinearConstruction514Names.Heap);
                await ((IAsyncDisposable)words).DisposeAsync();
                bool refused = false;
                try
                {
                    // Head(X1) can publish; tail(Y2) hits the genuinely closed
                    // physical store. Source(X0) must remain rooted on failure.
                    Call(
                        state,
                        Pair,
                        Opcode,
                        source,
                        Operand(false, 1),
                        Operand(true, 2),
                        cell
                    );
                }
                catch (ObjectDisposedException)
                {
                    refused = true;
                }
                Check(refused);
                Call(heap, LinearConstruction514Names.Collect);
                Check(ReferenceEquals(Read(state, false, 0), cell));
                Check(((BitString)((TupleTerm)cell.Head).Items[0]).ToArray().Length == BackingBytes);
            }
            finally
            {
                ((IDisposable)state).Dispose();
                await ((IAsyncDisposable)words).DisposeAsync();
            }

            return true;
        });
    }

    private static async Task WithState(bool native, Func<ProcessContext, object, object, object, Task> body)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            object? words = native ? Construct(BeamOwnedRoots507Names.WordType, Property(context, BeamOwnedRoots507Names.NativeArena)) : null;
            object state = Construct(BeamOwnedRoots507Names.StateType, words, context);
            object registry = Property(runtime, LinearConstruction514Names.Registry), heap = Property(context, LinearConstruction514Names.Heap);
            try
            {
                Call(state, BeamOwnedRoots507Names.Allocate, 3);
                await body(
                    context,
                    state,
                    registry,
                    heap
                );
            }
            finally
            {
                ((IDisposable)state).Dispose();
                if (words is not null)
                    await ((IAsyncDisposable)words).DisposeAsync();
            }
            Call(heap, LinearConstruction514Names.Collect);
            Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == 0);

            return true;
        });
    }

    private static void Seed(object state, bool y)
    {
        var binary = new BitString(new byte[BackingBytes]);
        Term cell = Term.List(Term.Tuple(binary), Term.Tuple(binary));
        Call(
            state,
            y ? BeamOwnedRoots507Names.WriteY : BeamOwnedRoots507Names.Write,
            0,
            cell
        );
    }

    private static object Operand(bool y, int index)
        => Construct(
            BeamOwnedRoots507Names.OperandType,
            y ? Y : BeamOwnedRoots507Names.X,
            new BigInteger(index),
            null
        );

    private static Term Read(object state, bool y, int index)
        => (Term)Call(state, y ? BeamOwnedRoots507Names.ReadY : BeamOwnedRoots507Names.Read, index)!;

    private static Term ReadOperand(object state, object operand)
        => Read(state, (string)Property(operand, Kind) == Y, (int)(BigInteger)Property(operand, Value));

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

    private static long Number(object owner, string name) => (long)Property(owner, name);

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}

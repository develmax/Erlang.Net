using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.BeamReceiveCustody557Names;

namespace Erlang.Tests;

internal static class BeamReceiveCustody557Access
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task WithState(
        bool native,
        Func<ProcessContext, object, object?, object, object, Task> body
    )
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            object heap = Property(context, LinearConstruction514Names.Heap);
            object registry = Property(runtime, LinearConstruction514Names.Registry);
            long frames = Number(Property(heap, BeamOwnedRoots507Names.Snapshot), OwnedGuest506Names.Roots);
            object? words = native ? Construct(BeamOwnedRoots507Names.WordType, Property(context, BeamOwnedRoots507Names.NativeArena)) : null;
            object state = Construct(BeamOwnedRoots507Names.StateType, words, context);
            try
            {
                Call(state, BeamOwnedRoots507Names.Allocate, 3);
                await body(
                    context,
                    state,
                    words,
                    registry,
                    heap
                );
            }
            finally
            {
                try
                {
                    ((IDisposable)state).Dispose();
                }
                finally
                {
                    if (words is not null) await ((IAsyncDisposable)words).DisposeAsync();
                }
            }
            Collect(heap);
            Check(Number(Property(registry, LinearConstruction514Names.Traversal), LinearConstruction514Names.References) == 0);
            Check(Number(Property(heap, BeamOwnedRoots507Names.Snapshot), BeamOwnedRoots507Names.Words) == 0);
            Check(Number(Property(heap, BeamOwnedRoots507Names.Snapshot), OwnedGuest506Names.Roots) == frames);

            return true;
        });
    }

    internal static object NewState(ProcessContext context, object? words)
        => Construct(BeamOwnedRoots507Names.StateType, words, context);

    internal static object Clone(ProcessContext context, object state)
        => Call(state, ReceiveClone, context)!;

    internal static void Write(
        object state,
        bool y,
        int index,
        Term value
    )
        => Call(
            state,
            y ? BeamOwnedRoots507Names.WriteY : BeamOwnedRoots507Names.Write,
            index,
            value
        );

    internal static Term Read(object state, bool y, int index)
        => (Term)Call(state, y ? BeamOwnedRoots507Names.ReadY : BeamOwnedRoots507Names.Read, index)!;

    internal static BitString Seed(
        object state,
        bool y,
        int index,
        byte value
    )
    {
        Write(
            state,
            y,
            index,
            Term.Tuple(new BitString(Enumerable.Repeat(value, BackingBytes).ToArray()))
        );
        BitString binary = Binary(Read(state, y, index));
        Check(Property(binary, OwnedBits) is not null && Property(binary, NativeIdentity) is not null);
        Check(binary.ToArray().Length == BackingBytes);

        return binary;
    }

    internal static BitString Binary(Term term) => (BitString)((TupleTerm)term).Items[0];

    internal static void Bytes(BitString binary, byte value)
        => Check(binary.ToArray().SequenceEqual(Enumerable.Repeat(value, BackingBytes)));

    internal static void Collect(object heap) => Call(heap, LinearConstruction514Names.Collect);

    internal static void ClearX(object state, params int[] live)
        => Call(state, BeamOwnedRoots507Names.Clear, (object)live);

    internal static void Retired(BitString binary)
        => Throws<ObjectDisposedException>(() => _ = binary.ToArray());

    internal static object Property(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    internal static object Field(object target, string name)
        => target.GetType().GetField(name, Members)!.GetValue(target)!;

    internal static long Number(object target, string name) => Convert.ToInt64(Property(target, name));

    internal static object? Call(object target, string name, params object?[] arguments)
    {
        MethodInfo method = target.GetType().GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == arguments.Length
            && candidate.GetParameters().Select((parameter, index) => arguments[index] is null
                || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(match => match));
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    internal static void Throws<T>(Action action) where T : Exception
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (T)
        {
            rejected = true;
        }
        Check(rejected);
    }

    internal static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static object Construct(string name, params object?[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            Members,
            null,
            arguments,
            null
        )!;
}

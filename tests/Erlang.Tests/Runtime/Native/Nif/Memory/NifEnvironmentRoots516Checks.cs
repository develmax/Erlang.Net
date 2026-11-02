using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NifEnvironmentRoots516Names;

namespace Erlang.Tests;

internal static class NifEnvironmentRoots516Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static async Task BoundPendingAndCopy()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Property(context, Heap);
            using var frame = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(frame, Reserve)!;
            ResourceReferenceTerm resource = runtime.ReferenceResources.CreateBinary(new byte[] { 1, 6 });
            Term expected = Term.Tuple(Term.List(Term.I(1), Term.I(2)), resource);
            Call(
                frame,
                Set,
                slot,
                expected
            );
            Term owned = (Term)Call(frame, Read, slot)!;
            object image = Image();
            using var source = (IDisposable)EnvironmentFor(context, image);
            using var destination = (IDisposable)Independent(runtime, image);
            long beforePortable = (long)Property(Property(heap, Snapshot), Words);
            Term portable = Term.Tuple(Term.I(516), Term.List(Term.I(1)));
            nuint portableHandle = (nuint)Call(source, Add, portable)!;
            Check(ReferenceEquals(portable, Call(source, Get, portableHandle)));
            Check((long)Property(Property(heap, Snapshot), Words) == beforePortable);
            nuint sourceHandle = (nuint)Call(source, AddValue, PendingTuple(owned))!;
            Call(frame, Clear, slot);
            Call(heap, Collect);
            TupleTerm rooted = (TupleTerm)Call(source, Get, sourceHandle)!;
            Check(rooted.Items[0].Equals(expected));
            nuint copied = (nuint)Call(destination, Copy, Call(source, GetValue, sourceHandle)!)!;
            Term returned = (Term)Call(source, Export, sourceHandle)!;
            source.Dispose();
            Call(heap, Collect);
            ExpectDisposed(() => _ = ((TupleTerm)owned).Items[0]);
            TupleTerm independent = (TupleTerm)Call(destination, Get, copied)!;
            Check(independent.Equals(returned));
            Check(independent.Items[0] is TupleTerm nested && nested.Items[1] is ResourceReferenceTerm magic
                && magic.ResourceKind == resource.ResourceKind);
            using var lease = runtime.ReferenceResources.AcquireBinary(((TupleTerm)independent.Items[0]).Items[1] as ReferenceTerm
                ?? throw new InvalidOperationException(Failure));
            Check(lease is not null && lease.Value.ToArray().SequenceEqual(new byte[] { 1, 6 }));
            Call(destination, ClearEnvironment);
            try
            {
                _ = Call(destination, Get, copied);
                throw new Exception(Failure);
            }
            catch (InvalidOperationException) { }

            return true;
        });
    }

    internal static async Task HeldRetirement()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            object heap = Property(context, Heap);
            var frame = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(frame, Reserve)!;
            Call(
                frame,
                Set,
                slot,
                Term.Tuple(Term.I(516))
            );
            Term owned = (Term)Call(frame, Read, slot)!;
            using var environment = (IDisposable)EnvironmentFor(context, Image());
            nuint handle = (nuint)Call(environment, Add, owned)!;
            frame.Dispose();
            // Real logical process exit closes dictionary admission and starts
            // owned retirement while this admitted callback still holds env.
            runtime.Exit(context, context.Self, Term.A(Kill));
            Task retirement = (Task)Call(heap, Retire)!;
            Check(!retirement.IsCompleted);
            Check(((Term)Call(environment, Get, handle)!).Equals(Term.Tuple(Term.I(516))));
            environment.Dispose();
            Check(retirement.IsCompletedSuccessfully);
            ExpectDisposed(() => _ = ((TupleTerm)owned).Items[0]);

            return true;
        });
    }

    internal static async Task OrdinaryProfile()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(false);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Invoke(context =>
        {
            Check(!(bool)Property(runtime, PrivateProfile));
            Check((bool)Property(context, StorageBacking));
            Check(!(bool)Property(context, GuestExecution));
            object? heap = typeof(ProcessContext).GetField(HeapField, Members)!.GetValue(context);
            long words = heap is null ? 0 : (long)Property(Property(heap, Snapshot), Words);
            using var environment = (IDisposable)EnvironmentFor(context, Image());
            Check(!(bool)Property(environment, EnvironmentExecution));
            Term value = Term.Tuple(Term.I(516));
            nuint handle = (nuint)Call(environment, Add, value)!;
            nuint copied = (nuint)Call(environment, Copy, Call(environment, GetValue, handle)!)!;
            Check(ReferenceEquals(value, Call(environment, Get, copied)));
            Check(ReferenceEquals(value, Call(environment, Export, handle)));
            Check(ReferenceEquals(heap, typeof(ProcessContext).GetField(HeapField, Members)!.GetValue(context)));
            Check(heap is null || (long)Property(Property(heap, Snapshot), Words) == words);
            Check(!(bool)Property(context, GuestExecution));
            Check(!(bool)Property(environment, EnvironmentExecution));

            return true;
        });
    }

    private static object EnvironmentFor(ProcessContext context, object image)
    {
        object environment = New(
            EnvironmentType,
            context,
            image,
            (nint)0,
            New(FailureType)
        );
        try
        {
            Call(environment, Attach, context);

            return environment;
        }
        catch
        {
            ((IDisposable)environment).Dispose();
            throw;
        }
    }

    private static object Independent(ProcessRuntime runtime, object image) => New(EnvironmentType, runtime, image);

    private static object Image() => Activator.CreateInstance(
        typeof(ModuleCodeImage),
        Members,
        null,
        [GuestModule],
        null
    )!;

    private static object PendingTuple(Term term)
    {
        Array elements = Array.CreateInstance(Core(NativeValueType), 1);
        elements.SetValue(New(NativeValueType, term), 0);

        return New(NativeValueType, New(TupleType, elements));
    }

    private static Type Core(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    private static object New(string name, params object?[] arguments)
        => Activator.CreateInstance(
            Core(name),
            Members,
            null,
            arguments,
            null
        )!;

    private static object Property(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Call(object value, string name, params object?[] arguments)
    {
        MethodInfo method = value.GetType().GetMethods(Members).Single(candidate => candidate.Name == name
            && candidate.GetParameters().Length == arguments.Length
            && candidate.GetParameters().Select((parameter, index) => arguments[index] is null
                || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(match => match));
        try
        {
            return method.Invoke(value, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void ExpectDisposed(Action body)
    {
        try
        {
            body();
            throw new Exception(Failure);
        }
        catch (ObjectDisposedException) { }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}

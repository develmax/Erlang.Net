using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Compiler;
using static Erlang.Tests.BeamLazyRootSlots825Names;

namespace Erlang.Tests;

internal static class BeamLazyRootSlots825Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Constructors = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Run()
    {
        foreach (bool pilot in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(pilot);
            await using var foreignActor = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext foreignContext = await foreignActor.Ready.WaitAsync(Deadline);
            object foreignHeap = Get(foreignContext, Heap);
            using var foreignFrame = (IDisposable)Call(foreignHeap, Enter)!;
            Term foreign = (Term)Call(foreignFrame, Retain, Term.Tuple(Term.I(825)))!;
            Exception? failure = null;
            bool observed = false;
            runtime.Modules.Register(
                Probe,
                BeamLazyRootSlots825Names.Check,
                0,
                (context, _) =>
            {
                try
                {
                    Require((bool)Get(context, Authority));
                    Exercise(context, foreign);
                    observed = true;
                }
                catch (Exception error)
                {
                    failure = error;
                    throw;
                }

                return ValueTask.FromResult<Term>(Nil.Value);
            }
            );
            new Parser(Source).ParseModule().Register(runtime.Modules);
            ProcessHandle actor = runtime.Spawn(context => runtime.Modules.Call(context, BeamLazyRootSlots825Names.Module, BeamLazyRootSlots825Names.Run));
            Term reason = await actor.Completion.WaitAsync(Deadline);
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
            Require(observed && reason.Equals(Term.A(ProcessExitReasons.Normal)));
            if (!pilot)
            {
                Require(!(bool)Get(foreignContext, Authority));
                using var hostState = (IDisposable)New(StateType, null, foreignContext);
                Require(Field(hostState, Roots) is null);
            }
        }
    }

    private static void Exercise(ProcessContext context, Term foreign)
    {
        object heap = Get(context, Heap);
        using var state = (IDisposable)New(StateType, null, context);
        object roots = Field(state, Roots)!;
        object mirror = Field(roots, Frame)!;
        Require((int)Get(mirror, Issued) == 0);
        // Clearing and rolling back an unused register cannot issue a pair.
        Call(roots, Rollback, 0);
        Require((int)Get(mirror, Issued) == 0);
        Call(
            state,
            Write,
            LastX,
            Term.Tuple(Term.I(1))
        );
        Term first = (Term)Call(state, Read, LastX)!;
        Require((int)Get(mirror, Issued) == 2);
        Call(
            state,
            Write,
            LastX,
            Term.Tuple(Term.I(2))
        );
        Term current = (Term)Call(state, Read, LastX)!;
        Require((int)Get(mirror, Issued) == 2);
        Call(heap, Collect);
        Require(((TupleTerm)current).Items[0].Equals(Term.I(2)));
        Throws<ObjectDisposedException>(() => _ = ((TupleTerm)first).Items[0]);

        object slot = Static(SlotType, FromTerm, Term.Tuple(Term.I(3)))!;
        object prepared = Call(
            roots,
            Prepare,
            LastX,
            slot,
            true
        )!;
        Term candidate = (Term)Call(prepared, ReadTerm)!;
        Call(heap, Collect);
        Require(((TupleTerm)candidate).Items[0].Equals(Term.I(3)));
        Require(ReferenceEquals(current, Call(state, Read, LastX)));
        Call(roots, Rollback, LastX);
        Call(heap, Collect);
        Throws<ObjectDisposedException>(() => _ = ((TupleTerm)candidate).Items[0]);
        Require(((TupleTerm)current).Items[0].Equals(Term.I(2)));
        object foreignSlot = Static(SlotType, FromTerm, foreign)!;
        Throws<InvalidOperationException>(() => Call(
            roots,
            Prepare,
            LastX,
            foreignSlot,
            false
        ));
        Require(ReferenceEquals(current, Call(state, Read, LastX)));
        Require((int)Get(mirror, Issued) == 2);

        Call(
            state,
            Write,
            0,
            Term.Tuple(Term.I(4))
        );
        Require((int)Get(mirror, Issued) == 4);
        Call(state, Clear, (object)Array.Empty<int>());
        Call(
            state,
            Write,
            LastX,
            Term.Tuple(Term.I(5))
        );
        Require((int)Get(mirror, Issued) == 4);
        Call(heap, Collect);
        Require(((TupleTerm)Call(state, Read, LastX)!).Items[0].Equals(Term.I(5)));

        object words = New(WordType, Get(context, NativeArena));
        try
        {
            using var native = (IDisposable)New(StateType, words, context);
            Call(native, Allocate, 3);
            object local = Call(Field(native, Frames)!, Peek)!;
            object localMirror = Field(Field(local, Roots)!, Frame)!;
            Require((int)Get(localMirror, Issued) == 0);
            Call(
                native,
                WriteY,
                2,
                Term.Tuple(Term.I(6))
            );
            Term kept = (Term)Call(native, ReadY, 2)!;
            Require((int)Get(localMirror, Issued) == 2);
            using var clone = (IDisposable)Call(local, Clone)!;
            Call(
                native,
                Trim,
                1,
                2
            );
            Call(heap, Collect);
            Require(ReferenceEquals(kept, Call(native, ReadY, 1)));
            Require(ReferenceEquals(kept, Call(clone, Read, 2)));
            Require(((TupleTerm)kept).Items[0].Equals(Term.I(6)));
        }
        finally
        {
            ((IAsyncDisposable)words).DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        state.Dispose();
        Throws<ObjectDisposedException>(() => Call(roots, Rollback, 1));
        Throws<ObjectDisposedException>(() => Call(mirror, Live));
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static Type Core(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    private static object New(string type, params object?[] arguments)
        => Activator.CreateInstance(
            Core(type),
            Constructors,
            null,
            arguments,
            null
        )!;

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value);

    private static object? Static(string type, string name, params object?[] arguments) => Invoke(
        Core(type),
        null,
        name,
        arguments
    );

    private static object? Call(object value, string name, params object?[] arguments) => Invoke(
        value.GetType(),
        value,
        name,
        arguments
    );

    private static object? Invoke(
        Type type,
        object? target,
        string name,
        object?[] arguments
    )
    {
        MethodInfo method = type.GetMethods(Members).Single(method => method.Name == name && method.GetParameters().Length == arguments.Length
            && method.GetParameters().Select((parameter, index) => arguments[index] is null || parameter.ParameterType.IsInstanceOfType(arguments[index])).All(match => match));
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }
}

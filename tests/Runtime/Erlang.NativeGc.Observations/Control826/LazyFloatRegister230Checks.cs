using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.LazyFloatRegister230Names;

namespace Erlang.Tests;

internal static class LazyFloatRegister230Checks
{
    private static readonly Type State = typeof(ProcessRuntime).Assembly.GetType(StateType, true)!;
    private static readonly Type Operand = typeof(ProcessRuntime).Assembly.GetType(OperandType, true)!;

    internal static async Task Run()
    {
        CheckState(null);
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                // Establish the same real ordinary guest collector admission as
                // genuine expression execution; no test-only ownership flag.
                _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
                object heap = Property(context, Heap);
                int before = (int)Property(Property(heap, Snapshot), RootFrames);
                CheckState(context);
                Require((int)Property(Property(heap, Snapshot), RootFrames) == before);

                return true;
            });
        }
    }

    private static void CheckState(ProcessContext? context)
    {
        using var state = NewState(context);
        Require(ArrayOf(state) is null);
        Reject<InvalidDataException>(() => Invoke(state, Read, F(0)), FrameError);
        Reject<IndexOutOfRangeException>(() => Invoke(state, Read, F(-1)));
        Reject<IndexOutOfRangeException>(() => Invoke(state, Read, F(RegisterCount)));
        Reject<OverflowException>(() => Invoke(state, Read, F(BigInteger.One << OverflowShift)));
        Reject<IndexOutOfRangeException>(() => Invoke(
            state,
            Write,
            F(-1),
            1.5
        ));
        Reject<IndexOutOfRangeException>(() => Invoke(
            state,
            Write,
            F(RegisterCount),
            1.5
        ));
        Reject<OverflowException>(() => Invoke(
            state,
            Write,
            F(BigInteger.One << OverflowShift),
            1.5
        ));
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            // Existing finite-value rejection wins BEFORE malformed indexing.
            bool rejected = false;
            try
            {
                Invoke(
                    state,
                    Write,
                    F(RegisterCount),
                    invalid
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(BadArithmetic)))
            {
                rejected = true;
            }
            Require(rejected);
        }
        Require(ArrayOf(state) is null);
        Invoke(state, Clear);
        Require(ArrayOf(state) is null);
        using (var emptyClone = (IDisposable)Invoke(state, CloneMethod)!)
        {
            Require(ArrayOf(emptyClone) is null);
            Reject<InvalidDataException>(() => Invoke(emptyClone, Read, F(LastRegister)), FrameError);
        }
        double negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
        Invoke(
            state,
            Write,
            F(0),
            negativeZero
        );
        Invoke(
            state,
            Write,
            F(LastRegister),
            1.5
        );
        Require(ArrayOf(state) is { Length: RegisterCount });
        Require(BitConverter.DoubleToInt64Bits((double)Invoke(state, Read, F(0))!) == long.MinValue
            && (double)Invoke(state, Read, F(LastRegister))! == 1.5);
        Reject<InvalidDataException>(() => Invoke(state, Read, F(1)), FrameError);
        using (var clone = (IDisposable)Invoke(state, CloneMethod)!)
        {
            Require(!ReferenceEquals(ArrayOf(state), ArrayOf(clone)));
            Require(BitConverter.DoubleToInt64Bits((double)Invoke(clone, Read, F(0))!) == long.MinValue);
            Invoke(
                clone,
                Write,
                F(0),
                7.25
            );
            Invoke(
                state,
                Write,
                F(LastRegister),
                -3.5
            );
            Require(BitConverter.DoubleToInt64Bits((double)Invoke(state, Read, F(0))!) == long.MinValue
                && (double)Invoke(clone, Read, F(LastRegister))! == 1.5);
            Invoke(state, Clear);
            Reject<InvalidDataException>(() => Invoke(state, Read, F(0)), FrameError);
            Reject<InvalidDataException>(() => Invoke(state, Read, F(LastRegister)), FrameError);
            Require((double)Invoke(clone, Read, F(0))! == 7.25);
            state.Dispose();
            Reject<InvalidDataException>(() => Invoke(state, Read, F(0)), FrameError);
            Require((double)Invoke(clone, Read, F(LastRegister))! == 1.5);
        }
        // Existing state has no new disposed-access policy: clear/reset and a
        // subsequent finite write retain the previous permitted behavior.
        Invoke(
            state,
            Write,
            F(0),
            2.5
        );
        Require((double)Invoke(state, Read, F(0))! == 2.5);
        Invoke(state, Clear);
        Reject<InvalidDataException>(() => Invoke(state, Read, F(0)), FrameError);
    }

    private static IDisposable NewState(ProcessContext? context)
        => (IDisposable)Activator.CreateInstance(
            State,
            Members,
            null,
            context is null ? [] : new object?[] { null, context },
            null
        )!;

    private static object F(BigInteger index)
        => Activator.CreateInstance(
            Operand,
            Members,
            null,
            [FloatKind, index, null],
            null
        )!;

    private static double?[]? ArrayOf(object state) => (double?[]?)State.GetField(Floating, Members)!.GetValue(state);

    private static object? Invoke(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Reject<T>(Action action, string? message = null) where T : Exception
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (T error) when (error.GetType() == typeof(T) && (message is null || error.Message == message))
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

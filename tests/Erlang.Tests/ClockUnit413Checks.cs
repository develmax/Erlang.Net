using System.Numerics;
using Erlang;

namespace Erlang.Tests;

internal static class ClockUnit413Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(ClockUnit413Names.Failure);
    }

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(2);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            async ValueTask<Term> Call(string function, params Term[] arguments)
                => await runtime.Modules.Call(
                    context,
                    ClockUnit413Names.ModuleName,
                    function,
                    arguments
                );
            BigInteger native = ((Integer)await Call(
                ClockUnit413Names.Convert,
                Term.I(1),
                Term.A(ClockUnit413Names.Second),
                Term.A(ClockUnit413Names.Native)
            )).Value;
            BigInteger performance = ((Integer)await Call(
                ClockUnit413Names.Convert,
                Term.I(1),
                Term.A(ClockUnit413Names.Second),
                Term.A(ClockUnit413Names.Performance)
            )).Value;
            Require(native > 0 && performance > 0);
            if (OperatingSystem.IsWindows()) Require(performance == ClockUnit413Names.NanosecondsPerSecond);
            foreach (string clock in new[] { ClockUnit413Names.Monotonic, ClockUnit413Names.System, ClockUnit413Names.Offset })
            {
                BigInteger before = ((Integer)await Call(clock, Term.A(ClockUnit413Names.Native))).Value;
                BigInteger value = ((Integer)await Call(clock)).Value;
                BigInteger after = ((Integer)await Call(clock, Term.A(ClockUnit413Names.Native))).Value;
                if (before <= after) Require(before <= value && value <= after);
            }
            BigInteger converted = ((Integer)await Call(
                ClockUnit413Names.Convert,
                Term.I(-1),
                Term.A(ClockUnit413Names.Nanosecond),
                Term.A(ClockUnit413Names.Native)
            )).Value;
            BigInteger expected = -((native + ClockUnit413Names.NanosecondsPerSecond - 1) / ClockUnit413Names.NanosecondsPerSecond);
            Require(converted == expected);
            BigInteger nativeMonotonic = ((Integer)await Call(ClockUnit413Names.Monotonic)).Value;
            BigInteger nativeToMillis = ((Integer)await Call(
                ClockUnit413Names.Convert,
                new Integer(nativeMonotonic),
                Term.A(ClockUnit413Names.Native),
                Term.A(ClockUnit413Names.Millisecond)
            )).Value;
            BigInteger millis = ((Integer)await Call(ClockUnit413Names.Monotonic, Term.A(ClockUnit413Names.Millisecond))).Value;
            Require(nativeToMillis <= millis);

            return Nil.Value;
        });
    }
}

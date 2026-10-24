using Erlang;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace Erlang.Tests;

// Component-only provider checks, not original-module or native10 binding proof.
internal static class OsClock399HostChecks
{
    private static readonly Type Values = typeof(ProcessRuntime).Assembly.GetType("Erlang.OsClockNativeValues", true)!;

    private static Term Read(string name, params object[] arguments)
    {
        MethodInfo method = Values.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(m => m.Name == name && m.GetParameters().Length == arguments.Length);

        return (Term)method.Invoke(null, arguments)!;
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException("OS native value provider invariant failed");
    }

    internal static void Run()
    {
        Term expected = Cons.From(Environment.ProcessId.ToString(CultureInfo.InvariantCulture).Select(c => (Term)Term.I(c)));
        Require(Read("GetPid").Equals(expected));
        long before = Stopwatch.GetTimestamp();
        Term counter = Read("PerformanceCounter");
        long after = Stopwatch.GetTimestamp();
        Require(counter is Integer);
        BigInteger counterFrequency = OperatingSystem.IsWindows() ? ClockUnit413Names.NanosecondsPerSecond : Stopwatch.Frequency;
        if (before <= after) Require(((Integer)counter).Value >= new BigInteger(before) * counterFrequency / Stopwatch.Frequency
            && ((Integer)counter).Value <= new BigInteger(after) * counterFrequency / Stopwatch.Frequency);
        Term time = Read("SystemTime");
        Require(time is Integer);
        foreach (Term unit in new Term[] { Term.A("second"), Term.A("millisecond"), Term.A("microsecond"), Term.A("nanosecond"), Term.A("native"), Term.A("perf_counter"), Term.I(17) })
            Require(Read("SystemTime", unit) is Integer);
        foreach (Term unit in new Term[] { Term.I(0), Term.I(-1), Term.A("bad_unit"), new FloatTerm(1) })
        {
            bool refused = false;
            try
            {
                Read("SystemTime", unit);
            }
            catch (TargetInvocationException e) when (e.InnerException is ErlangException failure && failure.Reason.Equals(Term.A("badarg")))
            {
                refused = true;
            }
            Require(refused);
        }
        Require(Read("Timestamp") is TupleTerm { Items.Count: 3 } tuple && tuple.Items.All(t => t is Integer)
            && ((Integer)tuple.Items[1]).Value >= 0 && ((Integer)tuple.Items[1]).Value < 1000000
            && ((Integer)tuple.Items[2]).Value >= 0 && ((Integer)tuple.Items[2]).Value < 1000000);
    }
}

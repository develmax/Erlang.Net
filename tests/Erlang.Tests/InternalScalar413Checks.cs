using System.Diagnostics;
using System.Reflection;
using System.Text;
using Erlang;

namespace Erlang.Tests;

internal static class InternalScalar413Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(InternalScalar413Names.Failure);
    }

    private static ErlangFunction? Capture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity
    )
    {
        Type type = typeof(ProcessRuntime).Assembly.GetType(InternalScalar413Names.RuntimeType, true)!;
        MethodInfo method = type.GetMethod(InternalScalar413Names.CaptureMethod, BindingFlags.Static | BindingFlags.NonPublic)!;
        object?[] arguments = [owner, module, function, arity, null];
        bool captured = (bool)method.Invoke(null, arguments)!;

        return captured ? (ErlangFunction)arguments[4]! : null;
    }

    internal static async Task Integers()
    {
        await using var runtime = new ProcessRuntime(2);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction binary = Capture(
            runtime,
            InternalScalar413Names.ModuleName,
            InternalScalar413Names.BinaryInteger,
            2
        )!;
        ErlangFunction list = Capture(
            runtime,
            InternalScalar413Names.ModuleName,
            InternalScalar413Names.ListInteger,
            2
        )!;
        Term tail = Term.A(InternalScalar413Names.Tail);
        Term remainder = Term.List(Term.I('x'));
        Term input = new Cons(Term.I('1'), new Cons(Term.I('2'), remainder));
        await actor.InvokeAsync(async context =>
        {
            Require((await binary(context, [new BitString(Encoding.ASCII.GetBytes("-00123")), Term.I(10)])).Equals(Term.I(-123)));
            Require((await binary(context, [new BitString(Encoding.ASCII.GetBytes(new string('x', 18))), Term.I(10)])).Equals(Term.A(InternalScalar413Names.Big)));
            Require((await binary(context, [new BitString([0x80], 1), Term.I(10)])).Equals(Term.A(InternalScalar413Names.BadArgument)));
            Require((await list(context, [Nil.Value, tail])).Equals(Term.A(InternalScalar413Names.NoInteger)));
            Require((await list(context, [tail, tail])).Equals(Term.A(InternalScalar413Names.NotList)));
            Require((await list(context, [Term.List(Term.I('1')), tail])).Equals(Term.A(InternalScalar413Names.BadArgument)));
            var result = (TupleTerm)await list(context, [input, Term.I(10)]);
            Require(result.Items[0].Equals(Term.I(12)) && ReferenceEquals(result.Items[1], remainder));
            Require((await list(context, [new Cons(Term.I('0'), tail), Term.I(10)])).Equals(Term.Tuple(Term.I(0), tail)));
            Require((await list(context, [Term.List(Term.I('0'), tail), Term.I(10)])).Equals(Term.A(InternalScalar413Names.NoInteger)));
            Require((await list(context, [Term.List(Term.I(304), Term.I(305), Term.I(306)), Term.I(10)])).Equals(Term.Tuple(Term.I(12), Nil.Value)));
            Require((await list(context, [Term.List(Term.I(-208), Term.I(-207)), Term.I(10)])).Equals(Term.Tuple(Term.I(1), Nil.Value)));
            Require((await list(context, [Cons.From(Enumerable.Repeat<Term>(Term.I('1'), 18)), Term.I(10)])).Equals(Term.A(InternalScalar413Names.Big)));

            return Nil.Value;
        });
    }

    internal static async Task Ownership()
    {
        await using var runtime = new ProcessRuntime(2);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Require(Capture(
            null,
            InternalScalar413Names.ModuleName,
            InternalScalar413Names.TimeUnit,
            0
        ) is null);
        Require(Capture(
            runtime,
            InternalScalar413Names.OtherModule,
            InternalScalar413Names.TimeUnit,
            0
        ) is null);
        Require(Capture(
            runtime,
            InternalScalar413Names.ModuleName,
            InternalScalar413Names.TimeUnit,
            1
        ) is null);
        Require(Capture(
            runtime,
            InternalScalar413Names.ModuleName,
            InternalScalar413Names.OtherFunction,
            0
        ) is null);
        Require(!runtime.Modules.HasExport(InternalScalar413Names.ModuleName, InternalScalar413Names.TimeUnit, 0));
        ErlangFunction unit = Capture(
            runtime,
            InternalScalar413Names.ModuleName,
            InternalScalar413Names.TimeUnit,
            0
        )!;
        ErlangFunction performance = Capture(
            runtime,
            InternalScalar413Names.ModuleName,
            InternalScalar413Names.PerformanceUnit,
            0
        )!;
        await actor.InvokeAsync(async context =>
        {
            Term expected = await runtime.Modules.Call(
                context,
                InternalScalar413Names.ErlangModule,
                InternalScalar413Names.ConvertUnit,
                Term.I(1),
                Term.A(InternalScalar413Names.Second),
                Term.A(InternalScalar413Names.Native)
            );
            Require((await unit(context, [])).Equals(expected));
            Require((await performance(context, [])).Equals(new Integer(OperatingSystem.IsWindows()
                ? ClockUnit413Names.NanosecondsPerSecond : Stopwatch.Frequency)));

            return Nil.Value;
        });
        Require(!runtime.Modules.HasExport(InternalScalar413Names.ModuleName, InternalScalar413Names.TimeUnit, 0));
        await using var foreign = new ProcessRuntime(2);
        await using var foreignActor = new RuntimeNodeFoundationHostActor(foreign);
        bool refused = false;
        try
        {
            await foreignActor.InvokeAsync(async context => await unit(context, []));
        }
        catch (InvalidOperationException)
        {
            refused = true;
        }
        Require(refused);
    }
}

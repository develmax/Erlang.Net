using Erlang;
using static Erlang.Tests.BeamSmallOpcodeControlNames;

namespace Erlang.Tests;

internal static class BeamSmallOpcodeControlChecks
{
    internal static async Task NumericInequality()
    {
        BeamModule module = BeamModule.Prepare(BeamSmallOpcodeControlBytes.Inequality());
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 2));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            (Term Left, Term Right, bool Different)[] cases =
            [
                (Term.I(1), new FloatTerm(1.0), false),
                (Term.Tuple(Term.I(1)), Term.Tuple(new FloatTerm(1.0)), false),
                (Term.List(Term.I(1)), Term.List(new FloatTerm(1.0)), false),
                (Term.I(9007199254740993L), new FloatTerm(9007199254740992.0), true),
                (Term.I(1), Term.A(BeamOpcodeEdgeNames.Marker), true),
                (Term.I(1), Term.I(2), true)
            ];
            foreach (var item in cases)
            {
                Term actual = await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    item.Left,
                    item.Right
                );
                Require(actual.Equals(Term.A(item.Different ? BeamOpcodeEdgeNames.Marker : BeamOpcodeEdgeNames.Other)));
            }

            return true;
        }).WaitAsync(Deadline);
    }

    internal static async Task SelectiveReceiveNumericInequality()
    {
        BeamModule module = BeamModule.Prepare(BeamSmallOpcodeControlBytes.ReceiveInequality());
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            runtime.Send(context.Self, Term.I(1));
            runtime.Send(context.Self, Term.I(2));
            Term actual = await module.Invoke(context, BeamOpcodeEdgeNames.Run, new FloatTerm(1.0));
            Require(actual.Equals(Term.I(2)));
            Term? retained = await context.Mailbox.ReceiveAsync<Term>(term => term, TimeSpan.Zero, context.Cancellation);
            Require(retained?.Equals(Term.I(1)) == true);

            return true;
        }).WaitAsync(Deadline);
    }

    internal static async Task NegationBits()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            foreach (bool inPlace in new[] { false, true })
                foreach (bool twice in new[] { false, true })
                {
                    BeamModule module = BeamModule.Prepare(BeamSmallOpcodeControlBytes.Negate(inPlace: inPlace, twice: twice));
                    Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
                    foreach (double input in new[] { 0.0, -0.0, 2.5, -2.5 })
                    {
                        Term actual = await module.Invoke(context, BeamOpcodeEdgeNames.Run, new FloatTerm(input));
                        long expected = BitConverter.DoubleToInt64Bits(twice ? input : -input);
                        Require(actual is FloatTerm number && BitConverter.DoubleToInt64Bits(number.Value) == expected);
                    }
                }

            return true;
        }).WaitAsync(Deadline);
    }

    internal static Task NegationUninitialized() => Refuse(BeamSmallOpcodeControlBytes.Negate(initialize: false), 1);

    internal static Task NegationStaleAfterCall() => Refuse(BeamSmallOpcodeControlBytes.Negate(nativeCall: true), 1);

    internal static async Task NegationFreshAfterCall()
    {
        BeamModule module = BeamModule.Prepare(BeamSmallOpcodeControlBytes.Negate(nativeCall: true, fresh: true));
        Require(module.Supports(BeamOpcodeEdgeNames.Run, 1));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term actual = await module.Invoke(context, BeamOpcodeEdgeNames.Run, new FloatTerm(2.5));
            Require(actual is FloatTerm number && number.Value == -3);

            return true;
        }).WaitAsync(Deadline);
    }

    internal static Task NegationWrongDestination() => Refuse(BeamSmallOpcodeControlBytes.Negate(wrongDestination: true), 1);

    internal static Task InequalityUninitializedY() => Refuse(BeamSmallOpcodeControlBytes.Inequality(unreadY: true), 2);

    private static Task Refuse(byte[] bytes, int arity)
    {
        bool refused;
        try
        {
            refused = !BeamModule.Prepare(bytes).Supports(BeamOpcodeEdgeNames.Run, arity);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused);

        return Task.CompletedTask;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}

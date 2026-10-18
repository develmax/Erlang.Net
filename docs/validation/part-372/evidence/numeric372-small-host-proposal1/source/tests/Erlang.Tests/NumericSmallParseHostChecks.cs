using System.Numerics;

namespace Erlang.Tests;

internal static class NumericSmallParseHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        BigInteger smallBoundary = BigInteger.One << NumericSmallParseHostNames.NativeSmallBits;
        await Value(actor, runtime, [Characters('1' + NumericSmallParseHostNames.ByteRange)], 1);
        await Value(actor, runtime, [Characters('1' - NumericSmallParseHostNames.ByteRange)], 1);
        await Value(actor, runtime, [Characters('1' + NumericSmallParseHostNames.ByteRange * NumericSmallParseHostNames.ByteRange)], 1);
        await Value(actor, runtime, [Term.List(new Integer(smallBoundary - NumericSmallParseHostNames.ByteRange + '1'))], 1);
        await Value(actor, runtime, [Term.List(new Integer(-smallBoundary + '1'))], 1);
        await Value(actor, runtime, [Characters('-', '1' + NumericSmallParseHostNames.ByteRange)], -1);
        await Value(actor, runtime, [Characters('+', '1' + NumericSmallParseHostNames.ByteRange)], 1);
        await Value(actor, runtime, [Characters('0' + NumericSmallParseHostNames.ByteRange, '1' + NumericSmallParseHostNames.ByteRange)], 1);
        Term zeroes = Cons.From(Enumerable.Repeat<Term>(Term.I('0' + NumericSmallParseHostNames.ByteRange), NumericSmallParseHostNames.LongZeroPrefix));
        await Value(actor, runtime, [zeroes], 0);
        await Value(actor, runtime, [Repeat('1' + NumericSmallParseHostNames.ByteRange, NumericSmallParseHostNames.DecimalSmallDigits)], NumericSmallParseHostNames.DecimalSmallOnes);
        await Value(actor, runtime, [Repeat('1', NumericSmallParseHostNames.DecimalSmallDigits + 1)], NumericSmallParseHostNames.DecimalFallbackOnes);
        await Value(actor, runtime, [Repeat('1' + NumericSmallParseHostNames.ByteRange, NumericSmallParseHostNames.HexadecimalSmallDigits), Term.I(NumericSmallParseHostNames.HexadecimalBase)], NumericSmallParseHostNames.HexadecimalSmallOnes);
        await Value(actor, runtime, [Repeat('1', NumericSmallParseHostNames.HexadecimalSmallDigits + 1), Term.I(NumericSmallParseHostNames.HexadecimalBase)], NumericSmallParseHostNames.HexadecimalFallbackOnes);
        for (int radix = NumericSmallParseHostNames.MinimumBase; radix <= NumericSmallParseHostNames.MaximumBase; radix++)
            await Value(actor, runtime, [Characters('1' + NumericSmallParseHostNames.ByteRange), Term.I(radix)], 1);
        await Error(actor, runtime, [Characters('-' + NumericSmallParseHostNames.ByteRange, '1')]);
        await Error(actor, runtime, [Characters('+' + NumericSmallParseHostNames.ByteRange, '1')]);
        await Error(actor, runtime, [Term.List(new Integer(smallBoundary + '1'))]);
        await Error(actor, runtime, [Term.List(new Integer(-smallBoundary - NumericSmallParseHostNames.ByteRange + '1'))]);
        await Error(actor, runtime, [Repeat('1' + NumericSmallParseHostNames.ByteRange, NumericSmallParseHostNames.DecimalSmallDigits + 1)]);
        await Error(actor, runtime, [Repeat('1' + NumericSmallParseHostNames.ByteRange, NumericSmallParseHostNames.HexadecimalSmallDigits + 1), Term.I(NumericSmallParseHostNames.HexadecimalBase)]);
        await Error(actor, runtime, [new Cons(Term.I('1' + NumericSmallParseHostNames.ByteRange), Term.A(NumericSmallParseHostNames.Tail))]);
        await Error(actor, runtime, [Characters('2' + NumericSmallParseHostNames.ByteRange), Term.I(NumericSmallParseHostNames.MinimumBase)]);
        await Error(actor, runtime, [Characters('1' + NumericSmallParseHostNames.ByteRange), Term.A(NumericSmallParseHostNames.InvalidBase)]);
        await Error(actor, runtime, [Term.A(NumericSmallParseHostNames.Tail), Term.I(NumericSmallParseHostNames.MinimumBase - 1)]);
        await Error(actor, runtime, [Nil.Value, Term.I(NumericSmallParseHostNames.MaximumBase + 1)]);
    }

    private static Term Characters(params int[] characters) => Cons.From(characters.Select(value => (Term)Term.I(value)));

    private static Term Repeat(int character, int count) => Cons.From(Enumerable.Repeat<Term>(Term.I(character), count));

    private static async Task Value(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        Term[] arguments,
        long expected
    )
    {
        Term result = await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            NumericSmallParseHostNames.Erlang,
            NumericSmallParseHostNames.Function,
            arguments
        ).AsTask());
        Require(result is Integer number && number.Value == expected);
    }

    private static async Task Error(
        RuntimeNodeFoundationHostActor actor,
        ProcessRuntime runtime,
        Term[] arguments
    )
    {
        bool caught = false;
        try
        {
            await actor.InvokeAsync(context => runtime.Modules.Call(
                context,
                NumericSmallParseHostNames.Erlang,
                NumericSmallParseHostNames.Function,
                arguments
            ).AsTask());
        }
        catch (ErlangException error)
        {
            caught = true;
            Require(error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
            Term firstTerm = Cons.Items(error.StackTraceTerm).First();
            Require(firstTerm is TupleTerm { Items.Count: 4 });
            var first = (TupleTerm)firstTerm;
            Require(first.Items[0].Equals(Term.A(NumericSmallParseHostNames.Erlang)) && first.Items[1].Equals(Term.A(NumericSmallParseHostNames.Function)));
            Term[] captured = Cons.Items(first.Items[2]).ToArray();
            Require(captured.Length == arguments.Length);
            for (int index = 0; index < captured.Length; index++)
                Require(ReferenceEquals(captured[index], arguments[index]));
            Term expectedInfo = Term.List(Term.Tuple(
                Term.A(NumericSmallParseHostNames.ErrorInfo),
                new MapTerm(new Dictionary<Term, Term>
                {
                    [Term.A(NumericSmallParseHostNames.ModuleKey)] = Term.A(NumericSmallParseHostNames.Formatter)
                })
            ));
            Require(first.Items[3].Equals(expectedInfo));
        }
        Require(caught);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NumericSmallParseHostNames.Failure);
    }
}

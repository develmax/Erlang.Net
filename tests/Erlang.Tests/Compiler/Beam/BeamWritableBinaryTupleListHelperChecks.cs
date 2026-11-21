using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWritableBinaryTupleListHelperChecks
{
    internal const string TestName = "compiler/beam/uses-a-writable-helper-for-list-of-byte-pairs";
    private const string Module = "beam_writable_byte_pairs";
    private const string Helper = "-pack/1-lbc$^0/2-0-";
    private const string ReorderedHelper = "-reordered/1-lbc$^0/2-0-";
    private const string WrongLayout = "Expected the writable helper to keep the tail, accumulator and projected byte operands.";
    private const string WrongValue = "The byte pair comprehension changed element order, skipping or generator errors.";
    private const string Source = """
        -module(beam_writable_byte_pairs).
        -export([pack/1, reordered/1]).
        pack(Items) ->
            << <<High:8, Low:8>> || {High, Low} <- Items >>.
        reordered(Items) ->
            << <<Low:8, High:8>> || {High, Low} <- Items >>.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        BeamModule module = BeamModule.Prepare(image);
        if (!module.Functions.Any(function => function.Name == Helper)
            || module.Functions.Any(function => function.Name == ReorderedHelper))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        CheckLayout(Code(image));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.Tuple(Term.I(1), Term.I(2));
            Term second = Term.Tuple(Term.I(3), Term.I(4));
            Term ignored = Term.A("ignored");
            Term items = Term.List(
                first,
                ignored,
                Term.Tuple(ignored),
                second
            );
            if (!new BitString([1, 2, 3, 4]).Equals(await module.Invoke(context, "pack", items))
                || !new BitString([2, 1, 4, 3]).Equals(await module.Invoke(context, "reordered", items))
                || !new BitString([]).Equals(await module.Invoke(context, "pack", Term.List(ignored))))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term improper = new Cons(ignored, new Cons(first, ignored));
            try
            {
                await module.Invoke(context, "pack", improper);
            }
            catch (ErlangException error) when (
                error.Reason.Equals(Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), ignored))
                && error.StackTraceTerm is Cons
                {
                    Head: TupleTerm { Items: [Atom { Name: Module }, Atom { Name: Helper }, _, _] }
                })
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static void CheckLayout(object code)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == Helper);
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions").Skip(first).Take(end - first).ToArray();
        object[] projections = body.Where(instruction => Opcode(instruction) == GetTupleElement).ToArray();
        object construction = body.Single(instruction => Opcode(instruction) == CreateBinary);
        object[] operands = Items(construction, "Operands").ToArray();
        if (body.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            || body.Count(instruction => Opcode(instruction) == TailLocalCall) != 2
            || Value(operands[2]) != 4 || Value(operands[3]) != 16 || Value(operands[4]) != 1
            || projections is not [var high, var low]
            || Items(high, "Operands").Select(Value).ToArray() is not [2, 0, 3]
            || Items(low, "Operands").Select(Value).ToArray() is not [2, 1, 2])
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

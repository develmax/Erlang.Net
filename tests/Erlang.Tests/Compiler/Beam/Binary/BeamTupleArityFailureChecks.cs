using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleArityFailureChecks
{
    internal const string TestName = "compiler/beam/shortens-isolated-tuple-arity-failures";
    private const string Fixture = "beam_tuple_arity_relay_probe.erl";
    private const string IncorrectEdge = "A tuple arity failure did not bypass its isolated non-tuple comparison.";
    private const string IncorrectSharedEdge = "A shared tuple fallback was redirected.";
    private const string IncorrectValue = "The tuple cases returned an unexpected value or failure.";

    internal const string Source = """
        -module(beam_tuple_arity_relay_probe).
        -export([list_head/1, shared/2]).

        list_head(Data) ->
            case hd(Data) of
                {_,_,_} -> tuple;
                undefined -> absent
            end.

        shared({A,B},{C,D}) -> {A,B,C,D};
        shared(undefined, _) -> left;
        shared(_, undefined) -> right.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] positive = Body("list_head");
        int tuplePair = FindPair(positive);
        int relay = Value(Operands(positive[tuplePair])[0]);
        int badArity = Value(Operands(positive[tuplePair + 1])[0]);
        int relayIndex = Array.FindIndex(positive, instruction =>
            Opcode(instruction) == Label && Value(Operands(instruction)[0]) == relay);
        if (relayIndex < 0 || relayIndex + 1 >= positive.Length
            || Opcode(positive[relayIndex + 1]) != ExactEqualTest
            || relay == badArity || Value(Operands(positive[relayIndex + 1])[0]) != badArity)
        {
            throw new InvalidOperationException(IncorrectEdge);
        }

        object[] shared = Body("shared");
        int sharedPair = FindPair(shared);
        if (shared.Skip(sharedPair).Take(4).Select(Opcode).SequenceEqual(new byte[] { IsTuple, TestArity, IsTuple, TestArity }) is false
            || shared.Skip(sharedPair).Take(4).Select(instruction => Value(Operands(instruction)[0])).Distinct().Count() != 1)
        {
            throw new InvalidOperationException(IncorrectSharedEdge);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term pair = Term.Tuple(Term.I(1), Term.I(2));
            Term undefined = Term.A("undefined");
            Term triple = Term.Tuple(
                Term.I(1),
                Term.I(2),
                Term.I(3)
            );
            if (!Term.A("tuple").Equals(await module.Invoke(context, "list_head", Term.List(triple)))
                || !Term.A("absent").Equals(await module.Invoke(context, "list_head", Term.List(undefined)))
                || !Term.A("left").Equals(await module.Invoke(
                    context,
                    "shared",
                    undefined,
                    pair
                ))
                || !Term.A("right").Equals(await module.Invoke(
                    context,
                    "shared",
                    pair,
                    undefined
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            try
            {
                _ = await module.Invoke(context, "list_head", Term.List(pair));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), pair)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);
        });

        object[] Body(string name)
        {
            int info = Array.FindIndex(instructions, instruction =>
                Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name);

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static int FindPair(object[] body)
    {
        int index = Array.FindIndex(body, instruction => Opcode(instruction) == IsTuple);
        if (index < 0 || index + 1 >= body.Length || Opcode(body[index + 1]) != TestArity)
        {
            throw new InvalidOperationException(IncorrectEdge);
        }

        return index;
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

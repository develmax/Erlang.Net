using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedConstructorAndCatchChecks
{
    internal const string TestName = "compiler/beam/keeps-constructor-results-and-catch-entry-arguments-in-x";
    private const string Fixture = "beam_owned_constructor_and_catch_probe.erl";
    private const string UnexpectedLayout = "Owned constructor and catch paths did not match their pinned layout.";

    internal const string Source = """
        -module(beam_owned_constructor_and_catch_probe).
        -export([encode/2, decode/2, mixed_constructor/0, opaque/2]).
        -record(wire, {wildcards, bits}).

        encode(_, Value) when Value =:= {entry, root, []} -> {ok, root};
        encode(Mode, Value) when (Value =:= {entry, true, [all]}) andalso
                                 (Mode =:= default) -> {ok, first_choice()};
        encode(Mode, Value) when (Value =:= {entry, true, [choose]}) andalso
                                 (Mode =:= default) -> {ok, second_choice()};
        encode(Mode, {entry, false, Value}) ->
            case catch first(Value, Mode) of
                {'EXIT', Reason} -> {error, Reason};
                Result -> {ok, Result}
            end;
        encode(Mode, {entry, true, Value}) ->
            case catch second(Value, Mode) of
                {'EXIT', Reason} -> {error, Reason};
                Result -> {ok, Result}
            end;
        encode(_, Value) -> {error, {bad_type, Value}}.

        decode(_, Value) when Value =:= {wire, [], root} -> {ok, root};
        decode(Mode, {wire, [], Value}) ->
            case catch first(Value, Mode) of
                {'EXIT', Reason} -> {error, Reason};
                Result -> {ok, Result}
            end;
        decode(Mode, {wire, Other, Value}) ->
            case catch third(Other, Value, Mode) of
                {'EXIT', Reason} -> {error, Reason};
                Result -> {ok, Result}
            end;
        decode(_, Value) -> {error, {bad_type, Value}}.

        first_choice() -> #wire{wildcards = [[41 + base()]], bits = [0, 0, 0]}.
        second_choice() -> #wire{wildcards = [[base() + 17]], bits = [0, 0, 0]}.
        mixed_constructor() -> #wire{wildcards = [[mixed() + 5]], bits = [0, 0, 0]}.
        base() -> lists:sum([7, 9, 11]) - 1.
        mixed() -> ?MODULE:opaque(nonnumeric, ignored).
        first(Value, Mode) -> ?MODULE:opaque({first, Value}, Mode).
        second(Value, Mode) -> ?MODULE:opaque({second, Value}, Mode).
        third(Other, Value, Mode) -> ?MODULE:opaque({third, Other, Value}, Mode).
        opaque(nonnumeric, _) -> not_a_number;
        opaque(_, die) -> exit(stopped);
        opaque(Value, _) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] first = Body("first_choice", 0);
        object[] second = Body("second_choice", 0);
        object[] encode = Body("encode", 2);
        object[] decode = Body("decode", 2);
        if (!ConstructorFrame(first) || !ConstructorFrame(second)
            || !CatchFrame(encode) || !CatchFrame(decode))
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        object[] Body(string name, int arity)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var width]
                    && atoms[Value(atom)] == name && Value(width) == arity
            );

            return info < 0 ? [] : instructions.Skip(info + 2)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool ConstructorFrame(object[] body)
    {
        int arithmetic = Array.FindIndex(body, instruction => Opcode(instruction) == GcBifTwo);

        return body.Length > 0 && Opcode(body[0]) == Allocate && FirstOperand(body[0]) == 0
            && arithmetic >= 0 && Opcode(body[arithmetic + 1]) == TestHeap
            && Opcode(body[arithmetic + 2]) == PutList;
    }

    private static bool CatchFrame(object[] body)
    {
        object[] frames = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();

        return frames.Count(instruction => FirstOperand(instruction) == 1) == 2
            && frames.All(instruction => FirstOperand(instruction) <= 1)
            && body.Count(instruction => Opcode(instruction) == CatchEnd) == 2
            && body.Count(instruction => Opcode(instruction) == Deallocate && FirstOperand(instruction) == 1) == 3;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int FirstOperand(object instruction) => Value(Items(instruction, "Operands").First());

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

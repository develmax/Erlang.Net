using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLiteralConsContinuationsChecks
{
    internal const string TestName = "compiler/beam/preserves-shared-list-prefixes-and-branch-local-frames";
    private const string Fixture = "beam_literal_cons_continuations.erl";
    private const string WrongEmpty = "The literal empty-list entry did not use a nil test.";
    private const string WrongPrefix = "The shared literal prefix allocated a frame before the list decision.";
    private const string WrongDispatch = "The string-key dispatch lost its first-element selection and tuple check.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_literal_cons_continuations).
        -export([scan/1, route/1, split_pair/2, tokens/3]).

        scan(Data) -> scan(Data, []).

        scan([], Acc) -> {[],lists:reverse(Acc)};
        scan([8,9,10], Acc) -> {lists:reverse(Acc) ++ [8,9],[]};
        scan([8,9,10|More], Acc) -> {lists:reverse(Acc) ++ [8,9],More};
        scan([Char|More], Acc) -> scan(More,[Char|Acc]).

        route("") -> {reply,[],7};
        route(Input) ->
            Pieces = string:tokens(Input,";"),
            route(Pieces,[],7,active).

        route([], Seen, Code, _) -> {reply,Seen,Code};
        route([Head|Tail], Seen, Code, Flag) ->
            {Key,Value} = ?MODULE:split_pair(Head,unused),
            case Key of
                "move" when Flag == active ->
                    route(Tail,[{Key,Value}|Seen],15,Flag);
                "state" ->
                    Updated = case ?MODULE:tokens(Value," ",2) of
                        {ok,[Digits,_]} -> list_to_integer(Digits);
                        _ -> 7
                    end,
                    route(Tail,Seen,Updated,inactive);
                _ ->
                    route(Tail,[{Key,Value}|Seen],Code,Flag)
            end.

        split_pair(Head, _) ->
            case string:split(Head,":",leading) of
                [Key,Value] -> {Key,Value};
                _ -> invalid
            end.

        tokens(Value, Separator, _Count) -> {ok,string:split(Value,Separator,leading)}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] wrapper = Operations("route", 1);
        if (!wrapper.Any(instruction => Opcode(instruction) == IsNil)
            || wrapper.Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(WrongEmpty);
        }

        object[] scan = Operations("scan", 2);
        int scanFrame = Array.FindIndex(scan, instruction => Opcode(instruction) == Allocate);
        if (scanFrame < 0 || scan.Take(scanFrame).Count(instruction => Opcode(instruction) == GetList) != 3
            || scan.Count(instruction => Opcode(instruction) == Allocate) != 3
            || !scan.Any(instruction => Opcode(instruction) == TailLocalCall))
        {
            throw new InvalidOperationException(WrongPrefix);
        }

        object[] dispatch = Operations("route", 4);
        int nonempty = Array.FindIndex(dispatch, instruction => Opcode(instruction) == IsNonemptyList);
        int frame = Array.FindIndex(dispatch, instruction => Opcode(instruction) == Allocate);
        if (nonempty < 0 || frame <= nonempty
            || dispatch.Count(instruction => Opcode(instruction) == SelectValue) != 1
            || !dispatch.Any(instruction => Opcode(instruction) == InitYRegisters)
            || !dispatch.Any(instruction => Opcode(instruction) == BadMatch))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        return Task.CompletedTask;

        object[] Operations(string name, int arity)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(Missing);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

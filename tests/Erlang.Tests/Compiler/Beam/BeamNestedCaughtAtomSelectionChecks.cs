using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedCaughtAtomSelectionChecks
{
    internal const string TestName = "compiler/beam/keeps-outer-handler-through-caught-atom-selection";
    private const string Fixture = "beam_nested_caught_atom_selection_probe.erl";
    private const string UnexpectedLayout = "The inner default must choose the original fallback while the outer handler stays installed.";

    private const string Source = """
        -module(beam_nested_caught_atom_selection_probe).
        -export([run/4]).
        run(Fun, [Key|_]=Args, From, State) ->
            try
                Path = try scoped_probe:read(index, {Key, kind}, 2)
                       catch error:_ -> {failed,[]}
                       end,
                if
                    Path =:= main; Path =:= spare ->
                        {reply, scoped_probe:invoke(target, Fun, [Path|Args]), State};
                    true ->
                        Worker = prepare(target, Fun, Args, From),
                        _Pid = erlang:spawn_link(Worker),
                        {deferred, State}
                end
            catch
                throw:Value -> {reply, Value, State};
                _:Reason -> {reply, {failed, {exception, Reason}}, State}
            end.
        prepare(Target, Fun, Args, From) -> fun() -> {Target, Fun, Args, From} end.
        """;

    internal static Task Run()
    {
        object[] body = Body(Source);
        object[] defaultAccepted = Body(Source.Replace(
            "catch error:_ -> {failed,[]}",
            "catch error:_ -> main",
            StringComparison.Ordinal
        ));
        object[] keyEffect = Body(Source.Replace("{Key, kind}", "{Key, scoped_probe:kind()}", StringComparison.Ordinal));
        if (!HasNestedLayout(body) || HasNestedLayout(defaultAccepted) || HasNestedLayout(keyEffect))
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        int[] landingTags = body.Where(instruction => Opcode(instruction) == TryCase)
            .Select(instruction => Value(Operands(instruction)[0])).ToArray();
        int[] endingTags = body.Where(instruction => Opcode(instruction) == TryEnd)
            .Select(instruction => Value(Operands(instruction)[0])).ToArray();
        int[][] clears = body.Where(instruction => Opcode(instruction) == InitYRegisters)
            .Select(instruction => Operands(instruction).Skip(1).Select(Value).ToArray()).ToArray();
        int selector = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);
        int firstEnd = Array.FindIndex(body, instruction => Opcode(instruction) == TryEnd);
        if (!landingTags.SequenceEqual([4, 5]) || !endingTags.SequenceEqual([4, 5]) || selector != firstEnd + 1
            || clears.Length != 3 || !clears[0].SequenceEqual([4])
            || !clears[1].SequenceEqual([1, 2, 3]) || !clears[2].SequenceEqual([1, 2, 3])
            || body.Count(instruction => Opcode(instruction) == Raise) != 1)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        return Task.CompletedTask;
    }

    private static bool HasNestedLayout(object[] body)
    {
        byte[] prefix =
        [
            IsNonemptyList, Allocate, InitYRegisters, Move, Move, Move, Move,
            GetHead, TryEnter, TestHeap, PutTuple, TryEnter
        ];

        return body.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
            && Value(Operands(body[1])[0]) == 6
            && Value(Operands(body[8])[0]) == 5 && Value(Operands(body[11])[0]) == 4;
    }

    private static object[] Body(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var name, var arity] && atoms[Value(name)] == "run" && Value(arity) == 4
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        return instructions.Skip(entry + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

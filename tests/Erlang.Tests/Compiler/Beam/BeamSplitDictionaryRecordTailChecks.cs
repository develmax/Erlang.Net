using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSplitDictionaryRecordTailChecks
{
    internal const string TestName = "compiler/beam/frames-literal-and-general-dictionary-record-tails";
    private const string Fixture = "beam_split_dictionary_record_tail_probe.erl";
    private const string WrongLayout = "The literal item arm and general item arm must preserve distinct branch-local roots.";

    private const string Source = """
        -module(beam_split_dictionary_record_tail_probe).
        -export([query/2]).
        -record(row,{id,extra}).
        query(Row, all = _Item) -> query(Row);
        query(#row{id=Id}, memory = Item) ->
            probe_store:report(get(verbosity),trace,"probe","request ~p ~p",[Id,Item]),
            probe_store:fetch(Id,file_size);
        query(#row{id=Id}, Item) ->
            probe_store:report(get(verbosity),trace,"probe","request ~p ~p",[Id,Item]),
            probe_store:fetch(Id,Item).
        query(Id) -> Id.
        """;

    internal static Task Run()
    {
        object[] body = Body(Source);
        object[] guard = Body(Source.Replace("memory = Item) ->", "memory = Item) when is_atom(Id) ->", StringComparison.Ordinal));
        object[] field = Body(Source.Replace("query(#row{id=Id}, Item)", "query(#row{extra=Id}, Item)", StringComparison.Ordinal));
        if (!HasSplitPrefix(body) || HasSplitPrefix(guard) || HasSplitPrefix(field))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var widths = body.Where(instruction => Opcode(instruction) == AllocateHeap)
            .Select(instruction => Operands(instruction).Select(Value).ToArray()).ToArray();
        var tails = body.Where(instruction => Opcode(instruction) == TailExternalCallWithFrame)
            .Select(instruction => Value(Operands(instruction)[2])).ToArray();
        object[] lists = body.Where(instruction => Opcode(instruction) == PutList).ToArray();
        if (widths.Length != 2 || !widths[0].SequenceEqual([1, 2, 1]) || !widths[1].SequenceEqual([2, 4, 2])
            || !tails.SequenceEqual([1, 2]) || lists.Length != 3 || (string)Property(Operands(lists[0])[1], "Kind") != "literal")
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static bool HasSplitPrefix(object[] body)
    {
        byte[] expected =
        [
            ExactEqualTest, TailLocalCall, Label, IsTaggedTuple, GetTupleElement, ExactEqualTest, AllocateHeap
        ];

        return body.Take(expected.Length).Select(Opcode).SequenceEqual(expected)
            && Value(Operands(body[4])[0]) == 0 && Value(Operands(body[4])[2]) == 0;
    }

    private static object[] Body(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var name, var arity] && atoms[Value(name)] == "query" && Value(arity) == 2
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return instructions.Skip(entry + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

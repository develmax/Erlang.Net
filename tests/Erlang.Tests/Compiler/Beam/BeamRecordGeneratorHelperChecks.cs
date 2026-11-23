using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordGeneratorHelperChecks
{
    internal const string TestName = "compiler/beam/lowers-filtered-record-list-generators-to-recursive-helpers";
    private const string Fixture = "beam_record_generator_probe.erl";
    private const string MissingHelper = "The filtered record field needs its own recursive list helper.";
    private const string TupleRecreated = "The recursive helper rebuilt the matched tuple.";
    private const string Source = """
        -module(beam_record_generator_probe).
        -export([select/1]).
        -record(box, {items = []}).
        select(Box) ->
            [{K,V} || {K,V} <- Box#box.items, K =/= skip].
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int helper = Array.FindIndex(
            instructions,
            item => Opcode(item) == FunctionInfo
                && atoms[Value(Items(item, "Operands").ElementAt(1))] == "-select/1-lc$^0/1-0-"
        );
        if (helper < 0)
        {
            throw new InvalidOperationException(MissingHelper);
        }

        object[] body = instructions.Skip(helper + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        if (body.Count(item => Opcode(item) == Allocate && Value(Items(item, "Operands").First()) == 1) != 1
            || body.Count(item => Opcode(item) == PutTuple) != 1)
        {
            throw new InvalidOperationException(TupleRecreated);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

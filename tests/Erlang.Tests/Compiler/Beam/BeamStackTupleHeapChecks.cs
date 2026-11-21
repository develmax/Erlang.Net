using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamStackTupleHeapChecks
{
    internal const string TestName = "compiler/beam/reserves-heap-before-projecting-a-stack-rooted-tuple";
    private const string IncorrectRoots = "The heap check must retain X sources and may reserve before reading a tuple rooted in Y.";
    private const string Source = """
        -module(y_tuple_heap_probe).
        -export([nonterminal/1, x_root/1]).
        -record(payload,{marker,data}).
        nonterminal(Row=#payload{}) ->
            remember(Row#payload.marker),
            Result = {wrapped,Row#payload.data},
            remember(Result),
            Result.
        x_root(Row=#payload{}) -> {wrapped,Row#payload.data}.
        remember(Value) -> erlang:put(last_tuple_heap,Value).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "y_tuple_heap_probe.erl"));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Verify("nonterminal", "y", 0);
        Verify("x_root", "x", 1);

        return Task.CompletedTask;

        void Verify(string name, string source, int count)
        {
            int info = Array.FindIndex(
                instructions,
                i => Opcode(i) == FunctionInfo && atoms[(int)Number(i, 1)] == name
            );
            object[] body = instructions.Skip(info + 1).TakeWhile(i => Opcode(i) != FunctionInfo).ToArray();
            int check = Array.FindIndex(body, i => Opcode(i) == TestHeap);
            if (info < 0 || check < 0 || check + 1 >= body.Length
                || Number(body[check], 1) != count || Opcode(body[check + 1]) != GetTupleElement
                || (string)Property(Items(body[check + 1], "Operands").First(), "Kind") != source)
            {
                throw new InvalidOperationException(IncorrectRoots);
            }
        }
    }

    private static BigInteger Number(object instruction, int index) =>
        (BigInteger)Property(Items(instruction, "Operands").ElementAt(index), "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}

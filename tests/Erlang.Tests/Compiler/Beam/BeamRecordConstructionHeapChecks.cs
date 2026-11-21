using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecordConstructionHeapChecks
{
    internal const string TestName = "compiler/beam/coalesces-record-update-and-construction-heap-checks";
    private const string Fixture = "beam_record_heap_regions_probe.erl";
    private const string WrongHeap = "The record update and its surrounding construction reserved the wrong heap regions.";
    private const string WrongResult = "The record construction changed a value or error.";

    private const string Source = """
        -module(beam_record_heap_regions_probe).
        -export([positive/2, barrier/2, guarded/2]).
        -record(row,{first,second,third}).
        positive(Record, Value) ->
            Updated = Record#row{first=Value},
            {Updated,[Value],[Record]}.
        barrier(Record, Value) ->
            Updated = Record#row{first=Value},
            erlang:put(record_heap,Updated),
            {Updated,[Value],[Record]}.
        guarded(#row{}=Record, Value) ->
            Updated = Record#row{first=Value},
            {Updated,[Value],[Record]};
        guarded(_, _) -> fallback.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckHeap(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term row = Term.A("row");
            Term before = Term.A("before");
            Term after = Term.A("after");
            Term none = Term.A("none");
            Term original = Term.Tuple(
                row,
                before,
                none,
                none
            );
            Term updated = Term.Tuple(
                row,
                after,
                none,
                none
            );
            Term expected = Term.Tuple(updated, Term.List(after), Term.List(original));
            if (!expected.Equals(await module.Invoke(
                context,
                "positive",
                original,
                after
            ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "barrier",
                    original,
                    after
                ))
                || !(await module.Invoke(
                    context,
                    "guarded",
                    none,
                    after
                )).Equals(Term.A("fallback")))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "positive",
                    none,
                    after
                );
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.Tuple(Term.A("badrecord"), none)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void CheckHeap(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("positive", [13]);
        Check("barrier", [5, 8]);
        Check("guarded", [13]);

        void Check(string function, int[] expected)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == function
            );
            int[] sizes = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) is TestHeap or AllocateHeap)
                .Select(instruction => (int)Value(Children(instruction, "Operands").ElementAt(Opcode(instruction) == TestHeap ? 0 : 1)))
                .ToArray();
            if (!sizes.SequenceEqual(expected))
            {
                throw new InvalidOperationException(WrongHeap);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

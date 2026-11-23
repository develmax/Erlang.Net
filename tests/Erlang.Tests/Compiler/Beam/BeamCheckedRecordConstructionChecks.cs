using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedRecordConstructionChecks
{
    internal const string TestName = "compiler/beam/constructs-with-checked-record-projections-in-x-registers";
    private const string Fixture = "beam_checked_record_construction.erl";
    private const string WrongCode = "A checked record construction did not keep its projections and results in X registers.";
    private const string WrongResult = "A checked record construction changed its result or badrecord reason.";

    private const string Source = """
        -module(beam_checked_record_construction).
        -export([prefixed/1, reversed/1, duplicate/1, effect/1]).
        -record(row, {head, tail, extra}).

        prefixed(R) ->
            [{tag,R#row.head}|R#row.tail].
        reversed(R) -> {R#row.extra,R#row.head,R#row.tail}.
        duplicate(R) -> {R#row.tail,{R#row.tail,R#row.extra}}.
        effect(R) -> {R#row.head, erlang:put(record_effect, marked), R#row.tail}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        AssertOperations(
            "prefixed",
            beam,
            [IsTaggedTuple, TestHeap, GetTupleElement, GetTupleElement, PutTuple, PutList, Return, BadRecord],
            5
        );
        AssertOperations(
            "reversed",
            beam,
            [IsTaggedTuple, TestHeap, GetTupleElement, GetTupleElement, GetTupleElement, PutTuple, Return, BadRecord],
            4
        );
        AssertOperations(
            "duplicate",
            beam,
            [IsTaggedTuple, TestHeap, GetTupleElement, GetTupleElement, PutTuple, PutTuple, Return, BadRecord],
            6
        );

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term head = Term.A("head");
            Term tail = Term.Tuple(Term.A("improper"));
            Term extra = Term.I(17);
            Term row = Term.Tuple(
                Term.A("row"),
                head,
                tail,
                extra
            );
            if (!(await image.Invoke(context, "prefixed", row)).Equals(new Cons(Term.Tuple(Term.A("tag"), head), tail))
                || !(await image.Invoke(context, "reversed", row)).Equals(Term.Tuple(extra, head, tail))
                || !(await image.Invoke(context, "duplicate", row)).Equals(Term.Tuple(tail, Term.Tuple(tail, extra)))
                || !(await image.Invoke(context, "effect", row)).Equals(Term.Tuple(head, Term.A("undefined"), tail)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            Term invalid = Term.Tuple(
                Term.A("other"),
                head,
                tail,
                extra
            );
            try
            {
                await image.Invoke(context, "prefixed", invalid);
            }
            catch (ErlangException error) when (Term.Tuple(Term.A(RecordAtoms.BadRecord), invalid).Equals(error.Reason))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void AssertOperations(
        string name,
        byte[] beam,
        byte[] expected,
        int heapSize
    )
    {
        object code = Code(beam);
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int entry = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
            && atoms[(int)Value(ReadOperands(item)[1])] == name);
        byte[] included = instructions[(entry + 1)..].TakeWhile(item => Opcode(item) != FunctionInfo)
            .Where(item => Opcode(item) is not (Label or Line)).Select(Opcode).ToArray();
        object[] heap = ReadOperands(instructions.Skip(entry).First(item => Opcode(item) == TestHeap));
        if (entry < 0 || !included.SequenceEqual(expected) || Value(heap[0]) != heapSize || Value(heap[1]) != 1)
        {
            throw new InvalidOperationException(WrongCode);
        }
    }

    private static object[] ReadOperands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static System.Numerics.BigInteger Value(object operand) =>
        (System.Numerics.BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}

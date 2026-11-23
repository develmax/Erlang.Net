using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedDictionaryRecordTailChecks
{
    internal const string TestName = "compiler/beam/validates-record-field-guards-before-dictionary-tail-frame";
    private const string Fixture = "beam_guarded_dictionary_record_tail_probe.erl";
    private const string WrongLayout = "The checked element guard must fail with the original arguments before the successful arm allocates.";

    private const string Source = """
        -module(beam_guarded_dictionary_record_tail_probe).
        -export([write/2, write_and_ack/2]).
        -record(row,{id,kind}).
        write(#row{id=Id,kind=Kind}, Rec) when is_tuple(Rec) andalso element(1, Rec) =:= Kind ->
            probe_store:report(get(verbosity),trace,"probe","write ~p",[Id]),
            probe_store:insert(Id,Rec).
        write_and_ack(#row{id=Id,kind=Kind}, Rec) when is_tuple(Rec) andalso element(1, Rec) =:= Kind ->
            probe_store:report(get(verbosity),trace,"probe","write ~p",[Id]),
            probe_store:insert(Id,Rec),
            ok.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object bad = Code(CodeGeneration.CompileBeamModule(
            Source.Replace("element(1, Rec)", "element(1, Kind)", StringComparison.Ordinal),
            Fixture
        ));
        object[] direct = Body(code, "write");
        object[] acknowledged = Body(code, "write_and_ack");
        if (!HasEntryPlan(direct) || !HasEntryPlan(acknowledged)
            || HasEntryPlan(Body(bad, "write")) || HasEntryPlan(Body(bad, "write_and_ack"))
            || direct.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != 1
            || acknowledged.Count(instruction => Opcode(instruction) == TrimFrame) != 1)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;
    }

    private static bool HasEntryPlan(object[] body)
    {
        byte[] prefix = [IsTaggedTuple, IsTuple, BifTwo, GetTupleElement, ExactEqualTest, AllocateHeap];
        if (!body.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix))
        {
            return false;
        }

        int failure = Value(Operands(body[0])[0]);
        object[] bif = Operands(body[2]);

        return Value(Operands(body[1])[0]) == failure
            && Value(bif[0]) == failure && Value(bif[3]) == 1 && Value(bif[4]) == 2
            && Property(bif[3], "TypeIndex") is int type && type > 0
            && Value(Operands(body[3])[0]) == 0 && Value(Operands(body[3])[2]) == 3
            && Value(Operands(body[4])[0]) == failure
            && Operands(body[5]).Select(Value).SequenceEqual([2, 2, 2]);
    }

    private static object[] Body(object code, string function)
    {
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var name, var arity] && atoms[Value(name)] == function && Value(arity) == 2
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

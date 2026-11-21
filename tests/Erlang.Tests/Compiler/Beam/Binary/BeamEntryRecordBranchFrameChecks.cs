using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryRecordBranchFrameChecks
{
    internal const string TestName = "compiler/beam/frames-the-continuing-record-branch";
    private const string Fixture = "entry_record_guard_probe.erl";
    private const string IncorrectBranches = "The checked record predicate must stay above the continuation's allocation.";
    private const string Source = """
        -module(entry_record_guard_probe).
        -export([run/1, calls_both/1]).
        -record(row,{method,data}).

        run(Info) ->
            case Info#row.method of
                selected ->
                    case selected(Info) of
                        true -> {proceed,Info#row.data};
                        false -> finish(Info)
                    end;
                _ -> {proceed,Info#row.data}
            end.

        calls_both(Info) ->
            case Info#row.method of
                selected -> {selected,selected(Info),Info#row.data};
                _ -> {rejected,selected(Info),Info#row.data}
            end.

        selected(Info) ->
            proplists:get_value(flag,Info#row.data,false) =:= true.

        finish(Info) -> {finished,Info#row.data}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        object[] branch = Body("run");
        byte[] branchOpcodes = branch.Select(Opcode).ToArray();
        int tag = Array.FindIndex(branchOpcodes, opcode => opcode == IsTaggedTuple);
        if (tag < 0 || !branchOpcodes.Skip(tag).Take(4)
            .SequenceEqual(new byte[] { IsTaggedTuple, GetTupleElement, ExactEqualTest, Allocate })
            || !ReadRejectingArm(branch, tag + 2).Select(Opcode)
                .SequenceEqual(new byte[] { TestHeap, GetTupleElement, PutTuple, Return }))
        {
            throw new InvalidOperationException(IncorrectBranches);
        }

        object[] both = Body("calls_both");
        int predicate = Array.FindIndex(both, instruction => Opcode(instruction) == ExactEqualTest);
        if (predicate < 0 || !ReadRejectingArm(both, predicate).Any(instruction =>
            Opcode(instruction) is LocalCall or ExternalCall or TailLocalCallWithFrame or TailExternalCallWithFrame))
        {
            throw new InvalidOperationException(IncorrectBranches);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static IEnumerable<object> ReadRejectingArm(object[] body, int predicate)
    {
        BigInteger failure = (BigInteger)Property(Items(body[predicate], "Operands").First(), "Value");
        int start = Array.FindIndex(
            body,
            instruction => Opcode(instruction) == Label
            && (BigInteger)Property(Items(instruction, "Operands").Single(), "Value") == failure
        );

        return body.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != Label);
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}

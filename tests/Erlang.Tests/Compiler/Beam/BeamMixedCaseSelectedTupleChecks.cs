using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedCaseSelectedTupleChecks
{
    internal const string TestName = "compiler/beam/reuses-the-checked-tuple-of-a-mixed-terminal-case";
    private const int TupleType = 0x0800;
    private const string WrongDispatch = "The selected tuple should remain in X0 after the disjoint tuple check.";
    private const string WrongPayload = "An arm that changes or observes the payload must retain its projection.";
    private const string Source = """
        -module(beam_mixed_case_selected_error).
        -export([same/1, changed/1, with_guard/2, with_effect/1]).

        same(Input) ->
            case mixed_case_provider:read(Input) of
                ok -> {ok, Input};
                {error, Reason} -> {error, Reason}
            end.

        changed(Input) ->
            case mixed_case_provider:read(Input) of
                ok -> {ok, Input};
                {error, Reason} -> {changed, Reason}
            end.

        with_guard(Input, Filter) ->
            case mixed_case_provider:read(Input) of
                ok -> {ok, Input};
                {error, Reason} when Reason =:= Filter -> {error, Reason}
            end.

        with_effect(Input) ->
            case mixed_case_provider:read(Input) of
                ok -> {ok, Input};
                {error, Reason} ->
                    mixed_case_provider:observe(Reason),
                    {error, Reason}
            end.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_mixed_case_selected_error.erl");
        _ = BeamModule.Prepare(image);
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        byte[] same = ReadOpcodes("same");
        int tupleTest = Array.IndexOf(same, IsTuple);
        int tagTest = Array.IndexOf(same, IsTaggedTuple);
        int atomTest = Array.IndexOf(same, ExactEqualTest);
        int? type = BeamAdjacentNumericResultChecks.InstructionInputType(
            image,
            IsTaggedTuple,
            1,
            first: true
        );
        if (tupleTest < 0 || tagTest <= tupleTest || atomTest <= tagTest || type != TupleType
            || same.Contains(GetTupleElement) || same.Count(op => op == PutTuple) != 1)
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        if (!ReadOpcodes("changed").Contains(GetTupleElement)
            || !ReadOpcodes("with_guard").Contains(GetTupleElement)
            || !ReadOpcodes("with_effect").Contains(GetTupleElement))
        {
            throw new InvalidOperationException(WrongPayload);
        }

        return Task.CompletedTask;

        byte[] ReadOpcodes(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, _]
                    && atoms[(int)(BigInteger)Property(name, "Value")] == function
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongDispatch);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}

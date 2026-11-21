using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListOrTaggedResultChecks
{
    internal const string TestName = "compiler/beam/checks-only-tuple-type-after-closed-list-or-tagged-result";
    private const string SourceName = "beam_list_or_tagged_result_probe.erl";
    private const string WrongType = "A closed list-or-tagged result must use its typed tuple check.";
    private const string WrongTag = "The mixed tuple tags must keep the exact tag test.";
    private const string WrongAtomResult = "Only the closed protected atom result can use a typed tuple test.";

    private const string Source = """
        -module(beam_list_or_tagged_result_probe).
        -export([decode/1, control/1, decode_atom/1, decode_case/1, control_shape/1, control_reason/1]).

        decode(Mode) ->
            case read(Mode) of
                {error, Reason} -> {failure, Reason};
                Result -> {ok,Result}
            end.

        control(Mode) ->
            case mixed(Mode) of
                {error, Reason} -> {failure, Reason};
                Result -> {ok,Result}
            end.

        read(empty) -> [];
        read(accepted) -> [one,two];
        read(next) -> read(accepted);
        read(_) -> {error,closed}.

        mixed(empty) -> [];
        mixed(accepted) -> [one,two];
        mixed(conflict) -> {other,valid};
        mixed(_) -> {error,closed}.

        decode_atom(Input) ->
            case read_atom(Input) of
                {error, Reason} -> {failure, Reason};
                Result -> {ok, Result}
            end.

        decode_case(Input) ->
            case read_case(Input) of
                {error, Reason} -> {failure, Reason};
                Result -> {ok, Result}
            end.

        control_shape(Input) ->
            case read_shape(Input) of
                {error, Reason} -> {failure, Reason};
                Result -> {ok, Result}
            end.

        control_reason(Input) ->
            case read_reason(Input) of
                {error, Reason} -> {failure, Reason};
                Result -> {ok, Result}
            end.

        read_atom(Input) ->
            try erlang:element(1, Input) of
                retry -> read_atom({done});
                done -> none;
                Error = {error, _} -> Error
            catch
                _:Reason -> {error, Reason}
            end.

        read_case(Input) ->
            case Input of
                Error = {error, _} -> Error;
                _ -> none
            end.

        read_shape(wrong_tag) -> {other, invalid};
        read_shape(wrong_arity) -> {error, invalid, extra};
        read_shape(Input) -> read_atom(Input).

        read_reason(Input) ->
            try erlang:apply(Input, []) of
                done -> none;
                {error, _} = Error -> Error
            catch
                _:Reason -> Reason
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, SourceName));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] decode = Body("decode");
        if (!decode.Any(instruction => Opcode(instruction) == IsTuple
            && Items(instruction, "Operands").ToArray() is [_, var operand]
            && Property(operand, "TypeIndex") is int))
        {
            throw new InvalidOperationException(WrongType);
        }

        if (!Body("control").Any(instruction => Opcode(instruction) == IsTaggedTuple))
        {
            throw new InvalidOperationException(WrongTag);
        }

        if (!HasTypedTupleTest("decode_atom") || !HasTypedTupleTest("decode_case")
            || !Body("control_shape").Any(instruction => Opcode(instruction) == IsTaggedTuple)
            || !Body("control_reason").Any(instruction => Opcode(instruction) == IsTaggedTuple))
        {
            throw new InvalidOperationException(WrongAtomResult);
        }

        return Task.CompletedTask;

        bool HasTypedTupleTest(string function) => Body(function).Any(instruction =>
            Opcode(instruction) == IsTuple
            && Items(instruction, "Operands").ToArray() is [_, var result]
            && Property(result, "TypeIndex") is int);

        object[] Body(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

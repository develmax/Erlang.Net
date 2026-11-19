using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedCallResultCleanupChecks
{
    private const string LostSelector = "Each call selector must be stored before its test and cleared on the first success edge.";
    private const string ChangedProtectedCleanup = "A protected cleanup must retain its exception handling.";

    internal static void Check()
    {
        const string source = """
            -module(beam_nested_call_result_cleanup).
            -export([run/2,caught/2]).
            run(A,B) ->
                Keys = [one,two],
                case dep:read(A,Keys) of
                    {found,Value} ->
                        case dep:write(B,Value) of
                            accept -> {saved,B};
                            Inner ->
                                dispose(B), Inner
                        end;
                    Outer ->
                        dispose(B), Outer
                end.
            caught(A,B) ->
                case dep:read(A,keys) of
                    {found,Value} ->
                        case dep:write(B,Value) of
                            accept -> {saved,B};
                            Inner ->
                                (catch dispose(B)), Inner
                        end;
                    Outer ->
                        dispose(B), Outer
                end.
            dispose(B) -> dep:dispose(B).
            """;
        byte[] image = CodeGeneration.CompileBeamModule(source, "beam_nested_call_result_cleanup.erl");
        byte[] code = BeamDocumentationEmissionChecks.Chunks(image).Single(chunk => chunk.Id == "Code").Data;
        ReadOnlySpan<byte> firstSpill = [ExternalCall, 0x20, 0, Move, 0x03, 0x04, IsTaggedTuple];
        ReadOnlySpan<byte> projectionAndClear =
        [
            GetTupleElement, 0x03, 0x10, 0x13,
            InitYRegisters, ExtendedListOperand, 0x10, 0x04,
            Move, 0x14, 0x03
        ];
        ReadOnlySpan<byte> secondSpill = [ExternalCall, 0x20, 0x10, Move, 0x03, 0x04, ExactEqualTest];
        if (code.AsSpan().IndexOf(firstSpill) < 0 || code.AsSpan().IndexOf(projectionAndClear) < 0
            || code.AsSpan().IndexOf(secondSpill) < 0)
        {
            throw new InvalidOperationException(LostSelector);
        }

        object instructions = Property(Code(image), "Instructions");
        if (!((IEnumerable)instructions).Cast<object>().Any(instruction =>
            (byte)Property(Property(instruction, "Definition"), "Opcode") is CatchEnter or TryEnter))
        {
            throw new InvalidOperationException(ChangedProtectedCleanup);
        }
    }
}

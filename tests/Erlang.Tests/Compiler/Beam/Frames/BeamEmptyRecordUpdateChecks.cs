using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEmptyRecordUpdateChecks
{
    internal const string TestName = "compiler/beam/empty-record-update-preserves-check";
    private const string MissingRecordCheck = "An empty update must still check the source record.";
    private const string UnexpectedUpdate = "An empty update emitted a field write.";
    private const string Source = """
        -module(beam_empty_record_update).
        -export([checked/1, unchecked/1, supplied/1]).
        -record(state,{first,second}).
        checked(R=#state{}) ->
            {ok,R#state{}}.
        unchecked(R) ->
            {ok,R#state{}}.
        supplied(F) ->
            {ok,(F())#state{}}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_empty_record_update.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>().ToArray();
        if (instructions.Any(instruction => Opcode(instruction) == UpdateRecord))
        {
            throw new InvalidOperationException(UnexpectedUpdate);
        }

        foreach (string name in new[] { "unchecked", "supplied" })
        {
            object function = functions.Single(candidate => (string)Property(candidate, "Name") == name);
            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            if (!instructions[start..end].Any(instruction => Opcode(instruction) == IsTaggedTuple))
            {
                throw new InvalidOperationException(MissingRecordCheck);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}

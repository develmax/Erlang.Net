using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDominatingRecordGuardChecks
{
    internal const string TestName = "compiler/beam/uses-dominating-record-guard-for-field-access";
    private const string Fixture = "beam_dominating_record_guard.erl";
    private const string WrongChecks = "Expected only the record checks that can fail on this branch.";
    private const string Source = """
        -module(beam_dominating_record_guard).
        -export([guarded/1, unguarded/1]).
        -record(entry, {kind, payload, name}).
        -record(inner, {value}).

        guarded([]) -> [];
        guarded([#entry{kind = explicit, payload = #inner{value = Value}, name = Name}|Tail]) ->
            [{Name, Value} | guarded(Tail)];
        guarded([#entry{kind = guarded, payload = Data, name = Name}|Tail])
          when is_record(Data, inner) ->
            [{Name, Data#inner.value} | guarded(Tail)];
        guarded([#entry{payload = undefined, name = Name}|Tail]) ->
            [{Name, undefined} | guarded(Tail)];
        guarded([_|Tail]) ->
            guarded(Tail).

        unguarded([]) -> [];
        unguarded([#entry{kind = unguarded, payload = Data, name = Name}|Tail]) ->
            [{Name, Data#inner.value} | unguarded(Tail)];
        unguarded([_|Tail]) ->
            unguarded(Tail).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>();

        foreach (var function in functions)
        {
            string name = (string)Property(function, "Name");
            if (name is not ("guarded" or "unguarded"))
            {
                continue;
            }

            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            bool hasBadRecord = instructions[start..end].Any(instruction =>
                (byte)Property(Property(instruction, "Definition"), "Opcode") == BadRecord);
            if (hasBadRecord != (name == "unguarded"))
            {
                throw new InvalidOperationException(WrongChecks);
            }
        }

        return Task.CompletedTask;
    }
}

using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamExhaustiveLocalCaseChecks
{
    internal const string TestName = "compiler/beam/removes-failure-for-an-exhaustive-local-tagged-case";
    private const string Fixture = "beam_exhaustive_local_case.erl";
    private const string WrongChecks = "Expected case failures only for selectors whose result is not covered by the clauses.";
    private const string Source = """
        -module(beam_exhaustive_local_case).
        -export([covered/2, unknown/2, bound/2]).
        covered(Key, Values) ->
            case erlang:phash2(Key) of
                0 -> skipped;
                _ ->
                    case tagged(Values) of
                        {yes, Value} -> {found, Value};
                        no -> missing
                    end
            end.
        unknown(Key, Values) ->
            case erlang:phash2(Key) of
                0 -> skipped;
                _ ->
                    case loose(Values) of
                        {yes, Value} -> {found, Value};
                        no -> missing
                    end
            end.
        bound(Key, Values) ->
            case erlang:phash2(Key) of
                0 -> skipped;
                _ ->
                    case tagged(Values) of
                        {yes, Key} -> found;
                        no -> missing
                    end
            end.
        tagged([]) -> no;
        tagged([{yes, Data}|_]) -> {yes, Data};
        tagged([_|Rest]) -> tagged(Rest).
        loose([]) -> no;
        loose([{yes, Data}|_]) -> {yes, Data};
        loose([_|Rest]) -> loose(Rest);
        loose(Value) -> Value.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>();

        foreach (var function in functions)
        {
            string name = (string)Property(function, "Name");
            if (name is not ("covered" or "unknown" or "bound"))
            {
                continue;
            }

            int start = (int)Property(function, "FirstInstructionIndex");
            int end = (int)Property(function, "EndInstructionIndex");
            bool hasCaseFailure = instructions[start..end].Any(instruction =>
                (byte)Property(Property(instruction, "Definition"), "Opcode") == CaseEnd);
            if (hasCaseFailure != (name != "covered"))
            {
                throw new InvalidOperationException(WrongChecks);
            }
        }

        return Task.CompletedTask;
    }
}

using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProjectedConstructionTailChecks
{
    internal const string TestName = "compiler/beam/shares-projected-tuple-construction-tails";
    private const string Fixture = "beam_projected_fallthrough.erl";
    private const string WrongReturn = "Identical projected tuple returns must share the later body and distinct returns must survive.";

    private const string Source = """
        -module(beam_projected_fallthrough).
        -export([same/1,different/1]).
        -record(opened, {path, token, state}).

        same(Path) ->
            case beam_return_provider:lookup(Path) of
                {error, Reason} -> {error, Reason};
                _ ->
                    case beam_return_provider:open(Path) of
                        {ok, Token} -> {ok, #opened{path=Path, token=Token, state=start}};
                        {repaired, Token, _, _} -> {ok, #opened{path=Path, token=Token, state=start}};
                        {error, Reason} -> {error, Reason}
                    end
            end.

        different(Path) ->
            case beam_return_provider:lookup(Path) of
                {error, Reason} -> {error, Reason};
                _ ->
                    case beam_return_provider:open(Path) of
                        {ok, Token} -> {ok, #opened{path=Path, token=Token, state=start}};
                        {repaired, Token, _, _} -> {ok, #opened{path=Path, token=Token, state=repaired}};
                        {error, Reason} -> {error, Reason}
                    end
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        if (CountHeapReservations(code, "same") != 1 || CountHeapReservations(code, "different") != 2)
        {
            throw new InvalidOperationException(WrongReturn);
        }

        return Task.CompletedTask;
    }

    private static int CountHeapReservations(object code, string name)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == name);
        int from = (int)Property(function, "FirstInstructionIndex");
        int until = (int)Property(function, "EndInstructionIndex");

        return Items(code, "Instructions")[from..until].Count(instruction =>
            (byte)Property(Property(instruction, "Definition"), "Opcode") == TestHeap
            && (BigInteger)Property(Items(instruction, "Operands")[0], "Value") == 8);
    }

    private static object[] Items(object receiver, string name) => ((IEnumerable)Property(receiver, name)).Cast<object>().ToArray();
}

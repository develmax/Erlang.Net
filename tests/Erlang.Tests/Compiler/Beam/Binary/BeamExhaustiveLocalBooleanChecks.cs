using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamExhaustiveLocalBooleanChecks
{
    internal const string TestName = "compiler/beam/folds-pure-boolean-dispatch-after-effectful-local-call";
    private const string Fixture = "local_boolean_call_probe.erl";
    private const string IncorrectSelector = "The local call must remain before its proven Boolean branch, with unmatched outcomes preserved.";
    private const string Source = """
        -module(local_boolean_call_probe).
        -export([route/1, guarded/2, raw/1, bound/2, bound_raw/2, bound_input/2]).
        -record(row,{method,data}).

        route(Row) ->
            case Row#row.method of
                selected ->
                    case has_response(Row) of
                        true -> {accepted,Row#row.data};
                        false -> finish(Row)
                    end;
                _ -> {ignored,Row#row.data}
            end.

        has_response(Row) ->
            erlang:put(last_key,Row#row.method),
            case proplists:get_value(status,Row#row.data) of
                {_,_,_} -> true;
                undefined ->
                    case proplists:get_value(response,Row#row.data) of
                        undefined -> false;
                        _ -> true
                    end
            end.

        finish(Row) -> {refused,Row#row.data}.

        guarded(Key, Gate) ->
            case flag(Key) of
                true when Gate =:= open -> {opened,Key};
                false -> {refused,Key}
            end.

        raw(Term) ->
            case opaque(Term) of
                true -> accepted;
                false -> refused
            end.

        flag(Key) ->
            erlang:put(last_key,Key),
            case Key of
                allowed -> true;
                _ -> false
            end.

        bound([],Key) -> {none,Key};
        bound(Data,Key) ->
            Verdict = negated(Data),
            Value = case Verdict of
                true -> {accepted,Data};
                false -> {refused,Data}
            end,
            {Key,Value}.

        bound_raw(Data,Key) ->
            Verdict = opaque(Data),
            Value = case Verdict of
                true -> {accepted,Data};
                false -> {refused,Data}
            end,
            {Key,Value}.

        bound_input(Verdict,Data) ->
            Verdict = negated(Data),
            Value = case Verdict of
                true -> {accepted,Data};
                false -> {refused,Data}
            end,
            {Data,Value}.

        negated([[Head|_]|_]) ->
            not is_integer(Head);
        negated(Data) when is_list(Data) ->
            false.

        opaque(Term) ->
            erlang:put(last_key,Term),
            Term.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string?>().ToArray();
        object[] route = Body("route");
        int called = Array.FindIndex(route, instruction => Opcode(instruction) == LocalCall);
        if (called < 0 || route.Count(instruction => Opcode(instruction) == LocalCall) != 1
            || !route.Skip(called + 1).Any(TestsTrue)
            || route.Any(instruction => Opcode(instruction) is SelectValue or CaseEnd)
            || !Body("guarded").Any(instruction => Opcode(instruction) == CaseEnd)
            || !Body("raw").Any(instruction => Opcode(instruction) == CaseEnd)
            || Body("bound").Count(instruction => Opcode(instruction) == LocalCall) != 1
            || !Body("bound").Any(TestsTrue)
            || Body("bound").Any(instruction => Opcode(instruction) == CaseEnd)
            || !Body("bound_raw").Any(instruction => Opcode(instruction) == CaseEnd)
            || !Body("bound_input").Any(instruction => Opcode(instruction) == BadMatch))
        {
            throw new InvalidOperationException(IncorrectSelector);
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

        bool TestsTrue(object instruction)
        {
            if (Opcode(instruction) != ExactEqualTest
                || Items(instruction, "Operands").ToArray() is not [_, _, var value]
                || (string)Property(value, "Kind") != "atom")
            {
                return false;
            }

            return atoms[(int)(BigInteger)Property(value, "Value")] == "true";
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object container, string name) =>
        ((IEnumerable)Property(container, name)).Cast<object>();
}

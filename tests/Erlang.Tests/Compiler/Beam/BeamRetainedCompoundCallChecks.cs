using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedCompoundCallChecks
{
    internal const string TestName = "compiler/beam/defers-the-nested-call-tuple-across-an-effect";
    private const string Fixture = "beam_retained_compound_call.erl";
    private const string WrongSchedule = "The entry list or call descriptor was allocated on the wrong side of the effect.";
    private const string WrongBranch = "The exact reroute or generic guarded branch has the wrong shape.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_retained_compound_call).
        -export([dispatch/4, guarded/4, delay/1, invoke/2, retry/2]).

        dispatch(A, B, C, D) ->
            M = target_mod,
            F = target_fun,
            Args = [A,B,C,D],
            Id = {M,A},
            Spec = {Id,{M,F,Args},light,?MODULE:delay(8),[]},
            case ?MODULE:invoke(key,Spec) of
                {error,retry} -> ?MODULE:retry(key,Id);
                Value -> Value
            end.

        guarded(A, B, C, D) ->
            M = other_m,
            F = other_f,
            Args = [A,B,C,D],
            Id = {M,A},
            Spec = {Id,{M,F,Args},heavy,?MODULE:delay(2),[]},
            case ?MODULE:invoke(key,Spec) of
                {error,Reason} when is_atom(Reason) -> ?MODULE:retry(key,Id);
                Value -> Value
            end.

        delay(N) ->
            observe(delay),
            case erlang:get(fail_delay) of true -> error({failed_delay,N}); _ -> N + 42 end.

        invoke(Key, Spec) ->
            observe(invoke),
            case erlang:get(result) of retry -> {error,retry}; reject -> {error,rejected}; _ -> {Key,Spec} end.

        retry(Key, Id) ->
            observe(retry),
            {retried,Key,Id}.

        observe(Event) ->
            case erlang:get(history) of undefined -> erlang:put(history,[Event]); L -> erlang:put(history,[Event|L]) end,
            ok.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] dispatch = Operations("dispatch");
        int firstCall = Array.FindIndex(dispatch, instruction => Opcode(instruction) == ExternalCall);
        int preCallTuple = dispatch.Take(firstCall).Count(instruction => Opcode(instruction) == PutTuple);
        if (firstCall < 0 || preCallTuple != 1
            || dispatch.Skip(firstCall + 1).TakeWhile(instruction => Opcode(instruction) != ExternalCall)
                .Count(instruction => Opcode(instruction) == PutTuple) != 2
            || dispatch.Take(firstCall).Count(instruction => Opcode(instruction) == PutList) != 4)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        if (dispatch.Count(instruction => Opcode(instruction) == ExactEqualTest) != 1
            || dispatch.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != 1
            || !Operations("guarded").Any(instruction => Opcode(instruction) == IsAtom))
        {
            throw new InvalidOperationException(WrongBranch);
        }

        return Task.CompletedTask;

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(Missing);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

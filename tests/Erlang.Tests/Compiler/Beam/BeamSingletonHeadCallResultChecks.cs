using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingletonHeadCallResultChecks
{
    internal const string TestName = "compiler/beam/emits-singleton-call-result-with-unframed-empty-default";
    private const string Fixture = "singleton_head_call_result_probe.erl";
    private const string WrongFrame = "The singleton entry did not check its tail before allocating a zero-slot frame.";
    private const string WrongFallback = "The empty list did not branch to a frameless local tail call.";

    private const string Source = """
        -module(singleton_head_call_result_probe).
        -export([init/1, only/1, will_fail/1]).
        init([]) -> init([default]);
        init([Input]) ->
            Restart = steady,
            Retries = 12,
            State = build(Input, []),
            {ok, {{Restart, Retries}, State}}.

        only([Input]) ->
            Rate = 48,
            State = build(Input, suffix),
            {read, {Rate, constant}, State}.

        will_fail([Input]) ->
            State = build(Input, failure),
            {state, State}.

        build(invalid, failure) -> erlang:error(rejected);
        build(Term, Option) -> {Term, Option}.

        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] fallback = CheckSingleton("init");
        object[] plain = CheckSingleton("only");
        if (fallback.Count(instruction => Opcode(instruction) == TailLocalCall) != 1
            || plain.Any(instruction => Opcode(instruction) is TailLocalCall or TailLocalCallWithFrame))
        {
            throw new InvalidOperationException(WrongFallback);
        }

        return Task.CompletedTask;

        object[] CheckSingleton(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[Value(Operands(instruction)[1])] == function
                    && Value(Operands(instruction)[2]) == 1
            );

            if (start < 0)
            {
                throw new InvalidOperationException(WrongFrame);
            }

            var operations = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
            if (operations.Select(Opcode).Take(4).ToArray() is not [IsNonemptyList, GetList, IsNil, Allocate]
                || operations.Count(instruction => Opcode(instruction) == Allocate) != 1
                || Operands(operations[3]) is not [var size, var live] || Value(size) != 0 || Value(live) != 2)
            {
                throw new InvalidOperationException(WrongFrame);
            }

            return operations;
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

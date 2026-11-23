using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSingletonListCallResultChecks
{
    internal const string TestName = "compiler/beam/builds-a-single-child-result-after-two-dependent-calls";
    private const string Fixture = "beam_singleton_list_call_result.erl";
    private const string WrongCalls = "A singleton list entry did not check its shape before the zero-slot frame and dependent calls.";
    private const string WrongConstructor = "The discarded literal prevented singleton argument propagation into the constructor.";

    private const string Source = """
        -module(beam_singleton_list_call_result).
        -export([init/1, retains/1]).

        -define(DIAGNOSTIC, ignored).

        init([Source]) ->
            ?DIAGNOSTIC,
            Value = read(Source),
            Flags = {mode,3,12},
            Child = wrap(wrapper, [], Value, [observer]),
            Children = [Child],
            {ready, {Flags,Children}}.

        wrap(Name, Args, Value, Modules) ->
            ?DIAGNOSTIC,
            {Name,{Name,launch,Args},Value,100,observer,[Name] ++ Modules}.

        retains([Source]) ->
            Value = read(Source),
            Child = both(Source, Value),
            {ready, {{mode,3,12}, [Child]}}.

        read(failure) -> erlang:error(read_failed);
        read(Source) ->
            erlang:put(read_value, Source),
            {value,Source}.

        both(Source, Value) ->
            erlang:put(wrapped_value, {Source, Value}),
            {Source,Value}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        object[] init = Operations("init");
        int tailCheck = Array.FindIndex(init, item => Opcode(item) == IsNil);
        int allocate = Array.FindIndex(init, item => Opcode(item) == Allocate);
        int firstCall = Array.FindIndex(init, item => Opcode(item) == LocalCall);
        var retains = Operations("retains");
        if (tailCheck < 0 || allocate <= tailCheck || firstCall <= allocate
            || Value(Operands(init[allocate])[0]) != 0 || Value(Operands(init[allocate])[1]) != 2
            || init.Count(item => Opcode(item) == LocalCall) != 2
            || !retains.Any(item => Opcode(item) == Allocate && Value(Operands(item)[0]) > 0))
        {
            throw new InvalidOperationException(WrongCalls);
        }

        var constructor = Operations("wrap");
        if (constructor.Count(item => Opcode(item) == PutTuple) != 1
            || !constructor.Any(item => Opcode(item) == PutList && Operands(item) is [var head, _, _]
                && (string)Property(head, "Kind") == BeamBinaryHostNames.AtomKind && atoms[Value(head)] == "wrapper"))
        {
            throw new InvalidOperationException(WrongConstructor);
        }

        return Task.CompletedTask;

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(WrongCalls);
        }
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

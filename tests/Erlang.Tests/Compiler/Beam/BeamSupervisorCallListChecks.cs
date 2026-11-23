using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSupervisorCallListChecks
{
    internal const string TestName = "compiler/beam/keeps-supervisor-call-results-and-badmatch-frames";
    private const string Fixture = "beam_supervisor_call_list.erl";
    private const string WrongList = "The constant argument calls did not retain reverse stack ownership for the result list.";
    private const string WrongBranches = "A closed selector or wrapped response missed its branch-local lowering.";
    private const string WrongChecks = "The second checked call retained a dead stack slot or changed its failure test.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_supervisor_call_list).
        -export([init/1,start/2,stop_child/1,invoke/3,release/2,forget/2,literal_tail/0]).

        init([]) -> specs();
        init([[]]) -> specs();
        init(Other) -> {rejected,{invalid,Other}}.

        specs() ->
            Modes = {group,7,8},
            Entries = [item(first), item(second), tag(worker,[sidecar]), tag(last,[sidecar])],
            {ready,{Modes,Entries}}.

        literal_tail() ->
            Flags = {group,8,9},
            Entries = [item(first),tag(last,[sidecar])],
            {discarded,{Flags,Entries}},
            kept.

        item(Name) -> {item,Name}.
        tag(Name, Flags) -> {tagged,Name,Flags}.

        start(selected, Payload) ->
            Label = {configured,controller},
            case ?MODULE:invoke(Label, controller, [Payload]) of
                {started,Ref} -> {active,Ref,{selected,Payload}};
                Other -> Other
            end;
        start(_,_) -> {error,unsupported_mode}.

        stop_child(Payload) ->
            accepted = ?MODULE:release(handler,Payload),
            finished = ?MODULE:forget(handler,Payload).

        invoke(Label, Controller, [Payload]) ->
            visit({invoke,Label,Controller,Payload}),
            case Payload of refused -> {error,refused}; _ -> {started,synthetic} end.

        release(Target, Payload) ->
            visit({release,Target,Payload}),
            case Payload of fail_release -> {error,release}; _ -> accepted end.

        forget(Target, Payload) ->
            visit({forget,Target,Payload}),
            case Payload of fail_forget -> {error,forget}; _ -> finished end.

        visit(Event) ->
            case erlang:get(history) of
                undefined -> erlang:put(history,[Event]);
                Previous -> erlang:put(history,[Event|Previous])
            end,
            ok.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] specs = Operations("specs", 0);
        int[] saved = specs.Where(instruction => Opcode(instruction) == Move
            && Operands(instruction) is [_, var target] && Property(target, "Kind").Equals("y"))
            .Select(instruction => Value(Operands(instruction)[1])).ToArray();
        if (saved is not [2, 1, 0] || specs.Count(instruction => Opcode(instruction) == PutList) != 4)
        {
            throw new InvalidOperationException(WrongList);
        }

        object[] init = Operations("init", 1);
        object[] start = Operations("start", 2);
        int list = Array.FindIndex(start, instruction => Opcode(instruction) == PutList);
        int firstLiteralMove = Array.FindIndex(
            start,
            instruction => Opcode(instruction) == Move
            && Operands(instruction) is [var value, _] && Property(value, "Kind").Equals("literal")
        );
        if (!init.Any(instruction => Opcode(instruction) == ExactNotEqualTest)
            || !init.Any(instruction => Opcode(instruction) == IsNil)
            || init.Any(instruction => Opcode(instruction) == Allocate)
            || init.Count(instruction => Opcode(instruction) == TailLocalCall) != 1
            || list < 0 || firstLiteralMove <= list
            || start.Count(instruction => Opcode(instruction) == Return) != 3)
        {
            throw new InvalidOperationException(WrongBranches);
        }

        object[] stop = Operations("stop_child", 1);
        if (!stop.Any(instruction => Opcode(instruction) == TrimFrame)
            || stop.Count(instruction => Opcode(instruction) == ExactEqualTest) != 2
            || stop.Count(instruction => Opcode(instruction) == BadMatch) != 2)
        {
            throw new InvalidOperationException(WrongChecks);
        }

        return Task.CompletedTask;

        object[] Operations(string name, int arity)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );

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

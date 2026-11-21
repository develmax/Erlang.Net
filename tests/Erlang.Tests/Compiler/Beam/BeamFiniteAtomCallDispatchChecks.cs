using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFiniteAtomCallDispatchChecks
{
    internal const string TestName = "compiler/beam/narrows-private-entry-and-singleton-call-dispatch";
    private const string Fixture = "beam_finite_atom_call_dispatch.erl";
    private const string WrongCase = "A proven case did not retain just the producer and selected arm.";
    private const string WrongEntry = "A private entry did not use its proven atom domain.";
    private const string WrongGroupedGuard = "Disjoint atom guards lost their grouped switch or the list guard lost its original input.";

    private const string Source = """
        -module(beam_finite_atom_call_dispatch).
        -export([dispatch/1, attach/1, wide/1, unknown/1]).

        dispatch(Mode) when Mode == global; Mode == local ->
            forward(Mode, default, pending);
        dispatch(Marker) when Marker == ready ->
            forward(global, default, Marker);
        dispatch(Settings) when is_list(Settings), is_integer(hd(Settings)) ->
            forward(global, Settings, pending).

        forward(Mode, Settings, ready) ->
            erlang:put(Mode, Settings);
        forward(Mode, Settings, pending) ->
            Marker = choose(),
            forward(Mode, Settings, Marker).

        attach(Flags) ->
            case choose() of
                ready -> erlang:put(Flags, {callback, start, []})
            end.

        wide(Mode) -> unproven(global, default, Mode).

        unproven(Mode, Settings, ready) ->
            erlang:put(Mode, Settings);
        unproven(Mode, Settings, pending) ->
            Marker = choose(),
            unproven(Mode, Settings, Marker).

        unknown(Flags) ->
            case erlang:get(dynamic_choice) of
                ready -> erlang:put(Flags, {callback, start, []})
            end.

        choose() ->
            erlang:put(selector_called, true),
            fail_when_requested(),
            ready.

        fail_when_requested() ->
            case erlang:get(selector_failure) of
                true -> erlang:error(selector_failed);
                _ -> ok
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        object[] attach = Operations("attach");
        if (attach.Count(instruction => Opcode(instruction) == LocalCall) != 1
            || attach.Any(instruction => Opcode(instruction) == CaseEnd)
            || !Operations("unknown").Any(instruction => Opcode(instruction) == CaseEnd))
        {
            throw new InvalidOperationException(WrongCase);
        }

        object[] forward = Operations("forward");
        if (forward.Count(instruction => Opcode(instruction) == ExactEqualTest) != 1
            || !HasAtomMove(forward, "ready", 2)
            || Operations("unproven").Count(instruction => Opcode(instruction) == ExactEqualTest) != 2)
        {
            throw new InvalidOperationException(WrongEntry);
        }

        object[] dispatch = Operations("dispatch");
        object? select = dispatch.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
        object? head = dispatch.SingleOrDefault(instruction => Opcode(instruction) == BifOne);
        if (select is null || head is null
            || Operands(select) is not [var selector, _, _, var first, var firstLabel, var second, var secondLabel, var third, var thirdLabel]
            || !Register(selector, 0) || !IsAtom(first, "global") || !IsAtom(second, "local") || !IsAtom(third, "ready")
            || Value(firstLabel) != Value(secondLabel) || Value(firstLabel) == Value(thirdLabel)
            || Operands(head) is not [_, _, var input, var destination]
            || !Register(input, 0) || Property(input, "TypeIndex") is null || !Register(destination, 1))
        {
            throw new InvalidOperationException(WrongGroupedGuard);
        }

        return Task.CompletedTask;

        object[] Operations(string name)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name);

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(WrongEntry);
        }

        bool IsAtom(object operand, string atom) =>
            (string)Property(operand, "Kind") == BeamBinaryHostNames.AtomKind && atoms[Value(operand)] == atom;

        bool HasAtomMove(object[] operations, string atom, int register) =>
            operations.Any(instruction => Opcode(instruction) == Move
                && IsAtom(Operands(instruction)[0], atom) && Register(Operands(instruction)[1], register));
    }

    private static bool Register(object operand, int register) =>
        (string)Property(operand, "Kind") == BeamBinaryHostNames.X && Value(operand) == register;

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

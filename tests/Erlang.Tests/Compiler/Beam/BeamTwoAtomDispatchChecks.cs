using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTwoAtomDispatchChecks
{
    internal const string TestName = "compiler/beam/dispatches-two-atom-alternatives-with-branch-owned-frames";
    private const string Fixture = "beam_two_atom_dispatch.erl";
    private const string WrongDispatch = "Two exact atom guards must share one select arm, while distinct inputs remain independent.";
    private const string WrongCase = "The atom case lost its selector call, constant argument, or unframed rejection value.";

    private const string Source = """
        -module(beam_two_atom_dispatch).
        -export([start/1, start_link/1, start_link/2, disjoint/2, unknown/2, mode_kind/1]).

        start(Mode) when Mode =:= local; Mode =:= remote ->
            start_link([{enabled,true}], Mode);
        start(Options) ->
            start_link([{owner,undefined}|Options], local).

        start_link(Mode) when local =:= Mode; remote =:= Mode ->
            start_link([{enabled,true}], Mode);
        start_link(Options) ->
            start_link(Options, local).

        start_link(Options, Mode) ->
            case Mode of
                remote -> lists:reverse(Options);
                local -> start_link(Options, choose())
            end.

        choose() ->
            erlang:put(selector_called, true),
            remote.

        disjoint(A, B) when A =:= local; B =:= remote -> yes;
        disjoint(_, _) -> no.

        unknown(Options, Value) ->
            case Value of
                remote -> lists:reverse(Options);
                local -> start_link(Options, choose_unknown())
            end.

        mode_kind(Mode) when Mode == local; Mode == remote; Mode == retained ->
            Mode;
        mode_kind(Mode) ->
            case lists:keyfind(Mode, 1, mode_aliases()) of
                false -> erlang:error({bad_type,{mode_kind,Mode}});
                {Alias, Handler} -> {ext, Alias, Handler}
            end.

        mode_aliases() -> erlang:get(mode_aliases).

        choose_unknown() -> erlang:get(unknown_selector).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        if (!HasSharedAtomSelection(Operations("start", 1))
            || !HasSharedAtomSelection(Operations("start_link", 1))
            || Operations("disjoint", 2).Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        var selected = Operations("start_link", 2);
        var unknown = Operations("unknown", 2);
        var multi = Operations("mode_kind", 1);
        if (multi.Any(instruction => Opcode(instruction) == SelectValue && Operands(instruction) is [_, _, _, _, _, _, _])
            || selected.Count(instruction => Opcode(instruction) == SelectValue) != 1
            || selected.Count(instruction => Opcode(instruction) == LocalCall) != 1
            || selected.Count(instruction => Opcode(instruction) == Allocate) != 1
            || !selected.Any(instruction => Opcode(instruction) == CaseEnd && Register(Operands(instruction)[0], 1))
            || !selected.Any(instruction => Opcode(instruction) == Move
                && Kind(Operands(instruction)[0]) == BeamBinaryHostNames.AtomKind
                && atoms[Value(Operands(instruction)[0])] == "remote"
                && Register(Operands(instruction)[1], 1))
            || unknown.Any(instruction => Opcode(instruction) == Move
                && Kind(Operands(instruction)[0]) == BeamBinaryHostNames.AtomKind
                && atoms[Value(Operands(instruction)[0])] == "remote"
                && Register(Operands(instruction)[1], 1)))
        {
            throw new InvalidOperationException(WrongCase);
        }

        return Task.CompletedTask;

        bool HasSharedAtomSelection(object[] operations)
        {
            object? select = operations.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
            if (select is null || Operands(select) is not [var selector, var failure, _, var first, var firstTarget, var second, var secondTarget])
            {
                return false;
            }

            return Register(selector, 0) && Value(failure) != Value(firstTarget)
                && Value(firstTarget) == Value(secondTarget)
                && atoms[Value(first)] == "local" && atoms[Value(second)] == "remote";
        }

        object[] Operations(string name, int arity)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction =>
                Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name
                && Value(Operands(instruction)[2]) == arity
            );

            return entry >= 0
                ? instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray()
                : throw new InvalidOperationException(WrongDispatch);
        }
    }

    private static bool Register(object operand, int register) => Kind(operand) == BeamBinaryHostNames.X && Value(operand) == register;

    private static string Kind(object operand) => (string)Property(operand, "Kind");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

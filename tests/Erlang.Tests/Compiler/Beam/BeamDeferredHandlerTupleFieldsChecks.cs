using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDeferredHandlerTupleFieldsChecks
{
    internal const string TestName = "compiler/beam/keeps-checked-handler-tuples-until-field-use";
    private const string Fixture = "beam_deferred_handler_tuple_fields_probe.erl";
    private const string UnexpectedLayout = "Deferred handler tuple fields did not match their pinned layout.";

    internal const string Source = """
        -module(beam_deferred_handler_tuple_fields_probe).
        -export([configure/1, dispatch/2, release/2, checked_release/2, consume/2, write/3, close/1]).

        configure({Key, Options0, Marker}) when is_list(Options0) ->
            process_flag(trap_exit, true),
            Options1 =
                case lists:keymember(label, 1, Options0) of
                    true -> Options0;
                    false -> [{label, default}|Options0]
                end,
            Options =
                case [M || M <- Options1, lists:member(M, [red, blue])] of
                    [] -> [red|Options1];
                    _ -> Options1
                end,
            case ?MODULE:consume(Key, Options) of
                {ready,Payload} -> {configured, {Payload, Key, Marker}};
                Other -> Other
            end.

        dispatch({_Kind, Origin, _Message}, State) when node(Origin) /= node() ->
            {unchanged, State};
        dispatch(Event, {Handle, Middle, Color}) ->
            _ = ?MODULE:write(Handle, Color, wrap(Event)),
            {unchanged, {Handle, Middle, Color}};
        dispatch(_, State) ->
            {unchanged, State}.

        release(_, {Handle, _Middle, _Color}) ->
            _ = ?MODULE:close(Handle),
            [].

        checked_release(Token, {Handle, Token, _Color}) ->
            _ = ?MODULE:close(Handle),
            [].

        consume(fails, _) -> {error, closed};
        consume(_, Options) -> {ready, Options}.
        write(Handle, Color, Wrapped) -> self() ! {write, Handle, Color, Wrapped}, ok.
        close(Handle) -> self() ! {close, Handle}, ok.
        wrap(Event) -> {wrapped, Event}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static void Run()
    {
        object code = Code(CompileFixture());
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] configure = Body("configure", 1);
        object[] dispatch = Body("dispatch", 2);
        object[] release = Body("release", 2);
        if (!HasFrame(configure, 2) || configure.Any(instruction => Opcode(instruction) == SelectValue)
            || !HasFrame(dispatch, 1) || dispatch.Count(instruction => Opcode(instruction) == GetTupleElement) != 3
            || !HasFrame(release, 0) || release.Count(instruction => Opcode(instruction) == GetTupleElement) != 1)
        {
            throw new InvalidOperationException(UnexpectedLayout);
        }

        object[] Body(string name, int arity)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var width]
                    && atoms[Value(atom)] == name && Value(width) == arity
            );

            return info < 0 ? [] : instructions.Skip(info + 2)
                .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasFrame(object[] body, int arity) =>
        body.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap)
            .Select(instruction => Value(Items(instruction, "Operands").First()))
            .SequenceEqual([arity]);

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

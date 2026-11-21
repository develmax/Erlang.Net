using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRematerializedLookupEffectChecks
{
    internal const string TestName = "compiler/beam/rematerializes-closed-receivers-across-checked-effects";
    private const string Fixture = "beam_rematerialized_lookup_effects.erl";
    private const string WrongFrame = "The closed receiver still takes a stack slot or the open receiver lost its stack slot.";
    private const string WrongJoin = "The discarded lookup result did not use a single constructor join.";
    private const string WrongList = "The forwarding list did not reuse its last incoming register.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_rematerialized_lookup_effects).
        -export([start/2, launch/2, halt_child/1, open_cleanup/2,
                 available/1, search/3, wipe/2, release/2, install/2]).

        start(Mod, Argument) ->
            Root = fixture_owner,
            Key = processor,
            Args = [self(), Argument],
            install_child(Root, Key, Mod, Args).

        launch(Priority, Options) ->
            Root = fixture_owner,
            Key = worker,
            Mod = custom_driver,
            Args = [Priority, default_mode, Options],
            install_child(Root, Key, Mod, Args).

        halt_child(Key) -> stop_child(fixture_owner, Key).

        install_child(Root, Key, Mod, Args) ->
            Children = ?MODULE:available(Root),
            case ?MODULE:search(Key, 2, Children) of
                {found, {_,_Unused,_}} -> stop_child(Root, Key);
                _ -> skipped
            end,
            Spec = {Key, {Mod, activate, Args}, transient, 400, [Mod]},
            ?MODULE:install(Root, Spec).

        stop_child(Root, Key) ->
            case erlang:whereis(Root) of
                undefined -> missing;
                _ -> ?MODULE:wipe(Root, Key), ?MODULE:release(Root, Key)
            end.

        open_cleanup(Root, Key) ->
            case erlang:whereis(Root) of
                undefined -> missing;
                _ -> ?MODULE:wipe(Root, Key), ?MODULE:release(Root, Key)
            end.

        available(Root) -> observe({available,Root}), erlang:get(children).
        search(Key, Position, Children) -> observe({search,Key,Position,Children}), erlang:get(reply).
        wipe(Root, Child) -> observe({wipe,Root,Child}).
        release(Root, Child) -> observe({release,Root,Child}), {released,Root,Child}.
        install(Root, Spec) -> observe({install,Root,Spec}), {activated,Spec}.

        observe(Event) ->
            Previous = case erlang:get(observations) of undefined -> []; Other -> Other end,
            erlang:put(observations,[Event|Previous]),ok.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        object[] launch = Operations("install_child", 4);
        object[] cleanup = Operations("stop_child", 2);
        object[] open = Operations("open_cleanup", 2);
        if (Frame(launch) != 3 || Frame(cleanup) != 1 || Frame(open) != 2)
        {
            throw new InvalidOperationException(WrongFrame);
        }

        if (launch.Any(instruction => Opcode(instruction) == Jump)
            || launch.Count(instruction => Opcode(instruction) == PutTuple) != 2)
        {
            throw new InvalidOperationException(WrongJoin);
        }

        byte[] direct = Operations("start", 2).Select(Opcode).ToArray();
        byte[] options = Operations("launch", 2).Select(Opcode).ToArray();
        if (direct.Contains(Allocate) || Array.IndexOf(direct, TestHeap) >= Array.IndexOf(direct, BifZero)
            || options.Contains(Allocate) || options.Count(opcode => opcode == PutList) != 3
            || Array.IndexOf(options, PutList) >= Array.IndexOf(options, Move))
        {
            throw new InvalidOperationException(WrongList);
        }

        return Task.CompletedTask;

        static int Frame(IEnumerable<object> body)
        {
            object? allocation = body.FirstOrDefault(instruction => Opcode(instruction) == Allocate);

            return allocation is not null ? Value(Operands(allocation)[0]) : -1;
        }

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

using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedTaggedChildCallChecks
{
    internal const string TestName = "compiler/beam/reuses-registers-across-tagged-child-lookup-and-cleanup";
    private const string Fixture = "beam_owned_tagged_child_calls.erl";
    private const string WrongFrame = "The known receiver occupied a frame slot or the tagged key was spilled before its first use.";
    private const string WrongList = "The outgoing list did not reuse its input registers.";
    private const string WrongCleanup = "Tagged cleanup kept a constructed key live across both external effects.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_owned_tagged_child_calls).
        -export([read/4, refresh/6, remove/1, children/1, find/3, clear/2, erase_entry/2, run/2]).

        read(First, Key, Middle, Last) ->
            Root = tagged_fixture,
            retained(Root, Key, [First,Middle,Last]).

        refresh(First, Second, Key, Third, Module, Last) ->
            Root = tagged_fixture,
            replaced(Root, Key, Module, [First,Second,Third,Last]).

        retained(Root, Key, Args) ->
            Children = ?MODULE:children(Root),
            case ?MODULE:find({old,Key}, 2, Children) of
                {found,{_,_,Value}} -> {read,Value};
                _ ->
                    Spec = {{old,Key},{fixed_module,bootstrap,Args},retained,20,[fixed_module]},
                    ?MODULE:run(Root,Spec)
            end.

        replaced(Root, Key, Module, Args) ->
            Children = ?MODULE:children(Root),
            case ?MODULE:find({entry,Key}, 3, Children) of
                {found,{_,_Ignored,_}} -> remove(Key);
                _ -> skipped
            end,
            Spec = {{entry,Key},{Module,activate,Args},different,123,[Module]},
            ?MODULE:run(Root,Spec).

        remove(Key) ->
            Root = tagged_fixture,
            case whereis(Root) of
                undefined -> missing;
                _ ->
                    ?MODULE:clear(Root,{entry,Key}),
                    ?MODULE:erase_entry(Root,{entry,Key})
            end.

        children(Root) -> observe({children,Root}), [seed].
        find(Key, Index, Children) -> observe({find,Key,Index,Children}), erlang:get(reply).
        clear(Root, Key) -> observe({clear,Root,Key}).
        erase_entry(Root, Key) -> observe({erase,Root,Key}), deleted.
        run(Root, Spec) -> observe({run,Root,Spec}), {new,Spec}.
        observe(Event) ->
            Prior = case erlang:get(events) of undefined -> []; Other -> Other end,
            erlang:put(events,[Event|Prior]),ok.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();

        object[] retained = Operations("retained", 3);
        object[] replaced = Operations("replaced", 4);
        object[] cleanup = Operations("remove", 1);
        if (Frame(retained) != 2 || Frame(replaced) != 3 || Frame(cleanup) != 1
            || replaced.Any(instruction => Opcode(instruction) == Jump))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        byte[] read = Operations("read", 4).Select(Opcode).ToArray();
        byte[] refresh = Operations("refresh", 6).Select(Opcode).ToArray();
        if (Array.IndexOf(read, PutList) >= Array.IndexOf(read, Move)
            || Array.IndexOf(refresh, PutList) >= Array.IndexOf(refresh, Move)
            || read.Count(opcode => opcode == PutList) != 3 || refresh.Count(opcode => opcode == PutList) != 4)
        {
            throw new InvalidOperationException(WrongList);
        }

        if (cleanup.Count(instruction => Opcode(instruction) == PutTuple) != 2
            || cleanup.Count(instruction => Opcode(instruction) == TestHeap) != 2)
        {
            throw new InvalidOperationException(WrongCleanup);
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

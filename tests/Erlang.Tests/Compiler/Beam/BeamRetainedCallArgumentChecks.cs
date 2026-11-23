using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRetainedCallArgumentChecks
{
    internal const string TestName = "compiler/beam/orders-saved-call-results-and-list-construction";
    private const string Fixture = "retained_call_frame_probe.erl";
    private const string WrongSchedule = "A saved result or an owned list spine was emitted with the wrong destination sequence.";
    internal const string Source = """
        -module(retained_call_frame_probe).
        -export([saved/1, list_save/1, changed/1, distinct_receiver/1, same_result/1, make/1, read/1, observe/3, observe/4, done/1]).
        saved(N) ->
            Value = ?MODULE:make(N),
            ?MODULE:observe(Value, 5, 5),
            ?MODULE:done(Value).
        list_save(N) ->
            First = ?MODULE:make(N),
            Second = ?MODULE:make(2),
            Third = ?MODULE:make(3),
            Items = [First, Second, Third],
            ?MODULE:observe(pause, p, q),
            ?MODULE:done(Items).
        changed(N) ->
            Value = ?MODULE:make(N),
            ?MODULE:observe(Value, ?MODULE:read(5), 5),
            ?MODULE:done(Value).
        distinct_receiver(Receiver) ->
            Value = ?MODULE:make(receiver),
            ?MODULE:observe(Receiver, 4, <<"text">>, []),
            ?MODULE:done({Receiver, Value}).
        same_result(N) ->
            Value = ?MODULE:make(N),
            ?MODULE:observe(Value, 4, <<"text">>, []),
            ?MODULE:done(Value).
        make(fail) -> erlang:error(known_error);
        make(N) -> {made, N}.
        read(N) -> N.
        observe(A, B, C) -> put(trace, [{A, B, C} | get(trace)]), ok.
        observe(A, B, C, D) -> put(trace, [{A, B, C, D} | get(trace)]), ok.
        done(Value) -> {done, Value}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[][] stores = Splice(Body("saved"), 3);
        object[][] control = Splice(Body("changed"), 3);
        object[] tail = Items(Body("list_save").Where(item => Opcode(item) == PutList).Last(), "Operands").ToArray();
        object[][] receiver = Splice(Body("distinct_receiver"), 4);
        object[][] same = Splice(Body("same_result"), 4);
        if (stores.Length != 3
            || !(Kind(stores[0][0]) == "x" && Value(stores[0][0]) == 0 && Kind(stores[0][1]) == "y")
            || !stores.Skip(1).Select(move => Value(move[1])).SequenceEqual([2, 1])
            || !control.Any(move => Kind(move[0]) == "y" && Kind(move[1]) == "x" && Value(move[1]) == 0)
            || Kind(tail[^1]) != "y" || Value(tail[^1]) != Value(tail[0])
            || !same.Skip(1).Select(move => Value(move[1])).SequenceEqual([2, 3, 1])
            || !receiver.Skip(1).Select(move => Value(move[1])).SequenceEqual([1, 2, 3, 0, 1])
            || Kind(receiver[1][0]) != "y" || Kind(receiver[4][0]) != "x" || Value(receiver[4][0]) != 1)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[Value(Items(item, "Operands").ElementAt(1))] == name);

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }
    }

    private static object[][] Splice(object[] body, int arity) =>
        body.SkipWhile(item => Opcode(item) != ExternalCall)
            .Skip(1).TakeWhile(item => Opcode(item) != ExternalCall || Value(Items(item, "Operands").First()) != arity)
            .Where(item => Opcode(item) == Move).Select(item => Items(item, "Operands").ToArray()).ToArray();

    private static string Kind(object item) => (string)Property(item, "Kind");

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

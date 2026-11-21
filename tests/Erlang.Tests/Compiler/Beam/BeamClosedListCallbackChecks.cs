using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedListCallbackChecks
{
    internal const string TestName = "compiler/beam/keeps-a-closed-list-callback-in-registers";
    private const string Fixture = "closed_list_callback_probe.erl";
    private const string WrongFrame = "The closed callback did not use a zero-slot safe call or the open control lost its checked call.";
    internal const string Source = """
        -module(closed_list_callback_probe).
        -export([run/2, open/3]).
        run(List, Value) ->
            bridge(target, fun () -> produce(Value) end, List).
        bridge(Key, Callback, List) -> search(Key, Callback, List).

        search(_Key, _Callback, []) -> absent;
        search(Key, Callback, [Key | _]) -> Callback();
        search(Key, Callback, [_ | Tail]) -> search(Key, Callback, Tail).

        open(_Key, _Callback, []) -> absent;
        open(Key, Callback, [Key | _]) -> Callback();
        open(Key, Callback, [_ | Tail]) -> open(Key, Callback, Tail).

        produce(fault) -> erlang:error(callback_fault);
        produce(Value) -> {called, Value}.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] closed = Body("search");
        object[] open = Body("open");
        if (closed.Where(instruction => Opcode(instruction) == Allocate).ToArray() is not [var frame]
            || Value(Items(frame, "Operands").First()) != 0
            || closed.Count(instruction => Opcode(instruction) == CallFunTyped) != 1
            || open.Any(instruction => Opcode(instruction) == CallFunTyped))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int start = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[Value(Items(item, "Operands").ElementAt(1))] == name);

            return instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object item) => (int)(BigInteger)Property(item, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

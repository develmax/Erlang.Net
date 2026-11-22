using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedSignalCounterLoopChecks
{
    internal const string TestName = "compiler/beam/localizes-a-closed-signal-counter-entry-frame";
    private const string Fixture = "beam_closed_signal_counter_loop_probe.erl";
    private const string WrongLayout = "The closed counter test and its self-send must run before the wide continuation frame.";
    private const string WrongResult = "The closed signal counter changed a send, an exit, or the open entry behavior.";

    private const string Source = """
        -module(beam_closed_signal_counter_loop_probe).
        -export([run/1, open/2]).
        run(Payload) -> walk(Payload, 0).
        walk(Payload, 2) -> self() ! Payload;
        walk(Payload, N) ->
            case probe(Payload) of
                more -> walk(Payload, N+1);
                _ -> ok
            end.

        open(Payload, 2) -> self() ! Payload;
        open(Payload, N) ->
            case probe(Payload) of
                more -> open(Payload, N+1);
                _ -> ok
            end.
        probe(stop) -> done;
        probe(_) -> more.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term message = Term.A("again");
            Term stop = Term.A("stop");
            Term ok = Term.A("ok");
            if (!message.Equals(await module.Invoke(context, "run", message))
                || !message.Equals(await context.ReceiveAsync(value => value, TimeSpan.FromSeconds(1)))
                || !ok.Equals(await module.Invoke(context, "run", stop))
                || !ok.Equals(await module.Invoke(
                    context,
                    "open",
                    stop,
                    Term.A("arbitrary_counter")
                )) || await context.ReceiveAsync(value => value, TimeSpan.Zero) is not null)
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = Function("walk");
        object[] frames = body.Where(item => Opcode(item) == Allocate).ToArray();
        int test = Array.FindIndex(body, item => Opcode(item) == ExactEqualTest);
        if (frames is not [var sendFrame, var loopFrame] || test < 0
            || !Operands(sendFrame).Select(Value).SequenceEqual([0, 1])
            || !Operands(loopFrame).Select(Value).SequenceEqual([2, 2])
            || Array.IndexOf(body, sendFrame) != test + 1
            || Operands(body[test]) is not [_, var compared, _]
            || Property(compared, "Kind") is not "x" || Value(compared) != 1
            || Property(compared, "TypeIndex") is not int type
            || !body.Any(item => Opcode(item) == GcBifTwo
                && Operands(item) is [_, _, _, var saved, _, var destination]
                && Property(saved, "Kind") is "y" && Value(saved) == 0
                && Property(saved, "TypeIndex") is int savedType && savedType == type && Value(destination) == 1))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Function(string name)
        {
            int info = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && Operands(item) is [_, var atom, _] && atoms[Value(atom)] == name);
            if (info < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            return instructions.Skip(info + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }
    }

    private static object[] Operands(object item) => Items(item, "Operands").ToArray();

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object item, string name) => ((IEnumerable)Property(item, name)).Cast<object>();
}

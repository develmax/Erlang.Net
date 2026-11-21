using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalClosureFrameChecks
{
    internal const string TestName = "compiler/beam/forwards-terminal-closures-without-a-frame";
    private const string Fixture = "beam_terminal_closure_frame_probe.erl";
    private const string TailClosureStillUsesFrame = "A terminal closure needs only its heap roots and the outgoing X register.";
    private const string InvalidValue = "Forwarding a terminal closure changed an argument or a captured value.";

    internal const string Source = """
        -module(beam_terminal_closure_frame_probe).
        -export([restored/2, direct/2, remote/2, retained/2, trapped/2]).

        restored(Left, Right) -> finish(Left, Right, fun pair/1).
        direct(First, Capture) -> invoke(First, fun(Value) -> {Capture, Value} end).
        remote(Capture, Values) -> lists:map(fun(Value) -> {Capture, Value} end, Values).
        retained(Capture, Values) -> {Capture, lists:map(fun(Value) -> {Capture, Value} end, Values)}.
        trapped(Pid, Captured) -> erlang:exit(Pid, fun() -> Captured end).
        finish(Left, Right, Callback) -> {Callback(Left), Right}.
        invoke(Value, Callback) -> Callback(Value).
        pair(Value) -> {paired, Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!TerminalClosure("restored", 2, TailLocalCall)
            || !TerminalClosure("direct", 1, TailLocalCall)
            || !TerminalClosure("remote", 0, TailExternalCall)
            || !Body("retained").Any(instruction => Opcode(instruction) == ExternalCall)
            || !Body("trapped").Any(instruction => Opcode(instruction) == TailExternalCallWithFrame))
        {
            throw new InvalidOperationException(TailClosureStillUsesFrame);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term left = Term.A("left");
            Term right = Term.A("right");
            if (!Term.Tuple(Term.Tuple(Term.A("paired"), left), right).Equals(await module.Invoke(context, "restored", [left, right]))
                || !Term.Tuple(right, left).Equals(await module.Invoke(context, "direct", [left, right])))
            {
                throw new InvalidOperationException(InvalidValue);
            }

            return Nil.Value;
        });

        bool TerminalClosure(string name, int register, byte tail)
        {
            object[] body = Body(name).Where(op => Opcode(op) is not (Line or Label or EndCode)).ToArray();

            return body.Length == 3 && Opcode(body[0]) == TestHeap && Opcode(body[1]) == MakeFun
                && Opcode(body[2]) == tail && Operands(body[1]) is [_, var destination, ..]
                && (string)Property(destination, "Kind") == "x" && Value(destination) == register;
        }

        IEnumerable<object> Body(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

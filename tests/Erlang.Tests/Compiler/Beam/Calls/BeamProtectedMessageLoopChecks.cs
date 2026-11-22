using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedMessageLoopChecks
{
    internal const string TestName = "compiler/beam/keeps-received-message-across-one-owned-try";
    private const string LayoutError = "Expected the consumed message to go directly to the protected call.";
    private const string Source = """
        -module(protected_message_loop).
        -export([run/0]).
        run() ->
            _ = receive Message -> try work(Message) catch _ : _ -> ignored end end,
            run().
        work(fail) -> erlang:error(requested);
        work(Pid) -> Pid ! handled.
        """;

    internal static void Run()
    {
        object normal = Code(CodeGeneration.CompileBeamModule(Source, "protected_message_loop.erl"));
        object control = Code(CodeGeneration.CompileBeamModule(
            Source.Replace("_ : _ -> ignored", "_ : _ -> work(retry)", StringComparison.Ordinal),
            "protected_message_loop.erl"
        ));
        Check(normal, direct: true);
        Check(control, direct: false);
    }

    private static void Check(object code, bool direct)
    {
        object[] instructions = ((System.Collections.IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int select = Array.FindIndex(instructions, instruction => Opcode(instruction) == LoopReceive);
        int consumed = Array.FindIndex(instructions, select, instruction => Opcode(instruction) == RemoveMessage);
        int handler = Array.FindIndex(instructions, consumed, instruction => Opcode(instruction) == TryEnter);
        int call = Array.FindIndex(instructions, handler, instruction => Opcode(instruction) == LocalCall);
        int release = Array.FindIndex(instructions, call, instruction => Opcode(instruction) == TryCase);
        bool compact = consumed + 1 == handler && call == handler + 2
            && instructions.Skip(release + 1).TakeWhile(instruction => Opcode(instruction) == Label).Any();
        if (select < 0 || release < 0 || compact != direct)
        {
            throw new InvalidOperationException(LayoutError);
        }
    }

    private static int Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}

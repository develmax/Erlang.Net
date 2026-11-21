using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedSingletonOptionsChecks
{
    internal const string TestName = "compiler/beam/shares-protected-singleton-options-and-catch-class";
    private const string Fixture = "beam_protected_singleton_options.erl";
    private const string WrongDecision = "Protected option dispatch did not share its selector and exception class.";

    private const string Source = """
        -module(beam_protected_singleton_options).
        -export([open/2]).
        -compile({inline, [report/2]}).

        open(Size, Options) ->
            try
                case Options of
                    [on] -> {on, provider:open(Size)};
                    [off] -> {off, provider:open(Size, false)};
                    [] -> {off, provider:open(Size, false)};
                    _ -> erlang:error(badopt)
                end
            catch
                error:badopt ->
                    Meta = [{error_info, #{module => erlang, cause => badopt}}],
                    erlang:error(badarg, [Size, Options], Meta);
                error:Reason ->
                    report(Reason, [Size, Options])
            end.

        report(Reason, Arguments) ->
            erlang:error(Reason, Arguments, [{error_info, #{module => erlang}}]).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int select = Array.IndexOf(opcodes, SelectValue);
        int tryCase = Array.IndexOf(opcodes, TryCase);
        int allocate = Array.IndexOf(opcodes, Allocate);
        if (select < 2 || opcodes[select - 1] != GetList || opcodes[select - 2] != IsNonemptyList
            || allocate < 0 || !Operands(instructions[allocate]).Select(Value).SequenceEqual([3, 2])
            || tryCase <= select || !opcodes.Skip(tryCase + 1).Take(3).SequenceEqual([ExactEqualTest, ExactEqualTest, TestHeap])
            || opcodes.Count(opcode => opcode == GetList) != 1
            || opcodes.Count(opcode => opcode == TailExternalCallWithFrame) != 2)
        {
            throw new InvalidOperationException(WrongDecision);
        }

        return Task.CompletedTask;
    }

    private static object[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}

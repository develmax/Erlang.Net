using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCallArgumentKillsChecks
{
    internal const string TestName = "compiler/beam/keeps-independent-x0-copy-after-dead-stack-slots";
    private const string Fixture = "beam_call_argument_kills_probe.erl";
    private const string WrongIndependentOrder = "An independent X0 copy was placed before a dead stack slot was cleared.";
    private const string WrongDependentOrder = "Clearing a stack slot moved before its last read.";

    private const string Source = """
        -module(beam_call_argument_kills_probe).
        -export([independent/1, dependent/1]).
        independent(Options) ->
            case lists:member(netload, Options) of
                true -> netload;
                false ->
                    case lists:member(load, Options) of
                        true -> load;
                        _ -> noload
                    end
            end.

        dependent(Options) ->
            case lists:member(netload, Options) of
                true -> netload;
                false ->
                    case lists:member(Options, [[portable], [debug]]) of
                        true -> selected;
                        _ -> noload
                    end
            end.
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").Where(instruction => Opcode(instruction) != Line).ToArray();

        var (independentFirst, independentSecond) = ReadSecondCallSetup("independent");
        if (Opcode(independentFirst) != InitYRegisters || Opcode(independentSecond) != Move
            || !CopiesToX0(independentSecond, "atom"))
        {
            throw new InvalidOperationException(WrongIndependentOrder);
        }

        var (dependentFirst, dependentSecond) = ReadSecondCallSetup("dependent");
        if (Opcode(dependentFirst) != Move || Opcode(dependentSecond) != InitYRegisters
            || !CopiesToX0(dependentFirst, "y")
            || !Items(dependentSecond, "Operands").Skip(1).Any(operand =>
                (string)Property(operand, "Kind") == "y"
                && Value(operand) == Value(Items(dependentFirst, "Operands").First())))
        {
            throw new InvalidOperationException(WrongDependentOrder);
        }

        CheckLiteralAfterListRootKill();

        return Task.CompletedTask;

        (object First, object Second) ReadSecondCallSetup(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Items(instruction, "Operands").ElementAt(1))] == name
            );
            int first = Array.FindIndex(instructions, start + 1, instruction => Opcode(instruction) == ExternalCall);
            int second = Array.FindIndex(instructions, first + 1, instruction => Opcode(instruction) == ExternalCall);

            return (instructions[second - 2], instructions[second - 1]);
        }
    }

    private static void CheckLiteralAfterListRootKill()
    {
        var compiler = new BeamModuleCompiler("beam_list_argument_root_order");
        _ = Call(compiler, "Intern", "format");
        byte[] prefix =
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1),
            Move, X(0), Y(0),
            InitYRegisters, ExtendedListOperand, U(1), Y(1),
            TestHeap, U(2), U(0),
            PutList, Y(0), A(0), X(1)
        ];
        byte[] literal = [Move, A(2), X(0)];
        byte[] killsHead = [InitYRegisters, ExtendedListOperand, U(1), Y(0)];
        byte[] killsAnother = [InitYRegisters, ExtendedListOperand, U(1), Y(1)];
        byte[] suffix = [Line, U(0), ExternalCall, U(2), U(0), Return, EndCode];
        byte[] input = [.. prefix, .. literal, .. killsHead, .. suffix];
        byte[] expected = [.. prefix, .. killsHead, .. literal, .. suffix];
        byte[] unrelated = [.. prefix, .. literal, .. killsAnother, .. suffix];
        if (Rewrite(input) is not { } result || !result.SequenceEqual(expected)
            || Rewrite(unrelated) is not null)
        {
            throw new InvalidOperationException(WrongIndependentOrder);
        }

        byte[]? Rewrite(ReadOnlyMemory<byte> body) => (byte[]?)Call(
            compiler,
            "OrderCallArgumentAfterKills",
            body,
            3,
            true
        );
    }

    private static bool CopiesToX0(object instruction, string kind)
    {
        object[] operands = Items(instruction, "Operands").ToArray();

        return operands.Length == 2 && (string)Property(operands[0], "Kind") == kind
            && (string)Property(operands[1], "Kind") == "x" && Value(operands[1]) == BigInteger.Zero;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");
}

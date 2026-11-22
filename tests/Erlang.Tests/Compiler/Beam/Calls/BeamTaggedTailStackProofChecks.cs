using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedTailStackProofChecks
{
    internal const string TestName = "compiler/beam/proves-the-stack-is-unobserved-before-lowering-a-tagged-tail-throw";
    private const string FileName = "beam_tagged_tail_stack_proof.erl";

    private const string Source = """
        -module(beam_tagged_tail_stack_proof).
        -export([run/4{{extra_export}}]).

        run(Phase, Mode, State, Driver) ->
            {{handler}}.
        {{extra_function}}

        prepare(Phase, Mode, State, Driver) ->
            Entries = case stage_store:read(State, active_steps) of
                {ok, undefined} -> throw({failure, {missing_steps, State}});
                {ok, Steps} -> Steps
            end,
            case lists:keyfind(Phase, 1, Entries) of
                false -> ok;
                {Phase, Args} ->
                    case catch Driver:advance(Phase, Mode, Args) of
                        ok -> ok;
                        {failure, Why} ->
                            throw({failure, {Why, {Driver, advance, [Phase, Mode, Args]}}});
                        Other ->
                            throw({failure, {invalid_result, {{Driver, advance, [Phase, Mode, Args]}, Other}}})
                    end
            end.
        """;

    private const string DirectCall = "prepare(Phase, Mode, State, Driver)";

    internal static Task Run()
    {
        // The inner caught-result case returns through the surrounding
        // keyfind case. Its ordinary arms still share that return.
        var (instructions, target) = ReadArm(Source.Replace("{{handler}}", $"catch {DirectCall}", StringComparison.Ordinal)
            .Replace("{{extra_export}}", string.Empty, StringComparison.Ordinal)
            .Replace("{{extra_function}}", string.Empty, StringComparison.Ordinal));
        if (target != "raise"
            || instructions.Select(Opcode).Take(8).ToArray() is not
            [TestHeap, GetTupleElement, PutList, PutList, PutList, PutTuple, PutTuple, PutTuple]
            || Value(Operands(instructions[0])[0]) != 16 || Value(Operands(instructions[0])[1]) != 1
            || Value(Operands(instructions[1])[2]) != 0
            || instructions.Skip(2).Take(3).Any(instruction => Value(Operands(instruction)[2]) != 1)
            || Value(Operands(instructions[5])[0]) != 1
            || Value(Operands(instructions[6])[0]) != 0
            || Value(Operands(instructions[7])[0]) != 1
            || instructions.Select(Opcode).Skip(8).ToArray() is not
                [Move, Move, Line, TailExternalCallWithFrame])
        {
            throw new InvalidOperationException("The checked payload must remain in X0 until the reason consumes it.");
        }

        string observed = Source.Replace(
            "{{handler}}",
            $"try {DirectCall} catch throw:Reason:Stack -> {{Reason, Stack}} end",
            StringComparison.Ordinal
        ).Replace("{{extra_export}}", string.Empty, StringComparison.Ordinal)
          .Replace("{{extra_function}}", string.Empty, StringComparison.Ordinal);
        string escaped = Source.Replace("{{handler}}", $"catch {DirectCall}", StringComparison.Ordinal)
            .Replace("{{extra_export}}", ", get_worker/0", StringComparison.Ordinal)
            .Replace("{{extra_function}}", "get_worker() -> fun prepare/4.", StringComparison.Ordinal);
        var (fallback, fallbackTarget) = ReadArm(
            Source.Replace("{{handler}}", $"catch {DirectCall}", StringComparison.Ordinal)
                .Replace("{{extra_export}}", string.Empty, StringComparison.Ordinal)
                .Replace("{{extra_function}}", string.Empty, StringComparison.Ordinal),
            fallback: true
        );
        if (fallbackTarget != "raise"
            || fallback.Select(Opcode).ToArray() is not
                [TestHeap, PutList, PutList, PutList, PutTuple, PutTuple, PutTuple, PutTuple,
                    Move, Move, Line, TailExternalCallWithFrame]
            || Value(Operands(fallback[0])[0]) != 19 || Value(Operands(fallback[0])[1]) != 1
            || fallback.Skip(1).Take(3).Any(instruction => Value(Operands(instruction)[2]) != 1)
            || Value(Operands(fallback[4])[0]) != 1
            || Value(Operands(fallback[5])[0]) != 0
            || Value(Operands(fallback[6])[0]) != 0
            || Value(Operands(fallback[7])[0]) != 1)
        {
            throw new InvalidOperationException("The fallback reason must consume the caught value from X0.");
        }

        if (ReadArm(observed).Target != "throw" || ReadArm(escaped).Target != "throw"
            || ReadArm(observed, fallback: true).Target != "throw"
            || ReadArm(escaped, fallback: true).Target != "throw")
        {
            throw new InvalidOperationException("An observed stack or escaping function must preserve the original throw.");
        }

        return Task.CompletedTask;
    }

    private static (object[] Instructions, string Target) ReadArm(string source, bool fallback = false)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, FileName));
        object structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] imports = Items(structure, "Imports").ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == "prepare"
        );
        if (entry < 0)
        {
            throw new InvalidOperationException("The private checked fixture function is missing.");
        }

        object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int tagged = Array.FindIndex(
            body,
            instruction => Opcode(instruction) == IsTaggedTuple && atoms[Value(Operands(instruction)[3])] == "failure"
        );
        if (tagged < 0)
        {
            throw new InvalidOperationException("The fixture has no checked failure-result arm.");
        }

        int start = tagged + 1;
        if (fallback)
        {
            int rejected = Value(Operands(body[tagged])[0]);
            start = Array.FindIndex(
                body,
                instruction => Opcode(instruction) == Label && Value(Operands(instruction)[0]) == rejected
            ) + 1;
            if (start == 0)
            {
                throw new InvalidOperationException("The tagged arm's fallback label is missing.");
            }
        }

        object[] arm = body.Skip(start).TakeWhile(instruction => Opcode(instruction) != Label).ToArray();
        object terminal = arm.Single(instruction => Opcode(instruction) is TailExternalCallWithFrame or ExternalCall);
        object import = imports[Value(Operands(terminal)[1])];

        return (arm, atoms[(int)Property(import, "FunctionAtomIndex")]);
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

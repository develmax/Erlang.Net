using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedNamedClauseChecks
{
    internal const string TestName = "compiler/beam/shared-head-atom-clause-order";
    private const string Fixture = "beam_shared_named_clause_probe.erl";
    private const string WrongOrder = "The shared named clauses did not retain the expected switch and terminal block order.";
    private const string WrongResult = "The shared named clauses changed dispatch or failure.";
    private const string Source = """
        -module(beam_shared_named_clause_probe).
        -export([forward/2, interleave/2, reversed/2, unshared/2]).
        forward(low, Value) -> Value;
        forward(mid, Value) -> Value;
        forward(top, Value) -> Converted = local(Value), finish(Converted, []).
        interleave(low, Value) -> Value;
        interleave(mid, Value) -> Converted = local(Value), finish(Converted, []);
        interleave(top, Value) -> Value.
        reversed(top, Value) -> Value;
        reversed(mid, Value) -> Converted = local(Value), finish(Converted, []);
        reversed(low, Value) -> Value.
        unshared(low, Value) -> {low,Value};
        unshared(top, Value) -> {top,Value};
        unshared(mid, Value) -> {mid,Value}.
        local(Value) -> {prepared,Value}.
        finish(Prepared, Rest) -> {Prepared,Rest}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] beam = CompileFixture();
        CheckInstructions(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.A("value");
            Term wasPrepared = Term.Tuple(Term.Tuple(Term.A("prepared"), input), Nil.Value);
            if (!input.Equals(await module.Invoke(
                context,
                "forward",
                Term.A("low"),
                input
            ))
                || !wasPrepared.Equals(await module.Invoke(
                    context,
                    "forward",
                    Term.A("top"),
                    input
                ))
                || !wasPrepared.Equals(await module.Invoke(
                    context,
                    "interleave",
                    Term.A("mid"),
                    input
                ))
                || !input.Equals(await module.Invoke(
                    context,
                    "interleave",
                    Term.A("top"),
                    input
                ))
                || !input.Equals(await module.Invoke(
                    context,
                    "reversed",
                    Term.A("top"),
                    input
                ))
                || !wasPrepared.Equals(await module.Invoke(
                    context,
                    "reversed",
                    Term.A("mid"),
                    input
                ))
                || !Term.Tuple(Term.A("mid"), input).Equals(await module.Invoke(
                    context,
                    "unshared",
                    Term.A("mid"),
                    input
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "forward",
                    Term.A("invalid"),
                    input
                );
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void CheckInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (string function in new[] { "forward", "interleave", "reversed" })
        {
            int index = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
                && atoms[(int)Value(Children(item, "Operands").ElementAt(1))] == function);
            object[] body = instructions.Skip(index + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
            int select = Array.FindIndex(body, item => Opcode(item) == SelectValue);
            object[] args = Children(body[select], "Operands").Skip(3).ToArray();
            BigInteger[] targets = args.Where((_, operand) => operand % 2 == 1).Select(Value).ToArray();
            BigInteger[] ordered = body.Skip(select + 1).Where(item => Opcode(item) == Label)
                .Take(2).Select(item => Value(Children(item, "Operands").First())).ToArray();
            int call = Array.FindIndex(body, item => Opcode(item) == LocalCall);
            int returnInstruction = Array.FindIndex(body, item => Opcode(item) == Return);
            if (targets.Length != 3 || targets.Distinct().Count() != 2
                || !ordered.SequenceEqual(targets.Distinct().Reverse())
                || call < 0 || returnInstruction < call)
            {
                throw new InvalidOperationException(WrongOrder);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

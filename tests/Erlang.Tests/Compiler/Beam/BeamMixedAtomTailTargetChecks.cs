using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedAtomTailTargetChecks
{
    internal const string TestName = "compiler/beam/orders-local-remote-and-return-atom-branches";
    private const string Fixture = "beam_mixed_atom_tail_target_probe.erl";
    private const string IncorrectBranchOrder = "The atom table body did not visit the return, local target and remote target in reverse order.";
    private const string IncorrectOutcome = "A closed atom branch produced a different result.";

    private const string Source = """
        -module(beam_mixed_atom_tail_target_probe).
        -export([entry/3, convert/3]).

        entry(extern, Value, _) ->
            lists:nth(Value, [7, 11, 13]);
        entry(local, Value, _) ->
            wrap(Value, marker);
        entry(value, Value, _) ->
            {Value, 0}.

        convert(extern, Value, _) ->
            Prepared = prepare(Value),
            lists:nth(Prepared, [7, 11, 13]);
        convert(local, Value, _) ->
            Prepared = prepare(Value),
            wrap(Prepared, marker);
        convert(value, Value, _) ->
            Value.

        prepare(Value) -> Value + 1.
        wrap(Value, Tag) -> {Tag, Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] functions = ["entry", "convert"];
        for (int index = 0; index < functions.Length; index++)
        {
            string function = functions[index];
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == function && Value(arity) == 3
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectBranchOrder);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            byte local = index == 0 ? TailLocalCall : TailLocalCallWithFrame;
            byte remote = index == 0 ? TailExternalCall : TailExternalCallWithFrame;
            byte[] terminals = body.Select(Opcode).Where(opcode => opcode == Return || opcode == local || opcode == remote).ToArray();
            if (terminals is not [Return, var localOpcode, var remoteOpcode] || localOpcode != local || remoteOpcode != remote)
            {
                throw new InvalidOperationException(IncorrectBranchOrder);
            }
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term marker = Term.A("marker");
            Term one = Term.I(1);
            if (!Term.I(7).Equals(await module.Invoke(
                context,
                "entry",
                Term.A("extern"),
                one,
                Nil.Value
            ))
                || !Term.I(11).Equals(await module.Invoke(
                    context,
                    "convert",
                    Term.A("extern"),
                    one,
                    Nil.Value
                ))
                || !Term.Tuple(marker, one).Equals(await module.Invoke(
                    context,
                    "entry",
                    Term.A("local"),
                    one,
                    Nil.Value
                ))
                || !Term.Tuple(marker, Term.I(2)).Equals(await module.Invoke(
                    context,
                    "convert",
                    Term.A("local"),
                    one,
                    Nil.Value
                ))
                || !Term.Tuple(one, Term.I(0)).Equals(await module.Invoke(
                    context,
                    "entry",
                    Term.A("value"),
                    one,
                    Nil.Value
                ))
                || !one.Equals(await module.Invoke(
                    context,
                    "convert",
                    Term.A("value"),
                    one,
                    Nil.Value
                )))
            {
                throw new InvalidOperationException(IncorrectOutcome);
            }

            return Nil.Value;
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

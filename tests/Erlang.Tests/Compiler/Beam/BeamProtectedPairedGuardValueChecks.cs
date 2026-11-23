using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProtectedPairedGuardValueChecks
{
    internal const string TestName = "compiler/beam/protects-paired-comma-guard-boolean-values";
    private const string WrongLayout = "The paired comma guard lost its boolean operations or local mask failure.";
    private const string WrongValue = "The paired guard changed a matching alternative or fallback.";
    private const string Source = """
        -module(beam_protected_paired_guard_values).
        -export([run/2, explicit/2]).

        run(Port, Addr) ->
            case erlang:get(enabled_guard) of
                yes when is_map(Addr);
                         Port =:= -1, Addr =:= undefined;
                         (Port band bnot 255) =:= 0, Addr =:= undefined ->
                    {accepted, Port, Addr};
                _ -> {rejected, Port, Addr}
            end.

        explicit(Port, Addr) ->
            case erlang:get(enabled_guard) of
                yes when is_map(Addr);
                         Port =:= -1 andalso Addr =:= undefined;
                         (Port band bnot 255) =:= 0 andalso Addr =:= undefined ->
                    {accepted, Port, Addr};
                _ -> {rejected, Port, Addr}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_protected_paired_guard_values.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        var structure = Property(code, "Structure");
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] imports = Items(structure, "Imports");
        object[] run = Function("run");
        object[] explicitGuard = Function("explicit");
        int count = run.Count(instruction => Opcode(instruction) == BifTwo && ImportName(instruction, 1) == "and");
        if (count != 2 || explicitGuard.Any(instruction => Opcode(instruction) == BifTwo
            && ImportName(instruction, 1) == "and")
            || !run.Any(instruction => Opcode(instruction) == GcBifTwo && ImportName(instruction, 2) == "band"
                && Value(Items(instruction, "Operands")[0]) > 0))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            context.Dictionary[Term.A("enabled_guard")] = Term.A("yes");
            Term undefined = Term.A("undefined");
            Term bad = Term.A("not_integer");
            foreach (string function in new[] { "run", "explicit" })
            {
                await Check(
                    function,
                    Term.I(-1),
                    undefined,
                    "accepted"
                );
                await Check(
                    function,
                    Term.I(7),
                    undefined,
                    "accepted"
                );
                await Check(
                    function,
                    Term.I(256),
                    undefined,
                    "rejected"
                );
                await Check(
                    function,
                    bad,
                    undefined,
                    "rejected"
                );
                await Check(
                    function,
                    bad,
                    new MapTerm([]),
                    "accepted"
                );
                await Check(
                    function,
                    Term.I(7),
                    bad,
                    "rejected"
                );
            }

            return Nil.Value;

            async Task Check(
                string function,
                Term port,
                Term address,
                string tag
            )
            {
                Term expected = Term.Tuple(Term.A(tag), port, address);
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    port,
                    address
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });

        string ImportName(object instruction, int operand)
        {
            int imported = Value(Items(instruction, "Operands")[operand]);

            return atoms[(int)Property(imports[imported], "FunctionAtomIndex")];
        }

        object[] Function(string name)
        {
            int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands")[1])] == name);
            if (info < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}

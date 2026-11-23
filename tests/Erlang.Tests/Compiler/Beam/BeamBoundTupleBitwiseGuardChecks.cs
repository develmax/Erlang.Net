using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBoundTupleBitwiseGuardChecks
{
    internal const string TestName = "compiler/beam/keeps-a-checked-tuple-guard-fold-in-x";
    private const string WrongLayout = "The guard used stack scratch or lost a live X input.";
    private const string WrongValue = "The tuple guard changed its success or failure continuation.";
    private const string Source = """
        -module(beam_bound_tuple_bitwise_guard).
        -export([run/1, repeated/1]).

        run(T) ->
            case erlang:get(guard_mode) of
                enabled when tuple_size(T) =:= 3 andalso
                                  ((element(1,T) bor element(2,T) bor element(3,T)) band bnot 255) =:= 0 ->
                    {accepted,T};
                _ -> {rejected,T}
            end.

        repeated(T) ->
            case erlang:get(guard_mode) of
                enabled when tuple_size(T) =:= 3 andalso
                                  ((element(1,T) bor element(1,T) bor element(3,T)) band bnot 255) =:= 0 ->
                    {accepted,T};
                _ -> {rejected,T}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_bound_tuple_bitwise_guard.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Items(instruction, "Operands")[1])] == "run");
        if (info < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[][] operations = body.Where(instruction => Opcode(instruction) == GcBifTwo)
            .Select(instruction => Items(instruction, "Operands")).ToArray();
        int[] roots = operations.Select(operands => Value(operands[1])).ToArray();
        if (!roots.SequenceEqual([2, 2, 1]) || body.Count(instruction => Opcode(instruction) == GetTupleElement) != 3
            || operations.Take(2).Any(operands => Value(operands[3]) != 0 || Value(operands[4]) != 1
                || Value(operands[5]) != 0)
            || Value(operations[2][3]) != 0 || Value(operations[2][5]) != 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            context.Dictionary[Term.A("guard_mode")] = Term.A("enabled");
            Term valid = Term.Tuple(Term.I(1), Term.I(2), Term.I(3));
            Term tooLarge = Term.Tuple(Term.I(1), Term.I(256), Term.I(3));
            Term malformed = Term.Tuple(Term.I(1), Term.A("bad"), Term.I(3));
            Term tooShort = Term.Tuple(Term.I(1), Term.I(2));
            await Check("run", valid, "accepted");
            await Check("run", tooLarge, "rejected");
            await Check("run", malformed, "rejected");
            await Check("run", tooShort, "rejected");
            await Check("run", Term.A("bad"), "rejected");
            await Check("repeated", tooLarge, "accepted");
            context.Dictionary.Remove(Term.A("guard_mode"));
            await Check("run", valid, "rejected");

            return Nil.Value;

            async Task Check(string function, Term input, string tag)
            {
                if (!Term.Tuple(Term.A(tag), input).Equals(await module.Invoke(context, function, input)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }
        });
    }

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}

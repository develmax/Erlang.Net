using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamKnownListBooleanCaseChecks
{
    internal const string TestName = "compiler/beam/dispatches-exhaustive-list-predicate-cases";
    private const string Fixture = "beam_known_list_boolean_case.erl";
    private const string IncorrectLayout = "Only a proven Boolean selector can omit the unmatched case.";
    private const string IncorrectValue = "Case selection changed a branch value, evaluation or failure.";

    private const string Source = """
        -module(beam_known_list_boolean_case).
        %% options
        -export([known/3, opaque/1]).

        known(Value, Values, Prefix) ->
            case lists:member(Value, Values) of
                true -> found;
                false ->
                    case lists:prefix(Prefix, Values) of
                        false -> absent;
                        true -> starts
                    end
            end.

        opaque(Input) ->
            case identity(Input) of
                true -> found;
                false -> absent
            end.

        identity(Value) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = CodeGeneration.CompileBeamModule(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            Fixture
        );
        if (HasCaseFailure(image, "known") || !HasCaseFailure(image, "opaque")
            || !HasCaseFailure(disabled, "known"))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists")]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(
            selection,
            RuntimeNativePrimitives.InternalScalars413,
            CancellationToken.None
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term other = Term.A("other");
            Term values = Term.List(first);
            await Check(
                "found",
                first,
                values,
                Term.I(42)
            );
            await Check(
                "starts",
                other,
                values,
                Nil.Value
            );
            await Check(
                "absent",
                other,
                values,
                Term.List(other)
            );

            Term bad = Term.A("not_boolean");
            try
            {
                await module.Invoke(context, "opaque", bad);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.CaseClause), bad)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(IncorrectValue);

            async Task Check(string result, params Term[] arguments)
            {
                if (!Term.A(result).Equals(await module.Invoke(context, "known", arguments)))
                {
                    throw new InvalidOperationException(IncorrectValue);
                }
            }
        });
    }

    private static bool HasCaseFailure(byte[] image, string name)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, _]
                && atoms[Value(function)] == name
        );
        if (entry < 0)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == CaseEnd);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}

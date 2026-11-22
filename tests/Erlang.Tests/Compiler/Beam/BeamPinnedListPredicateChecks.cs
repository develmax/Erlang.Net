using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPinnedListPredicateChecks
{
    internal const string TestName = "compiler/beam/short-circuits-proven-list-predicate-results";
    private const string Fixture = "beam_pinned_list_predicate.erl";
    private const string WrongLayout = "Only the statically resolved list predicate has a proven boolean result.";
    private const string WrongResult = "A list predicate's short circuit lost the result or evaluated the right operand.";

    private const string Source = """
        -module(beam_pinned_list_predicate).
        %% options
        -export([known/3, shadowed/3, prefix/3]).

        known(Value, Values, Right) -> {tag, lists:member(Value, Values) orelse hd(Right)}.
        prefix(Prefix, Values, Right) -> {tag, lists:prefix(Prefix, Values) andalso hd(Right)}.
        shadowed(Value, Values, Right) -> {tag, member(Value, Values) orelse hd(Right)}.
        member(Value, _Ignored) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = CodeGeneration.CompileBeamModule(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            Fixture
        );
        if (HasInvalidBooleanTest(image, "known") || HasInvalidBooleanTest(image, "prefix")
            || !HasInvalidBooleanTest(image, "shadowed") || !HasInvalidBooleanTest(disabled, "known"))
        {
            throw new InvalidOperationException(WrongLayout);
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
            Term item = Term.A("item");
            Term value = Term.A("value");
            Term yes = Term.A("true");
            Term no = Term.A("false");
            await Check(
                "known",
                item,
                Term.List(item),
                Nil.Value,
                yes
            );
            await Check(
                "known",
                item,
                Nil.Value,
                Term.List(value),
                value
            );
            await Check(
                "prefix",
                Term.List(item),
                Nil.Value,
                Nil.Value,
                no
            );
            await Check(
                "prefix",
                Nil.Value,
                Term.List(item),
                Term.List(value),
                value
            );
            await Check(
                "shadowed",
                no,
                Nil.Value,
                Term.List(value),
                value
            );
            try
            {
                await module.Invoke(
                    context,
                    "known",
                    item,
                    Term.I(42),
                    Nil.Value
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);

            async Task Check(
                string function,
                Term left,
                Term right,
                Term tail,
                Term expected
            )
            {
                Term observed = await module.Invoke(
                    context,
                    function,
                    left,
                    right,
                    tail
                );
                if (!observed.Equals(Term.Tuple(Term.A("tag"), expected)))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }
        });
    }

    private static bool HasInvalidBooleanTest(byte[] image, string name)
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
            throw new InvalidOperationException(WrongLayout);
        }

        return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Select(Opcode).Count(opcode => opcode is ExactEqualTest or ExactNotEqualTest) > 1;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}

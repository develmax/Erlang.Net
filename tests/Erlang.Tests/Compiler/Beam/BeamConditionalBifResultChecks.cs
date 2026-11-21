using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConditionalBifResultChecks
{
    internal const string TestName = "compiler/beam/stores-tested-bif-results-required-across-calls";
    private const string Fixture = "beam_checked_bif_store_probe.erl";
    private const string WrongResultStore = "The checked arithmetic result was stored in the wrong register class.";
    private const string WrongResult = "The checked arithmetic control returned a different value.";

    private const string Source = """
        -module(beam_checked_bif_store_probe).
        -export([positive/3,after_guard/4]).
        positive(Key, Level, Limit) ->
            A = erlang:phash2(Key, power2(Level + 1)) + 1,
            if A > Limit -> A - power2(Level); true -> A end.
        after_guard(Key, Level, Limit, Tag) ->
            A = erlang:phash2(Key, power2(Level + 1)) + 1,
            if A > Limit -> {A, Tag}; true -> A end.
        power2(Level) -> 1 bsl Level.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(beam);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        RequireDestination("positive", BeamBinaryHostNames.Y);
        RequireDestination("after_guard", BeamBinaryHostNames.X);

        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term level = Term.I(2);
            Term high = Term.I(8);
            Term low = Term.I(0);
            Term positive = await module.Invoke(
                context,
                "positive",
                key,
                level,
                high
            );
            Term shifted = await module.Invoke(
                context,
                "positive",
                key,
                level,
                low
            );
            Term wrapped = await module.Invoke(
                context,
                "after_guard",
                key,
                level,
                low,
                key
            );
            if (positive is not Integer result || shifted is not Integer after || result.Value - after.Value != 4
                || !wrapped.Equals(Term.Tuple(result, key)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });

        void RequireDestination(string name, string expected)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(System.Numerics.BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );
            object result = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .SkipWhile(instruction => Opcode(instruction) != ExternalCall)
                .First(instruction => Opcode(instruction) == GcBifTwo);
            if ((string)Property(Items(result, "Operands").Last(), "Kind") != expected)
            {
                throw new InvalidOperationException(WrongResultStore);
            }
        }
    }

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}

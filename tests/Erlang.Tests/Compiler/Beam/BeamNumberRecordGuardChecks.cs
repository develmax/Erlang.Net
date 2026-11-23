using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNumberRecordGuardChecks
{
    internal const string TestName = "compiler/beam/refines-guarded-number-record-fields";
    private const string Fixture = "beam_number_record_guard_probe.erl";
    private const string WrongType = "The record update did not preserve the comparison's proven number range.";
    private const string WrongResult = "The number guard changed the selected record update.";
    private const int NumberType = 0x28;
    private const int LowerBoundFlag = 1 << 13;

    private const string Source = """
        -module(beam_number_record_guard_probe).
        %% options
        -export([positive/2,unknown/2,after_call/2,other/3]).
        -record(row,{field,other}).

        positive(#row{field = Count} = R, X) ->
            N = Count - 1,
            if N < 1 -> {small, X}; true -> R#row{field = N} end.
        unknown(#row{} = R, N) ->
            if N < 1 -> small; true -> R#row{field = N} end.
        after_call(#row{} = R, Count) ->
            N = Count - 1,
            if N < 1 -> small; true -> erlang:put(seen, N), R#row{field = N} end.
        other(#row{} = R, Count, K) ->
            N = Count - 1,
            if K < 1 -> {small, N}; true -> R#row{field = N} end.
        """;

    internal static async Task Run()
    {
        byte[] enabled = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        CheckTypes(enabled, ssaEnabled: true);
        CheckTypes(disabled, ssaEnabled: false);

        var module = BeamModule.Prepare(enabled);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term row = Term.A("row");
            Term value = Term.A("value");
            Term small = Term.A("small");
            Term before = Term.Tuple(row, Term.I(2), value);
            Term after = Term.Tuple(row, Term.I(1), value);
            Term fractional = Term.Tuple(row, new FloatTerm(2.5), value);
            Term adjusted = Term.Tuple(row, new FloatTerm(1.5), value);
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("positive", [before, value], after),
                ("positive", [fractional, value], adjusted),
                ("positive", [after, value], Term.Tuple(small, value)),
                ("unknown", [before, value], Term.Tuple(row, value, value)),
                ("after_call", [before, Term.I(2)], after),
                ("other", [before, Term.I(2), Term.I(2)], after),
                ("other", [before, Term.I(2), Term.I(0)], Term.Tuple(small, Term.I(1)))
            ];
            foreach (var test in cases)
            {
                if (!test.Expected.Equals(await module.Invoke(context, test.Function, test.Arguments)))
                {
                    throw new InvalidOperationException(WrongResult);
                }
            }

            return Nil.Value;
        });
    }

    private static void CheckTypes(byte[] beam, bool ssaEnabled)
    {
        object code = Code(beam);
        object structure = Property(code, "Structure");
        object[] types = Items(Call(Type("BeamTypeTableReader"), "Read", structure), "Entries").ToArray();
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("positive", ssaEnabled);
        Check("unknown", false);
        Check("after_call", false);
        Check("other", false);

        void Check(string function, bool bounded)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
            );
            object update = instructions.Skip(entry + 1).TakeWhile(item => Opcode(item) != FunctionInfo)
                .Single(item => Opcode(item) == UpdateRecord);
            object operand = Items(update, "Operands").Last();
            bool hasBound = Property(operand, "TypeIndex") is int type
                && (int)Property(types[type], "TypeUnion") == NumberType
                && (long)Property(types[type], "Minimum") == 1
                && (int)Property(types[type], "MetadataFlags") == LowerBoundFlag;
            if (hasBound != bounded)
            {
                throw new InvalidOperationException(WrongType);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

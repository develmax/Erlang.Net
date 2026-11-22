using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCrossCallNumericComparisonChecks
{
    internal const string TestName = "compiler/beam/types-numeric-case-comparisons-across-local-calls";
    private const string Fixture = "beam_numeric_case_probe.erl";
    private const string WrongTypes = "The numeric comparison lost a proven prior value or added an invalid type.";
    private const string WrongResult = "The numeric comparison selected a different counter update.";
    private const int IntegerType = 0x20;
    private const int NumberType = 0x28;

    private const string Source = """
        -module(beam_numeric_case_probe).
        %% options
        -export([positive/1,unbounded_integer/2,unknown/2,unrelated/2]).
        -record(counter,{n,p,l}).

        positive(#counter{p=Index,n=Count,l=Level}=Original) ->
            Next = Index + 1,
            Larger = Count + 1,
            Result = case power2(Level) + 1 of
                Early when Early == Next ->
                    Original#counter{n=Larger,p=1,l=Level+1};
                _ ->
                    Original#counter{n=Larger,p=Next}
            end,
            {Result,[Index],[Larger]}.
        unbounded_integer(#counter{l=Level}=Original, Value) ->
            Result = case power2(Level)+1 of
                Early when Early == Value -> Original#counter{l=1};
                _ -> Original#counter{l=Value}
            end,
            Result.
        unknown(#counter{p=Index,l=Level}=Original, Tag) ->
            Next=Index+1,
            Result = case opaque(Level)+1 of
                Early when Early==Next -> Original#counter{p=2};
                _ -> Original#counter{p=Next}
            end,
            {Tag,Result}.
        unrelated(#counter{p=Index,l=Level}=Original, Value) ->
            Next=Index+1,
            Result = case power2(Level)+1 of
                Early when Early==Value -> Original#counter{p=2};
                _ -> Original#counter{p=Next}
            end,
            Result.
        power2(Level) -> 1 bsl Level.
        opaque(Key) -> erlang:get(Key).
        """;

    internal static async Task Run()
    {
        byte[] enabled = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).\n", StringComparison.Ordinal),
            disabled: true
        );
        CheckTypes(enabled, ssaEnabled: true);
        CheckTypes(disabled, ssaEnabled: false);

        var module = BeamModule.Prepare(enabled);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("counter");
            Term a = Term.I(1);
            Term b = Term.I(2);
            Term c = Term.I(3);
            Term d = Term.I(4);
            Term exact = Term.Tuple(
                tag,
                a,
                b,
                a
            );
            Term fractional = Term.Tuple(
                tag,
                a,
                new FloatTerm(2.5),
                a
            );
            Term expectedExact = Term.Tuple(Term.Tuple(
                tag,
                b,
                a,
                b
            ), Term.List(b), Term.List(b));
            Term expectedFractional = Term.Tuple(
                Term.Tuple(
                    tag,
                    b,
                    new FloatTerm(3.5),
                    a
                ),
                Term.List(new FloatTerm(2.5)),
                Term.List(b)
            );
            Term exactResult = await module.Invoke(context, "positive", exact);
            Term fractionalResult = await module.Invoke(context, "positive", fractional);
            if (!exactResult.Equals(expectedExact) || !fractionalResult.Equals(expectedFractional)
                || !(await module.Invoke(
                    context,
                    "unbounded_integer",
                    exact,
                    d
                )).Equals(Term.Tuple(
                    tag,
                    a,
                    b,
                    d
                ))
                || !(await module.Invoke(
                    context,
                    "unrelated",
                    exact,
                    c
                )).Equals(Term.Tuple(
                    tag,
                    a,
                    b,
                    a
                )))
            {
                throw new InvalidOperationException(WrongResult);
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
        Check("positive", ssaEnabled ? [IntegerType, NumberType] : []);
        Check("unbounded_integer", ssaEnabled ? [IntegerType] : []);
        Check("unknown", []);
        Check("unrelated", ssaEnabled ? [IntegerType] : []);

        void Check(string function, int[] expected)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
            );
            object compare = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Single(instruction => Opcode(instruction) == NumericEqualTest);
            int[] actual = Items(compare, "Operands").Skip(1).Select(operand => Property(operand, "TypeIndex"))
                .OfType<int>().Select(type => (int)Property(types[type], "TypeUnion")).Order().ToArray();
            if (!actual.SequenceEqual(expected))
            {
                throw new InvalidOperationException(WrongTypes);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

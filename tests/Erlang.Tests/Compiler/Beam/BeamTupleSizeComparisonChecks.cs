using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTupleSizeComparisonChecks
{
    internal const string TestName = "compiler/beam/uses-successful-tuple-size-bounds-in-guards";
    private const string Fixture = "beam_tuple_range_guard_probe.erl";
    private const string WrongCompare = "The tuple size comparison must carry the successful arity bounds.";
    private const string WrongResult = "The tuple size guard selected a different branch.";
    private const int IntegerType = 0x20;
    private const long MaximumTupleArity = 0xffffff;

    private const string Source = """
        -module(beam_tuple_range_guard_probe).
        %% options
        -export([checked/1,implicit/1,less/1,float_order/1,unrelated/2,wrapped/1]).
        checked(T) when is_tuple(T), tuple_size(T) > 2 -> element(2,T);
        checked(_) -> fallback.
        implicit(T) when tuple_size(T) > 2 -> element(2,T);
        implicit(_) -> fallback.
        less(T) when is_tuple(T), tuple_size(T) < 3 -> yes;
        less(_) -> fallback.
        float_order(T) when is_tuple(T), tuple_size(T) > 2.5 -> yes;
        float_order(_) -> fallback.
        unrelated(A,B) when is_tuple(A), tuple_size(B) > 2 -> yes;
        unrelated(_,_) -> fallback.
        wrapped(X) -> case X of
            [{Pt,_,_}] when is_tuple(Pt), tuple_size(Pt) > 2 -> element(2,Pt);
            _ -> fallback
        end.
        """;

    internal static async Task Run()
    {
        byte[] enabled = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        CheckComparisons(enabled, ssaEnabled: true);
        CheckComparisons(disabled, ssaEnabled: false);

        var module = BeamModule.Prepare(enabled);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term t = Term.A("term");
            Term a = Term.A("a");
            Term b = Term.A("b");
            Term c = Term.A("c");
            Term fallback = Term.A("fallback");
            Term yes = Term.A("yes");
            Term pair = Term.Tuple(a, b);
            Term triple = Term.Tuple(a, b, c);
            (string Function, Term[] Arguments, Term Expected)[] cases =
            [
                ("checked", [triple], b), ("checked", [pair], fallback), ("checked", [t], fallback),
                ("implicit", [triple], b), ("implicit", [t], fallback),
                ("less", [pair], yes), ("less", [triple], fallback), ("less", [t], fallback),
                ("float_order", [pair], fallback), ("float_order", [triple], yes),
                ("unrelated", [pair, triple], yes), ("unrelated", [pair, t], fallback),
                ("wrapped", [Term.List(Term.Tuple(triple, a, a))], b),
                ("wrapped", [Term.List(Term.Tuple(t, a, a))], fallback)
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

    private static void CheckComparisons(byte[] beam, bool ssaEnabled)
    {
        object code = Code(beam);
        object structure = Property(code, "Structure");
        object[] types = Items(Call(Type("BeamTypeTableReader"), "Read", structure), "Entries").ToArray();
        string[] atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check(
            "checked",
            ssaEnabled,
            1,
            3
        );
        Check(
            "implicit",
            ssaEnabled,
            1,
            3
        );
        Check(
            "less",
            ssaEnabled,
            2,
            2
        );
        Check(
            "unrelated",
            ssaEnabled,
            1,
            3
        );
        Check(
            "wrapped",
            ssaEnabled,
            1,
            3
        );
        Check(
            "float_order",
            false,
            1,
            0
        );

        void Check(
            string function,
            bool typed,
            int position,
            int threshold
        )
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == function
            );
            object instruction = instructions.Skip(entry + 1).TakeWhile(item => Opcode(item) != FunctionInfo)
                .Single(item => Opcode(item) is IsLess or IsGreaterOrEqual);
            if (Opcode(instruction) != (typed ? IsGreaterOrEqual : IsLess))
            {
                throw new InvalidOperationException(WrongCompare);
            }
            if (!typed)
            {
                return;
            }

            object[] operands = Items(instruction, "Operands").ToArray();
            if (Property(operands[position], "TypeIndex") is not int type
                || (int)Property(types[type], "TypeUnion") != IntegerType
                || (long)Property(types[type], "Minimum") != 0
                || (long)Property(types[type], "Maximum") != MaximumTupleArity
                || (BigInteger)Property(operands[3 - position], "Value") != threshold)
            {
                throw new InvalidOperationException(WrongCompare);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

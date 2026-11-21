using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFiniteIntegerBoundsChecks
{
    internal const string TestName = "compiler/beam/finite-integer-ranges-from-masks-shifts-and-addition";
    private const string WrongRange = "Expected finite integer bounds only for a proved arithmetic operand.";
    private const string WrongResult = "The bounded integer expression changed its result or error.";
    private const string Source = """
        -module(bitwise_range_probe).
        -export([session/2, adjacent/1, compare/2, negative/2, dynamic/2, checked/2]).

        session(Value,N) ->
            T = mask(Value),
            S = (T bsl 20) band 16#ffffffff,
            O = bitwise_range_probe_peer:opaque(N) - 1,
            {S bor O,T bsl 32}.

        mask(Value) ->
            V = bitwise_range_probe_peer:opaque(Value),
            V band 16#ffffffff.

        adjacent(Value) -> ((Value band 63) bsl 4) + 2.

        compare(Key,Limit) ->
            A = erlang:phash2(Key,512) + 1,
            if
                A > Limit -> above;
                true -> other
            end.

        negative(Value,Other) -> (Value band -1) bor Other.
        dynamic(Value,Offset) -> ((Value band 63) bsl Offset) + 2.
        checked(Value,Limit) when (Value band 63) > Limit -> above;
        checked(_,_) -> other.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, "bitwise_range_probe.erl");
        CheckRanges(beam);
        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!(await image.Invoke(context, "adjacent", Term.I(-1))).Equals(Term.I(1010))
                || !(await image.Invoke(
                    context,
                    "dynamic",
                    Term.I(-1),
                    Term.I(-2)
                )).Equals(Term.I(17))
                || !(await image.Invoke(
                    context,
                    "checked",
                    Term.A("atom"),
                    Term.I(0)
                )).Equals(Term.A("other")))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await image.Invoke(context, "adjacent", Term.A("atom"));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarith")))
            {
            }

            return Nil.Value;
        });
    }

    private static void CheckRanges(byte[] beam)
    {
        object code = Code(beam);
        object structure = Property(code, "Structure");
        string?[] atoms = ((IEnumerable)Property(structure, "Atoms")).Cast<string?>().ToArray();
        object table = Call(Type("BeamTypeTableReader"), "Read", structure);
        object[] types = ((IEnumerable)Property(table, "Entries")).Cast<object>().ToArray();
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();

        object[] session = Find("session", GcBifTwo);
        Expect(
            session[0],
            3,
            0,
            uint.MaxValue
        );
        Expect(
            session[1],
            3,
            0,
            (long)uint.MaxValue << 20
        );
        Expect(
            session[3],
            3,
            0,
            uint.MaxValue
        );
        Expect(
            session[4],
            3,
            0,
            uint.MaxValue
        );
        object[] adjacent = Find("adjacent", GcBifTwo);
        Expect(
            adjacent[1],
            3,
            0,
            63
        );
        Expect(
            adjacent[2],
            3,
            0,
            1008
        );
        Expect(
            Find("compare", IsLess).Single(),
            2,
            1,
            4294967296
        );
        Expect(
            Find("negative", GcBifTwo)[1],
            3,
            null,
            null
        );
        object[] dynamic = Find("dynamic", GcBifTwo);
        Expect(
            dynamic[1],
            3,
            0,
            63
        );
        Expect(
            dynamic[2],
            3,
            null,
            null
        );
        Expect(
            Find("checked", IsLess).Single(),
            2,
            null,
            null
        );

        object[] Find(string name, byte opcode)
        {
            int item = Array.FindIndex(entries, position => atoms[(int)(BigInteger)Property(Operand(instructions[position], 1), "Value")] == name);

            return instructions[entries[item]..entries[item + 1]].Where(operation => Opcode(operation) == opcode).ToArray();
        }

        void Expect(
            object operation,
            int position,
            long? minimum,
            long? maximum
        )
        {
            object? index = Property(Operand(operation, position), "TypeIndex");
            if (index is not int type)
            {
                if (minimum is not null || maximum is not null)
                {
                    throw new InvalidOperationException(WrongRange);
                }

                return;
            }

            object entry = types[type];
            int flags = (int)Property(entry, "MetadataFlags");
            long? emittedMinimum = (flags & LowerBoundFlag) == 0 ? null : (long)Property(entry, "Minimum");
            long? emittedMaximum = (flags & UpperBoundFlag) == 0 ? null : (long)Property(entry, "Maximum");
            if (minimum != emittedMinimum || maximum != emittedMaximum)
            {
                throw new InvalidOperationException(WrongRange);
            }
        }
    }

    private const int LowerBoundFlag = 1 << 13;
    private const int UpperBoundFlag = 1 << 14;

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object Operand(object instruction, int position) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(position);
}

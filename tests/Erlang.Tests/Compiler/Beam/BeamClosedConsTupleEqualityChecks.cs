using System.Collections;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedConsTupleEqualityChecks
{
    internal const string TestName = "compiler/beam/shares-closed-cons-guard-and-exact-equality-fallback";
    private const string WrongLayout = "The proper-list loop did not share its cons projection or allocate only on exhaustion.";
    private const string WrongValue = "The guarded cons loop changed an exact duplicate, term fallback or malformed entry.";
    private const string Source = """
        -module(beam_closed_cons_tuple_equality).
        -export([run/1]).
        run(Input) -> scan(lists:reverse(Input), []).
        scan([], Result) -> Result;
        scan([Opt | Tail], Result) -> scan(Opt, Tail, [], Result).
        scan(Opt, [], Rest, Result) -> scan(lists:reverse(Rest), [Opt | Result]);
        scan(First, [Next | Tail], Rest, Result)
          when tuple_size(First) =:= tuple_size(Next), element(1, First) =:= element(1, Next) ->
            scan(First, Tail, Rest, Result);
        scan(Opt, [Opt | Tail], Rest, Result) -> scan(Opt, Tail, Rest, Result);
        scan(Opt, [Next | Tail], Rest, Result) -> scan(Opt, Tail, [Next | Rest], Result).
        """;

    internal static async Task Run()
    {
        byte[] closed = CodeGeneration.CompileBeamModule(Source, "beam_closed_cons_tuple_equality.erl");
        string exported = Source.Replace("-export([run/1]).", "-export([run/1,scan/2,scan/4]).", StringComparison.Ordinal);
        byte[] publicEntries = CodeGeneration.CompileBeamModule(exported, "beam_closed_cons_tuple_equality.erl");
        byte[] scanTwo = ReadOpcodes(closed, 2);
        byte[] scanFour = ReadOpcodes(closed, 4);
        int cons = Array.IndexOf(scanFour, IsNonemptyList);
        int frame = Array.IndexOf(scanFour, Allocate);
        if (scanTwo.Length == 0 || scanTwo.Count(op => op == GetList) != 1 || scanTwo.Contains(IsNil)
            || cons < 0 || frame <= cons || scanFour.Count(op => op == GetList) != 1
            || scanFour.Count(op => op == BifOne) != 2 || scanFour.Count(op => op == BifTwo) != 2
            || !scanFour.Contains(ExactNotEqualTest))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        CheckLocalTargets(closed);

        var closedModule = BeamModule.Prepare(closed);
        var publicModule = BeamModule.Prepare(publicEntries);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("name");
            Term plain = Term.A("plain");
            Term old = Term.Tuple(key, Term.I(1));
            Term newest = Term.Tuple(key, Term.I(7));
            Term otherArity = Term.Tuple(key, Term.I(3), Term.I(4));
            Term emptyTuple = Term.Tuple();
            Term input = Term.List(
                old,
                plain,
                otherArity,
                emptyTuple,
                plain,
                newest,
                emptyTuple
            );
            Term expected = Term.List(
                otherArity,
                plain,
                newest,
                emptyTuple
            );
            if (!expected.Equals(await closedModule.Invoke(context, "run", input))
                || !Nil.Value.Equals(await closedModule.Invoke(context, "run", Nil.Value)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await Reject([plain, Nil.Value]);
            await Reject([plain, plain, Nil.Value, Nil.Value]);

            return Nil.Value;

            async Task Reject(Term[] arguments)
            {
                try
                {
                    _ = await publicModule.Invoke(context, "scan", arguments);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }


    private static void CheckLocalTargets(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int two = Entry(2);
        int four = Entry(4);
        if (two < 0 || four < 0
            || !Targets(two, TailLocalCall).SequenceEqual([four])
            || !Targets(four, TailLocalCall).SequenceEqual([four, four])
            || !Targets(four, TailLocalCallWithFrame).SequenceEqual([two]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int Entry(int arity)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands")[1])] == "scan"
                && Value(Items(instruction, "Operands")[2]) == arity
            );

            return info >= 0 && Opcode(instructions[info + 1]) == Label
                ? Value(Items(instructions[info + 1], "Operands")[0])
                : -1;
        }

        IEnumerable<int> Targets(int entry, byte opcode)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == Label
                && Value(Items(instruction, "Operands")[0]) == entry);
            if (start < 0)
            {
                return [];
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) == opcode)
                .Select(instruction => Value(Items(instruction, "Operands")[1]));
        }
    }

    private static byte[] ReadOpcodes(byte[] image, int arity)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Items(instruction, "Operands")[1])] == "scan"
            && Value(Items(instruction, "Operands")[2]) == arity
        );
        if (info < 0)
        {
            return [];
        }

        return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Select(Opcode).ToArray();
    }

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static byte Opcode(object item) => (byte)Property(Property(item, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(System.Numerics.BigInteger)Property(operand, "Value");
}

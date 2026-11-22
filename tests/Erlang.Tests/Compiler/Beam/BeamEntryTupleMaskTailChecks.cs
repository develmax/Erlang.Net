using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryTupleMaskTailChecks
{
    internal const string TestName = "compiler/beam/streams-checked-tuple-fields-through-tail-call-guards";
    private const string WrongLayout = "The tuple mask guard failed to preserve the original arguments and two scratch registers.";
    private const string WrongBehavior = "The tuple mask guard changed a successful tail call or its failure.";
    private const string Source = """
        -module(beam_entry_tuple_mask_tail).
        -export([find/3, fallback/3]).

        find(Key = {A,B,C,D}, Pos, Rows)
          when ((A bor B bor C bor D) band bnot 255) =:= 0, (Pos band bnot 255) =:= 0 ->
            lists:keyfind(Key, Pos, Rows).

        fallback(Key = {A,B}, Pos, Rows)
          when ((A bor B) band bnot 255) =:= 0, (Pos band bnot 255) =:= 0 ->
            lists:keyfind(Key, Pos, Rows);
        fallback(_, _, _) -> missed.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_entry_tuple_mask_tail.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(instructions, operation => Opcode(operation) == FunctionInfo
            && atoms[Value(Items(operation, "Operands")[1])] == "find");
        if (info <= 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int infoLabel = info - 1;
        while (infoLabel >= 0 && Opcode(instructions[infoLabel]) == Line)
        {
            infoLabel--;
        }

        object[] body = instructions.Skip(info + 2).TakeWhile(operation => Opcode(operation) != TailExternalCall).ToArray();
        byte[] expected = [
            IsTuple, TestArity, GetTupleElement, GetTupleElement, GcBifTwo,
            GetTupleElement, GcBifTwo, GetTupleElement, GcBifTwo,
            Line, GcBifTwo, ExactEqualTest, GcBifTwo, ExactEqualTest, Line
        ];
        if (infoLabel < 0 || Opcode(instructions[infoLabel]) != Label
            || !expected.SequenceEqual(body.Select(Opcode)))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int rejected = Value(Items(instructions[infoLabel], "Operands")[0]);
        object[][] gc = body.Where(operation => Opcode(operation) == GcBifTwo)
            .Select(operation => Items(operation, "Operands")).ToArray();
        if (!gc.Select(operands => Value(operands[0])).SequenceEqual([rejected, rejected, rejected, 0, rejected])
            || !gc.Select(operands => Value(operands[1])).SequenceEqual([5, 5, 5, 4, 3])
            || gc.Any(operands => Value(operands[^1]) != 3)
            || !body.Where(operation => Opcode(operation) == GetTupleElement)
                .Select(operation => Value(Items(operation, "Operands")[^1])).SequenceEqual([3, 4, 4, 4]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.Tuple(
                Term.I(1),
                Term.I(2),
                Term.I(3),
                Term.I(4)
            );
            Term row = Term.Tuple(key, Term.A("payload"));
            Term rows = Term.List(row);
            if (!row.Equals(await module.Invoke(
                context,
                "find",
                key,
                Term.I(1),
                rows
            )) || !Term.A("missed").Equals(await module.Invoke(
                context,
                "fallback",
                Term.Tuple(Term.I(1), Term.I(256)),
                Term.I(1),
                rows
            )))
            {
                throw new InvalidOperationException(WrongBehavior);
            }

            await CheckFailure(Term.A("not_tuple"), Term.I(1));
            await CheckFailure(Term.Tuple(Term.I(1), Term.I(2)), Term.I(1));
            await CheckFailure(Term.Tuple(
                Term.I(1),
                Term.I(2),
                Term.I(3),
                Term.A("not_integer")
            ), Term.I(1));
            await CheckFailure(key, new FloatTerm(1.0));
            await CheckFailure(key, Term.I(256));

            return Nil.Value;

            async Task CheckFailure(Term input, Term position)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "find",
                        input,
                        position,
                        rows
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongBehavior);
            }
        });
    }

    private static object[] Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}

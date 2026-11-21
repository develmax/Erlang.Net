using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedTaggedConsEntryChecks
{
    internal const string TestName = "compiler/beam/shares-tagged-cons-entry-roots-across-the-retry";
    private const string WrongLayout = "Expected one first list split, two independent tail roots and one hot heap check.";
    private const string WrongValue = "The shared list prefix changed a grouped result or its rejected input.";
    private const string Source = """
        -module(shared_tagged_cons_entry).
        -export([pick/2]).

        pick(ready, State) -> State;
        pick({open, _, root, _, _}, []) -> [{root, []}];
        pick({open, _, Name, _, _}, State) -> [{Name, []}|State];
        pick({write, Value}, [{Name, _}|State]) -> [{Name, Value}|State];
        pick({close, _Ignored, _, _}, [Entity, {Parent, Old}|Tail]) ->
            Cell = {Parent, [Entity|Old]},
            [Cell|Tail];
        pick({close, root, _, _}, [{Value, root}]) -> Value;
        pick(done, {_, Result}) -> Result;
        pick(_, State) -> State.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "shared_tagged_cons_entry.erl");
        CheckLayout(bytes);
        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term root = Term.A("root");
            Term other = Term.A("other");
            Term close = Term.Tuple(
                Term.A("close"),
                root,
                Nil.Value,
                Nil.Value
            );
            Term rejected = Term.Tuple(
                Term.A("close"),
                other,
                Nil.Value,
                Nil.Value
            );
            Term entry = Term.Tuple(Term.I(9), root);
            Term tail = Term.I(3);
            Term singleton = Term.List(entry);
            Term improper = new Cons(entry, tail);
            Term malformed = Term.List(entry, other);
            Term hot = Term.List(entry, Term.Tuple(other, Nil.Value), tail);
            Term hotResult = Term.List(Term.Tuple(other, Term.List(entry)), tail);
            var cases = new (Term Event, Term State, Term Expected)[]
            {
                (close, hot, hotResult),
                (close, singleton, Term.I(9)),
                (rejected, singleton, singleton),
                (close, improper, improper),
                (close, malformed, malformed),
                (close, Nil.Value, Nil.Value),
                (close, other, other)
            };
            foreach (var (eventTerm, state, expected) in cases)
            {
                if (!expected.Equals(await image.Invoke(
                    context,
                    "pick",
                    eventTerm,
                    state
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });
    }

    private static void CheckLayout(byte[] bytes)
    {
        object[] instructions = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>().ToArray();
        int select = Array.FindIndex(instructions, instruction => Opcode(instruction) == SelectTupleArity);
        if (select < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int[] cases = Operands(instructions[select]);
        int close = LabelForArity(4);
        int write = LabelForArity(2);
        if (close >= write)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] branch = instructions[close..write];
        object[] splits = branch.Where(operation => Opcode(operation) == GetList).ToArray();
        if (splits is not [var first, var second] || Operands(first) is not [1, 2, 3]
            || Operands(second) is not [3, 4, 5]
            || !branch.Any(operation => Opcode(operation) == TestHeap && Operands(operation) is [7, 6]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int LabelForArity(int arity)
        {
            for (int index = 3; index + 1 < cases.Length; index += 2)
            {
                if (cases[index] == arity)
                {
                    int name = cases[index + 1];
                    int position = Array.FindIndex(instructions, operation => Opcode(operation) == Label && Operands(operation) is [var label]
                        && label == name);

                    return position >= 0 ? position : throw new InvalidOperationException(WrongLayout);
                }
            }

            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
        .Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray();
}

using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedListHeadArityChecks
{
    internal const string TestName = "compiler/beam/shared-cons-head-partitions-disjoint-tuple-arities";
    private const string WrongDispatch = "The shared list head must dispatch once by ascending tuple arity.";
    private const string WrongFallback = "The shared list head must preserve clause priority and failure.";
    private const int TupleType = 1 << 11;
    private const string Source = """
        -module(list_head_arity_probe).
        -export([entry/1, wildcard/1]).

        entry([separator|Tail]) -> reply(separator,Tail);
        entry([{Name,_N,cascade,Items}|Tail]) -> reply(cascade,Name,Items,Tail);
        entry([{Name,Position}|Tail]) -> reply(plain,Name,Position,Tail);
        entry([{Name,N,check}|Tail]) when is_integer(N) -> reply(checked,Name,N,Tail);
        entry([{Name,N,check}|Tail]) -> reply(check_fallback,Name,N,Tail);
        entry([{Name,N,radio}|Tail]) -> reply(radio,Name,N,Tail);
        entry([]) -> empty.

        wildcard([separator|Tail]) -> reply(separator,Tail);
        wildcard([Head|Tail]) when is_atom(Head) -> reply(Head,Tail);
        wildcard([{A,B,C}|_]) -> reply(A,B,C);
        wildcard([{A,B}|_]) -> reply(A,B);
        wildcard([]) -> empty.

        reply(A,B) -> {A,B}.
        reply(A,B,C) -> {A,B,C}.
        reply(A,B,C,D) -> {A,B,C,D}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "list_head_arity_probe.erl");
        CheckDispatch(image);

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tail = Term.A("improper_tail");
            Term name = Term.A("name");
            Term checkedHead = Term.Tuple(name, Term.I(4), Term.A("check"));
            Term nonInteger = Term.A("a_number_is_not_required");
            Term uncheckedHead = Term.Tuple(name, nonInteger, Term.A("check"));
            Term radioHead = Term.Tuple(name, Term.I(9), Term.A("radio"));
            Term separator = Term.A("separator");
            Term other = Term.A("other");

            await Expect("entry", new Cons(checkedHead, tail), Term.Tuple(
                Term.A("checked"),
                name,
                Term.I(4),
                tail
            ));
            await Expect("entry", new Cons(uncheckedHead, tail), Term.Tuple(
                Term.A("check_fallback"),
                name,
                nonInteger,
                tail
            ));
            await Expect("entry", new Cons(radioHead, tail), Term.Tuple(
                Term.A("radio"),
                name,
                Term.I(9),
                tail
            ));
            await Expect("entry", new Cons(separator, tail), Term.Tuple(separator, tail));
            await Expect("entry", Nil.Value, Term.A("empty"));
            await Expect("wildcard", new Cons(other, tail), Term.Tuple(other, tail));
            await Expect("wildcard", new Cons(separator, tail), Term.Tuple(separator, tail));

            await ExpectFailure(new Cons(Term.Tuple(name), tail));
            await ExpectFailure(new Cons(Term.Tuple(name, Term.I(1), other), tail));
            await ExpectFailure(other);

            return Nil.Value;

            async Task Expect(string function, Term argument, Term expected)
            {
                if (!(await module.Invoke(context, function, argument)).Equals(expected))
                {
                    throw new InvalidOperationException(WrongFallback);
                }
            }

            async Task ExpectFailure(Term input)
            {
                try
                {
                    await module.Invoke(context, "entry", input);
                    throw new InvalidOperationException(WrongFallback);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
                {
                }
            }
        });
    }

    private static void CheckDispatch(byte[] image)
    {
        object code = Code(image);
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();
        object[] positive = instructions[entries[0]..entries[1]];
        object[] negative = instructions[entries[1]..entries[2]];
        int selectIndex = Array.FindIndex(positive, instruction => Opcode(instruction) == SelectTupleArity);
        if (selectIndex < 2 || Opcode(positive[selectIndex - 1]) != IsTuple
            || Opcode(positive[selectIndex - 2]) != GetList
            || positive.Count(instruction => Opcode(instruction) == SelectTupleArity) != 1
            || positive.Any(instruction => Opcode(instruction) == TestArity)
            || negative.Any(instruction => Opcode(instruction) == SelectTupleArity))
        {
            throw new InvalidOperationException(WrongDispatch);
        }

        object[] selector = Operands(positive[selectIndex]);
        object table = Call(Type("BeamTypeTableReader"), "Read", Property(code, "Structure"));
        object[] types = ((IEnumerable)Property(table, "Entries")).Cast<object>().ToArray();
        object[] typeTest = Operands(positive[selectIndex - 1]);
        if (Property(selector[0], "TypeIndex") is not int index
            || (int)Property(types[index], "TypeUnion") != TupleType
            || selector.Length != 9
            || !Enumerable.Range(0, 3).All(item => (BigInteger)Property(selector[3 + 2 * item], "Value") == item + 2)
            || Property(selector[1], "Value").Equals(Property(typeTest[0], "Value")))
        {
            throw new InvalidOperationException(WrongDispatch);
        }
    }

    private static object[] Operands(object instruction) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}

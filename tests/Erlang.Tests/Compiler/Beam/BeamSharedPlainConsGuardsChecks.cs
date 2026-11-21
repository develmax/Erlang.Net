using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedPlainConsGuardsChecks
{
    internal const string TestName = "compiler/beam/shares-plain-cons-guards-before-allocation";
    internal const string InsertionTestName = "compiler/beam/shares-plain-cons-insertion-tests";
    private const string InsertionModule = "beam_shared_cons_insertion";
    private const string Insertion = "insert";
    private const string WrongInsertion = "The ordered insertion changed its result or shared entry.";
    private const string Module = "beam_shared_plain_cons_guards";
    private const string Function = "merge";
    private const string WrongEntry = "The ordered clauses did not share a register-only entry decision.";
    private const string WrongReturn = "The ordered merge changed a branch result.";
    private const string WrongFailure = "The ordered merge changed its function-clause failure.";

    private const string Source = """
        -module(beam_shared_plain_cons_guards).
        -export([merge/2]).

        merge([A|As], [B|_]=Right) when A < B -> [A|merge(As, Right)];
        merge([A|_]=Left, [B|Bs]) when A > B -> [B|merge(Bs, Left)];
        merge([A|As], [_|Bs]) -> [A|merge(As, Bs)];
        merge([], Right) -> Right;
        merge(Left, []) -> Left.
        """;

    private const string InsertionSource = """
        -module(beam_shared_cons_insertion).
        -export([insert/2]).

        insert(Key, [Head|Tail]) when Key > Head -> [Head|insert(Key, Tail)];
        insert(Key, [Head|_]=Input) when Key < Head -> [Key|Input];
        insert(_, [_|_]=Input) -> Input;
        insert(Key, []) -> [Key].
        """;

    internal static async Task RunInsertion()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(InsertionSource, InsertionModule + ".erl");
        object[] instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        byte[] entry = opcodes.SkipWhile(opcode => opcode != IsNonemptyList).TakeWhile(opcode => opcode != Allocate).ToArray();
        if (!entry.SequenceEqual([IsNonemptyList, GetList, IsLess])
            || opcodes.Count(opcode => opcode == IsNonemptyList) != 1
            || opcodes.Count(opcode => opcode == GetList) != 1
            || opcodes.Count(opcode => opcode == Allocate) != 1)
        {
            throw new InvalidOperationException(WrongInsertion);
        }

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term bad = Term.A("bad_tail");
            Term first = Term.List(one);
            Term second = Term.List(two);
            Term both = Term.List(one, two);
            foreach (var (key, input, expected) in new (Term Key, Term Input, Term Expected)[]
            {
                (one, second, both),
                (two, first, both),
                (two, both, both),
                (one, Nil.Value, first),
                (one, new Cons(one, bad), new Cons(one, bad))
            })
            {
                if (!expected.Equals(await image.Invoke(
                    context,
                    Insertion,
                    key,
                    input
                )))
                {
                    throw new InvalidOperationException(WrongInsertion);
                }
            }

            try
            {
                await image.Invoke(
                    context,
                    Insertion,
                    two,
                    new Cons(one, bad)
                );
                throw new InvalidOperationException(WrongFailure);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }
        });
    }

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object[] instructions = ((IEnumerable)Property(Code(beam), "Instructions")).Cast<object>().ToArray();
        byte[] opcodes = instructions.Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        byte[] entry = opcodes.SkipWhile(opcode => opcode != IsNonemptyList).TakeWhile(opcode => opcode != Allocate).ToArray();
        if (!entry.SequenceEqual([IsNonemptyList, GetList, IsNonemptyList, GetList, IsLess])
            || opcodes.Count(opcode => opcode == IsNonemptyList) != 2
            || opcodes.Count(opcode => opcode == GetList) != 2)
        {
            throw new InvalidOperationException(WrongEntry);
        }

        var frames = instructions.Where((_, index) => opcodes[index] == Allocate)
            .Select(instruction => ((IEnumerable)Property(instruction, "Operands")).Cast<object>()
                .Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray()).ToArray();
        if (frames.Length != 3 || !frames[0].SequenceEqual([1, 4])
            || !frames[1].SequenceEqual([1, 6]) || !frames[2].SequenceEqual([1, 6]))
        {
            throw new InvalidOperationException(WrongEntry);
        }

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term bad = Term.A("bad_tail");
            Term first = Term.List(one);
            Term second = Term.List(two);
            Term merged = Term.List(one, two);
            foreach (var (left, right, expected) in new (Term Left, Term Right, Term Expected)[]
            {
                (first, second, merged),
                (second, first, merged),
                (first, first, first),
                (Nil.Value, bad, bad),
                (bad, Nil.Value, bad)
            })
            {
                if (!expected.Equals(await image.Invoke(
                    context,
                    Function,
                    left,
                    right
                )))
                {
                    throw new InvalidOperationException(WrongReturn);
                }
            }

            try
            {
                await image.Invoke(
                    context,
                    Function,
                    new Cons(one, bad),
                    second
                );
                throw new InvalidOperationException(WrongFailure);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }
        });
    }
}

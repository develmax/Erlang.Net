using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedConsHeadDispatchChecks
{
    internal const string TestName = "compiler/beam/groups-checked-cons-head-dispatch-by-arity-and-tag";
    private const string WrongSchedule = "Expected one checked list head and disjoint tuple groups before the branch frames.";
    private const string WrongResult = "Reordering disjoint head patterns changed an ordered retry or fallback.";
    private const string WrongRoots = "The checked prefix failed to reuse its temporary head and cursor before allocation.";
    private const string Source = """
        -module(beam_shared_cons_head_dispatch).
        -export([walk/1]).
        walk([{word,N,W},{choose,_},{middle,M},{ending,_}|Rest]) ->
            [{chosen,N,W,M}|walk(Rest)];
        walk([{short,N},{finish,_}|Rest]) ->
            [{compact,N}|walk(Rest)];
        walk([{word,N,W},_|Rest]) ->
            [{ordinary,N,W}|walk(Rest)];
        walk([{short,N},{choose,_}|Rest]) ->
            [{short_chosen,N}|walk(Rest)];
        walk([Head={name,N,W}|Rest]) ->
            [{seen,N,W,Head}|walk(Rest)];
        walk([Head|Rest]) ->
            [Head|walk(Rest)];
        walk([]) ->
            [].
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, "beam_shared_cons_head_dispatch.erl");
        object[] instructions = Items(Code(beam), "Instructions").ToArray();
        byte[] body = instructions.Select(Opcode)
            .Where(opcode => opcode is not (FunctionInfo or Label or Line)).ToArray();
        if (body is not [IsNonemptyList, GetList, IsTuple, SelectTupleArity, ..]
            || body.Count(opcode => opcode == SelectTupleArity) != 1
            || body.Count(opcode => opcode == SelectValue) != 1)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        CheckSharedScratch(instructions);

        var image = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term word = Term.Tuple(Term.A("word"), Term.I(1), Term.A("w"));
            Term shortHead = Term.Tuple(Term.A("short"), Term.I(2));
            Term name = Term.Tuple(Term.A("name"), Term.I(3), Term.A("n"));
            Term choose = Term.Tuple(Term.A("choose"), Term.I(4));
            Term middle = Term.Tuple(Term.A("middle"), Term.I(8));
            Term ending = Term.Tuple(Term.A("ending"), Term.I(9));
            Term finish = Term.Tuple(Term.A("finish"), Term.I(5));
            Term any = Term.Tuple(Term.A("any"), Term.I(6));
            Term invalid = Term.Tuple(Term.A("unknown"), Term.I(7));

            Term input = Term.List(
                word,
                choose,
                middle,
                ending,
                shortHead,
                finish,
                word,
                any,
                shortHead,
                choose,
                name,
                invalid,
                word
            );
            Term expected = Term.List(
                Term.Tuple(
                    Term.A("chosen"),
                    Term.I(1),
                    Term.A("w"),
                    Term.I(8)
                ),
                Term.Tuple(Term.A("compact"), Term.I(2)),
                Term.Tuple(Term.A("ordinary"), Term.I(1), Term.A("w")),
                Term.Tuple(Term.A("short_chosen"), Term.I(2)),
                Term.Tuple(
                    Term.A("seen"),
                    Term.I(3),
                    Term.A("n"),
                    name
                ),
                invalid,
                word
            );
            if (!expected.Equals(await image.Invoke(context, "walk", input))
                || !Nil.Value.Equals(await image.Invoke(context, "walk", Nil.Value))
                || !Term.List(Term.Tuple(Term.A("ordinary"), Term.I(1), Term.A("w")), invalid)
                    .Equals(await image.Invoke(context, "walk", Term.List(word, choose, invalid))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckSharedScratch(object[] instructions)
    {
        int frame = Array.FindIndex(instructions, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        object[][] cells = instructions.Take(frame).Where(instruction => Opcode(instruction) == GetList)
            .Select(instruction => Items(instruction, "Operands").ToArray()).ToArray();
        if (cells is not [_, [var initial, var head, var tail], [var middle, var middleHead, var middleTail],
            [var final, var finalHead, var finalTail]]
            || Register(initial) != 0 || Register(head) == 1 || Register(tail) == 0
            || !middle.Equals(tail) || !final.Equals(tail)
            || !middleHead.Equals(head) || !finalHead.Equals(head)
            || !middleTail.Equals(tail) || !finalTail.Equals(tail)
            || Register(Items(instructions[frame], "Operands").Last()) != 7)
        {
            throw new InvalidOperationException(WrongRoots);
        }
    }

    private static int Register(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

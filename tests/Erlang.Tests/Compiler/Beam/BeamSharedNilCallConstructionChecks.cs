using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSharedNilCallConstructionChecks
{
    internal const string TestName = "compiler/beam/keeps-the-shared-list-entry-outside-calling-nil-construction";
    private const string Lists = "lists";
    private const string WrongSchedule = "The list checks and terminal nil call must have separate frame lifetimes.";
    private const string WrongResult = "The framed nil construction changed the list result or its fallback.";
    private const string Source = """
        -module(beam_shared_nil_call_construction).
        -export([walk/1]).
        walk([{start,N},{token,M}|Rest]) ->
            Pair = {N,M},
            [{event,Pair}|walk(Rest)];
        walk([{plain,N}|Rest]) ->
            [{single,N}|walk(Rest)];
        walk([Head|Rest]) ->
            [Head|walk(Rest)];
        walk([]) ->
            [{finished,lists:reverse([first,second])}].
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, "beam_shared_nil_call_construction.erl");
        object[] body = Items(Code(beam), "Instructions")
            .Where(instruction => Opcode(instruction) is not (FunctionInfo or Label or Line)).ToArray();
        if (body.Take(2).Select(Opcode).ToArray() is not [IsNonemptyList, GetList])
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        int empty = Array.FindLastIndex(body, instruction => Opcode(instruction) == IsNil);
        if (empty < 0 || empty + 4 >= body.Length || Opcode(body[empty + 1]) != Allocate
            || Operands(body[empty + 1]).Select(Value).ToArray() is not [0, 0]
            || Opcode(body[empty + 2]) != Move || Opcode(body[empty + 3]) != ExternalCall
            || Opcode(body[empty + 4]) != TestHeap)
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        var image = BeamModule.Prepare(beam, new BeamDependencySelection([Lists]));
        var selection = new RuntimeLibrarySelection(
        [
            RuntimeModuleSelection.Managed("erlang"),
            RuntimeModuleSelection.Managed(Lists)
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(selection);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.Tuple(Term.A("start"), Term.I(1));
            Term second = Term.Tuple(Term.A("token"), Term.I(2));
            Term single = Term.Tuple(Term.A("plain"), Term.I(3));
            Term finish = Term.Tuple(Term.A("finished"), Term.List(Term.A("second"), Term.A("first")));
            Term matching = Term.Tuple(Term.A("event"), Term.Tuple(Term.I(1), Term.I(2)));
            Term converted = Term.Tuple(Term.A("single"), Term.I(3));
            Term other = Term.Tuple(Term.A("token"), Term.I(2), Term.I(4));

            if (!Term.List(finish).Equals(await image.Invoke(context, "walk", Nil.Value))
                || !Term.List(matching, converted, finish).Equals(
                    await image.Invoke(context, "walk", Term.List(first, second, single)))
                || !Term.List(first, other, finish).Equals(
                    await image.Invoke(context, "walk", Term.List(first, other))))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await image.Invoke(context, "walk", Term.A("not_a_list"));
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}

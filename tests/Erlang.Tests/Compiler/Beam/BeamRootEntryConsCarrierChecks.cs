using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRootEntryConsCarrierChecks
{
    internal const string TestName = "compiler/beam/forwards-root-entry-cons-to-local-tail-call";
    private const string WrongLayout = "Expected the original list cell to reach its direct tail call.";
    private const string WrongValue = "Unexpected retained cell, guard fallback or live tail.";
    private const string Source = """
        -module(root_entry_cons_carrier).
        -export([guarded/2, unguarded/2, live_tail/2, standalone/1]).

        guarded([Head | Tail], Tag) when is_tuple(Head) ->
            carry([Head | Tail], Tag);
        guarded(Value, Tag) ->
            {fallback, Tag, Value}.

        unguarded(Tag, [Head | Tail]) ->
            carry([Head | Tail], Tag);
        unguarded(Tag, _) ->
            Tag.

        live_tail([Head | Tail], Tag) when is_tuple(Head) ->
            carry_live([Head | Tail], Tail, Tag);
        live_tail(_, Tag) ->
            Tag.

        standalone([Head | Tail]) ->
            carry([Head | Tail], standalone).

        carry(Value, Tag) ->
            {Tag, Value}.

        carry_live(Value, Tail, Tag) ->
            {Tag, Value, Tail}.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "root_entry_cons_carrier.erl");
        byte[] opcodes = ((IEnumerable)Property(Code(bytes), "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int[] entries = Enumerable.Range(0, opcodes.Length).Where(index => opcodes[index] == FunctionInfo).ToArray();
        if (entries.Length < 4)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        byte[] guarded = opcodes[entries[0]..entries[1]];
        byte[] unguarded = opcodes[entries[1]..entries[2]];
        byte[] liveTail = opcodes[entries[2]..entries[3]];
        if (guarded.Contains(PutList) || !guarded.Contains(IsNonemptyList) || !guarded.Contains(GetHead)
            || unguarded.Contains(PutList) || !unguarded.Contains(IsNonemptyList) || unguarded.Contains(GetList)
            || !liveTail.Contains(PutList))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term head = Term.Tuple(Term.A("head"), Term.I(7));
            Term tail = Term.A("improper_tail");
            Term cons = new Cons(head, tail);
            Term tag = Term.A("tag");
            Term rejectedHead = Term.List(Term.A("not_tuple"));
            if (!Term.Tuple(tag, cons).Equals(await image.Invoke(
                context,
                "guarded",
                cons,
                tag
            ))
                || !Term.Tuple(tag, cons).Equals(await image.Invoke(
                    context,
                    "unguarded",
                    tag,
                    cons
                ))
                || !Term.Tuple(tag, cons, tail).Equals(await image.Invoke(
                    context,
                    "live_tail",
                    cons,
                    tag
                ))
                || !Term.Tuple(Term.A("fallback"), tag, rejectedHead).Equals(
                    await image.Invoke(
                        context,
                        "guarded",
                        rejectedHead,
                        tag
                    ))
                || !tag.Equals(await image.Invoke(
                    context,
                    "unguarded",
                    tag,
                    Nil.Value
                ))
                || !Term.Tuple(Term.A("standalone"), cons).Equals(await image.Invoke(context, "standalone", cons)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });
    }
}

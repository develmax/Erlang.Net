using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGroupedRecordListPrefixChecks
{
    internal const string TestName = "compiler/beam/checks-a-grouped-record-list-prefix-before-allocating";
    private const string File = "beam_grouped_record_list_prefix.erl";
    private const string WrongShape = "A grouped record arm allocated before its checked list prefix.";
    private const string WrongValue = "A grouped record pattern changed the retained values, mismatch, or effect order.";
    private const string Source = """
        -module(beam_grouped_record_list_prefix).
        -export([build/2, observed/0]).
        -record(state, {names, nodes}).
        -record(node, {name}).

        build({close, A, B, C}, #state{names=[_ | Tail], nodes=[#node{name=V}=Child,{W,U} | Rest]} = State) ->
            erlang:put(observed, {A, B, C, V, W, U}),
            {owned, Child, State#state{names=Tail, nodes=Rest}};
        build({open, A, B}, State) ->
            {opened, A, B, State};
        build(Event, State) ->
            {fallback, Event, State}.

        observed() -> erlang:get(observed).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, File);
        CheckShape(beam);
        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term two = Term.I(2);
            Term three = Term.I(3);
            Term state = Term.A("state");
            Term child = Term.Tuple(Term.A("node"), one);
            Term nodes = Term.List(child, Term.Tuple(two, three));
            Term evt = Term.Tuple(
                Term.A("close"),
                one,
                two,
                three
            );
            Term record = Term.Tuple(state, Term.List(three), nodes);
            Term result = Term.Tuple(Term.A("owned"), child, Term.Tuple(state, Nil.Value, Nil.Value));
            if (!result.Equals(await module.Invoke(
                context,
                "build",
                evt,
                record
            )) || !Term.Tuple(
                one,
                two,
                three,
                one,
                two,
                three
            ).Equals(await module.Invoke(context, "observed")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            foreach (Term invalid in new[]
            {
                Term.Tuple(state, Nil.Value, nodes),
                Term.Tuple(state, Term.List(three), Term.List(child)),
                Term.Tuple(state, Term.List(three), Term.List(child, Term.Tuple(two))),
                Term.Tuple(state, Term.List(three), Term.List(Term.Tuple(Term.A("wrong"), one), Term.Tuple(two, three)))
            })
            {
                Term unseenEvent = Term.Tuple(
                    Term.A("close"),
                    three,
                    two,
                    one
                );
                if (!Term.Tuple(Term.A("fallback"), unseenEvent, invalid).Equals(await module.Invoke(
                    context,
                    "build",
                    unseenEvent,
                    invalid
                )) || !Term.Tuple(
                    one,
                    two,
                    three,
                    one,
                    two,
                    three
                ).Equals(await module.Invoke(context, "observed")))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });
    }

    private static void CheckShape(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Items(instruction, "Operands").ElementAt(1))] == "build"
        );
        if (info < 0)
        {
            throw new InvalidOperationException(WrongShape);
        }

        byte[] body = instructions.Skip(info + 1).Select(Opcode).TakeWhile(op => op != FunctionInfo).ToArray();
        int allocation = Array.FindIndex(body, op => op is Allocate or AllocateHeap);
        if (allocation < 0 || !body.Take(allocation).Contains(IsTaggedTuple)
            || body.Take(allocation).Count(op => op == IsNonemptyList) != 3
            || body.Take(allocation).Count(op => op == GetList) != 2
            || body.Take(allocation).Count(op => op == GetTail) != 1)
        {
            throw new InvalidOperationException(WrongShape);
        }
    }

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object instance, string name) => ((IEnumerable)Property(instance, name)).Cast<object>();
}

using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedCheckedEntryRecordChecks
{
    internal const string TestName = "compiler/beam/shares-record-fields-across-an-exclusive-literal-retry";
    private const string Fixture = "beam_shared_checked_entry_record.erl";
    private const string WrongLayout = "The checked record field lost its original branch or live register prefix.";
    private const string WrongValue = "The shared checked projections changed the arguments or record failure.";
    private const string Source = """
        -module(beam_shared_checked_entry_record).
        -export([run/2, observed/0]).
        -record(event, {kind, node, data}).

        run(#event{kind = start, node = Node, data = Data}, State) ->
            erlang:put(first, [Data, Node]),
            erlang:put(second, {Node, Data}),
            {ok, State};
        run(#event{kind = Kind, node = Node, data = Data}, State) ->
            erlang:put(first, other),
            erlang:put(second, [Kind, Node, Data]),
            {ok, State}.

        observed() -> {erlang:get(first), erlang:get(second)}.
        """;

    internal static async Task Run()
    {
        CheckLayout();
        BeamModule module = BeamModule.Prepare(CodeGeneration.CompileBeamModule(Source, Fixture));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("event");
            Term start = Term.A("start");
            Term other = Term.A("other");
            Term node = Term.A("node");
            Term data = Term.A("data");
            Term state = Term.A("state");
            Term expected = Term.Tuple(Term.A("ok"), state);
            if (!expected.Equals(await module.Invoke(
                context,
                "run",
                Term.Tuple(
                    tag,
                    start,
                    node,
                    data
                ),
                state
            )) || !Term.Tuple(Term.List(data, node), Term.Tuple(node, data))
                .Equals(await module.Invoke(context, "observed"))
                || !expected.Equals(await module.Invoke(
                    context,
                    "run",
                    Term.Tuple(
                        tag,
                        other,
                        node,
                        data
                    ),
                    state
                )) || !Term.Tuple(other, Term.List(other, node, data))
                    .Equals(await module.Invoke(context, "observed")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "run",
                    Term.Tuple(tag, start, node),
                    state
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static void CheckLayout()
    {
        const int badShape = 1;
        const int entry = 2;
        const int fallback = 3;
        const int labels = 4;
        byte[] prefix =
        [
            .. Start(
                badShape,
                entry,
                2,
                1
            ),
            IsTaggedTuple, F(badShape), X(0), U(3), A(3),
            GetTupleElement, X(0), U(1), X(1)
        ];
        byte[] select = [ExactEqualTest, F(fallback), X(1), A(4)];
        byte[] payload = [GetTupleElement, X(0), U(2), X(2)];
        byte[] reused = [GetTupleElement, X(0), U(2), X(0)];
        byte[] hot = [Allocate, U(0), U(3), Move, X(2), X(0)];
        byte[] cold = [Allocate, U(0), U(3), Move, X(2), X(0), Move, X(1), X(1)];
        byte[] first = [ExternalCall, U(1), U(0), Deallocate, U(0), Return, Label, U(fallback)];
        byte[] last = [ExternalCall, U(2), U(1), Deallocate, U(0), Return, EndCode];
        byte[] result = Rewrite([.. prefix, .. select, .. payload, .. hot, .. first, .. payload, .. cold, .. last]) ?? [];
        if (!result.AsSpan().SequenceEqual([
            .. prefix, .. reused, .. select, Allocate, U(0), U(1), .. first,
            Allocate, U(0), U(2), Move, X(1), X(1), .. last
        ]) || Rewrite([
            .. prefix, .. select, .. payload, Allocate, U(0), U(3), Move, A(4), X(0), Move, X(2), X(0),
            .. first, .. payload, .. cold, .. last
        ]) is not null
            || Rewrite([
                .. prefix, .. select, .. payload, Allocate, U(0), U(3), Move, X(0), X(1), Move, X(2), X(0),
                .. first, .. payload, .. cold, .. last
            ]) is not null
            || Rewrite([
                .. prefix, .. select, .. payload, .. hot, .. first,
                GetTupleElement, X(0), U(1), X(2), .. cold, .. last
            ]) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        static byte[]? Rewrite(byte[] instructions)
        {
            ReadOnlyMemory<byte> code = instructions;
            object header = New(
                "BeamCodeHeader",
                0,
                (int)DebugLine,
                labels,
                1,
                code
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                8,
                0,
                null,
                null
            );

            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                "ShareCheckedEntryRecordFields",
                code,
                body
            );
        }
    }
}

using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamHeapArithmeticDestinationChecks
{
    internal const string TestName = "compiler/beam/forwards-arithmetic-destination-through-a-cons-heap-check";
    private const string WrongRewrite = "Arithmetic forwarding changed the GC prefix, bypassed a live X0 use, or changed a failure edge.";
    private const string WrongResult = "Arithmetic forwarding changed the constructed list or the badarith reason.";
    private const int LabelCount = 4;

    internal static async Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_heap_arithmetic_destination_probe");
        int plus = (int)Call(
            compiler,
            "Import",
            ErlangModuleNames.Module,
            ErlangOperators.Plus,
            2
        );
        int other = (int)Call(
            compiler,
            "Import",
            ErlangModuleNames.Module,
            "min",
            2
        );
        byte[] before = Fixture(plus);
        byte[] expected = Fixture(plus, forwarded: true);
        if (Rewrite(before) is not { } after || !after.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null
            || Rewrite(Fixture(plus, arithmeticLive: 0)) is not null
            || Rewrite(Fixture(plus, heapLive: 2)) is not null
            || Rewrite(Fixture(plus, readsX0: true)) is not null
            || Rewrite(Fixture(plus, failure: 3)) is not null
            || Rewrite(Fixture(other)) is not null)
        {
            throw new InvalidOperationException(WrongRewrite);
        }

        byte[] wrapped = Fixture(plus, wrapsCons: true);
        if (RewriteWrapped(wrapped) is not { } wrappedAfter
            || !wrappedAfter.AsSpan().SequenceEqual(Fixture(plus, forwarded: true, wrapsCons: true))
            || RewriteWrapped(wrappedAfter) is not null
            || RewriteWrapped(Fixture(plus, wrapsCons: true, tupleReadsX0: true)) is not null
            || RewriteWrapped(Fixture(plus, wrapsCons: true, heapLive: 2)) is not null)
        {
            throw new InvalidOperationException(WrongRewrite);
        }

        byte[] payload = WrappedPayloadFixture(plus);
        if (KeepPayload(payload) is not { } compacted
            || !compacted.AsSpan().SequenceEqual(WrappedPayloadFixture(plus, compacted: true))
            || KeepPayload(compacted) is not null
            || KeepPayload(WrappedPayloadFixture(plus, observesSavedPayload: true)) is not null
            || KeepPayload(WrappedPayloadFixture(plus, usesSavedPayloadInArithmetic: true)) is not null)
        {
            throw new InvalidOperationException(WrongRewrite);
        }

        BeamModule baseline = Prepare(before);
        BeamModule improved = Prepare(after);
        BeamModule wrappedBaseline = Prepare(wrapped);
        BeamModule wrappedImproved = Prepare(wrappedAfter);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            BigInteger large = BigInteger.One << 90;
            Term tail = Term.A("tail");
            Term list = Term.List(tail);
            Term wanted = Term.List(new Integer(large + 1), tail);

            var images = new (BeamModule Module, Term Result)[]
            {
                (baseline, wanted),
                (improved, wanted),
                (wrappedBaseline, Term.Tuple(wanted)),
                (wrappedImproved, Term.Tuple(wanted))
            };
            foreach (var (module, result) in images)
            {
                if (!result.Equals(await module.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    new Integer(large),
                    list
                )))
                {
                    throw new InvalidOperationException(WrongResult);
                }

                try
                {
                    _ = await module.Invoke(
                        context,
                        BeamOpcodeEdgeNames.Run,
                        tail,
                        list
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A("badarith")))
                {
                    continue;
                }

                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });

        byte[]? KeepPayload(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "KeepWrappedArithmeticPayloadInX0",
            code,
            LabelCount
        );

        byte[]? RewriteWrapped(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ForwardWrappedHeapArithmeticDestination",
            code,
            LabelCount
        );

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "ForwardHeapArithmeticDestination",
            code,
            LabelCount
        );
    }

    private static BeamModule Prepare(byte[] code) => BeamModule.Prepare(Image(
        code[..^1],
        labels: LabelCount,
        arity: 2,
        importFunction: ErlangOperators.Plus,
        importArity: 2
    ));

    private static byte[] WrappedPayloadFixture(
        int import,
        bool compacted = false,
        bool observesSavedPayload = false,
        bool usesSavedPayloadInArithmetic = false
    )
    {
        byte result = X(compacted ? 2 : 3);
        byte[] save = compacted ? [] : [Move, X(0), X(2)];
        byte[] observe = observesSavedPayload ? [Move, X(2), X(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            .. save, Line, U(1),
            GcBifTwo, F(0), U(compacted ? 2 : 3), U(import), X(usesSavedPayloadInArithmetic ? 2 : 1), I(1), result,
            TestHeap, U(5), U(compacted ? 3 : 4),
            PutList, result, X(1), result,
            PutTuple, X(0), ExtendedListOperand, U(2), X(compacted ? 0 : 2), result,
            .. observe, Return, EndCode
        ];
    }

    private static byte[] Fixture(
        int import,
        bool forwarded = false,
        int arithmeticLive = 2,
        int heapLive = 3,
        bool readsX0 = false,
        int failure = 0,
        bool wrapsCons = false,
        bool tupleReadsX0 = false
    )
    {
        byte[] copy = forwarded ? [] : [Move, X(0), X(2)];
        byte[] rejected = failure == 0 ? [] : [Label, U(failure), Move, X(1), X(0), Return];
        byte[] tuple = wrapsCons ? [PutTuple, X(0), ExtendedListOperand, U(1), X(tupleReadsX0 ? 0 : 2)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            GcBifTwo, F(failure), U(arithmeticLive), U(import), X(0), I(1), X(forwarded ? 2 : 0),
            .. copy,
            TestHeap, U(wrapsCons ? 4 : 2), U(heapLive),
            PutList, X(2), X(readsX0 ? 0 : 1), X(wrapsCons ? 2 : 0),
            .. tuple, Return,
            .. rejected,
            EndCode
        ];
    }
}

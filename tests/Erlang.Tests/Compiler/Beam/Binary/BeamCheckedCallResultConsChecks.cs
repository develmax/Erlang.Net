using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamCheckedCallResultConsChecks
{
    internal const string TestName = "compiler/beam/roots-a-checked-call-tuple-before-building-an-outgoing-cons";
    private const string WrongLayout = "The singleton argument must use the checked tuple's X root without changing a live frame slot.";
    private const int Reject = 3;
    private const int ProducerInfo = 4;
    private const int Producer = 5;
    private const int Handler = 6;
    private const int Labels = 7;
    private const int FunctionCount = 2;
    private const int MarkerAtom = 7;
    private const int NilAtom = 0;

    internal static async Task Run()
    {
        byte[] ordinary = Fixture(caught: false);
        byte[] caught = Fixture(caught: true);
        if (Rewrite(ordinary, out var retired) is not { } tail || !tail.SequenceEqual(Fixture(false, rooted: true))
            || !retired.SetEquals([2])
            || Rewrite(caught, out var retained) is not { } withHandler
            || !withHandler.SequenceEqual(Fixture(true, rooted: true)) || retained.Count != 0
            || Rewrite(Fixture(false, readSlot: true), out _) is not null
            || Rewrite(Fixture(true, initialized: false), out _) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term match = Term.Tuple(Term.A(BeamOpcodeEdgeNames.Marker), Term.I(42));
            Term mismatch = Term.A(BeamOpcodeEdgeNames.Other);
            foreach (byte[] code in new[] { ordinary, tail, caught, withHandler })
            {
                BeamModule module = BeamModule.Prepare(ToImage(code));
                if (!Term.I(1).Equals(await module.Invoke(context, BeamOpcodeEdgeNames.Run, match))
                    || !mismatch.Equals(await module.Invoke(context, BeamOpcodeEdgeNames.Run, mismatch)))
                {
                    throw new InvalidOperationException(WrongLayout);
                }
            }

            return Nil.Value;
        });
    }

    private static byte[] Fixture(
        bool caught,
        bool rooted = false,
        bool readSlot = false,
        bool initialized = true
    )
    {
        byte[] projection = [GetTupleElement, X(0), U(1), X(0)];
        byte[] construction = rooted
            ? [TestHeap, U(2), U(1), .. projection, PutList, X(0), A(NilAtom), X(0)]
            : [.. projection, Move, X(0), Y(0), TestHeap, U(2), U(0), PutList, Y(0), A(NilAtom), X(0)];
        byte[] continuation = caught
            ? [ExternalCall, U(1), U(0), CatchEnter, Y(0), F(Handler), Label, U(Handler), CatchEnd, Y(0), Deallocate, U(1), Return]
            : [TailExternalCallWithFrame, U(1), U(0), U(1)];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            Allocate, U(1), U(1),
            .. (initialized ? new byte[] { InitYRegisters, ExtendedListOperand, U(1), Y(0) } : []),
            LocalCall, U(1), F(Producer),
            IsTaggedTuple, F(Reject), X(0), U(2), A(MarkerAtom),
            .. construction,
            .. (readSlot ? new byte[] { Move, Y(0), X(1) } : []),
            .. continuation,
            Label, U(Reject), Deallocate, U(1), Return,
            .. Start(
                ProducerInfo,
                Producer,
                3,
                1
            ),
            Return,
            EndCode
        ];
    }

    private static byte[] ToImage(byte[] code) => Image(code[..^1], labels: Labels, functions: FunctionCount);

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code, out HashSet<int> emptyFrames)
    {
        object decoded = Code(ToImage(code.ToArray()));
        MethodInfo method = typeof(CodeGeneration).Assembly.GetType("Erlang.Compiler.BeamModuleCompiler")!
            .GetMethod("RewriteCheckedCallResultThroughCons", BindingFlags.NonPublic | BindingFlags.Static)!;
        emptyFrames = [];

        return (byte[]?)method.Invoke(null, [code, Property(decoded, "Instructions"), emptyFrames]);
    }
}

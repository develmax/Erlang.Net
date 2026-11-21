using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTupleTagRetryChecks
{
    internal const string TestName = "compiler/beam/shares-a-repeated-tuple-tag-at-an-owned-retry";
    private const string WrongPath = "A repeated tuple tag changed the retry, return value, or rejected tuple.";
    private const int FunctionInfoLabel = 1;
    private const int EntryLabel = 2;
    private const int RetryLabel = 3;
    private const int RejectedLabel = 4;
    private const int LabelCount = 5;
    private const int Arity = 3;

    internal static async Task Run()
    {
        byte[] original = Fixture();
        byte[] expected = Fixture(shared: true);
        if (Rewrite(original) is not { } shared || !shared.SequenceEqual(expected)
            || Rewrite(Fixture(clobberCarrier: true)) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        var before = BeamModule.Prepare(Image(original[..^1], labels: LabelCount, arity: Arity));
        var after = BeamModule.Prepare(Image(shared[..^1], labels: LabelCount, arity: Arity));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term accepted = Term.A(BeamOpcodeEdgeNames.Module);
            Term rejected = Term.A(BeamOpcodeEdgeNames.Run);
            Term value = Term.I(42);
            Term other = Term.A("nonempty");
            foreach (var (input, condition, wanted) in new[]
            {
                (Term.Tuple(accepted, value), (Term)Nil.Value, accepted),
                (Term.Tuple(accepted, value), other, value),
                (Term.Tuple(rejected, value), other, rejected)
            })
            {
                if (!wanted.Equals(await before.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    value,
                    condition
                )) || !wanted.Equals(await after.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    value,
                    condition
                )))
                {
                    throw new InvalidOperationException(WrongPath);
                }
            }

            return Nil.Value;
        });
    }

    private static byte[] Fixture(bool shared = false, bool clobberCarrier = false)
    {
        byte[] tested = shared ? []
            : [GetTupleElement, X(0), U(0), X(1), ExactEqualTest, F(RejectedLabel), X(1), A(1)];
        byte[] overwritten = clobberCarrier ? [Move, X(2), X(0)] : [];

        return
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                2,
                Arity
            ),
            IsTuple, F(RejectedLabel), X(0), TestArity, F(RejectedLabel), X(0), U(2),
            GetTupleElement, X(0), U(0), X(1),
            ExactEqualTest, F(shared ? RejectedLabel : RetryLabel), X(1), A(1), .. overwritten,
            IsNil, F(RetryLabel), X(2), Move, A(1), X(0), Return,
            Label, U(RetryLabel), .. tested, GetTupleElement, X(0), U(1), X(1), Move, X(1), X(0), Return,
            Label, U(RejectedLabel), Move, A(2), X(0), Return, EndCode
        ];
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
        var labels = new List<(int Start, int End, int Label)>();
        Action<int, int, int> label = (start, end, key) => labels.Add((start, end, key));
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            LabelCount,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            8,
            0,
            label,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RemoveRepeatedFailureProjections",
            code,
            body,
            labels,
            true
        );
    }
}

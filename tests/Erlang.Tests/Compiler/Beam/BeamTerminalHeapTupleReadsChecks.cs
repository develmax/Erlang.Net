using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamTerminalHeapTupleReadsChecks
{
    internal const string TestName = "compiler/beam/sinks-independent-checked-tuple-fields-through-terminal-heap-reservation";
    private const string WrongPath = "The reservation must keep both tuple roots and preserve alternate uses.";
    private const int FunctionInfoLabel = 1;
    private const int EntryLabel = 2;
    private const int RejectLabel = 3;
    private const int LabelCount = 4;
    private const int Arity = 2;

    internal static async Task Run()
    {
        byte[] original = Fixture();
        if (Rewrite(original) is not { } rewritten || !rewritten.SequenceEqual(Fixture(sunk: true))
            || Rewrite(Fixture(readOnSuccess: true)) is not null
            || Rewrite(Fixture(readOnFailure: true)) is not null)
        {
            throw new InvalidOperationException(WrongPath);
        }

        var before = BeamModule.Prepare(Image(original[..^1], labels: LabelCount, arity: Arity));
        var after = BeamModule.Prepare(Image(rewritten[..^1], labels: LabelCount, arity: Arity));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term name = Term.A("row");
            Term value = Term.I(4);
            Term eventTuple = Term.Tuple(Term.I(9), value);
            Term suffix = Term.I(3);
            Term state = Term.List(Term.Tuple(name, Nil.Value), suffix);
            Term result = Term.List(Term.Tuple(name, value), suffix);
            Term wrongShape = Term.List(name, suffix);
            var cases = new (Term Input, Term State, Term Expected)[]
            {
                (eventTuple, state, result),
                (eventTuple, Nil.Value, Nil.Value),
                (eventTuple, wrongShape, wrongShape),
                (name, state, state)
            };
            foreach (var (input, previous, expected) in cases)
            {
                Term baseline = await before.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    previous
                );
                Term optimized = await after.Invoke(
                    context,
                    BeamOpcodeEdgeNames.Run,
                    input,
                    previous
                );
                if (!expected.Equals(baseline) || !expected.Equals(optimized))
                {
                    throw new InvalidOperationException(WrongPath);
                }
            }

            return Nil.Value;
        });
    }

    private static byte[] Fixture(bool sunk = false, bool readOnSuccess = false, bool readOnFailure = false)
    {
        byte[] outer = [GetTupleElement, X(0), U(1), X(0)];
        byte[] inner = [GetTupleElement, X(2), U(0), X(1)];
        byte[] extraGuard = readOnSuccess ? [ExactEqualTest, F(RejectLabel), X(0), I(4)] : [];
        byte[] restore = readOnFailure ? [] : [Move, X(1), X(0)];

        return
        [
            .. Start(
                FunctionInfoLabel,
                EntryLabel,
                2,
                Arity
            ),
            IsTuple, F(RejectLabel), X(0), TestArity, F(RejectLabel), X(0), U(2),
            .. (sunk ? [] : outer), .. extraGuard,
            IsNonemptyList, F(RejectLabel), X(1), GetList, X(1), X(2), X(3),
            IsTuple, F(RejectLabel), X(2), TestArity, F(RejectLabel), X(2), U(2),
            .. (sunk ? [] : inner), TestHeap, U(5), U(4),
            .. (sunk ? outer : []), .. (sunk ? inner : []),
            PutTuple, X(0), ExtendedListOperand, U(2), X(1), X(0),
            PutList, X(0), X(3), X(0), Return,
            Label, U(RejectLabel), .. restore, Return, EndCode
        ];
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code)
    {
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
            null,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "MoveTerminalTupleReadsPastHeap",
            code,
            body
        );
    }
}

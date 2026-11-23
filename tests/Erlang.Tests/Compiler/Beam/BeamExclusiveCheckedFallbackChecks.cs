using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamExclusiveCheckedFallbackChecks
{
    internal const string TestName = "compiler/beam/shares-checked-tuple-projection-on-exclusive-fallback";
    private const string Name = "checked_fallback_probe";
    private const string Source = """
        -module(checked_fallback_probe).
        -export([route/1, run/0]).

        route(Input) ->
            case Input of
                {ok, {prompt,"login: "}, Saved} ->
                    {login,Saved};
                {ok, [{prompt,First},{prompt,Second}], _} ->
                    {prompts,First,Second};
                Other ->
                    {unmatched,Other}
            end.

        run() ->
            {login, data} = route({ok,{prompt,"login: "},data}),
            {prompts, first, second} = route({ok,[{prompt,first},{prompt,second}],data}),
            {unmatched, invalid} = route(invalid),
            {unmatched, {ok, short}} = route({ok,short}),
            {unmatched, {no, [{prompt,first},{prompt,second}], data}} =
                route({no,[{prompt,first},{prompt,second}],data}),
            {unmatched, {ok, wrong, data}} = route({ok,wrong,data}),
            ok.
        """;

    private enum Variant
    {
        Ordinary,
        ThirdEdge,
        Fallthrough,
        Aliased,
        DifferentField,
        Protected,
        Receive,
        Debug,
        Heap,
        DifferentInput
    }

    internal static async Task Run()
    {
        byte[] original = Fixture(Variant.Ordinary);
        byte[] expected = Fixture(Variant.Ordinary, compact: true);
        if (Rewrite(original) is not { } rewritten || !rewritten.AsSpan().SequenceEqual(expected)
            || Rewrite(expected) is not null || Rewrite(original, allowSharing: false) is not null)
        {
            throw new InvalidOperationException(BeamExclusiveCheckedFallbackDiagnostics.Exclusive);
        }

        foreach (Variant variant in Enum.GetValues<Variant>().Where(variant => variant != Variant.Ordinary))
        {
            if (Rewrite(Fixture(variant)) is not null)
            {
                throw new InvalidOperationException(BeamExclusiveCheckedFallbackDiagnostics.Shared);
            }
        }

        byte[] arity = ArityFallback(Variant.Ordinary);
        byte[] compactArity = ArityFallback(Variant.Ordinary, compact: true);
        if (Rewrite(arity) is not { } arityRewrite || !arityRewrite.AsSpan().SequenceEqual(compactArity)
            || Rewrite(compactArity) is not null || Rewrite(arity, allowSharing: false) is not null)
        {
            throw new InvalidOperationException(BeamExclusiveCheckedFallbackDiagnostics.Exclusive);
        }

        foreach (Variant variant in new[]
        {
            Variant.ThirdEdge, Variant.Fallthrough, Variant.Aliased, Variant.DifferentField, Variant.DifferentInput
        })
        {
            if (Rewrite(ArityFallback(variant)) is not null)
            {
                throw new InvalidOperationException(BeamExclusiveCheckedFallbackDiagnostics.Shared);
            }
        }

        byte[] sourceImage = CodeGeneration.CompileBeamModule(Source, Name + ".erl");
        var module = BeamModule.Prepare(sourceImage, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            if (!Term.A("ok").Equals(await module.Invoke(context, "run")))
            {
                throw new InvalidOperationException(BeamExclusiveCheckedFallbackDiagnostics.Source);
            }

            return Nil.Value;
        });
    }

    private static byte[]? Rewrite(ReadOnlyMemory<byte> code, bool allowSharing = true)
    {
        var labels = new List<(int Start, int End, int Label)>();
        Action<int, int, int> recordLabel = (start, end, label) => labels.Add((start, end, label));
        object header = New(
            "BeamCodeHeader",
            0,
            (int)DebugLine,
            8,
            1,
            code
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            4,
            0,
            recordLabel,
            null
        );

        return (byte[]?)Call(
            typeof(BeamModuleCompiler),
            "RemoveRepeatedFailureProjections",
            code,
            body,
            labels,
            allowSharing
        );
    }

    private static byte[] ArityFallback(Variant variant, bool compact = false)
    {
        byte destination = variant == Variant.Aliased ? X(0) : X(2);
        byte[] extra = variant == Variant.ThirdEdge ? [ExactEqualTest, F(3), X(1), A(2)] : [];
        byte[] success = variant == Variant.Fallthrough ? [] : [Return];
        byte[] repeated = compact ? [] :
        [
            IsTuple, F(4), X(0),
            TestArity, F(4), X(0), U(2),
            GetTupleElement, X(0), U(variant == Variant.DifferentField ? 0 : 1), destination
        ];
        byte failure = F(compact ? 4 : 3);

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            .. extra,
            IsTuple, failure, X(0),
            TestArity, failure, X(0), U(2),
            GetTupleElement, X(0), U(1), destination,
            IsLess, F(3), variant == Variant.DifferentInput ? X(0) : destination, X(1),
            Move, destination, X(0),
            .. success,
            Label, U(3),
            .. repeated,
            Move, destination, X(0),
            Return,
            Label, U(4),
            Move, X(1), X(0),
            Return,
            EndCode
        ];
    }

    private static byte[] Fixture(Variant variant, bool compact = false)
    {
        byte destination = variant == Variant.Aliased ? Y(0) : X(0);
        byte[] prefix = variant switch
        {
            Variant.ThirdEdge => [ExactEqualTest, F(3), X(0), A(2)],
            Variant.Protected => [TryEnter, Y(1), F(6)],
            Variant.Receive => [LoopReceive, F(6), X(0)],
            Variant.Debug => [DebugLine, A(1), U(1), U(1), U(1)],
            _ => []
        };
        byte[] heapTest = variant == Variant.Heap ? [TestHeap, U(0), U(1)] : [];
        byte[] successJump = variant == Variant.Fallthrough ? [] : [Jump, F(5)];
        byte[] duplicate = compact ? [] :
        [
            IsTaggedTuple, F(4), Y(0), U(3), A(1),
            GetTupleElement, Y(0), U(variant == Variant.DifferentField ? 2 : 1), destination
        ];
        byte[] finishProtected = variant == Variant.Protected ? [TryEnd, Y(1)] : [];
        byte[] sideExit = variant switch
        {
            Variant.Protected => [Label, U(6), TryCase, Y(1), Deallocate, U(2), Return],
            Variant.Receive => [Label, U(6), Wait, F(2)],
            _ => []
        };

        return
        [
            .. Start(
                1,
                2,
                1,
                1
            ),
            Allocate, U(2), U(1),
            Move, X(0), Y(0),
            InitYRegisters, ExtendedListOperand, U(1), Y(1),
            .. prefix,
            IsTaggedTuple, F(compact ? 4 : 3), Y(0), U(3), A(1),
            .. heapTest,
            GetTupleElement, Y(0), U(1), destination,
            ExactEqualTest, F(3), variant == Variant.DifferentInput ? Y(1) : destination, A(2),
            Move, A(2), X(0),
            .. successJump,
            Label, U(3),
            .. duplicate,
            IsNonemptyList, F(4), destination,
            Line, U(3),
            Move, A(3), X(0),
            Jump, F(5),
            Label, U(4),
            Line, U(4),
            Move, Y(0), X(0),
            Label, U(5),
            .. finishProtected,
            Deallocate, U(2),
            Return,
            .. sideExit,
            EndCode
        ];
    }
}

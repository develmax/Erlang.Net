using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSharedArityDispatchRegisterOrderChecks
{
    internal const string TestName = "compiler/beam/orders-registers-in-exclusive-arity-selection-arms";
    private const string WrongLayout = "Reassigning a tag and payload requires an exclusive region with unchanged implicit inputs.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_shared_arity_dispatch_register_order");
        _ = Call(compiler, "Intern", "first");
        _ = Call(compiler, "Intern", "second");
        _ = Call(compiler, "Intern", "third");
        if (Apply(Fixture()) is not { } changed || !changed.SequenceEqual(Fixture(ordered: true))
            || Apply(changed) is not null || Apply(Fixture(usesFallback: true)) is not null
            || Apply(Fixture(partialHeap: true)) is not null || Apply(Fixture(wideCall: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "OrderSharedArityDispatchRegisters",
            code,
            9
        );
    }

    private static byte[] Fixture(
        bool ordered = false,
        bool usesFallback = false,
        bool partialHeap = false,
        bool wideCall = false
    )
    {
        byte tag = X(ordered ? 2 : 3);
        byte payload = X(ordered ? 3 : 2);
        byte[] defaultRead = usesFallback ? [Move, X(2), X(0)] : [];
        byte[] call = wideCall ? [ExternalCall, U(3), U(0)] : [];

        return
        [
            .. Start(
                1,
                2,
                1,
                2
            ),
            Move, X(0), X(2),
            IsTuple, F(8), X(1), SelectTupleArity, X(1), F(8), ExtendedListOperand, U(2), U(2), F(3),
            Label, U(3), GetTupleElement, X(1), U(0), tag, GetTupleElement, X(1), U(1), payload,
            SelectValue, tag, F(8), ExtendedListOperand, U(6), A(2), F(4), A(3), F(5), A(4), F(6),
            Label, U(4), TestHeap, U(3), U(partialHeap ? 3 : 4),
            PutTuple, X(0), ExtendedListOperand, U(2), payload, X(0), Return,
            Label, U(5), Move, payload, X(0), Return,
            Label, U(6), IsNil, F(8), X(0), .. call, Move, payload, X(0), Return,
            Label, U(8), .. defaultRead, Return, EndCode
        ];
    }
}

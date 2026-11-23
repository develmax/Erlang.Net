using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamLiteralRecordLeafChecks
{
    internal const string TestName = "compiler/beam/keeps-literal-record-leaf-and-badrecord-in-x";
    private const string WrongLayout = "The literal update and its exclusive error must use the same original X register.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_literal_record_leaf_probe");
        int tag = (int)Call(compiler, "Intern", "state");
        int hint = (int)Call(compiler, "Intern", "reuse");
        int newValue = (int)Call(compiler, "Intern", "new_value");
        byte[] before = Fixture();
        byte[] expected = Fixture(compacted: true);
        if (Rewrite(before) is not { } after || !after.AsSpan().SequenceEqual(expected)
            || Rewrite(after) is not null || Rewrite(Fixture(incoming: 1)) is not null
            || Rewrite(Fixture(dynamicField: true)) is not null
            || Rewrite(Fixture(sharedError: true)) is not null)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        CheckNestedUpdate(compiler, tag, hint);

        return Task.CompletedTask;

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "CompactLiteralRecordUpdateLeaf",
            code,
            4
        );

        byte[] Fixture(
            bool compacted = false,
            int incoming = 2,
            bool dynamicField = false,
            bool sharedError = false
        )
        {
            byte record = compacted ? X(1) : Y(0);
            byte[] allocate = compacted ? [] : [Allocate, U(1), U(incoming), Move, X(1), Y(0)];
            byte[] release = compacted ? [] : [Deallocate, U(1)];
            byte[] other = sharedError ? [IsAtom, F(3), X(0)] : [];

            return
            [
                .. Start(
                    1,
                    2,
                    1,
                    2
                ),
                .. other, .. allocate,
                IsTaggedTuple, F(3), record, U(2), A(tag), TestHeap, U(3), U(compacted ? 2 : 0),
                UpdateRecord, A(hint), U(2), record, X(0), ExtendedListOperand, U(2),
                U(2), dynamicField ? X(0) : A(newValue),
                .. release, Return, Label, U(3), Line, U(1), BadRecord, record, EndCode
            ];
        }
    }

    private static void CheckNestedUpdate(BeamModuleCompiler compiler, int tag, int hint)
    {
        const int TupleBits = 0x0800;
        int type = (int)Call(
            compiler,
            "GetOperandTypeIndex",
            (TupleBits, (long?)null, (long?)null, (int?)null)
        )!;
        foreach (int parent in new[] { 0, 1 })
        {
            if (Rewrite(Nested()) is not { } actual || !actual.SequenceEqual(Nested(direct: true))
                || Rewrite(actual) is not null || Rewrite(Nested(otherPayload: true)) is not null
                || Rewrite(Nested(wrongField: true)) is not null || Rewrite(Nested(sharedError: true)) is not null)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            byte[] Nested(
                bool direct = false,
                bool otherPayload = false,
                bool wrongField = false,
                bool sharedError = false
            )
            {
                byte outer = direct ? X(parent) : Y(1);
                byte child = direct ? X(2) : Y(0);
                byte input = direct ? X(1 - parent) : Y(2);
                byte relay = direct ? X(1 - parent) : Y(2);
                byte innerResult = direct ? relay : X(0);
                byte[] relayStore = direct ? [] : [Move, X(0), Y(2)];
                byte[] frame = direct ? [] : [Allocate, U(3), U(2), Move, X(parent), Y(1), Move, X(1 - parent), Y(2)];
                byte[] release = direct ? [] : [Deallocate, U(3)];
                byte[] value = direct ? [ExtendedTypedRegisterOperand, relay, U(type)] : [relay];

                return
                [
                    .. Start(
                        1,
                        2,
                        1,
                        2
                    ),
                    IsTaggedTuple, F(sharedError ? 3 : 1), X(parent), U(2), A(tag), .. frame,
                    GetTupleElement, outer, U(1), child, IsTaggedTuple, F(3), child, U(2), A(tag),
                    TestHeap, U(6), U(direct ? 3 : 0),
                    UpdateRecord, A(hint), U(2), ExtendedTypedRegisterOperand, child, U(type), innerResult,
                    ExtendedListOperand, U(2), U(2), input, .. relayStore,
                    UpdateRecord, A(hint), U(2), ExtendedTypedRegisterOperand, outer, U(type), X(0),
                    ExtendedListOperand, U(2), U(wrongField ? 1 : 2), .. value, .. release,
                    Return, Label, U(3), Line, U(1), BadRecord, otherPayload ? outer : child, EndCode
                ];
            }
        }

        byte[]? Rewrite(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "CompactCheckedNestedRecordLeaf",
            code,
            4
        );
    }

}

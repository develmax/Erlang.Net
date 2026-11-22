using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamSavedConsRecordFieldsChecks
{
    internal const string TestName = "compiler/beam/types-saved-cons-fields-through-a-join-and-trim";
    private const string WrongType = "A saved cons type must hold at every incoming edge of the terminal record update.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_saved_cons_record_fields");
        _ = Call(compiler, "Intern", "subject");
        _ = Call(compiler, "Intern", "copy");
        _ = Call(compiler, "LiteralOperand", Term.List(Term.I(1)));

        if (Apply(Body()) is not { } annotated || !annotated.SequenceEqual(Body(typed: true))
            || Apply(annotated) is not null || Apply(Body(nonConsArm: true)) is not null
            || Apply(Body(cleared: true)) is not null)
        {
            throw new InvalidOperationException(WrongType);
        }

        return Task.CompletedTask;

        byte[]? Apply(ReadOnlyMemory<byte> code) => (byte[]?)Call(
            compiler,
            "AnnotateSavedConsRecordFields",
            code,
            5
        );
    }

    private static byte[] Body(bool typed = false, bool nonConsArm = false, bool cleared = false)
    {
        byte[] literal = nonConsArm ? [I(1)] : [ExtendedLiteralOperand, U(0)];
        byte[] clear = cleared ? [InitYRegisters, ExtendedListOperand, U(1), Y(0)] : [];
        byte[] value = typed ? [ExtendedTypedRegisterOperand, Y(0), U(1)] : [Y(0)];

        return
        [
            .. Start(
                1,
                2,
                2,
                2
            ),
            Allocate, U(3), U(2), Move, X(0), Y(2),
            IsNil, F(3), X(1),
            TestHeap, U(2), U(2), PutList, X(1), A(0), Y(1), Jump, F(4),
            Label, U(3), Move, .. literal, Y(1),
            Label, U(4), TrimFrame, U(1), U(2), .. clear, ExternalCall, U(0), U(0),
            TestHeap, U(3), U(1),
            UpdateRecord, A(3), U(2), Y(1), X(0), ExtendedListOperand, U(2), U(2), .. value,
            Deallocate, U(2), Return, EndCode
        ];
    }
}

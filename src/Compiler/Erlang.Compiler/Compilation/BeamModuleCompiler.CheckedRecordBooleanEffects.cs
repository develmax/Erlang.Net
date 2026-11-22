using static Erlang.Compiler.BeamEmissionFormat;
using SourcePattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CheckedRecordEffectExits(
            int CaseFailure,
            int InnerFailure,
            int OuterFailure,
            int OuterRegister,
            BeamSourceLocation Location
        );

        private bool TryEmitCheckedRecordBooleanEffect(
            GuardedTaggedStateEntry plan,
            List<CheckedRecordEffectExits> exits
        )
        {
            if (plan.Clause.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Case
                    {
                        Value: Expr.RecordAccess
                        {
                            Value: Expr.RecordAccess { Value: Expr.Variable source } outer
                        } inner,
                        Clauses: [var run, var skip]
                    } choice,
                        Expr.Literal { Value: Atom result }
                    ]
                }
                || !plan.LiveRegisters.TryGetValue(source.Name, out int outerRegister)
                || outerRegister < 2 || outerRegister >= plan.Clause.Patterns.Count
                || run is not
                {
                    Guard: null,
                    Patterns: [SourcePattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } effectKey }],
                    Body: Expr.Call
                    {
                        Arguments:
                        [
                            Expr.Literal { Value: var firstArgument },
                            Expr.List { Items: [Expr.Variable message], Tail: null }
                        ]
                    } call
                }
                || skip is not
                {
                    Guard: null,
                    Patterns: [SourcePattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } skipKey }],
                    Body: Expr.Literal { Value: Atom }
                }
                || !plan.LiveRegisters.TryGetValue(message.Name, out int payloadRegister)
                || payloadRegister != plan.Clause.Patterns.Count + 1
                || CaseSourceLocation(choice) is not { Line: > 0 } at
                || at != new BeamSourceLocation(outer.PreprocessorFile ?? location.File, outer.SourceLine)
                || at != new BeamSourceLocation(inner.PreprocessorFile ?? location.File, inner.SourceLine)
                || !TryResolveOrdinaryCallTarget(call, out string? module))
            {
                return false;
            }

            int outerFailure = Label();
            int innerFailure = Label();
            int caseFailure = Label();
            int effect = Label();
            int done = Label();
            int live = payloadRegister + 1;

            // Both projections are guarded before the only frame is entered.
            // The message remains in its original register until the effect call.
            EmitDetailRecordTest(
                outerRegister,
                outer.Arity,
                Term.A(outer.Name),
                outerFailure
            );
            owner.EmitTuplePart(outerRegister, outer.Index, 0);
            EmitDetailRecordTest(
                0,
                inner.Arity,
                Term.A(inner.Name),
                innerFailure
            );
            EmitFrameAllocation(live, 0);
            owner.EmitTuplePart(0, inner.Index, 0);
            owner.EmitMembershipSelect(0, caseFailure, [(effectKey, effect), (skipKey, done)]);

            Mark(effect);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Op(PutList, XRegisterTag, payloadRegister);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(firstArgument, 0);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, call.Arguments.Count);

            Mark(done);
            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn(0);
            exits.Add(new(
                caseFailure,
                innerFailure,
                outerFailure,
                outerRegister,
                at
            ));

            return true;
        }

        private void EmitCheckedRecordEffectExits(CheckedRecordEffectExits exits)
        {
            // All three failures carry the same source annotation. As in
            // beam_jump, they form a contiguous group after the entry arms.
            EmitBooleanCaseFailure(exits.CaseFailure, exits.Location);
            Mark(exits.InnerFailure);
            owner.Op(BadRecord, XRegisterTag, 0);
            Mark(exits.OuterFailure);
            owner.Op(BadRecord, XRegisterTag, exits.OuterRegister);
        }
    }
}

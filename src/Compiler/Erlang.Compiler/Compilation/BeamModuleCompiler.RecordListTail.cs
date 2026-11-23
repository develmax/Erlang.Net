using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitForwardedRecordList(FunctionDefinition function)
        {
            if (function.Arity is < 4 or >= FirstReservedXRegister
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Literal { Value: Atom selector },
                                Expr.List { Tail: null } list,
                                Expr.RecordAccess { Value: Expr.Variable record } field,
                                Expr.Variable retained
                            ]
                    } call
                } clause
                ]
                || !HasUnconditionalParameters(clause)
                || clause.Patterns[^1] is not Pattern.Variable parameter
                || record.Name != parameter.Name || retained.Name != parameter.Name
                || parameter.Name == VariableScopeNames.Wildcard
                || !entries.TryGetValue((call.Function, call.Arguments.Count), out int target)
                || list.Items.Count != function.Arity - 1)
            {
                return false;
            }

            for (int index = 0; index < list.Items.Count; index++)
            {
                if (list.Items[index] is not Expr.Variable item
                    || item.Name == VariableScopeNames.Wildcard
                    || item.Name != ((Pattern.Variable)clause.Patterns[index]).Name)
                {
                    return false;
                }
            }

            int recordRegister = function.Arity - 1;
            int failure = Label();
            owner.EmitRecordTest(
                failure,
                XRegisterTag,
                recordRegister,
                Term.A(field.Name),
                field.Arity
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, list.Items.Count * 2);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);

            // Consume the original list inputs from right to left. Keep X0
            // until its last use and leave the resulting list in argument X1.
            for (int index = list.Items.Count - 1; index >= 0; index--)
            {
                owner.Op(PutList, XRegisterTag, index);
                if (index == list.Items.Count - 1)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, index + 1);
                }

                owner.Operand(XRegisterTag, Math.Max(index, 1));
            }

            owner.EmitTuplePart(recordRegister, field.Index, 2);
            if (recordRegister != 3)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    recordRegister,
                    XRegisterTag,
                    3
                );
            }

            owner.EmitConstant(selector, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(LabelTag, target);
            Mark(failure);
            owner.EmitLine(new(field.PreprocessorFile, field.SourceLine));
            owner.Op(BadRecord, XRegisterTag, recordRegister);

            return true;
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListCallAndConstant(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Sequence
                {
                    Items: [Expr.Call { Module: null, DynamicRemote: false, Arguments:
                        [var format, Expr.List { Items.Count: > 0, Tail: null } values] } call, var result]
                } } clause]
                || !HasUnconditionalParameters(clause) || values.Items.Count != function.Arity
                || !entries.TryGetValue((call.Function, 2), out int target)
                || !AttributeLiteral.TryNormalizeConstant(format, out Term prefix)
                || !AttributeLiteral.TryNormalizeConstant(result, out Term continuation))
            {
                return false;
            }

            for (int index = 0; index < values.Items.Count; index++)
            {
                if (values.Items[index] is not Expr.Variable { Name: not VariableScopeNames.Wildcard } value
                    || value.Name != ((Pattern.Variable)clause.Patterns[index]).Name)
                {
                    return false;
                }
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, function.Arity * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            for (int index = function.Arity - 1; index >= 0; index--)
            {
                owner.Op(PutList, XRegisterTag, index);
                if (index == function.Arity - 1)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, index + 1);
                }

                owner.Operand(XRegisterTag, Math.Max(index, 1));
            }

            owner.EmitConstant(prefix, 0);
            owner.EmitCallLine(call, location);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, target);
            owner.EmitConstant(continuation, 0);
            owner.EmitFrameReturn(0);

            return true;
        }

        private bool TryEmitAppendFormatTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments:
                    [
                        Expr.Binary { Operator: ErlangOperators.Append, Left: var prefix,
                            Right: Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Variable format,
                                Right: var suffix } inner } outer,
                        var arguments
                    ]
                } call } clause]
                || !HasUnconditionalParameters(clause)
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term head)
                || !AttributeLiteral.TryNormalizeConstant(suffix, out Term tail))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(VariableScopeNames.Wildcard))
            {
                return false;
            }

            bool cons = function.Arity == 3 && format.Name == parameters[1]
                && arguments is Expr.List { Items: [Expr.Variable first], Tail: Expr.Variable rest }
                && first.Name == parameters[0] && rest.Name == parameters[2];
            bool forward = function.Arity == 2 && format.Name == parameters[0]
                && arguments is Expr.Variable input && input.Name == parameters[1];
            if (!cons && !forward)
            {
                return false;
            }

            int frame = cons ? 2 : 1;
            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, function.Arity);
            owner.MoveRegister(XRegisterTag, function.Arity - 1, YRegisterTag, 0);
            if (cons)
            {
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            }

            owner.EmitConstant(tail, 1);
            var previous = location;
            owner.EmitArithmeticLine(inner.SourceLine, ref previous);
            int append = owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, append);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(head, 0);
            owner.EmitArithmeticLine(outer.SourceLine, ref previous);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, append);

            if (cons)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(PutList, YRegisterTag, 1);
                owner.Operand(YRegisterTag, 0);
                owner.Operand(XRegisterTag, 1);
            }
            else
            {
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
            }

            owner.EmitCallLine(call, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module, call.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;
        }
    }
}

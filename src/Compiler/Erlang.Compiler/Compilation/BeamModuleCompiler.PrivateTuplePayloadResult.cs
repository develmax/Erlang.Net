using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitPrivateTuplePayloadResult(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable input, Pattern.Variable first, Pattern.Variable second],
                        Body: Expr.Case
                        {
                            Value: Expr.Call
                            {
                                Arguments:
                                [
                                    var moduleArgument,
                                    var functionArgument,
                                    Expr.List { Items: [Expr.Variable source, var suffix], Tail: null }
                                ]
                            } producer,
                            Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple
                                {
                                    Items:
                                        [
                                            Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom errorTag }, Pattern.Variable reason] }
                                        ]
                                }],
                                Body: Expr.Call { Arguments: [Expr.Variable rejected] } failureCall
                            },
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple { Items: [Pattern.Variable measured, Pattern.Variable payload] }],
                                Body: Expr.Sequence
                                {
                                    Items:
                                        [
                                            Expr.Call
                                        {
                                            Arguments:
                                                [
                                                    var format,
                                                    Expr.List { Items: [var firstValue, var secondValue, Expr.Variable amount], Tail: null }
                                                ]
                                        } effect,
                                            Expr.Variable returned
                                        ]
                                }
                            }
                            ]
                        } decision
                    }
                ]
                || !DistinctBindings(
                    input,
                    first,
                    second,
                    measured,
                    payload,
                    reason
                )
                || source.Name != input.Name || rejected.Name != reason.Name || amount.Name != measured.Name
                || returned.Name != payload.Name
                || !TryResolveOrdinaryCallTarget(producer, out string? producerModule)
                || !TryResolveOrdinaryCallTarget(failureCall, out string? failureModule, allowControlTransfer: true)
                || !TryResolveOrdinaryCallTarget(effect, out string? effectModule)
                || !AttributeLiteral.TryNormalizeConstant(moduleArgument, out Term callModule)
                || !AttributeLiteral.TryNormalizeConstant(functionArgument, out Term callFunction)
                || !AttributeLiteral.TryNormalizeConstant(suffix, out Term listEnd)
                || !AttributeLiteral.TryNormalizeConstant(format, out Term formatValue)
                || ReadFirstValue() is not Atom firstConstant
                || ReadSecondValue() is not Integer secondConstant)
            {
                return false;
            }

            const int frame = 1;
            int success = Label();
            int invalid = Label();
            EmitFrameAllocation(1, frame, ConsHeapWords);
            EmitFrameInitialization(0, frame);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(new Cons(listEnd, Nil.Value));
            owner.Operand(XRegisterTag, 2);
            owner.EmitConstant(callFunction, 1);
            owner.EmitConstant(callModule, 0);
            owner.EmitCallLine(producer, location);
            Call(producerModule, producer.Function, 3);
            var producerLine = CallLocation(producer, location);

            owner.EmitTupleCheck(0, 2, invalid);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, success);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(errorTag);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitPreparedFrameTail(new EntryCall(
                failureCall,
                failureModule,
                [],
                []
            ), producerLine, frame);

            Mark(success);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords * 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 0, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(secondConstant);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(firstConstant);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(formatValue, 0);
            owner.EmitCallLine(effect, producerLine);
            Call(effectModule, effect.Function, 2);
            owner.MoveToX0(YRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(invalid);
            EmitCaseEnd(CaseSourceLocation(decision));

            return true;

            Term? ReadFirstValue() => firstValue switch
            {
                Expr.Literal literal => literal.Value,
                Expr.Variable variable when variable.Name == first.Name => ReadClosedSingletonArgument(function, 1),
                _ => null
            };

            Term? ReadSecondValue() => secondValue switch
            {
                Expr.Literal literal => literal.Value,
                Expr.Variable variable when variable.Name == second.Name => ReadPrivateIntegerConstant(function, 2, []),
                _ => null
            };
        }
    }
}

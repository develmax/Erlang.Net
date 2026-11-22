using static Erlang.Compiler.BeamEmissionFormat;
using SourcePattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedNilStatePairResult(
            CheckedNilStateRetry plan,
            List<(int Label, BeamSourceLocation Location)> failures
        )
        {
            if (plan.First.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: SourcePattern.Tuple
                        {
                            Items: [SourcePattern.Variable left, SourcePattern.Variable right]
                        },
                        Value: Expr.Call consumer
                    } match,
                        Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom tag }, Expr.Variable resultLeft, Expr.Variable resultRight]
                    }
                    ]
                }
                || left.Name == VariableScopeNames.Wildcard || right.Name == VariableScopeNames.Wildcard
                || left.Name == right.Name || plan.FirstInputs.ContainsKey(left.Name) || plan.FirstInputs.ContainsKey(right.Name)
                || left.Name != resultLeft.Name || right.Name != resultRight.Name || match.SourceLine <= 0
                || consumer.Arguments.Count < 3 || consumer.Arguments.Count >= FirstReservedXRegister
                || !TryResolveOrdinaryCallTarget(consumer, out string? consumerModule))
            {
                return false;
            }

            Expr.Call? producer = null;
            Term? producerLiteral = null;
            int producerTarget = -1;
            int retainedTarget = -1;
            int stateTarget = -1;
            int projection = -1;
            var constants = new List<(int Destination, Term Value)>();
            for (int destination = 0; destination < consumer.Arguments.Count; destination++)
            {
                Expr argument = consumer.Arguments[destination];
                if (AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                {
                    constants.Add((destination, constant));
                    continue;
                }

                if (destination == 0)
                {
                    return false;
                }

                if (argument is Expr.Variable variable && plan.FirstInputs.TryGetValue(variable.Name, out int register))
                {
                    if (register == 3 && retainedTarget < 0)
                    {
                        retainedTarget = destination;
                        continue;
                    }

                    if (register == 2 && stateTarget < 0)
                    {
                        stateTarget = destination;
                        constants.Add((destination, Term.Tuple(plan.State, plan.Role)));
                        continue;
                    }

                    return false;
                }

                if (producer is not null
                    || argument is not Expr.Call
                    {
                        Arguments:
                        [
                            Expr.Literal { Value: var literal },
                            Expr.List
                        {
                            Items: [Expr.RecordAccess { Value: Expr.Variable source } field],
                            Tail: null
                        }
                        ]
                    } call
                    || !plan.FirstInputs.TryGetValue(source.Name, out int carrier) || carrier != 3
                    || plan.OuterTag.Name != field.Name || plan.OuterWidth != field.Arity
                    || field.Index <= 0 || field.Index >= field.Arity)
                {
                    return false;
                }

                producer = call;
                producerTarget = destination;
                producerLiteral = literal;
                projection = field.Index;
            }

            if (producer is null || retainedTarget < 0 || stateTarget < 0
                || !TryResolveOrdinaryCallTarget(producer, out string? producerModule))
            {
                return false;
            }

            // The closed state is rebuilt as a literal. Only the original
            // checked outer record is live through the first ordinary call.
            EmitFrameAllocation(plan.First.Patterns.Count, 1, 2);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(3, projection, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(producerLiteral!, 0);
            owner.EmitCallLine(producer, location);
            Call(producerModule, producer.Function, producer.Arguments.Count);

            owner.EmitOrderedTailMoves(
                [(producerTarget, 0)],
                constants,
                [(retainedTarget, 0)],
                deferX0Constant: true
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitConstant(constants[0].Value, 0);
            owner.EmitCallLine(consumer, CallLocation(producer, location));
            Call(consumerModule, consumer.Function, consumer.Arguments.Count);

            int failure = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                failure
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(tag);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(0);
            failures.Add((failure, new(match.PreprocessorFile ?? location.File, match.SourceLine)));

            return true;
        }
    }
}

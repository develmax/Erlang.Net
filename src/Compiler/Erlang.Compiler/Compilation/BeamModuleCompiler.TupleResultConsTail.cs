using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNilOrTupleResultConsTail(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil }, Pattern.Variable emptyAcc],
                    Body: Expr.Call empty
                },
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable accumulator],
                    Body: Expr.Case
                    {
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable source] } producer,
                        Clauses:
                            [
                            {
                                Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Variable item, Pattern.Variable rest] }],
                                Body: Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable next, Expr.List { Items: [Expr.Variable head], Tail: Expr.Variable tail }]
                                } consumer
                            }
                            ]
                    }
                }
                ]
                || !DistinctBindings(
                    input,
                    accumulator,
                    item,
                    rest
                ) || emptyAcc.Name == VariableScopeNames.Wildcard
                || source.Name != input.Name || next.Name != rest.Name || head.Name != item.Name || tail.Name != accumulator.Name
                || !entries.TryGetValue((producer.Function, 1), out int producerEntry)
                || !entries.TryGetValue((consumer.Function, 2), out int consumerEntry)
                || !ReturnsTuple(
                    producer,
                    2,
                    [],
                    exact: true
                ))
            {
                return false;
            }

            var inputs = new Dictionary<string, int> { [emptyAcc.Name] = 1 };
            if (!TryEmitRegisterConstructedArgument(empty, inputs, validateOnly: true))
            {
                return false;
            }

            int nonempty = Label();
            owner.EmitNilTest(XRegisterTag, 0, nonempty);
            TryEmitRegisterConstructedArgument(empty, inputs);

            Mark(nonempty);
            EmitFrameAllocation(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(producer, location);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, producerEntry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTuplePart(0, 1, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, consumerEntry);
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }
    }
}

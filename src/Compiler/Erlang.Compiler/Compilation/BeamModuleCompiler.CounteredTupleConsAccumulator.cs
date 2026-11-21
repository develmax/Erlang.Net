using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCounteredTupleConsAccumulator(FunctionDefinition function, int failure)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null
                || owner.IsDeclaredNative((function.Name, function.Arity)) || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [
                            Pattern.Variable { Name: VariableScopeNames.Wildcard },
                            Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable { Name: VariableScopeNames.Wildcard },
                            Pattern.Variable emptyAccum
                        ],
                    Body: Expr.Variable returned
                },
                {
                    Guard: null,
                    Patterns: [
                            Pattern.Variable forwarded,
                            Pattern.List { Items: [Pattern.Tuple tuple], Tail: Pattern.Variable tail },
                            Pattern.Variable counter,
                            Pattern.Variable accumulator
                        ],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [
                                Expr.Variable forwardedAgain,
                                Expr.Variable rest,
                                Expr.Binary
                        {
                            Operator: ErlangOperators.Plus,
                            Left: Expr.Variable count,
                            Right: Expr.Literal { Value: Integer step }
                        } increment,
                                Expr.List
                        {
                            Tail: Expr.Variable prior,
                            Items: [
                                        Expr.Tuple
                            {
                                Items: [
                                                Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable first] } conversion,
                                                Expr.Variable second
                                            ]
                            }
                                    ]
                        }
                            ]
                    } recur
                }
                ] || emptyAccum.Name == VariableScopeNames.Wildcard || returned.Name != emptyAccum.Name
                || !DistinctBindings(
                    forwarded,
                    tail,
                    counter,
                    accumulator
                )
                || recur.Function != function.Name || forwardedAgain.Name != forwarded.Name || rest.Name != tail.Name
                || count.Name != counter.Name || prior.Name != accumulator.Name || step.Value <= 0
                || !entries.TryGetValue((function.Name, function.Arity), out int entry)
                || !TryResolveOrdinaryCallTarget(conversion, out string? module))
            {
                return false;
            }

            var fields = tuple.Items.OfType<Pattern.Variable>().ToArray();
            if (fields.Length != tuple.Items.Count || !DistinctBindings(
                [forwarded, tail, counter, accumulator, .. fields.Where(field => field.Name != VariableScopeNames.Wildcard)]))
            {
                return false;
            }

            int inputField = Array.FindIndex(fields, field => field.Name == first.Name);
            int outputField = Array.FindIndex(fields, field => field.Name == second.Name);
            if (inputField < 0 || outputField < 0 || inputField == outputField)
            {
                return false;
            }

            long? lower = null;
            if (!HasOnlyLocalCalls(
                function,
                function.Arity,
                (caller, call) =>
                {
                    if (ReferenceEquals(caller, function))
                    {
                        return ReferenceEquals(call, recur);
                    }

                    if (call.Arguments[2] is not Expr.Literal { Value: Integer seed }
                        || seed.Value <= 0 || seed.Value > long.MaxValue)
                    {
                        return false;
                    }

                    long value = (long)seed.Value;
                    lower = Math.Min(lower ?? value, value);

                    return true;
                }
            ) || lower is not long minimum)
            {
                return false;
            }

            const int frame = 5;
            const int forwardedRegister = 0;
            const int listRegister = 1;
            const int counterRegister = 2;
            const int accumulatorRegister = 3;
            const int headRegister = 4;
            const int tailRegister = 5;
            const int headSlot = 0;
            const int tailSlot = 1;
            const int counterSlot = 2;
            const int accumulatorSlot = 3;
            const int forwardedSlot = 4;
            const int pairHeapWords = 3;
            int empty = Label();

            owner.EmitNonemptyList(listRegister, empty);
            owner.EmitListParts(
                listRegister,
                XRegisterTag,
                headRegister,
                XRegisterTag,
                tailRegister
            );
            EmitTupleShapeTest(
                XRegisterTag,
                headRegister,
                tuple.Items.Count,
                failure
            );

            // All local entries are seeded with a positive integer and the only
            // recursive edge adds a positive literal. The sum can still allocate.
            var previous = location;
            owner.EmitArithmeticLine(increment.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, tailRegister + 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, increment.Operator, 2));
            owner.EmitTypedRegister(counterRegister, IntegerTypeBits, lower: minimum);
            owner.LiteralOperand(step);
            owner.Operand(XRegisterTag, listRegister);

            EmitFrameAllocation(tailRegister + 1, frame);
            SaveRoot(headRegister, headSlot);
            SaveRoot(tailRegister, tailSlot);
            SaveRoot(listRegister, counterSlot);
            SaveRoot(accumulatorRegister, accumulatorSlot);
            SaveRoot(forwardedRegister, forwardedSlot);

            owner.EmitTuplePart(headRegister, inputField, forwardedRegister);
            owner.EmitCallLine(conversion, previous);
            Call(module, conversion.Function, 1);

            // The unprojected head roots the second field across the call.
            owner.Op(TestHeap, BeamWire.UnsignedTag, pairHeapWords + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(
                headSlot,
                outputField,
                listRegister,
                YRegisterTag
            );
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (XRegisterTag, listRegister));
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, accumulatorSlot);
            owner.Operand(XRegisterTag, accumulatorRegister);
            RestoreRoot(tailSlot, listRegister);
            RestoreRoot(counterSlot, counterRegister);
            RestoreRoot(forwardedSlot, forwardedRegister);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, function.Arity);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, frame);

            Mark(empty);
            owner.EmitNilTest(XRegisterTag, listRegister, failure);
            owner.MoveToX0(XRegisterTag, accumulatorRegister);
            owner.instructions.WriteByte(Return);

            return true;

            void SaveRoot(int source, int target) => owner.MoveRegister(
                XRegisterTag,
                source,
                YRegisterTag,
                target
            );

            void RestoreRoot(int source, int target) => owner.MoveRegister(
                YRegisterTag,
                source,
                XRegisterTag,
                target
            );
        }
    }
}

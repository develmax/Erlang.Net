using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSerialCaughtCallSelection(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [global::Erlang.Pattern.Variable a, global::Erlang.Pattern.Variable b,
                            global::Erlang.Pattern.Variable c, global::Erlang.Pattern.Variable d],
                        Body: Expr.Case
                        {
                            Value: Expr.Try
                            {
                                Body: Expr.Call { Arguments: [var firstArgument, Expr.Tuple { Items.Count: 2 } key, var thirdArgument] } lookup,
                                Clauses: [],
                                Catches:
                                [
                                {
                                    Guard: null,
                                    Patterns: [global::Erlang.Pattern.Tuple
                                    {
                                        Items:
                                        [
                                            global::Erlang.Pattern.Literal { Value: Atom caughtClass },
                                            global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                            global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                        ]
                                    }],
                                    Body: var defaultValue
                                }
                                ],
                                After: null
                            },
                            Clauses:
                            [
                            {
                                Patterns: [global::Erlang.Pattern.Tuple
                                {
                                    Items:
                                        [global::Erlang.Pattern.Variable selected,
                                         global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                                }],
                                Guard: Expr.Binary
                                {
                                    Operator: ErlangOperators.Greater, Left: Expr.Variable inspected, Right: var threshold
                                },
                                Body: Expr.Try
                                {
                                    Body: Expr.Call
                                    {
                                        Arguments: [Expr.Tuple { Items.Count: 2 } first, Expr.Tuple { Items.Count: 4 } second, var last]
                                    } invoke,
                                    Clauses: [],
                                    Catches:
                                        [
                                        {
                                            Guard: null,
                                            Patterns: [global::Erlang.Pattern.Tuple
                                            {
                                                Items:
                                                [
                                                    global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                                    global::Erlang.Pattern.Variable reason,
                                                    global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                                ]
                                            }],
                                            Body: Expr.Tuple
                                            {
                                                Items:
                                                [
                                                    Expr.Literal { Value: Atom wrapped },
                                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom inner }, Expr.Variable caught] }
                                                ]
                                            }
                                        }
                                        ],
                                    After: null
                                }
                            },
                            {
                                Guard: null,
                                Patterns: [global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                Body: Expr.Call fallback
                            }
                            ]
                        }
                    }
                ]
                || !DistinctBindings(
                    a,
                    b,
                    c,
                    d,
                    selected,
                    reason
                ) || selected.Name != inspected.Name || reason.Name != caught.Name
                || !AttributeLiteral.TryNormalizeConstant(firstArgument, out Term lookupHead)
                || !AttributeLiteral.TryNormalizeConstant(thirdArgument, out Term lookupTail)
                || !AttributeLiteral.TryNormalizeConstant(defaultValue, out Term errorValue)
                || !AttributeLiteral.TryNormalizeConstant(threshold, out Term limit)
                || !AttributeLiteral.TryNormalizeConstant(last, out Term invokeTail)
                || !TryResolveOrdinaryCallTarget(lookup, out string? lookupModule) || lookupModule is null
                || !TryResolveOrdinaryCallTarget(invoke, out string? invokeModule) || invokeModule is null
                || !TryResolveOrdinaryCallTarget(fallback, out string? fallbackModule) || fallbackModule is null)
            {
                return false;
            }

            string[] inputs = [a.Name, b.Name, c.Name, d.Name];
            if (!ReadsSerialInputs(fallback.Arguments, inputs)
                || ReadSerialTupleFields(key, inputs) is not { } keyFields
                || ReadSerialTupleFields(first, inputs) is not { } firstFields
                || ReadSerialTupleFields(second, inputs) is not { } secondFields)
            {
                return false;
            }

            int token = inputs.Length;
            int frame = token + 1;
            int lookupHandler = Label();
            int joined = Label();
            int invokeHandler = Label();
            int rejected = Label();
            int reraise = Label();

            EmitFrameAllocation(inputs.Length, frame, keyFields.Count + 1);
            for (int register = inputs.Length - 1; register >= 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    SlotFor(register)
                );
            }

            // Source variables and constants already have values. Preparing
            // these fixed tuples introduces no Erlang failure before the call.
            EmitTuple(1, keyFields, saved: false);
            Enter(lookupHandler);
            owner.EmitConstant(lookupTail, 2);
            owner.EmitConstant(lookupHead, 0);
            owner.EmitCallLine(lookup, location);
            Call(lookupModule, lookup.Function, 3);
            owner.Op(TryEnd, YRegisterTag, token);
            owner.Op(Jump, LabelTag, joined);

            Mark(lookupHandler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.Op(ExactEqualTest, LabelTag, reraise);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(caughtClass);
            owner.EmitConstant(errorValue, 0);

            Mark(joined);
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                rejected
            );
            owner.EmitTuplePart(0, 0, 0);
            owner.Op(IsLess, LabelTag, rejected);
            owner.LiteralOperand(limit);
            owner.Operand(XRegisterTag, 0);

            owner.Op(TestHeap, BeamWire.UnsignedTag, firstFields.Count + secondFields.Count + 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitTuple(0, firstFields, saved: true);
            EmitTuple(1, secondFields, saved: true);

            // Both exits of the first try have released its tag. After the
            // guard, only the false edge still consumes the original arguments.
            Enter(invokeHandler);
            owner.EmitConstant(invokeTail, 2);
            EmitFrameInitialization(0, token);
            owner.EmitCallLine(invoke, location);
            Call(invokeModule, invoke.Function, 3);
            owner.Op(TryEnd, YRegisterTag, token);
            owner.EmitFrameReturn(frame);

            Mark(invokeHandler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(inner.Name)), (XRegisterTag, 1));
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(wrapped.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(frame);

            Mark(rejected);
            for (int register = inputs.Length - 1; register >= 0; register--)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    SlotFor(register),
                    XRegisterTag,
                    register
                );
            }

            owner.EmitCallLine(fallback, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, inputs.Length);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(fallbackModule, fallback.Function, inputs.Length));
            owner.Operand(BeamWire.UnsignedTag, frame);

            Mark(reraise);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            return true;

            int SlotFor(int register) => inputs.Length - register - 1;

            void Enter(int handler)
            {
                owner.Op(TryEnter, YRegisterTag, token);
                owner.Operand(LabelTag, handler);
            }

            void EmitTuple(int destination, IReadOnlyList<(int Input, Term? Constant)> fields, bool saved)
            {
                owner.Op(PutTuple, XRegisterTag, destination);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, fields.Count);
                foreach (var (input, constant) in fields)
                {
                    if (constant is null)
                    {
                        owner.Operand(saved ? YRegisterTag : XRegisterTag, saved ? SlotFor(input) : input);
                    }
                    else
                    {
                        owner.LiteralOperand(constant);
                    }
                }
            }
        }

        private static bool ReadsSerialInputs(IReadOnlyList<Expr> arguments, string[] inputs) =>
            arguments.Count == inputs.Length && arguments.Select((argument, register) =>
                argument is Expr.Variable variable && variable.Name == inputs[register]).All(same => same);

        private static IReadOnlyList<(int Input, Term? Constant)>? ReadSerialTupleFields(Expr.Tuple tuple, string[] inputs)
        {
            var fields = new List<(int Input, Term? Constant)>(tuple.Items.Count);
            foreach (Expr field in tuple.Items)
            {
                if (field is Expr.Variable variable && Array.IndexOf(inputs, variable.Name) is int index && index >= 0)
                {
                    fields.Add((index, null));
                }
                else if (AttributeLiteral.TryNormalizeConstant(field, out Term value))
                {
                    fields.Add((-1, value));
                }
                else
                {
                    return null;
                }
            }

            // A completely closed tuple is materialized as a constant by Core.
            return fields.Any(field => field.Constant is null) ? fields : null;
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitFramedTaggedGuardTails(Expr body)
        {
            if (body is not Expr.Case
                {
                    Value: Expr.Variable input,
                    Clauses:
                    [
                    {
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable field] }],
                        Guard: { } firstGuard,
                        Body: Expr.Call
                        {
                            Arguments: [Expr.Tuple { Items: [Expr.Variable payload, Expr.Variable retained] }]
                        } first
                    },
                    {
                        Patterns: [Pattern.Tuple
                        {
                            Items:
                                [
                                    Pattern.Literal { Value: Atom repeated },
                                    global::Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                ]
                        }],
                        Guard: { } secondGuard,
                        Body: Expr.Call { Module: null, DynamicRemote: false } second
                    },
                    {
                        Patterns: [Pattern.Variable fallback],
                        Guard: null,
                        Body: var otherwise
                    }
                    ]
                }
                || !tag.Equals(repeated) || field.Name == VariableScopeNames.Wildcard || field.Name != payload.Name
                || variables.ContainsKey(field.Name) || deferredPairMembers.ContainsKey(field.Name)
                || !variables.TryGetValue(input.Name, out int source)
                || !variables.TryGetValue(retained.Name, out int kept)
                || fallback.Name != VariableScopeNames.Wildcard
                    && (variables.ContainsKey(fallback.Name) || deferredPairMembers.ContainsKey(fallback.Name))
                || ReadSavedAtomEquality(firstGuard) is not { } high || ReadSavedAtomEquality(secondGuard) is not { } low
                || high.Variable != low.Variable || TermOrder.Compare(high.Key, low.Key, exact: true) <= 0
                || !variables.TryGetValue(high.Variable, out int selector)
                || !TryResolveOrdinaryCall(
                    first,
                    out string? target,
                    preparedArgument: 0,
                    allowControlTransfer: true
                )
                || target is null
                || !entries.TryGetValue((second.Function, second.Arguments.Count), out int secondEntry)
                || !second.Arguments.All(argument => argument is Expr.Variable variable && variables.ContainsKey(variable.Name)
                    || AttributeLiteral.TryNormalizeConstant(argument, out _)))
            {
                return false;
            }

            int firstArm = Label();
            int secondArm = Label();
            int fallbackArm = Label();
            owner.EmitRecordTest(
                fallbackArm,
                YRegisterTag,
                source,
                tag,
                2
            );
            owner.Op(SelectValue, YRegisterTag, selector);
            owner.Operand(LabelTag, fallbackArm);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(low.Key);
            owner.Operand(LabelTag, secondArm);
            owner.LiteralOperand(high.Key);
            owner.Operand(LabelTag, firstArm);

            // Preserve the checked tuple as a Y root during heap reservation. The
            // field is read only by this arm and goes straight to the constructor.
            Mark(firstArm);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(GetTupleElement, YRegisterTag, source);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, kept);
            owner.EmitCallLine(first, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(target, first.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frameSize);

            Mark(secondArm);
            Expr[] arguments = second.Arguments.Select(argument =>
                argument is Expr.Variable variable && variable.Name == low.Variable
                    ? new Expr.Literal(low.Key) : argument).ToArray();
            LoadCallArguments(arguments, null, orderSimpleOperands: true);
            EmitFramedLocalTail(second.Arguments.Count, secondEntry);

            Mark(fallbackArm);
            if (fallback.Name != VariableScopeNames.Wildcard)
            {
                variables.Add(fallback.Name, source);
            }

            if (!TailLocal(otherwise, reuseCallSlots: true, directFunctionReturn: true))
            {
                EmitReturnExpression(otherwise, reuseCallSlots: true, terminalCase: true);
                owner.EmitFrameReturn(frameSize);
            }

            return true;
        }

        private static (string Variable, Atom Key)? ReadSavedAtomEquality(Expr guard)
        {
            if (guard is not Expr.Binary { Operator: ErlangOperators.ExactEqual or ErlangOperators.NumericEqual } compare)
            {
                return null;
            }

            return (compare.Left, compare.Right) switch
            {
                (Expr.Variable variable, Expr.Literal { Value: Atom key }) => (variable.Name, key),
                (Expr.Literal { Value: Atom key }, Expr.Variable variable) => (variable.Name, key),
                _ => null
            };
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedTaggedAppendLoop(FunctionDefinition function, int functionInfo)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [var nil, Erlang.Pattern.Variable fallback],
                    Body: Expr.List { Items: [var defaultHead], Tail: Expr.Variable defaultTail }
                },
                {
                    Guard: null,
                    Patterns: [AliasPattern alias, Erlang.Pattern.Variable leftAccumulator],
                    Body: Expr.Binary
                    {
                        Operator: ErlangOperators.Append, Left: Expr.Variable original, Right: Expr.Variable right
                    } append
                },
                {
                    Guard: null,
                    Patterns:
                        [
                            Erlang.Pattern.List { Items: [Erlang.Pattern.Variable head], Tail: Erlang.Pattern.Variable tail },
                            Erlang.Pattern.Variable accumulator
                        ],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable remaining, Expr.List { Items: [Expr.Variable item], Tail: Expr.Variable carried }]
                    } recur
                }
                ]
                || ListEntryTest(nil) != IsNil || fallback.Name == VariableScopeNames.Wildcard || fallback.Name != defaultTail.Name
                || !AttributeLiteral.TryNormalizeConstant(defaultHead, out Term defaultValue)
                || !TryReadPatternAlias(alias, out var input, out var shape)
                || shape is not Erlang.Pattern.List
                {
                    Tail: Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Items: [Erlang.Pattern.Tuple
                    {
                        Items: [
                            Erlang.Pattern.Literal { Value: Atom outer },
                            Erlang.Pattern.Tuple { Items: [Erlang.Pattern.Literal { Value: Atom inner }, Erlang.Pattern.Variable] } nested
                        ]
                    }]
                }
                || !DistinctBindings(input, leftAccumulator) || !DistinctBindings(head, tail, accumulator)
                || nested.Items[1] is Erlang.Pattern.Variable binding && binding.Name != VariableScopeNames.Wildcard
                    && (binding.Name == input.Name || binding.Name == leftAccumulator.Name)
                || input.Name != original.Name || leftAccumulator.Name != right.Name
                || remaining.Name != tail.Name || item.Name != head.Name || carried.Name != accumulator.Name
                || recur.Function != function.Name || !entries.TryGetValue((recur.Function, 2), out int entry))
            {
                return false;
            }

            bool proper = HasProperListEntry(
                function,
                0,
                followForwarding: true,
                followReturnedLists: true
            );
            int collect = Label();
            int empty = Label();
            const int headRegister = 2;
            const int tailRegister = 3;
            const int payload = 4;
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                headRegister,
                XRegisterTag,
                tailRegister
            );
            EmitDetailRecordTest(
                headRegister,
                2,
                outer,
                collect
            );
            owner.EmitTuplePart(headRegister, 1, payload);
            EmitDetailRecordTest(
                payload,
                2,
                inner,
                collect
            );
            var previous = location;
            owner.EmitArithmeticLine(append.SourceLine, ref previous);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));

            Mark(collect);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, tailRegister + 1);
            owner.Op(PutList, XRegisterTag, headRegister);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                tailRegister,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);

            Mark(empty);
            if (!proper)
            {
                owner.EmitNilTest(XRegisterTag, 0, functionInfo);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(defaultValue);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}

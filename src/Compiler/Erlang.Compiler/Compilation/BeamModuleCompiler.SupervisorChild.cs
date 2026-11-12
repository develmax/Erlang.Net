using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListTupleTailCall(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [AliasPattern { Left: Pattern.List { Tail: null } list, Right: Pattern.Variable whole }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [
                    Expr.Literal { Value: Atom selector }, Expr.Variable projected, Expr.Variable forwarded]
                    } call
                }
            ] || whole.Name == VariableScopeNames.Wildcard || whole.Name != forwarded.Name
                || !entries.TryGetValue((call.Function, 3), out int target))
            {
                return false;
            }

            int tupleIndex = -1;
            int fieldIndex = -1;
            int tupleArity = 0;
            var names = new HashSet<string>(StringComparer.Ordinal) { whole.Name };
            for (int index = 0; index < list.Items.Count; index++)
            {
                if (list.Items[index] is Pattern.Tuple tuple && tupleIndex < 0)
                {
                    tupleIndex = index;
                    tupleArity = tuple.Items.Count;
                    for (int field = 0; field < tupleArity; field++)
                    {
                        if (!ReadVariable(tuple.Items[field]))
                        {
                            return false;
                        }
                        if (tuple.Items[field] is Pattern.Variable variable && variable.Name == projected.Name)
                        {
                            fieldIndex = field;
                        }
                    }
                }
                else if (!ReadVariable(list.Items[index]))
                {
                    return false;
                }
            }

            if (fieldIndex < 0 || projected.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            for (int index = 0; index < list.Items.Count; index++)
            {
                int input = index == 0 ? 0 : 1;
                owner.EmitNonemptyList(input, failure);
                if (index == tupleIndex)
                {
                    owner.EmitListParts(
                        input,
                        XRegisterTag,
                        2,
                        XRegisterTag,
                        1
                    );
                    owner.EmitTupleCheck(2, tupleArity, failure);
                }
                else
                {
                    owner.Op(GetTail, XRegisterTag, input);
                    owner.Operand(XRegisterTag, 1);
                }
            }

            owner.EmitNilTest(XRegisterTag, 1, failure);
            owner.EmitTuplePart(2, fieldIndex, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.EmitConstant(selector, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, target);

            return true;

            bool ReadVariable(Pattern pattern) => pattern is Pattern.Variable variable
                && (variable.Name == VariableScopeNames.Wildcard || names.Add(variable.Name));
        }

        private bool TryEmitSupervisorChildNotification(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable supervisor, Pattern.Variable map, Pattern.Variable args],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable options, Value: Expr.Call
                            {
                                Module: "maps", Function: "get", DynamicRemote: false, Arguments: [
                            Expr.Literal { Value: Atom key }, Expr.Variable input, var defaultOptions]
                            }
                        },
                    Expr.Case
                        {
                            Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Variable ownerArg, Expr.List { Tail: null, Items: [Expr.Call firstSelf, Expr.Variable optionArg] }
                      ]
                            } start, Clauses: [
                            {
                                Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom ok }, Pattern.Variable pid] }],
                                Body: Expr.Sequence
                                {
                                    Items: [
                            Expr.Binary
                                    {
                                        Operator: ErlangOperators.Send, Left: Expr.Variable recipient,
                                        Right: Expr.Tuple { Items: [Expr.Call secondSelf, Expr.Literal { Value: Atom tag }, Expr.Variable payload] }
                                    } send,
                            Expr.Tuple { Items: [Expr.Literal { Value: Atom returnedTag }, Expr.Variable returnedPid] }
                          ]
                                }
                            },
                            { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable returnedError }
                      ]
                        }
                  ]
                    }
                }
            ] || !DistinctBindings(
                supervisor,
                map,
                args,
                options,
                pid
            ) || !DistinctBindings(
                supervisor,
                map,
                args,
                options,
                error
            )
                || input.Name != map.Name || ownerArg.Name != supervisor.Name || optionArg.Name != options.Name
                || recipient.Name != pid.Name || returnedPid.Name != pid.Name || !ok.Equals(returnedTag)
                || payload.Name != args.Name || returnedError.Name != error.Name
                || !IsSelf(firstSelf) || !IsSelf(secondSelf)
                || !AttributeLiteral.TryNormalizeConstant(defaultOptions, out Term defaultValue))
            {
                return false;
            }

            int absent = Label();
            int found = Label();
            int rejected = Label();
            int badMap = Label();
            EmitMapTest(1, badMap);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.InitStackSlot(0);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            owner.Op(GetMapElements, LabelTag, absent);
            owner.EmitTypedRegister(1, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(key);
            owner.Operand(XRegisterTag, 1);
            JumpTo(found);
            Mark(absent);
            owner.EmitConstant(defaultValue, 1);
            Mark(found);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            Self(2);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            var previous = location;
            EmitWindowCall(start, ref previous);
            SaveWindowResult(0);
            EmitDetailRecordTest(
                0,
                2,
                ok,
                rejected
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Self(0);
            owner.EmitTupleHeader(XRegisterTag, 3, 1);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, 1);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            TrimDetailFrame(1, 1);
            owner.EmitArithmeticLine(send.SourceLine, ref previous);
            owner.instructions.WriteByte(Send);
            Load(0);
            owner.EmitFrameReturn(1);

            Mark(rejected);
            owner.EmitFrameReturn(2);
            Mark(badMap);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(Term.A("badmap"));
            owner.Operand(XRegisterTag, 1);
            owner.EmitLine(default);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));

            return true;

            bool IsSelf(Expr.Call call) => call is
            {
                Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                Function: AutoImportNames.Self, Arguments: []
            } && owner.ResolvesErlangBif(call);

            void Self(int destination)
            {
                owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
                owner.Operand(XRegisterTag, destination);
            }
        }
    }
}

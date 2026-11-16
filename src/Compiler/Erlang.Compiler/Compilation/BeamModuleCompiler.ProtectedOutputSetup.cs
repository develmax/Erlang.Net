using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedOutputSetup(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable socket, Pattern.Variable handle],
                Body: Expr.Try
                {
                    Clauses: [], After: null,
                    Body: Expr.Sequence { Items: [
                        Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable notifyHandle] } notify,
                        Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom ok }, Pattern.Variable size] },
                            Value: Expr.Call { Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable optionSocket, var optionKey] } getopt },
                        Expr.Match { Pattern: Pattern.Variable state, Value: Expr.RecordConstruction { Wildcard: null } record },
                        Expr.Match { Pattern: Pattern.Variable buffer, Value: var empty },
                        Expr.Match { Pattern: Pattern.Variable count, Value: var zero },
                        Expr.Match { Pattern: Pattern.Variable pending, Value: var noData },
                        Expr.Match { Pattern: Pattern.Variable selection, Value: var undefined },
                        Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                            Expr.Variable stateArg, Expr.Variable bufferArg, Expr.Variable countArg,
                            Expr.Variable pendingArg, Expr.Variable selectionArg
                        ] } next
                    ] },
                    Catches: [{
                        Patterns: [Pattern.Tuple { Items: [Pattern.Variable kind, Pattern.Variable reason, Pattern.Variable stack] }],
                        Guard: Expr.Binary { Operator: ErlangOperators.ExactEqual, Left: Expr.Variable checkedKind,
                            Right: Expr.Literal { Value: Atom { Name: "error" } error } },
                        Body: Expr.Sequence { Items: [
                            Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                                Expr.List { Tail: null, Items: [
                                    Expr.Literal marker,
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom classTag }, Expr.Variable reportClass] },
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom reasonTag }, Expr.Variable reportReason] },
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom stackTag }, Expr.Variable reportStack] }
                                ] }
                            ] } report,
                            Expr.Call { Module: ErlangModuleNames.Module, Function: "raise", DynamicRemote: false,
                                Arguments: [Expr.Variable raiseClass, Expr.Variable raiseReason, Expr.Variable raiseStack] } raise
                        ] }
                    }]
                }
            }] || !DistinctBindings(socket, handle, size, state, buffer, count, pending, selection)
                || !DistinctBindings(socket, handle, kind, reason, stack)
                || notifyHandle.Name != handle.Name || optionSocket.Name != socket.Name
                || stateArg.Name != state.Name || bufferArg.Name != buffer.Name || countArg.Name != count.Name
                || pendingArg.Name != pending.Name || selectionArg.Name != selection.Name
                || checkedKind.Name != kind.Name || reportClass.Name != kind.Name || raiseClass.Name != kind.Name
                || reportReason.Name != reason.Name || raiseReason.Name != reason.Name
                || reportStack.Name != stack.Name || raiseStack.Name != stack.Name
                || !AttributeLiteral.TryNormalizeConstant(optionKey, out Term key)
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term bufferValue)
                || !AttributeLiteral.TryNormalizeConstant(zero, out Term countValue)
                || !AttributeLiteral.TryNormalizeConstant(noData, out Term pendingValue)
                || !AttributeLiteral.TryNormalizeConstant(undefined, out Term selectionValue)
                || RecordExpressions.Create(record).Items is not [
                    Expr.Literal { Value: Atom tag }, Expr.Variable stateSocket, Expr.Variable stateHandle,
                    Expr.Binary { Operator: ErlangOperators.ShiftRight, Left: Expr.Variable input,
                        Right: Expr.Literal { Value: Integer shift } } watermark
                ] || stateSocket.Name != socket.Name || stateHandle.Name != handle.Name || input.Name != size.Name
                || shift.Value != 1 || !entries.TryGetValue((next.Function, 5), out int target)
                || !NeverReturns(next, []))
            {
                return false;
            }

            int handler = Label();
            int propagate = Label();
            int badMatch = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
            owner.Op(TryEnter, YRegisterTag, 2);
            owner.Operand(LabelTag, handler);
            owner.MoveToX0(XRegisterTag, 1);
            var previous = location;
            EmitWindowCall(notify, ref previous);
            owner.EmitConstant(key, 1);
            Load(1);
            EmitWindowCall(getopt, ref previous);
            EmitDetailRecordTest(0, 2, ok, badMatch);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitArithmeticLine(watermark.SourceLine, ref previous);
            owner.GcArithmetic(GcBifTwo, ErlangOperators.ShiftRight, 1, 0,
                (XRegisterTag, 0, 0), (BeamWire.IntegerTag, 1, 0));
            Heap(5, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (YRegisterTag, 1), (YRegisterTag, 0), (XRegisterTag, 0));
            owner.EmitConstant(pendingValue, 3);
            owner.EmitConstant(countValue, 2);
            owner.EmitConstant(selectionValue, 4);
            owner.EmitConstant(bufferValue, 1);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            EmitWindowLocal(next, target, ref previous);
            Mark(handler);
            owner.Op(TryCase, YRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 2);
            owner.Op(ExactEqualTest, LabelTag, propagate);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(error);
            owner.MoveToX0(XRegisterTag, 2);
            owner.instructions.WriteByte(BuildStacktrace);
            SaveWindowResult(1);
            Heap(14, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(stackTag.Name)), (YRegisterTag, 1));
            Cons(XRegisterTag, 0, first: true);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(reasonTag.Name)), (YRegisterTag, 2));
            Cons(XRegisterTag, 1);
            LiteralCons(Term.Tuple(classTag, error));
            LiteralCons(marker.Value);
            EmitWindowCall(report, ref previous);
            Load(1, 2);
            Load(2, 1);
            owner.EmitConstant(error, 0);
            owner.EmitCallLine(raise, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, raise.Function, 3));
            owner.Operand(BeamWire.UnsignedTag, 3);
            Mark(propagate);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            Mark(badMatch);
            owner.EmitLine(CallLocation(getopt, location));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void Cons(int registerTag, int register, bool first = false)
            {
                owner.Op(PutList, registerTag, register);
                if (first)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }
                owner.Operand(XRegisterTag, 0);
            }

            void LiteralCons(Term term)
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(term);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
        }

        private bool NeverReturns(Expr expression, HashSet<(string Name, int Arity)> active)
        {
            switch (expression)
            {
                case Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Arguments.Count: 1,
                    Function: AutoImportNames.Error or AutoImportNames.Exit or AutoImportNames.Throw } call
                    when owner.ResolvesErlangBif(call):
                    return true;
                case Expr.Call { Module: null, DynamicRemote: false } local:
                    var key = (local.Function, local.Arguments.Count);
                    if (!localFunctions.TryGetValue(key, out var function))
                    {
                        return false;
                    }
                    if (!active.Add(key))
                    {
                        return true;
                    }
                    bool result = function.Clauses.All(clause => NeverReturns(clause.Body, active));
                    active.Remove(key);
                    return result;
                case Expr.Sequence { Items.Count: > 0 } sequence:
                    return NeverReturns(sequence.Items[^1], active);
                case Expr.Case decision:
                    return decision.Clauses.All(clause => NeverReturns(clause.Body, active));
                case Expr.If conditional:
                    return conditional.Clauses.All(clause => NeverReturns(clause.Body, active));
                case Expr.Receive receive:
                    return receive.Clauses.All(clause => NeverReturns(clause.Body, active))
                        && (receive.After is null || NeverReturns(receive.After, active));
                default:
                    return false;
            }
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedListenerLookup(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Try
                    {
                        After: null, Body: Expr.Call lookup,
                        Catches: [
                            {
                                Guard: null, Patterns: [Pattern.Tuple
                                {
                                    Items: [
                        Pattern.Variable { Name: "_" }, Pattern.Variable { Name: "_" }, Pattern.Variable { Name: "_" }]
                                }],
                                Body: Expr.Literal { Value: Atom caught }
                            }],
                        Clauses: [
                        {
                            Guard: null, Patterns: [Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil }],
                            Body: Expr.Literal { Value: Atom empty }
                        },
                        {
                            Guard: null, Patterns: [Pattern.List
                            {
                                Tail: null, Items: [Pattern.Tuple
                                {
                                    Items: [
                            Pattern.Tuple { Items: [Pattern.Variable keyFirst, Pattern.Variable keySecond] },
                            Pattern.Tuple { Items: [Pattern.Variable ownerVar, Pattern.Variable handler] }
                          ]
                                }]
                            }], Body: Expr.Case
                            {
                                Value: Expr.Call handlerAlive, Clauses: [
                                {
                                    Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }],
                                    Body: Expr.Case
                                    {
                                        Value: Expr.Binary
                                        {
                                            Operator: ErlangOperators.AndAlso,
                                            Left: Expr.Binary
                                            {
                                                Operator: ErlangOperators.ExactNotEqual, Left: Expr.Variable ownerInput,
                                                Right: Expr.Literal { Value: Atom absent }
                                            },
                                            Right: Expr.Call ownerAlive
                                        } testOwner, Clauses: [
                                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }], Body: var conflict },
                                        {
                                            Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                                            Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom ok }, Expr.Variable existing] }
                                        }
                                ]
                                    }
                                },
                                {
                                    Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                                    Body: Expr.Sequence { Items: [Expr.Call delete, Expr.Literal { Value: Atom deleted }] }
                                }
                        ]
                            }
                        }
                    ]
                    }
                }
            ] || !DistinctBindings(
                first,
                second,
                ownerVar,
                handler
            )
                || keyFirst.Name != first.Name || keySecond.Name != second.Name
                || !empty.Equals(caught) || !empty.Equals(deleted) || !empty.Equals(absent)
                || ownerInput.Name != ownerVar.Name || existing.Name != handler.Name
                || !IsWindowCall(handlerAlive, handler) || !IsWindowCall(ownerAlive, ownerVar)
                || !AttributeLiteral.TryNormalizeConstant(conflict, out Term conflictValue)
                || !ReadKey(
                    lookup,
                    first,
                    second,
                    out Atom? table
                ) || !ReadKey(
                    delete,
                    first,
                    second,
                    out Atom? otherTable
                )
                || !table!.Equals(otherTable))
            {
                return false;
            }

            int alive = Label();
            int occupied = Label();
            int available = Label();
            int dead = Label();
            int nil = Label();
            int caughtLabel = Label();
            int badOwner = Label();
            int badHandler = Label();
            int badResult = Label();
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            ClearDetailSlots(0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.EmitTagMatchTuple(1, (XRegisterTag, 0), (XRegisterTag, 1));
            owner.Op(TryEnter, YRegisterTag, 3);
            owner.Operand(LabelTag, caughtLabel);
            owner.EmitConstant(table, 0);
            var previous = location;
            EmitWindowCall(lookup, ref previous);
            var lookupLine = previous;
            owner.Op(TryEnd, YRegisterTag, 3);
            owner.EmitNonemptyList(0, nil);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitTupleCheck(1, 2, badResult);
            owner.EmitTuplePart(1, 0, 3);
            owner.EmitTupleCheck(3, 2, badResult);
            GetTupleField(
                1,
                1,
                YRegisterTag,
                3
            );
            owner.Op(IsTuple, LabelTag, badResult);
            owner.Operand(YRegisterTag, 3);
            owner.Op(TestArity, LabelTag, badResult);
            owner.Operand(YRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitNilTest(XRegisterTag, 2, badResult);
            CheckKey(0, 2);
            CheckKey(1, 1);
            SavedField(1, YRegisterTag, 0);
            Load(0);
            EmitWindowCall(handlerAlive, ref previous);
            var handlerLine = previous;
            owner.EmitAtomSelection(
                0,
                badHandler,
                "false",
                dead,
                "true",
                alive
            );

            Mark(alive);
            SavedField(0, XRegisterTag, 0);
            owner.Op(ExactNotEqualTest, LabelTag, available);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(absent);
            ClearDetailSlots(1, 2, 3);
            EmitWindowCall(ownerAlive, ref previous);
            owner.EmitAtomSelection(
                0,
                badOwner,
                "false",
                available,
                "true",
                occupied
            );
            Mark(occupied);
            owner.EmitConstant(conflictValue, 0);
            owner.EmitFrameReturn(4);

            Mark(available);
            HeapTuple();
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(ok);
            owner.Operand(YRegisterTag, 0);
            owner.EmitFrameReturn(4);

            Mark(dead);
            HeapTuple();
            owner.EmitTagMatchTuple(1, (YRegisterTag, 2), (YRegisterTag, 1));
            TrimDetailFrame(4, 0);
            owner.EmitConstant(table, 0);
            EmitWindowCall(delete, ref previous);
            ReturnEmpty(0);

            Mark(nil);
            owner.EmitNilTest(XRegisterTag, 0, badResult);
            ReturnEmpty(4);

            Mark(caughtLabel);
            owner.Op(TryCase, YRegisterTag, 3);
            ReturnEmpty(4);

            Mark(badOwner);
            owner.EmitLine(new(
                ownerAlive.PreprocessorFile,
                testOwner.SourceLine != 0
                ? testOwner.SourceLine : ownerAlive.SourceLine
            ));
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(badHandler);
            owner.EmitLine(handlerLine);
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(badResult);
            owner.EmitLine(lookupLine);
            owner.Op(TryCaseEnd, XRegisterTag, 0);

            return true;

            void CheckKey(int index, int slot)
            {
                owner.EmitTuplePart(3, index, 1);
                owner.Op(ExactEqualTest, LabelTag, badResult);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(YRegisterTag, slot);
            }

            void SavedField(int field, int destinationTag, int destination)
            {
                owner.Op(GetTupleElement, YRegisterTag, 3);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(destinationTag, destination);
            }

            void HeapTuple()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }

            void ReturnEmpty(int frame)
            {
                owner.EmitConstant(empty, 0);
                owner.EmitFrameReturn(frame);
            }
        }

        private static bool ReadKey(
            Expr.Call call,
            Pattern.Variable first,
            Pattern.Variable second,
            out Atom? table
        )
        {
            if (IsStaticWindowCall(call) && call.Arguments is [
                Expr.Literal { Value: Atom atom }, Expr.Tuple { Items: [Expr.Variable left, Expr.Variable right] }]
                && left.Name == first.Name && right.Name == second.Name)
            {
                table = atom;

                return true;
            }

            table = null;

            return false;
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedPageArguments(FunctionDefinition function)
        {
            if (function.Arity != 1 || function.Clauses is not [var atom, var thunk, var direct, var nested, var tagged, var plain]
                || atom is not
                {
                    Patterns: [Pattern.List { Tail: null, Items: [Pattern.Variable parent, Pattern.Variable callback] }],
                    Guard: Expr.Call { Function: AutoImportNames.IsAtom, DynamicRemote: false } atomTest,
                    Body: Expr.Call { Module: null, DynamicRemote: false } atomCall
                }
                || thunk is not
                {
                    Patterns: [Pattern.List
                    {
                        Tail: null, Items: [Pattern.Variable thunkParent,
                    Pattern.Tuple { Items: [Pattern.Variable thunkApp, Pattern.Variable thunkFun] }]
                    }],
                    Guard: Expr.Call { Function: AutoImportNames.IsFunction, DynamicRemote: false } funTest,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [Expr.List
                        {
                            Tail: null, Items: [
                        Expr.Variable savedParent, Expr.Tuple
                            {
                                Items: [Expr.Variable savedApp,
                            Expr.Apply { Function: Expr.Variable fun, Arguments: [] } apply]
                            }
                    ]
                        }]
                    } recurse
                }
                || direct is not
                {
                    Guard: null, Patterns: [Pattern.List
                    {
                        Tail: null, Items: [Pattern.Variable directParent,
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable directText, Pattern.Variable directTable] }]
                    }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [
                        Expr.Variable parentArg, Expr.Variable textArg, Expr.Variable tableArg, Expr.Literal { Value: Atom defaultApp }]
                    } directCall
                }
                || nested is not
                {
                    Guard: null, Patterns: [Pattern.List
                    {
                        Tail: null, Items: [Pattern.Variable nestedParent,
                    Pattern.Tuple
                        {
                            Items: [Pattern.Variable nestedApp, Pattern.Tuple
                            {
                                Items: [
                        Pattern.Literal { Value: Atom nestedTag }, Pattern.Variable nestedText, Pattern.Variable nestedTable]
                            }]
                        }]
                    }],
                    Body: Expr.Call { Module: null, DynamicRemote: false } nestedCall
                }
                || tagged is not
                {
                    Guard: null, Patterns: [Pattern.List
                    {
                        Tail: null, Items: [Pattern.Variable taggedParent,
                    Pattern.Tuple { Items: [Pattern.Variable taggedApp, Pattern.Variable taggedText] }]
                    }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [
                        Expr.Variable taggedParentArg, Expr.Variable taggedTextArg, Expr.Literal { Value: Atom defaultTable }, Expr.Variable taggedAppArg]
                    } taggedCall
                }
                || plain is not
                {
                    Guard: null, Patterns: [Pattern.List { Tail: null, Items: [Pattern.Variable plainParent, Pattern.Variable plainText] }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [
                        Expr.Variable plainParentArg, Expr.Variable plainTextArg, Expr.Literal { Value: Atom plainTable }, Expr.Literal { Value: Atom plainApp }]
                    } plainCall
                }
                || !DistinctBindings(parent, callback) || !DistinctBindings(thunkParent, thunkApp, thunkFun)
                || !DistinctBindings(directParent, directText, directTable) || !DistinctBindings(
                    nestedParent,
                    nestedApp,
                    nestedText,
                    nestedTable
                )
                || !DistinctBindings(taggedParent, taggedApp, taggedText) || !DistinctBindings(plainParent, plainText)
                || !MatchesDetailArguments(atomTest, callback) || !owner.ResolvesErlangBif(atomTest)
                || !MatchesDetailArguments(funTest, thunkFun) || !owner.ResolvesErlangBif(funTest)
                || !MatchesDetailArguments(atomCall, parent, callback)
                || savedParent.Name != thunkParent.Name || savedApp.Name != thunkApp.Name || fun.Name != thunkFun.Name || recurse.Function != function.Name
                || parentArg.Name != directParent.Name || textArg.Name != directText.Name || tableArg.Name != directTable.Name
                || !MatchesDetailArguments(
                    nestedCall,
                    nestedParent,
                    nestedText,
                    nestedTable,
                    nestedApp
                )
                || taggedParentArg.Name != taggedParent.Name || taggedTextArg.Name != taggedText.Name || taggedAppArg.Name != taggedApp.Name
                || plainParentArg.Name != plainParent.Name || plainTextArg.Name != plainText.Name
                || !tag.Equals(nestedTag) || !defaultApp.Equals(plainApp) || !defaultTable.Equals(plainTable)
                || directCall.Function != nestedCall.Function || directCall.Function != taggedCall.Function || directCall.Function != plainCall.Function
                || !entries.TryGetValue((atomCall.Function, 2), out int callbackEntry)
                || !entries.TryGetValue((directCall.Function, 4), out int htmlEntry)
                || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int tuple = Label();
            int triple = Label();
            int pair = Label();
            int nestedTuple = Label();
            int taggedTextLabel = Label();
            int plainTextLabel = Label();
            owner.EmitNonemptyList(0, functionInfo);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitNonemptyList(2, functionInfo);
            owner.EmitListParts(
                2,
                XRegisterTag,
                3,
                XRegisterTag,
                2
            );
            owner.EmitNilTest(XRegisterTag, 2, tuple);
            owner.Op(IsAtom, LabelTag, tuple);
            owner.Operand(XRegisterTag, 3);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                1
            );
            Tail(2, callbackEntry);

            Mark(tuple);
            owner.Op(IsTuple, LabelTag, plainTextLabel);
            owner.Operand(XRegisterTag, 3);
            owner.instructions.WriteByte(SelectTupleArity);
            owner.EmitTypedRegister(3, TupleTypeBits);
            owner.Operand(LabelTag, plainTextLabel);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, pair);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, triple);

            Mark(triple);
            owner.EmitTuplePart(3, 0, 4);
            owner.EmitAtomTest(4, tag.Name, plainTextLabel);
            owner.EmitNilTest(XRegisterTag, 2, functionInfo);
            owner.EmitTuplePart(3, 1, 0);
            owner.EmitTuplePart(3, 2, 2);
            owner.EmitConstant(defaultApp, 3);
            SwapArguments(0, 1);
            Tail(4, htmlEntry);

            Mark(pair);
            owner.EmitNilTest(XRegisterTag, 2, functionInfo);
            owner.EmitTuplePart(3, 0, 0);
            owner.EmitTuplePart(3, 1, 2);
            owner.Op(IsFunction, LabelTag, nestedTuple);
            owner.Operand(XRegisterTag, 2);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            if (apply.SourceLine != 0)
            {
                owner.EmitLine(new(recurse.PreprocessorFile, apply.SourceLine));
            }

            owner.Op(CallFunTyped, AtomTag, owner.Intern("unsafe"));
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTypedRegister(0, FunTypeBits);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 0), (XRegisterTag, 0));
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 2);

            Mark(nestedTuple);
            EmitDetailRecordTest(
                2,
                3,
                tag,
                taggedTextLabel
            );
            owner.EmitTuplePart(2, 1, 3);
            owner.EmitTuplePart(2, 2, 2);
            SwapArguments(3, 1);
            SwapArguments(0, 3);
            Tail(4, htmlEntry);

            Mark(taggedTextLabel);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                1
            );
            owner.EmitConstant(defaultTable, 2);
            Tail(4, htmlEntry);

            Mark(plainTextLabel);
            owner.EmitNilTest(XRegisterTag, 2, functionInfo);
            owner.EmitConstant(defaultTable, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                1
            );
            owner.EmitConstant(defaultApp, 3);
            Tail(4, htmlEntry);

            return true;

            void Tail(int arity, int target)
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, target);
            }

            void SwapArguments(int left, int right)
            {
                owner.Op(Swap, XRegisterTag, left);
                owner.Operand(XRegisterTag, right);
            }
        }
    }
}

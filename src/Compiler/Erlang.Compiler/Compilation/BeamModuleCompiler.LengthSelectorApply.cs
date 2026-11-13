using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLengthSelectorApply(Clause clause)
        {
            if (clause is not
                {
                    Guard: null, Patterns: [Pattern.Variable key, Pattern.Variable arguments],
                    Body: Expr.Case
                    {
                        Value: Expr.Call { Arguments: [Expr.Variable selectorKey, Expr.Call length] } selector,
                        Clauses:
                        [
                            { Patterns: [Pattern.Literal { Value: Atom missing }], Guard: null, Body: Expr.Call fallback },
                            { Patterns: [Pattern.Variable selected], Guard: Expr.Call guard, Body: Expr.Call apply }
                        ]
                    } decision
                }
                || !DistinctBindings(key, arguments, selected)
                || selectorKey.Name != key.Name || !ReadsLength(length, arguments.Name)
                || !IsFunctionArityCall(guard) || !owner.ResolvesErlangBif(guard)
                || guard.Arguments is not [Expr.Variable tested, Expr.Call guardLength]
                || tested.Name != selected.Name || !ReadsLength(guardLength, arguments.Name)
                || apply is not { DynamicRemote: false, Function: AutoImportNames.Apply,
                    Arguments: [Expr.Variable function, Expr.Variable values] }
                || !owner.ResolvesErlangBif(apply) || function.Name != selected.Name || values.Name != arguments.Name
                || ReadEntryCall(selector, [key.Name, arguments.Name], preparedOperand: 1) is not { } read
                || ReadEntryCall(fallback, [], [arguments.Name, key.Name], allowControlTransfer: true) is not { } absent)
            {
                return false;
            }

            // The first length call proves that the saved argument is a proper list.
            // The same immutable list is used in the guard and in apply.
            owner.EmitCallLine(length, location);
            EmitSizeInstruction(AutoImportNames.Length, 0, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            EmitFrameAllocation(3, 2);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
            var firstLine = CallLocation(length, location);
            EmitEntryCall(read, firstLine);
            var selectorLine = CallLocation(selector, firstLine);

            int guardLabel = Label();
            int failure = Label();
            owner.EmitAtomTest(0, missing.Name, guardLabel);
            owner.EmitOrderedTailMoves(absent.Moves, absent.Constants, absent.SavedInputs);
            EmitPreparedFrameTail(absent, selectorLine, 2);

            Mark(guardLabel);
            owner.EmitCallLine(guardLength, selectorLine);
            EmitSizeInstruction(AutoImportNames.Length, 0, 1);
            owner.EmitTypedRegister(0, ProperListTypeBits, registerTag: YRegisterTag);
            owner.Operand(XRegisterTag, 1);
            owner.Op(IsFunctionWithArity, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTypedRegister(1, IntegerTypeBits, 0, MaximumBitSize);
            owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
            EmitPreparedFrameTail(new EntryCall(apply, ErlangModuleNames.Module, [], []), CallLocation(guardLength, selectorLine), 2);

            Mark(failure);
            owner.EmitLine(decision.SourceLine == 0 ? selectorLine : new(decision.PreprocessorFile, decision.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);
            return true;
        }

        private bool ReadsLength(Expr.Call call, string input) =>
            call is { DynamicRemote: false, Function: AutoImportNames.Length, Arguments: [Expr.Variable argument] }
            && argument.Name == input && owner.ResolvesErlangBif(call);
    }
}

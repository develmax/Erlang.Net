using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int MaximumFoldedAppendPrefixLength = 4;

        private bool TryEmitProvenCatchArgumentPreparation(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable text, Pattern.Variable tail],
                    Body: Expr.Catch
                    {
                        Operand: Expr.Call
                        {
                            Arguments: [Expr.Binary append, Expr.List { Tail: Expr.Variable rest } arguments]
                        } consumer
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause) || text.Name == VariableScopeNames.Wildcard
                || tail.Name == VariableScopeNames.Wildcard || rest.Name != tail.Name
                || !owner.HasProperListEntryFact((function.Name, function.Arity), 0)
                || !TryResolveOrdinaryCallTarget(consumer, out string? consumerModule)
                || arguments.Items is not
                [
                    ..,
                    Expr.Call
                {
                    Module: null or ErlangModuleNames.Module,
                    Function: AutoImportNames.Self,
                    DynamicRemote: false,
                    Arguments: []
                } self
                ]
                || !owner.ResolvesErlangBif(self))
            {
                return false;
            }

            var listHeads = new Term[arguments.Items.Count - 1];
            for (int index = 0; index < listHeads.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(arguments.Items[index], out Term head))
                {
                    return false;
                }

                listHeads[index] = head;
            }

            var prefixes = new List<(Term Prefix, int SourceLine)>();
            Expr current = append;
            while (current is Expr.Binary { Operator: ErlangOperators.Append } outer
                && AttributeLiteral.TryNormalizeConstant(outer.Left, out Term literalPrefix)
                && IsUnexpandedAppendPrefix(literalPrefix))
            {
                prefixes.Add((literalPrefix, outer.SourceLine));
                current = outer.Right;
            }

            if (current is not Expr.Binary
                {
                    Operator: ErlangOperators.Append,
                    Left: Expr.Variable input
                } inner
                || input.Name != text.Name || !AttributeLiteral.TryNormalizeConstant(inner.Right, out Term suffix)
                || suffix is Nil)
            {
                return false;
            }

            // Each append has a proven proper left operand. Preparing the nested result
            // and the cons prefix preserves effect order before installing the catch.
            EmitFrameAllocation(function.Arity, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitConstant(suffix, 1);
            var previous = location;
            owner.EmitArithmeticLine(inner.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            for (int index = prefixes.Count - 1; index >= 0; index--)
            {
                var (prefix, line) = prefixes[index];
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
                owner.EmitConstant(prefix, 0);
                owner.EmitArithmeticLine(line, ref previous);
                Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, arguments.Items.Count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            for (int index = listHeads.Length - 1; index >= 0; index--)
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(listHeads[index]);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(XRegisterTag, 1);
            }

            // The last cons read consumes the saved tail, freeing Y0 for the handler tag.
            int handler = Label();
            EnterCatch(0, handler);
            owner.EmitCallLine(consumer, previous);
            Call(consumerModule, consumer.Function, consumer.Arguments.Count);
            EndCatch(0, handler);
            owner.EmitFrameReturn(1);

            return true;
        }

        private static bool IsUnexpandedAppendPrefix(Term value)
        {
            int length = 0;
            while (value is Cons cell)
            {
                length++;
                value = cell.Tail;
            }

            // sys_core_fold expands shorter literal prefixes as individual cons cells.
            return value is Nil && length > MaximumFoldedAppendPrefixLength;
        }
    }
}

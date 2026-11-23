using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListFunctionTailAndTupleCall(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable list],
                    Guard: Expr.Call { Arguments: [Expr.Variable tested] } test,
                    Body: Expr.Call { Arguments: [Expr.FunctionReference, Expr.Variable forwarded] } iterate
                } listClause,
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] }],
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Call
                        {
                            Arguments:
                                [Expr.Variable field, Expr.List { Items.Count: > 0, Tail: Expr.Variable rest } prefix]
                        } call, var result]
                    }
                }
                ]
                || !HasUnconditionalParameters(listClause) || list.Name == VariableScopeNames.Wildcard
                || tested.Name != list.Name || forwarded.Name != list.Name
                || !IsGuardBif(test, AutoImportNames.IsList) || !owner.ResolvesErlangBif(test)
                || ReadEntryCall(iterate, [list.Name], allowFunctionArgument: true)
                    is not { FunctionArgument: { } reference } tail
                || tail.Module is { } remote && IsTrappingTailBif(remote, iterate.Function, iterate.Arguments.Count)
                || !DistinctBindings(first, second)
                || !(field.Name == first.Name && rest.Name == second.Name
                    || field.Name == second.Name && rest.Name == first.Name)
                || !TryResolveOrdinaryCallTarget(call, out string? module)
                || !AttributeLiteral.TryNormalizeConstant(result, out Term constant))
            {
                return false;
            }

            var heads = new Term[prefix.Items.Count];
            for (int index = 0; index < heads.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(prefix.Items[index], out heads[index]))
                {
                    return false;
                }
            }

            int tuple = Label();
            owner.Op(IsList, LabelTag, tuple);
            owner.Operand(XRegisterTag, 0);
            owner.EmitClosureAllocation(0, 1, 1);
            EmitEntryCallArguments(tail);
            owner.EmitDeclaredFunctionReference(reference.Reference, reference.Entry, reference.Destination);
            EmitRegisterTailTarget(iterate with { Module = tail.Module }, iterate.Arguments.Count);

            Mark(tuple);
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                functionInfo
            );
            EmitFrameAllocation(1, 0, heads.Length * ConsHeapWords);
            int firstField = field.Name == first.Name ? 0 : 1;
            owner.EmitTuplePart(0, 1 - firstField, 1);
            for (int index = heads.Length - 1; index >= 0; index--)
            {
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(heads[index]);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(XRegisterTag, 1);
            }

            owner.EmitTuplePart(0, firstField, 0);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, call.Arguments.Count);
            owner.EmitConstant(constant, 0);
            owner.EmitFrameReturn(0);

            return true;
        }
    }
}

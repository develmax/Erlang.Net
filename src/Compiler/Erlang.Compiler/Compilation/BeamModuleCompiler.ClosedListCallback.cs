using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string SafeFunctionCallAtom = "safe";

        private bool TryEmitClosedListCallback(FunctionDefinition function)
        {
            if (recursiveClosure is not null || function.Arity != 3
                || function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Variable emptyKey, Pattern.Variable emptyFun, var empty], Body: var emptyResult },
                {
                    Guard: null,
                    Patterns:
                        [
                            Pattern.Variable key,
                            Pattern.Variable callback,
                            Pattern.List { Items: [Pattern.Variable matching], Tail: Pattern.Variable unused }
                        ],
                    Body: Expr.Apply { Function: Expr.Variable invoked, Arguments: [] } apply
                },
                {
                    Guard: null,
                    Patterns:
                        [
                            Pattern.Variable original,
                            Pattern.Variable originalCallback,
                            Pattern.List { Items: [Pattern.Variable ignored], Tail: Pattern.Variable rest }
                        ],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable nextKey, Expr.Variable nextCallback, Expr.Variable nextList]
                    } tail
                }
                ]
                || ListEntryTest(empty) != IsNil || !DistinctBindings(emptyKey, emptyFun)
                || matching.Name != key.Name || key.Name == VariableScopeNames.Wildcard
                || callback.Name != invoked.Name || callback.Name == key.Name || callback.Name == VariableScopeNames.Wildcard
                || unused.Name != VariableScopeNames.Wildcard || ignored.Name != VariableScopeNames.Wildcard
                || !DistinctBindings(original, originalCallback, rest)
                || original.Name != nextKey.Name || originalCallback.Name != nextCallback.Name
                || rest.Name != nextList.Name || tail.Function != function.Name
                || !entries.TryGetValue((function.Name, function.Arity), out int entry)
                || !AttributeLiteral.TryNormalizeConstant(emptyResult, out Term result)
                || ReadClosedArgumentAtom(function, 0) is not { } atom
                || ReadClosedArgumentFact(function, 1, ReadFunctionArgument) is not { Arity: 0 })
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int recur = Label();
            int emptyList = Label();
            const int list = 2;
            const int head = list + 1;
            owner.EmitNonemptyList(list, emptyList);
            owner.EmitListParts(
                list,
                XRegisterTag,
                head,
                XRegisterTag,
                list
            );
            owner.Op(ExactEqualTest, LabelTag, recur);
            owner.Operand(XRegisterTag, head);
            owner.LiteralOperand(atom);
            EmitFrameAllocation(list, 0);
            owner.MoveToX0(XRegisterTag, 1);
            if (apply.SourceLine != 0)
            {
                owner.EmitLine(new(location.File, apply.SourceLine));
            }

            owner.Op(CallFunTyped, AtomTag, owner.Intern(SafeFunctionCallAtom));
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTypedRegister(0, FunTypeBits);
            owner.EmitFrameReturn(0);
            Mark(recur);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, function.Arity);
            owner.Operand(LabelTag, entry);
            Mark(emptyList);
            owner.Op(IsNil, LabelTag, failure);
            owner.Operand(XRegisterTag, list);
            owner.EmitConstant(result, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static ClosedFunctionArity? ReadFunctionArgument(Expr expression) => expression switch
        {
            Expr.Fun { Clauses: [var clause, ..] } => new(clause.Patterns.Count),
            Expr.FunctionReference reference => new(reference.Arity),
            _ => null
        };

        private sealed record ClosedFunctionArity(int Arity);
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedTupleElementMembership(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || owner.OnLoadFunction is not null
                || owner.IsDeclaredNative((function.Name, function.Arity)) || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple],
                    Guard: Expr.Call { Function: AutoImportNames.IsBinary, DynamicRemote: false, Arguments: [Expr.Variable binary] } testBinary,
                    Body: Expr.Literal { Value: Atom matched }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Function: AutoImportNames.Element, DynamicRemote: false,
                            Arguments: [Expr.Literal { Value: Integer index }, Expr.Variable selector]
                        } read, Clauses.Count: >= 4
                    } decision
                }
                ]
                || !DistinctBindings(input) || input.Name != selector.Name || !index.Value.IsOne
                || !owner.ResolvesErlangBif(read) || !owner.ResolvesErlangBif(testBinary)
                || tuple.Items.Skip(1).Any(item => item is not Erlang.Pattern.Variable)
                || tuple.Items.OfType<Pattern.Variable>().Where(item => item.Name != VariableScopeNames.Wildcard)
                    .GroupBy(item => item.Name, StringComparer.Ordinal).Any(group => group.Count() > 1)
                || Array.FindIndex(tuple.Items.ToArray(), item => item is Pattern.Variable field && field.Name == binary.Name)
                    is not (> 0 and var binaryField)
                || decision.Clauses[0] is not
                {
                    Patterns: [Pattern.Variable value],
                    Guard: Expr.Call { Function: AutoImportNames.IsInteger, DynamicRemote: false, Arguments: [Expr.Variable checkedValue] } integer,
                    Body: Expr.Literal { Value: Atom onInteger }
                }
                || !DistinctBindings(value, input) || checkedValue.Name != value.Name || !onInteger.Equals(matched)
                || !owner.ResolvesErlangBif(integer)
                || decision.Clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Literal otherwise
                }
                || !HasOnlyLocalCalls(
                    function,
                    1,
                    (caller, invocation) =>
                    caller.Clauses.Any(clause => ReferenceEquals(ReadTupleConversionFallback(clause)?.Keep, invocation))
                ))
            {
                return false;
            }

            var atoms = new SortedSet<Atom>(MapKeyOrder);
            foreach (Clause branch in decision.Clauses.Skip(1).SkipLast(1))
            {
                if (branch is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Atom atom }], Body: Expr.Literal { Value: Atom result }
                    } || !result.Equals(matched) || !atoms.Add(atom))
                {
                    return false;
                }
            }

            int element = Label();
            int choices = Label();
            int yes = Label();
            int no = Label();
            owner.Op(IsTaggedTuple, LabelTag, element);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            owner.EmitTuplePart(0, binaryField, 1);
            owner.Op(IsBinary, LabelTag, element);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(matched, 0);
            owner.instructions.WriteByte(Return);

            Mark(element);
            owner.EmitCallLine(read, location);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
            owner.LiteralOperand(index);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsInteger, LabelTag, choices);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(matched, 0);
            owner.instructions.WriteByte(Return);
            Mark(choices);
            owner.EmitMembershipSelect(0, no, atoms.Select(atom => ((Term)atom, yes)));
            Mark(yes);
            owner.EmitConstant(matched, 0);
            owner.instructions.WriteByte(Return);
            Mark(no);
            owner.EmitConstant(otherwise.Value, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}

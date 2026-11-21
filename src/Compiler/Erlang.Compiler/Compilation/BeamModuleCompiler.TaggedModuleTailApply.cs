using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedModuleTailApply(FunctionDefinition function)
        {
            int arity = function.Arity;
            if (!CanUseEntryTypeFacts() || arity == 0 || arity + 2 > FirstReservedXRegister
                || function.Clauses is not
                [
                {
                    Patterns: [AliasPattern alias, ..],
                    Guard: Expr.Call
                    {
                        DynamicRemote: false, Function: AutoImportNames.IsAtom,
                        Arguments: [Expr.Variable guarded]
                    } guard,
                    Body: Expr.Call { DynamicRemote: true } call
                } first,
                {
                    Guard: null,
                    Body: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } fallback
                } last
                ]
                || (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is not
                { Name: not VariableScopeNames.Wildcard } original
                || (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is not Pattern.Tuple
                { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple
                || guard.Module is not (null or ErlangModuleNames.Module)
                || !owner.ResolvesErlangBif(guard)
                || ReadFixedApply(call) is not
                    (Expr.Variable module, Expr.Literal { Value: Atom method }, var forwarded)
                || module.Name != guarded.Name || guarded.Name == VariableScopeNames.Wildcard
                || !HasUnconditionalParameters(last))
            {
                return false;
            }

            int moduleField = -1;
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                if (tuple.Items[index] is not Pattern.Variable field
                    || field.Name != VariableScopeNames.Wildcard && (field.Name != guarded.Name || moduleField >= 0))
                {
                    return false;
                }

                if (field.Name == guarded.Name)
                {
                    moduleField = index;
                }
            }

            if (moduleField < 0 || first.Patterns.Skip(1).Any(pattern =>
                    pattern is not global::Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard }))
            {
                return false;
            }

            string[] parameters = [original.Name, .. first.Patterns.Skip(1).Cast<Pattern.Variable>().Select(parameter => parameter.Name)];
            string[] fallbackParameters = last.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Distinct(StringComparer.Ordinal).Count() != arity || parameters.Contains(guarded.Name, StringComparer.Ordinal)
                || !ForwardsAll(forwarded, parameters) || !ForwardsAll(fallback.Arguments, fallbackParameters)
                || ReadEntryCall(fallback, fallbackParameters) is not { } target)
            {
                return false;
            }

            int other = Label();
            owner.Op(IsTaggedTuple, LabelTag, other);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            owner.LiteralOperand(tag);
            owner.EmitTuplePart(0, moduleField, arity);
            owner.Op(IsAtom, LabelTag, other);
            owner.Operand(XRegisterTag, arity);

            // All original arguments still occupy their incoming registers. Only the dynamic
            // continuation needs a frame, and it has no live stack values.
            EmitFrameAllocation(arity + 1, 0);
            owner.EmitConstant(method, arity + 1);
            owner.EmitCallLine(call, location);
            owner.Op(TailDynamicApply, BeamWire.UnsignedTag, arity);
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(other);
            EmitRegisterTailCall(target);

            return true;

            static bool ForwardsAll(IReadOnlyList<Expr> operands, string[] names) =>
                operands.Count == names.Length && operands.Zip(names).All(pair =>
                    pair.First is Expr.Variable variable && variable.Name == pair.Second);
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitMatchedTupleArgumentEffect(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Call { Arguments: [Expr.Variable forwarded, Expr.Tuple reconstructed] } call,
                                Expr.Literal returned
                            ]
                    }
                } matched,
                { Guard: null, Body: Expr.Literal otherwise } fallback
                ]
                || !HasUnconditionalParameters(fallback)
                || !returned.Value.Equals(otherwise.Value)
                || matched.Patterns.Count < 2
                || matched.Patterns[0] is not Erlang.Pattern.Variable first
                || first.Name != forwarded.Name || first.Name == VariableScopeNames.Wildcard
                || matched.Patterns.Count(pattern => pattern is Erlang.Pattern.Tuple) != 1
                || !matched.Patterns.All(pattern => pattern is Erlang.Pattern.Variable or Erlang.Pattern.Tuple)
                || !TryResolveOrdinaryCall(call, out string? module, preparedArgument: 1) || module is null)
            {
                return false;
            }

            var names = matched.Patterns.SelectMany(Semantics.Variables)
                .Where(name => name != VariableScopeNames.Wildcard).ToArray();
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            {
                return false;
            }

            var selected = matched.Patterns.Select((pattern, index) => (pattern, index))
                .Single(entry => entry.pattern is Erlang.Pattern.Tuple);
            var outer = (Erlang.Pattern.Tuple)selected.pattern;
            if (ReadSingleTupleField(outer) is not { Field: Erlang.Pattern.Tuple inner } parent
                || inner.Items is not
                [
                    Erlang.Pattern.Literal { Value: Atom tag },
                    Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard }
                ]
                || !ReturnsMatchedValue(inner, reconstructed))
            {
                return false;
            }

            // The tagged match already identifies the exact outgoing value. Its two
            // fields do not need extraction or a new tuple before the effectful call.
            int failure = Label();
            EmitTagCheck(
                selected.index,
                outer.Items.Count,
                parent.Tag
            );
            owner.EmitTuplePart(
                selected.index,
                parent.Index,
                1
            );
            EmitTagCheck(
                1,
                inner.Items.Count,
                tag
            );
            EmitFrameAllocation(call.Arguments.Count, 0);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, call.Arguments.Count);
            owner.EmitConstant(returned.Value, 0);
            owner.EmitFrameReturn(0);
            Mark(failure);
            owner.EmitConstant(otherwise.Value, 0);
            owner.instructions.WriteByte(Return);

            return true;

            void EmitTagCheck(int register, int arity, Atom expected)
            {
                owner.Op(IsTaggedTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
                owner.Operand(BeamWire.UnsignedTag, arity);
                owner.Operand(AtomTag, owner.Intern(expected.Name));
            }
        }
    }
}

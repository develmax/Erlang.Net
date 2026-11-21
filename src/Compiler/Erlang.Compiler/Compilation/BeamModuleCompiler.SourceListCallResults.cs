using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConstantArgumentChildList(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Arity != 0
                || function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
                || ReadCallListResult(clause with { Body = SkipIgnoredLiterals(sequence) }) is not { } result
                || !result.Calls.Items.OfType<Expr.Call>().Any(call => call.Arguments.Count != 0))
            {
                return false;
            }

            var calls = new List<EntryCall>();
            foreach (var item in result.Calls.Items)
            {
                if (item is not Expr.Call sourceCall
                    || ReadEntryCall(sourceCall, []) is not { Module: null, SavedInputs: null or { Count: 0 } } call)
                {
                    return false;
                }

                calls.Add(call);
            }

            int slots = calls.Count - 1;
            owner.EmitEmptyCallFrame(slots);
            var previous = location;
            for (int index = 0; index < calls.Count; index++)
            {
                var call = calls[index];
                EmitEntryCall(call, previous);
                previous = CallLocation(call.Call, previous);
                if (index < slots)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        slots - index - 1
                    );
                }
            }

            owner.EmitChildListResult(result.Tag, result.Flags, calls.Count);
            owner.EmitFrameReturn(slots);

            return true;
        }

        private bool TryEmitSharedClosedListLocalTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                { Guard: null, Patterns: [var empty], Body: Expr.Call { Module: null, Arguments: [], DynamicRemote: false } first },
                { Guard: null, Patterns: [var closed], Body: Expr.Call { Module: null, Arguments: [], DynamicRemote: false } second },
                { Guard: null, Patterns: [Pattern.Variable fallback], Body: var rejected }
                ]
                || !IsEmptyListPattern(empty) || ReadClosedListPattern(closed) is not Cons literal
                || first.Function != second.Function || !entries.TryGetValue((first.Function, 0), out int target)
                || fallback.Name == VariableScopeNames.Wildcard
                || SkipIgnoredLiterals(rejected) is not Expr.Tuple
                {
                    Items:
                    [
                        Expr.Literal { Value: Atom outer },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom inner }, Expr.Variable returned] }
                    ]
                }
                || returned.Name != fallback.Name)
            {
                return false;
            }

            int shared = Label();
            int failure = Label();
            owner.Op(ExactNotEqualTest, LabelTag, shared);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(literal);
            owner.EmitNilTest(XRegisterTag, 0, failure);
            Mark(shared);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 0);
            owner.Operand(LabelTag, target);

            Mark(failure);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(inner.Name)), (XRegisterTag, 0));
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(outer.Name)), (XRegisterTag, 0));
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static Term? ReadClosedListPattern(Pattern pattern)
        {
            if (pattern is Pattern.Literal literal)
            {
                return literal.Value;
            }
            if (pattern is not Pattern.List { Tail: null } list)
            {
                return null;
            }

            var items = new Term[list.Items.Count];
            for (int index = 0; index < items.Length; index++)
            {
                if (ReadClosedListPattern(list.Items[index]) is not { } item)
                {
                    return null;
                }

                items[index] = item;
            }

            return Cons.From(items);
        }

        private static Expr SkipIgnoredLiterals(Expr body)
        {
            if (body is not Expr.Sequence { Items.Count: > 0 } sequence)
            {
                return body;
            }

            var items = sequence.Items.Take(sequence.Items.Count - 1).Where(item => item is not Expr.Literal)
                .Append(sequence.Items[^1]).ToArray();

            return items is [var last] ? last : sequence with { Items = items };
        }
    }
}

using AstPattern = Erlang.Pattern;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record SharedTaggedConsEntry(
            Atom Tag,
            int LiteralField,
            Term Literal,
            int RetryArity,
            int RetryLiteralField,
            Term RetryLiteral,
            int RetryResultField,
            CallArgumentConstructions Construction
        );

        private bool TryEmitSharedTaggedConsEntry(
            IReadOnlyList<Clause> clauses,
            int input,
            Clause? fallback,
            int failure
        )
        {
            if (input != 0 || !owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || fallback is not
                {
                    Patterns: [_, AstPattern.Variable state],
                    Body: Expr.Variable rejected
                }
                || state.Name != rejected.Name
                || ReadSharedTaggedConsEntry(clauses) is not { } plan)
            {
                return false;
            }

            const int eventInput = 0;
            const int stateInput = 1;
            const int head = 2;
            const int remaining = 3;
            const int nested = 4;
            const int tail = 5;
            int retry = Label();
            owner.EmitTuplePart(eventInput, 0, head);
            owner.EmitAtomTest(head, plan.Tag.Name, failure);
            owner.EmitNonemptyList(stateInput, failure);
            owner.EmitListParts(
                stateInput,
                XRegisterTag,
                head,
                XRegisterTag,
                remaining
            );
            owner.EmitNonemptyList(remaining, retry);
            owner.EmitListParts(
                remaining,
                XRegisterTag,
                nested,
                XRegisterTag,
                tail
            );
            owner.EmitTupleCheck(nested, 2, retry);

            // The original event and state are live on failure only. All six
            // inputs still hold initialized terms before the success collector.
            owner.Op(TestHeap, BeamWire.UnsignedTag, plan.Construction.Constructions.Sum(item => item.HeapWords));
            owner.Operand(BeamWire.UnsignedTag, tail + 1);
            owner.EmitTuplePart(nested, 0, eventInput);
            owner.EmitTuplePart(nested, 1, stateInput);
            EmitCallArgumentConstructions(plan.Construction, -1, heapChecked: true);
            owner.instructions.WriteByte(Return);

            Mark(retry);
            owner.EmitTuplePart(eventInput, plan.LiteralField, eventInput);
            EmitLiteral(eventInput, plan.Literal);
            owner.EmitTupleCheck(head, plan.RetryArity, failure);
            owner.EmitTuplePart(head, plan.RetryLiteralField, eventInput);
            EmitLiteral(eventInput, plan.RetryLiteral);
            owner.Op(IsNil, LabelTag, failure);
            owner.Operand(XRegisterTag, remaining);
            owner.EmitTuplePart(head, plan.RetryResultField, eventInput);
            owner.instructions.WriteByte(Return);

            return true;

            void EmitLiteral(int register, Term value)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(value);
            }
        }

        private static SharedTaggedConsEntry? ReadSharedTaggedConsEntry(IReadOnlyList<Clause> clauses)
        {
            if (clauses is not
                [
                {
                    Guard: null,
                    Patterns:
                        [
                            AstPattern.Tuple hotEvent,
                            AstPattern.List
                        {
                            Items: [AstPattern.Variable head, AstPattern.Tuple { Items: [AstPattern.Variable name, AstPattern.Variable children] }],
                            Tail: AstPattern.Variable tail
                        }
                        ]
                } hot,
                {
                    Guard: null,
                    Patterns:
                        [
                            AstPattern.Tuple coldEvent,
                            AstPattern.List { Items: [AstPattern.Tuple coldHead], Tail: null }
                        ],
                    Body: Expr.Variable returned
                } cold
                ]
                || !HasTupleTag(hotEvent, out Atom tag) || !HasTupleTag(coldEvent, out Atom coldTag)
                || !tag.Equals(coldTag) || hotEvent.Items.Count != coldEvent.Items.Count
                || !DistinctBindings(
                    head,
                    name,
                    children,
                    tail
                )
                || !HasIndependentBindings(hot) || !HasIndependentBindings(cold)
                || !hotEvent.Items.Skip(1).All(item => item is AstPattern.Variable or AstPattern.Any)
                || ReadLiteralColumn(coldEvent, 1, null) is not { } eventCheck
                || ReadLiteralColumn(coldHead, 0, returned.Name) is not { } headCheck)
            {
                return null;
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [head.Name] = 2,
                [name.Name] = 0,
                [children.Name] = 1,
                [tail.Name] = 5
            };
            var reads = LocalFunctionEntries.Walk(hot.Body).OfType<Expr.Variable>()
                .Select(read => read.Name).ToHashSet(StringComparer.Ordinal);
            if (inputs.Keys.Any(binding => !reads.Contains(binding)))
            {
                return null;
            }

            IReadOnlyList<(string Name, Expr Value)>? bindings = null;
            Expr body = hot.Body;
            if (body is Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: AstPattern.Variable binding, Value: Expr.List or Expr.Tuple } assignment,
                        var terminal
                    ]
                })
            {
                if (binding.Name == VariableScopeNames.Wildcard || inputs.ContainsKey(binding.Name)
                    || Semantics.Variables(hotEvent).Contains(binding.Name, StringComparer.Ordinal)
                    || !LocalFunctionEntries.Walk(terminal).OfType<Expr.Variable>().Any(read => read.Name == binding.Name))
                {
                    return null;
                }

                bindings = [(binding.Name, assignment.Value)];
                body = terminal;
            }

            if (body is not (Expr.List or Expr.Tuple)
                || ReadCallArgumentConstructions(
                    [body],
                    -1,
                    new Dictionary<string, int>(),
                    inputs: inputs,
                    bindings: bindings
                ) is not { } construction)
            {
                return null;
            }

            return new(
                tag,
                eventCheck.Field,
                eventCheck.Value,
                coldHead.Items.Count,
                headCheck.Field,
                headCheck.Value,
                headCheck.Result,
                construction
            );

            static bool HasIndependentBindings(Clause clause)
            {
                string[] names = clause.Patterns.SelectMany(Semantics.Variables)
                    .Where(name => name != VariableScopeNames.Wildcard).ToArray();

                return names.Distinct(StringComparer.Ordinal).Count() == names.Length;
            }

            static (int Field, Term Value, int Result)? ReadLiteralColumn(
                AstPattern.Tuple tuple,
                int first,
                string? result
            )
            {
                int field = -1;
                int returned = -1;
                Term? constant = null;
                for (int index = first; index < tuple.Items.Count; index++)
                {
                    switch (tuple.Items[index])
                    {
                        case AstPattern.Literal literal when field < 0:
                            field = index;
                            constant = literal.Value;
                            break;
                        case AstPattern.Variable binding when result is not null && binding.Name == result:
                            returned = index;
                            break;
                        case AstPattern.Variable or AstPattern.Any:
                            break;
                        default:
                            return null;
                    }
                }

                return constant is not null && (result is null || returned >= 0)
                    ? (field, constant, returned) : null;
            }
        }
    }
}

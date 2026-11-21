using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedTupleExternalTailPrefix(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity != 1)
            {
                return false;
            }

            var branches = new List<ProjectedExternalTail>();
            var tags = new HashSet<Atom>();
            int arity = 0;
            foreach (Clause clause in function.Clauses)
            {
                if (clause.Patterns is not [Erlang.Pattern.Tuple tuple]
                    || arity != 0 && tuple.Items.Count != arity
                    || ReadProjectedExternalTail(clause, tuple) is not { } branch
                    || !tags.Add(branch.Key))
                {
                    break;
                }

                arity = tuple.Items.Count;
                branches.Add(branch);
            }

            // Only introduce a selector for a common continuation.
            int sharedCandidates = branches.Count(branch => branch.Identity is not null);
            if (sharedCandidates < 2 || sharedCandidates == branches.Where(branch => branch.Identity is not null)
                .Select(branch => branch.Identity).Distinct().Count())
            {
                return false;
            }

            // The first equivalent target in the ordered BEAM selector keeps its call location.
            branches.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));
            var labels = new Dictionary<(string Module, string Name, int Field), int>();
            var entries = new List<(ProjectedExternalTail Branch, int Label)>();
            var targets = new List<(Atom Key, int Label)>();
            foreach (ProjectedExternalTail branch in branches)
            {
                if (branch.Identity is not { } identity || !labels.TryGetValue(identity, out int label))
                {
                    label = Label();
                    entries.Add((branch, label));
                    if (branch.Identity is { } shared)
                    {
                        labels.Add(shared, label);
                    }
                }

                targets.Add((branch.Key, label));
            }

            bool hasRemainder = branches.Count < function.Clauses.Count;
            int failure = hasRemainder ? Label() : functionInfo;
            var previousLocation = owner.lastEmittedLine;
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                arity,
                failure
            );
            owner.EmitTuplePart(0, 0, 1);
            EmitAritySelect(1, targets.Count, failure);
            foreach (var (key, label) in targets)
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, label);
            }

            for (int index = entries.Count - 1; index >= 0; index--)
            {
                var (branch, label) = entries[index];
                Mark(label);
                owner.EmitTuplePart(0, branch.Field, 0);
                if (branch.Tail.ConstructedArgument is { } constructed)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, constructed.HeapWords);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                }

                EmitEntryCallArguments(branch.Tail);
                // An ordinary external tail target retains its call-site line, even at the function entry line.
                owner.EmitLine(CallLocation(branch.Tail.Call, location));
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, branch.Tail.Call.Arguments.Count);
                owner.Operand(
                    BeamWire.UnsignedTag,
                    owner.Import(branch.Tail.Module!, branch.Tail.Call.Function, branch.Tail.Call.Arguments.Count)
                );
            }

            if (hasRemainder)
            {
                Mark(failure);
                owner.lastEmittedLine = previousLocation;
                EmitGeneralFunction(function with { Clauses = function.Clauses.Skip(branches.Count).ToArray() }, functionInfo);
            }

            return true;
        }

        private ProjectedExternalTail? ReadProjectedExternalTail(Clause clause, Erlang.Pattern.Tuple tuple)
        {
            if (clause.Guard is not null || tuple.Items is not [Erlang.Pattern.Literal { Value: Atom key }, ..]
                || clause.Body is not Expr.Call { DynamicRemote: false } call)
            {
                return null;
            }

            Expr.Variable input;
            bool direct;
            if (call.Arguments is [Expr.Variable operand])
            {
                input = operand;
                direct = true;
            }
            else if (call.Arguments is [var head, Expr.List { Items: [Expr.Variable value], Tail: null }]
                && AttributeLiteral.TryNormalizeConstant(head, out _))
            {
                input = value;
                direct = false;
            }
            else
            {
                return null;
            }

            int field = -1;
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 1; index < tuple.Items.Count; index++)
            {
                switch (tuple.Items[index])
                {
                    case Erlang.Pattern.Any:
                    case Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Erlang.Pattern.Variable variable when bindings.Add(variable.Name):
                        if (variable.Name == input.Name)
                        {
                            field = index;
                        }

                        break;
                    default:
                        return null;
                }
            }

            if (field < 0 || ReadEntryCall(call, [input.Name], allowConstructedArgument: !direct) is not
                { Module: { } module } tail || module == ErlangModuleNames.Module)
            {
                return null;
            }

            return new(
                key,
                field,
                tail,
                direct ? (module, tail.Call.Function, field) : null
            );
        }

        private sealed record ProjectedExternalTail(
            Atom Key,
            int Field,
            EntryCall Tail,
            (string Module, string Name, int Field)? Identity
        );
    }
}

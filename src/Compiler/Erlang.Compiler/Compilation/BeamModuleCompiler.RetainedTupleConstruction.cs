using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRetainedTupleConstruction(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not [{ Guard: null } clause])
            {
                return false;
            }

            IReadOnlyList<Expr> body = clause.Body switch
            {
                Expr.Sequence sequence => sequence.Items,
                Expr.Tuple inline => FlattenCallArguments([], inline),
                _ => []
            };
            if (body is not
                [
                    Expr.Match
                {
                    Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                    Value: Expr.Call producer
                },
                    Expr.Tuple continuation
                ])
            {
                return false;
            }

            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal) { binding.Name };
            var parameters = new string[clause.Patterns.Count];
            int tupleInput = -1;
            int tupleArity = 0;
            for (int input = 0; input < parameters.Length; input++)
            {
                var pattern = clause.Patterns[input];
                if (pattern is Pattern.Variable parameter && Bind(parameter))
                {
                    parameters[input] = parameter.Name;
                    continue;
                }

                if (tupleInput >= 0 || pattern is not Pattern.Tuple tuple)
                {
                    return false;
                }

                tupleInput = input;
                tupleArity = tuple.Items.Count;
                parameters[input] = VariableScopeNames.Wildcard;
                for (int field = 0; field < tupleArity; field++)
                {
                    if (tuple.Items[field] is not Pattern.Variable variable || !Bind(variable))
                    {
                        return false;
                    }

                    if (variable.Name != VariableScopeNames.Wildcard)
                    {
                        fields.Add(variable.Name, field);
                    }
                }
            }

            if (tupleInput < 0)
            {
                return false;
            }

            string[] early = producer.Arguments.OfType<Expr.Variable>().Select(variable => variable.Name)
                .Where(fields.ContainsKey).Distinct(StringComparer.Ordinal).ToArray();
            var late = LocalFunctionEntries.Walk(continuation).OfType<Expr.Variable>().Select(variable => variable.Name)
                .ToHashSet(StringComparer.Ordinal);
            if (early.Length > 1 || early.Any(late.Contains) || !late.Remove(binding.Name)
                || late.Count < 2 || late.Any(name => !fields.ContainsKey(name))
                || late.Count >= FirstReservedXRegister)
            {
                return false;
            }

            if (early is [var read])
            {
                parameters[tupleInput] = read;
            }

            if (ReadEntryCall(producer, parameters) is not { } call)
            {
                return false;
            }

            var ordered = late.OrderBy(name => fields[name]).ToArray();
            var inputs = ordered.Select((name, offset) => (name, register: offset + 1))
                .ToDictionary(input => input.name, input => input.register, StringComparer.Ordinal);
            inputs.Add(binding.Name, 0);
            if (ReadCallArgumentConstructions(
                [continuation],
                -1,
                new Dictionary<string, int>(),
                inputs: inputs
            ) is not { } constructor)
            {
                return false;
            }

            // Keep one checked tuple through the producer. None of the moved
            // projections has an early use, or an independent failure edge.
            owner.EmitTupleCheck(tupleInput, tupleArity, owner.nextLabel - 2);
            EmitFrameAllocation(Math.Max(tupleInput + 1, EntryCallLiveCount(call, parameters)), 1);
            owner.MoveRegister(
                XRegisterTag,
                tupleInput,
                YRegisterTag,
                0
            );
            if (early is [var projected])
            {
                int destination = tupleInput;
                int[] uses = producer.Arguments.Select((argument, index) =>
                    argument is Expr.Variable variable && variable.Name == projected ? index : -1
                ).Where(index => index >= 0).ToArray();
                if (uses is [var outgoing] && outgoing != tupleInput
                    && call.Moves.All(move => move.Source != outgoing))
                {
                    // No outgoing move reads the argument displaced by the direct projection.
                    destination = outgoing;
                    call = call with { Moves = call.Moves.Where(move => move.Destination != outgoing).ToList() };
                }

                owner.EmitTuplePart(tupleInput, fields[projected], destination);
            }

            EmitEntryCall(call);
            owner.Op(TestHeap, BeamWire.UnsignedTag, constructor.Constructions.Sum(item => item.HeapWords));
            owner.Operand(BeamWire.UnsignedTag, 1);
            foreach (string name in ordered)
            {
                owner.EmitTuplePart(
                    0,
                    fields[name],
                    inputs[name],
                    inputTag: YRegisterTag
                );
            }

            EmitCallArgumentConstructions(constructor, -1, heapChecked: true);
            owner.EmitFrameReturn(1);

            return true;

            bool Bind(Pattern.Variable variable) => variable.Name == VariableScopeNames.Wildcard || names.Add(variable.Name);
        }
    }
}

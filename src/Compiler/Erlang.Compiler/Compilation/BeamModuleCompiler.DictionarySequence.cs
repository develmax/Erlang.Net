using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record DictionaryRead(int Destination, ConstructionOperand Key, ConstructionOperand[]? Fields);

        private bool TryEmitDictionarySequenceTail(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence { Items: [Expr.Match, _, .., Expr.Call tail] } sequence
                || !TryResolveOrdinaryCall(tail, out string? module))
            {
                return false;
            }

            var statements = sequence.Items;
            var arguments = tail.Arguments.ToArray();
            var uses = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < statements.Count; index++)
            {
                Expr expression = statements[index] is Expr.Match match ? match.Value : statements[index];
                foreach (var variable in LocalFunctionEntries.Walk(expression).OfType<Expr.Variable>())
                {
                    uses[variable.Name] = index;
                }
            }

            var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                string name = ((Pattern.Variable)clause.Patterns[index]).Name;
                if (name != VariableScopeNames.Wildcard)
                {
                    names.Add(name);
                    if (uses.ContainsKey(name))
                    {
                        bindings.Add(name, index);
                    }
                }
            }

            int live = bindings.Values.DefaultIfEmpty(-1).Max() + 1;
            var steps = new List<DictionaryRead>();
            int heap = 0;
            for (int index = 0; index < statements.Count - 1; index++)
            {
                if (statements[index] is not Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } output,
                        Value: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.Get, Arguments: [var key]
                        } read
                    } || !names.Add(output.Name) || !owner.ResolvesErlangBif(read))
                {
                    return false;
                }

                var operand = ReadOperand(key);
                ConstructionOperand[]? fields = null;
                if (operand is null)
                {
                    if (key is not Expr.Tuple tuple)
                    {
                        return false;
                    }

                    var tupleOperands = tuple.Items.Select(ReadOperand).ToArray();
                    if (tupleOperands.Any(field => field is null))
                    {
                        return false;
                    }

                    fields = tupleOperands.Select(field => field!.Value).ToArray();
                    heap += fields.Length + 1;
                }

                foreach (string expired in bindings.Keys.Where(name => uses[name] == index).ToArray())
                {
                    bindings.Remove(expired);
                }

                int preferred = Array.FindIndex(arguments, argument => argument is Expr.Variable variable && variable.Name == output.Name);
                int destination = preferred >= 0 && !bindings.ContainsValue(preferred) ? preferred : 0;
                while (bindings.ContainsValue(destination))
                {
                    destination++;
                }

                if (destination >= FirstReservedXRegister)
                {
                    return false;
                }

                if (uses.ContainsKey(output.Name))
                {
                    bindings.Add(output.Name, destination);
                }

                steps.Add(new DictionaryRead(destination, operand ?? new ConstructionOperand(XRegisterTag, destination), fields));
            }

            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int index = 0; index < tail.Arguments.Count; index++)
            {
                if (ReadOperand(tail.Arguments[index]) is not { } operand)
                {
                    return false;
                }

                if (operand.Literal is { } literal)
                {
                    constants.Add((index, literal));
                }
                else if (operand.Register != index)
                {
                    moves.Add((index, operand.Register));
                }
            }

            if (heap != 0)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, heap);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            foreach (var step in steps)
            {
                if (step.Fields is { } fields)
                {
                    owner.EmitTupleHeader(XRegisterTag, fields.Length, step.Destination);
                    foreach (var field in fields)
                    {
                        EmitOperand(field);
                    }
                }

                owner.Op(BifOne, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Get, 1));
                if (step.Fields is null)
                {
                    EmitOperand(step.Key);
                }
                else
                {
                    owner.EmitTypedRegister(step.Destination, TupleTypeBits);
                }

                owner.Operand(XRegisterTag, step.Destination);
            }

            owner.EmitOrderedTailMoves(moves, constants);
            if (module is null)
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, tail.Arguments.Count);
                owner.Operand(LabelTag, entries[(tail.Function, tail.Arguments.Count)]);
            }
            else
            {
                owner.EmitCallLine(tail, location);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, tail.Arguments.Count);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, tail.Function, tail.Arguments.Count));
            }

            return true;

            ConstructionOperand? ReadOperand(Expr expression) =>
                expression is Expr.Variable variable && bindings.TryGetValue(variable.Name, out int register)
                    ? new ConstructionOperand(XRegisterTag, register)
                    : AttributeLiteral.TryNormalizeConstant(expression, out Term constant)
                        ? new ConstructionOperand(XRegisterTag, 0, constant) : null;

            void EmitOperand(ConstructionOperand operand)
            {
                if (operand.Literal is { } literal)
                {
                    owner.LiteralOperand(literal);
                }
                else
                {
                    owner.Operand(operand.Tag, operand.Register);
                }
            }
        }
    }
}

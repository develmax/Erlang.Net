using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleEntryListCall(Clause clause, int failure)
        {
            if (clause is not
                {
                    Guard: null, Patterns: [Pattern.Tuple tuple],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: Pattern.Variable list, Value: Expr.List { Tail: null } value },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable result,
                            Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } call
                        },
                            Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned] }
                        ]
                    }
                }
                || tuple.Items.Count < 2 || tuple.Items.Count >= FirstReservedXRegister
                || tuple.Items.Any(field => field is not Erlang.Pattern.Variable)
                || returned.Name != result.Name)
            {
                return false;
            }

            var fields = tuple.Items.Cast<Pattern.Variable>().ToArray();
            if (!DistinctBindings([.. fields, list, result]))
            {
                return false;
            }

            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < fields.Length; index++)
            {
                // The final projection may overwrite the tuple after all other fields are read.
                inputs.Add(fields[index].Name, index + 1 == fields.Length ? 0 : index + 1);
            }

            const string produced = "\0tuple-entry-list-bif";
            Expr.Call? producer = null;
            var items = new Expr[value.Items.Count];
            var readFields = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < items.Length; index++)
            {
                Expr item = value.Items[index];
                if (item is Expr.Variable field && inputs.ContainsKey(field.Name))
                {
                    items[index] = field;
                    readFields.Add(field.Name);
                }
                else if (producer is null && item is Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Function: AutoImportNames.Self or AutoImportNames.Node, Arguments.Count: 0
                } bif && owner.ResolvesErlangBif(bif))
                {
                    producer = bif;
                    items[index] = new Expr.Variable(produced);
                }
                else
                {
                    return false;
                }
            }

            if (producer is null || readFields.Count != fields.Length)
            {
                return false;
            }

            var arguments = new Expr[call.Arguments.Count];
            int listUses = 0;
            for (int index = 0; index < arguments.Length; index++)
            {
                Expr argument = call.Arguments[index];
                if (argument is Expr.Variable operand && operand.Name == list.Name)
                {
                    arguments[index] = value with { Items = items };
                    listUses++;
                }
                else if (AttributeLiteral.TryNormalizeConstant(argument, out Term constant))
                {
                    arguments[index] = new Expr.Literal(constant);
                }
                else
                {
                    return false;
                }
            }

            inputs.Add(produced, fields.Length);
            if (listUses != 1 || ReadCallArgumentConstructions(
                arguments,
                -1,
                new Dictionary<string, int>(),
                inputs: inputs
            ) is not { Moves.Count: 0, Loads.Count: 0 } plan)
            {
                return false;
            }

            owner.EmitTupleCheck(0, fields.Length, failure);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, plan.Constructions.Sum(construction => construction.HeapWords));
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = 0; index < fields.Length; index++)
            {
                owner.EmitTuplePart(0, index, inputs[fields[index].Name]);
            }

            // self/0 and node/0 preserve X registers and do not need a line or a heap check.
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, producer.Function, 0));
            owner.Operand(XRegisterTag, fields.Length);
            EmitCallArgumentConstructions(plan, -1, heapChecked: true);
            owner.EmitCallLine(call, location);
            owner.EmitStaticRemoteCall(call);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn();

            return true;
        }
    }
}

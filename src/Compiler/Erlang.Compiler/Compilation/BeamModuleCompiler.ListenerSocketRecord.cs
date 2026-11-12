using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListenerSocketRecord(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [AliasPattern alias, Pattern.Variable listener, Pattern.Variable port],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable socket, Value: Expr.RecordConstruction result },
                    Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable options, Expr.Variable argument] } check,
                    Expr.Variable returned
                ] }
            }] || !TryReadPatternAlias(alias, out var config, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } record
                || !DistinctBindings(config, listener, port, socket, new(options.Name))
                || socket.Name != argument.Name || socket.Name != returned.Name
                || !entries.TryGetValue((check.Function, 2), out int target))
            {
                return false;
            }

            int optionField = -1;
            for (int index = 1; index < record.Items.Count; index++)
            {
                if (record.Items[index] is Pattern.Variable field && field.Name == options.Name && optionField < 0)
                {
                    optionField = index;
                }
                else if (record.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            var fields = RecordExpressions.Create(result).Items;
            var operands = new (int Register, Term? Value)[fields.Count];
            bool hasPair = false;
            for (int index = 0; index < fields.Count; index++)
            {
                switch (fields[index])
                {
                    case Expr.Tuple { Items: [Expr.Variable handle, Expr.Variable number] } when !hasPair
                        && handle.Name == listener.Name && number.Name == port.Name:
                        operands[index] = (1, null);
                        hasPair = true;
                        break;
                    case Expr.Variable source when source.Name == config.Name:
                        operands[index] = (0, null);
                        break;
                    default:
                        if (!AttributeLiteral.TryNormalizeConstant(fields[index], out Term value))
                        {
                            return false;
                        }

                        operands[index] = (-1, value);
                        break;
                }
            }

            if (optionField < 0 || !hasPair)
            {
                return false;
            }

            if (!AlwaysReceivesTaggedArgument(function, 0, tag, record.Items.Count))
            {
                EmitDetailRecordTest(0, record.Items.Count, tag, owner.nextLabel - 2);
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, fields.Count + 4);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTagMatchTuple(1, (XRegisterTag, 1), (XRegisterTag, 2));
            owner.EmitTupleHeader(YRegisterTag, fields.Count);
            foreach (var (register, value) in operands)
            {
                if (value is null)
                {
                    owner.Operand(XRegisterTag, register);
                }
                else
                {
                    owner.LiteralOperand(value);
                }
            }

            owner.EmitTuplePart(0, optionField, 0);
            Load(0, 1);
            var previous = location;
            EmitWindowLocal(check, target, ref previous);
            Load(0);
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}

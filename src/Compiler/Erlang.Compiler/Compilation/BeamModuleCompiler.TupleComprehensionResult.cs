using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleComprehensionResult(FunctionDefinition function)
        {
            Clause match = function.Clauses[0];
            if (!function.IsRecursiveListMapper || match is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Tuple input] }, ..],
                    Body: Expr.List { Items: [Expr.Tuple mapped] }
                }
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            var fields = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < input.Items.Count; index++)
            {
                if (input.Items[index] is not Pattern.Variable variable
                    || variable.Name != VariableScopeNames.Wildcard && !fields.TryAdd(variable.Name, index))
                {
                    return false;
                }
            }

            var captureNames = match.Patterns.Skip(1).Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
            var projected = new SortedSet<int>();
            var operands = new (int Field, int Capture, Term? Constant)[mapped.Items.Count];
            for (int index = 0; index < mapped.Items.Count; index++)
            {
                Expr item = mapped.Items[index];
                if (item is Expr.Variable variable)
                {
                    if (fields.TryGetValue(variable.Name, out int field))
                    {
                        projected.Add(field);
                        operands[index] = (field, -1, null);
                    }
                    else
                    {
                        int capture = Array.IndexOf(captureNames, variable.Name);
                        if (capture < 0)
                        {
                            return false;
                        }

                        operands[index] = (-1, capture, null);
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(item, out Term value))
                {
                    operands[index] = (-1, -1, value);
                }
                else
                {
                    return false;
                }
            }

            if (projected.Count >= FirstReservedXRegister || operands.All(operand => operand.Constant is not null))
            {
                return false;
            }

            bool identity = operands.Length == input.Items.Count
                && operands.Select((operand, index) => operand.Field == index).All(match => match);
            int arity = function.Arity;
            bool preserveTuple = projected.Count != 0;
            int slots = captureNames.Length + (preserveTuple ? 1 : 0);
            int skip = Label();
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                arity,
                XRegisterTag,
                0
            );
            EmitTupleShapeTest(
                XRegisterTag,
                arity,
                input.Items.Count,
                skip
            );
            EmitFrameAllocation(arity + (preserveTuple ? 1 : 0), slots);
            if (preserveTuple)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    arity,
                    YRegisterTag,
                    0
                );
            }

            for (int register = arity - 1; register > 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    slots - register
                );
            }

            owner.EmitFieldCall(arity, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + (identity ? 0 : mapped.Items.Count + 1));
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (!identity)
            {
                var registers = new Dictionary<int, int>();
                foreach (int field in projected)
                {
                    int register = registers.Count + 1;
                    registers.Add(field, register);
                    owner.Op(GetTupleElement, YRegisterTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, field);
                    owner.Operand(XRegisterTag, register);
                }

                owner.Op(PutTuple, XRegisterTag, 1);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, mapped.Items.Count);
                foreach (var (field, capture, constant) in operands)
                {
                    if (constant is not null)
                    {
                        owner.LiteralOperand(constant);
                    }
                    else if (field >= 0)
                    {
                        owner.Operand(XRegisterTag, registers[field]);
                    }
                    else
                    {
                        owner.Operand(YRegisterTag, slots - 1 - capture);
                    }
                }
            }

            owner.Op(PutList, identity ? YRegisterTag : XRegisterTag, identity ? 0 : 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(slots);
            Mark(skip);
            EmitRecursiveListMappingRejection(function, entry, arity);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}

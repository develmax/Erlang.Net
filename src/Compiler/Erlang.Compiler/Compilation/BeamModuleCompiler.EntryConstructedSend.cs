using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryConstructedSend(Clause clause, int functionInfo)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns:
                    [
                        Pattern.Tuple { Items: [Pattern.Variable field, Pattern.Variable receiver] },
                        Pattern.Variable input
                    ],
                    Body: Expr.Sequence
                    {
                        Items: [Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Variable destination } send, var result]
                    }
                }
                || receiver.Name != destination.Name
                || field.Name == receiver.Name || field.Name == input.Name || receiver.Name == input.Name
                || receiver.Name == VariableScopeNames.Wildcard
                || LocalFunctionEntries.Walk(send.Right).OfType<Expr.Variable>().Any(value => value.Name == receiver.Name)
                || !AttributeLiteral.TryNormalizeConstant(result, out Term continuation))
            {
                return false;
            }

            const int tupleRegister = 0;
            const int inputRegister = 1;
            const int fieldRegister = 2;
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [receiver.Name] = tupleRegister };
            if (field.Name != VariableScopeNames.Wildcard)
            {
                inputs.Add(field.Name, fieldRegister);
            }

            if (input.Name != VariableScopeNames.Wildcard)
            {
                inputs.Add(input.Name, inputRegister);
            }

            if (ReadCallArgumentConstructions(
                [destination, send.Right],
                result: -1,
                saved: new Dictionary<string, int>(),
                inputs: inputs,
                preserveX0: true
            ) is not { } plan || plan.Live != fieldRegister + 1)
            {
                return false;
            }

            // The tuple lives in X0 until the message is ready. Only then replace it with the receiver.
            EmitTupleShapeTest(XRegisterTag, tupleRegister, 2, functionInfo);
            EmitFrameAllocation(2, 0, plan.Constructions.Sum(item => item.HeapWords));
            owner.Op(GetTupleElement, XRegisterTag, tupleRegister);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, fieldRegister);
            EmitCallArgumentConstructions(plan, -1, heapChecked: true);
            owner.EmitTupleElement(1, tupleRegister);
            var sendLocation = location;
            owner.EmitArithmeticLine(send.SourceLine, ref sendLocation);
            owner.instructions.WriteByte(Send);
            owner.EmitConstant(continuation, 0);
            owner.EmitFrameReturn();

            return true;
        }
    }
}

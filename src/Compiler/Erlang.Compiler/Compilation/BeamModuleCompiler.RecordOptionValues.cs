using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordOptionValues(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [
                    Pattern.Variable options, Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom tag }, Pattern.Variable first, Pattern.Variable second,
                        Pattern.Variable third, Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable last
                    ] }
                ], Body: Expr.RecordConstruction record
            }] || !DistinctBindings(options, first, second, third, last)
                || record.Wildcard is not null
                || !record.Fields.Select(field => field.Name).SequenceEqual(record.Definition.Fields
                    .Select(field => field.Name).Where(name => record.Fields.Any(field => field.Name == name)))
                || RecordExpressions.Create(record).Items is not [
                    Expr.Literal { Value: Atom resultTag }, Expr.Call firstCall, Expr.Call secondCall, Expr.Call thirdCall,
                    var constant, Expr.Call activeCall
                ]
                || !tag.Equals(resultTag) || !AttributeLiteral.TryNormalizeConstant(constant, out Term fixedValue)
                || !ReadOptionValue(firstCall, first, out var firstKey)
                || !ReadOptionValue(secondCall, second, out var secondKey)
                || !ReadOptionValue(thirdCall, third, out var thirdKey)
                || activeCall is not { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input, Expr.Variable fallback] }
                || input.Name != options.Name || fallback.Name != last.Name
                || !entries.TryGetValue((activeCall.Function, 2), out int activeEntry))
            {
                return false;
            }

            EmitDetailRecordTest(1, 6, tag, owner.nextLabel - 2);
            owner.Op(Allocate, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 2);
            SaveWindowResult(3);
            owner.EmitTuplePart(1, 1, 2);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(firstKey, 0);
            var previous = location;
            EmitWindowCall(firstCall, ref previous);
            LoadDefault(2, 2);
            SaveWindowResult(1);
            Load(3, 1);
            owner.EmitConstant(secondKey, 0);
            EmitWindowCall(secondCall, ref previous);
            LoadDefault(3, 2);
            SaveWindowResult(0);
            Load(3, 1);
            owner.EmitConstant(thirdKey, 0);
            EmitWindowCall(thirdCall, ref previous);
            LoadDefault(5, 1);
            owner.Op(Swap, YRegisterTag, 3);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 2);
            TrimDetailFrame(1, 3);
            EmitWindowLocal(activeCall, activeEntry, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 6);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(YRegisterTag, 2);
            owner.LiteralOperand(fixedValue);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(3);

            return true;

            bool ReadOptionValue(Expr.Call call, Pattern.Variable boundDefault, out Atom key)
            {
                if (call is { Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Literal { Value: Atom atom }, Expr.Variable source, Expr.Variable value
                ] } && source.Name == options.Name && value.Name == boundDefault.Name && !owner.ResolvesErlangBif(call))
                {
                    key = atom;

                    return true;
                }

                key = null!;
                return false;
            }

            void LoadDefault(int field, int register)
            {
                owner.Op(GetTupleElement, YRegisterTag, 2);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, register);
            }
        }
    }
}

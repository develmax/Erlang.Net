using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListConversionTuple(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Sequence sequence }]
                || sequence.Items is not [
                    Expr.Match
                {
                    Pattern: Pattern.List { Tail: null } list,
                    Value: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable source, Expr.Literal separator] } split
                } match,
                    Expr.Tuple tuple
                ]
                || !DistinctBindings(input) || source.Name != input.Name || list.Items.Count < 2
                || list.Items.Count != tuple.Items.Count || list.Items.Any(item => item is not Pattern.Variable { }))
            {
                return false;
            }

            var bindings = list.Items.Cast<Pattern.Variable>().ToArray();
            if (!DistinctBindings([input, .. bindings]))
            {
                return false;
            }
            var conversions = new List<Expr.Call>(bindings.Length);
            for (int index = 0; index < bindings.Length; index++)
            {
                if (tuple.Items[index] is not Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable operand] } convert
                    || operand.Name != bindings[index].Name)
                {
                    return false;
                }
                conversions.Add(convert);
            }

            int slots = bindings.Length - 1;
            int failure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, slots);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, slots);
            for (int slot = 0; slot < slots; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            owner.EmitConstant(separator.Value, 1);
            var previous = location;
            Invoke(split);

            for (int index = 0; index < bindings.Length; index++)
            {
                int tail = index == 0 ? 0 : 2;
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(XRegisterTag, tail);
                owner.Op(GetList, XRegisterTag, tail);
                owner.Operand(index == 0 ? XRegisterTag : YRegisterTag, index == 0 ? 1 : bindings.Length - 1 - index);
                owner.Operand(XRegisterTag, 2);
            }
            owner.Op(IsNil, LabelTag, failure);
            owner.Operand(XRegisterTag, 2);
            owner.MoveToX0(XRegisterTag, 1);
            for (int index = 0; index < conversions.Count; index++)
            {
                if (index != 0)
                {
                    owner.Op(Swap, YRegisterTag, conversions.Count - 1 - index);
                    owner.Operand(XRegisterTag, 0);
                }
                Invoke(conversions[index]);
            }
            owner.Op(TestHeap, BeamWire.UnsignedTag, conversions.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, conversions.Count);
            for (int slot = slots - 1; slot >= 0; slot--)
            {
                owner.Operand(YRegisterTag, slot);
            }
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(slots);
            Mark(failure);
            owner.EmitLine(match.SourceLine == 0 ? location : new(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void Invoke(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                Call(call.Module, call.Function, call.Arguments.Count);
                previous = CallLocation(call, previous);
            }
        }
    }
}

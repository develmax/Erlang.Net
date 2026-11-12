using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CallListItem(
            Expr.Call Report,
            Term Argument,
            Expr.Call Read,
            int Entry,
            Term Title,
            Term View
        );

        private bool TryEmitInterleavedCallList(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [], Body: Expr.Sequence { Items: var sequence } }
            ] || sequence.Count < 4
                || sequence[^1] is not Expr.List { Tail: null, Items: var result }
                || sequence.Count != 2 * result.Count + 2
                || ReadCallListReport(sequence[^2]) is not { } final)
            {
                return false;
            }

            var items = new List<CallListItem>(result.Count);
            var variables = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < result.Count; index++)
            {
                if (ReadCallListReport(sequence[2 * index]) is not { } report
                    || sequence[2 * index + 1] is not Expr.Match
                    {
                        Pattern: Pattern.Variable bound,
                        Value: Expr.Call { Module: null, Arguments: [] } read
                    }
                    || !DistinctBindings(bound) || !variables.Add(bound.Name)
                    || result[index] is not Expr.Tuple { Items: [Expr.Literal title, Expr.Literal view, Expr.Variable value] }
                    || value.Name != bound.Name || !entries.TryGetValue((read.Function, 0), out int entry))
                {
                    return false;
                }
                items.Add(new(
                    report.Call,
                    report.Argument,
                    read,
                    entry,
                    title.Value,
                    view.Value
                ));
            }

            int frame = items.Count;
            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, frame);
            for (int slot = 0; slot < frame; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }

            var previous = location;
            for (int index = 0; index < items.Count; index++)
            {
                var item = items[index];
                EmitCallListReport(item.Report, item.Argument, previous);
                var reportLocation = CallLocation(item.Report, previous);
                owner.EmitCallLine(item.Read, reportLocation);
                owner.EmitFieldCall(0, item.Entry);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    frame - index - 1
                );
                previous = CallLocation(item.Read, reportLocation);
            }
            EmitCallListReport(final.Call, final.Argument, previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, frame * (4 + ConsHeapWords));
            owner.Operand(BeamWire.UnsignedTag, 0);
            for (int slot = 0; slot < frame; slot++)
            {
                var item = items[frame - slot - 1];
                int register = slot == 0 ? 0 : 1;
                owner.EmitTupleHeader(XRegisterTag, 3, register);
                owner.LiteralOperand(item.Title);
                owner.LiteralOperand(item.View);
                owner.Operand(YRegisterTag, slot);
                owner.Op(PutList, XRegisterTag, register);
                if (slot == 0)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }
                owner.Operand(XRegisterTag, 0);
            }
            owner.EmitFrameReturn(frame);

            return true;
        }

        private static (Expr.Call Call, Term Argument)? ReadCallListReport(Expr expression) =>
            expression is Expr.Call { Module: not null, DynamicRemote: false, Arguments: [var argument] } call
            && AttributeLiteral.TryNormalizeConstant(argument, out Term value) ? (call, value) : null;

        private void EmitCallListReport(Expr.Call call, Term argument, BeamSourceLocation previous)
        {
            owner.EmitConstant(argument, 0);
            owner.EmitCallLine(call, previous);
            owner.EmitStaticRemoteCall(call);
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTerminalCallList(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.List { Tail: null } list } clause]
                || !HasUnconditionalParameters(clause) || list.Items.Count < 2
                || list.Items[0] is not Expr.Call first)
            {
                return false;
            }

            var inputs = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(first, inputs) is not { } firstCall)
            {
                return false;
            }

            // Entry arguments are dead after the first call if the remaining calls
            // can prepare their arguments without any of the entry registers.
            var calls = new List<EntryCall>(list.Items.Count) { firstCall };
            foreach (Expr item in list.Items.Skip(1))
            {
                if (item is not Expr.Call call || ReadEntryCall(call, []) is not { } plan)
                {
                    return false;
                }

                calls.Add(plan);
            }

            int savedResults = calls.Count - 1;
            EmitGeneralFrame(EntryCallLiveCount(firstCall, inputs), slots: savedResults);
            var previous = location;
            for (int index = 0; index < calls.Count; index++)
            {
                EntryCall call = calls[index];
                EmitEntryCall(call, previous);
                previous = CallLocation(call.Call, previous);
                if (index < savedResults)
                {
                    owner.MoveRegister(XRegisterTag, 0, YRegisterTag, savedResults - index - 1);
                }
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, calls.Count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = 0; index < calls.Count; index++)
            {
                owner.instructions.WriteByte(PutList);
                owner.Operand(index == 0 ? XRegisterTag : YRegisterTag, Math.Max(0, index - 1));
                if (index == 0)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }

                owner.Operand(XRegisterTag, 0);
            }
            owner.EmitFrameReturn(savedResults);
            return true;
        }
    }
}

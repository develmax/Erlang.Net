using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConsCallContinuation(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments: [Expr.Call { Module: null, DynamicRemote: false } producer, Expr.Variable savedTail]
                    } continuation
                } cons,
                { Guard: null, Body: Expr.Variable passthrough } empty
                ]
                || !entries.TryGetValue((continuation.Function, 2), out int target))
            {
                return false;
            }

            bool nested;
            Pattern.Variable value;
            Pattern.Variable head;
            Pattern.Variable tail;
            if (cons.Patterns is
                [
                Pattern.List { Items: [Pattern.Variable first, Pattern.Variable second], Tail: Pattern.Variable rest }
                ]
                && empty.Patterns is [Pattern.List { Items: [Pattern.Variable singleton], Tail: null }]
                && singleton.Name == passthrough.Name)
            {
                nested = true;
                value = first;
                head = second;
                tail = rest;
            }
            else if (cons.Patterns is
                [
                Pattern.Variable argument,
                Pattern.List { Items: [Pattern.Variable item], Tail: Pattern.Variable more }
                ]
                && empty.Patterns is [Pattern.Variable fallback, var nil]
                && ListEntryTest(nil) == IsNil && fallback.Name == passthrough.Name)
            {
                nested = false;
                value = argument;
                head = item;
                tail = more;
            }
            else
            {
                return false;
            }

            if (!DistinctBindings(value, head, tail) || savedTail.Name != tail.Name
                || passthrough.Name == VariableScopeNames.Wildcard
                || producer.Arguments is not [Expr.Variable left, Expr.Variable right]
                || left.Name != value.Name || right.Name != head.Name
                || ReadEntryCall(producer, nested ? [head.Name, value.Name] : [value.Name, head.Name]) is not { Module: null } call)
            {
                return false;
            }

            const int firstHead = 1;
            const int firstTail = 2;
            const int frame = 1;
            const int tailSlot = 0;
            int nonCons = Label();
            int input = nested ? firstTail : 1;
            if (nested)
            {
                owner.EmitNonemptyList(0, functionInfo);
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    firstHead,
                    XRegisterTag,
                    firstTail
                );
            }

            owner.EmitNonemptyList(input, nonCons);
            EmitFrameAllocation(input + 1, frame);
            owner.EmitListParts(
                input,
                XRegisterTag,
                nested ? 0 : 1,
                YRegisterTag,
                tailSlot
            );
            EmitEntryCall(call);
            owner.MoveRegister(
                YRegisterTag,
                tailSlot,
                XRegisterTag,
                1
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, continuation.Arguments.Count);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, frame);

            Mark(nonCons);
            owner.EmitNilTest(XRegisterTag, input, functionInfo);
            if (nested)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    firstHead,
                    XRegisterTag,
                    0
                );
            }

            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}

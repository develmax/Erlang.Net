using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEffectBeforeReceiveCallCase(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable state],
                        Body: Expr.Sequence
                        {
                            Items:
                            [
                                Expr.Call { Arguments: [Expr.Fun { SelfName: null } closure, var first, var second] } effect,
                                Expr.Receive
                            {
                                Timeout: null,
                                After: null,
                                Clauses:
                                    [
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                                        Body: Expr.Literal
                                    }
                                    ]
                            },
                                Expr.Case
                            {
                                Value: Expr.Call { Arguments: [Expr.Variable received] } read,
                                Clauses:
                                    [
                                    { Guard: null, Patterns: [Pattern.Variable expected], Body: Expr.Call { Arguments.Count: 0 } match },
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Variable other],
                                        Body: Expr.Call { Arguments: [Expr.Variable value] } fallback
                                    }
                                    ]
                            }
                            ]
                        }
                    }
                ]
                || !DistinctBindings(state, payload, other) || expected.Name != state.Name
                || received.Name != payload.Name || value.Name != other.Name
                || closure.Clauses[0].Patterns.Count != 0
                || ClosureCaptures.Capture(closure, new Dictionary<string, Term>(StringComparer.Ordinal) { [state.Name] = Nil.Value }).Count != 0
                || !AttributeLiteral.TryNormalizeConstant(first, out Term firstValue)
                || !AttributeLiteral.TryNormalizeConstant(second, out Term secondValue)
                || !TryResolveOrdinaryCallTarget(effect, out string? effectModule)
                || !TryResolveOrdinaryCallTarget(read, out string? readModule)
                || !TryResolveOrdinaryCallTarget(match, out string? matchModule, allowControlTransfer: true)
                || !TryResolveOrdinaryCallTarget(fallback, out string? fallbackModule, allowControlTransfer: true))
            {
                return false;
            }

            bool atomState = ReadFiniteAtomArguments(function, 0, admitGuardFacts: true) is { Count: > 0 };
            const int frame = 1;
            int scan = Label();
            int mismatch = Label();
            int retry = Label();
            int wait = Label();

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, frame);
            owner.EmitClosureHeap(0, 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            EmitGeneralClosure(closure, heapChecked: true);
            owner.EmitConstant(secondValue, 2);
            owner.EmitConstant(firstValue, 1);
            owner.EmitCallLine(effect, location);
            Call(effectModule, effect.Function, 3);

            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, retry);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            owner.instructions.WriteByte(RemoveMessage);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitCallLine(read, CallLocation(effect, location));
            Call(readModule, read.Function, 1);
            var readLine = CallLocation(read, location);
            owner.Op(ExactEqualTest, LabelTag, mismatch);
            owner.Operand(XRegisterTag, 0);
            if (atomState)
            {
                owner.EmitTypedRegister(0, AtomTypeBits, registerTag: YRegisterTag);
            }
            else
            {
                owner.Operand(YRegisterTag, 0);
            }

            EmitPreparedFrameTail(new EntryCall(
                match,
                matchModule,
                [],
                []
            ), readLine, frame);
            Mark(mismatch);
            EmitPreparedFrameTail(new EntryCall(
                fallback,
                fallbackModule,
                [],
                []
            ), readLine, frame);
            Mark(retry);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(Wait, LabelTag, scan);

            return true;
        }
    }
}

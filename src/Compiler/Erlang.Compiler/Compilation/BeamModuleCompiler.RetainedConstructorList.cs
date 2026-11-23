using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRetainedConstructorList(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || parentFunction is null
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable input],
                        Body: Expr.Sequence
                        {
                            Items: [
                                Expr.Match { Pattern: Pattern.Variable retained, Value: Expr.Call constructor },
                                Expr.Match { Pattern: Pattern.Variable list, Value: Expr.List { Tail: null } values },
                                Expr.Match
                            {
                                Pattern: Pattern.Variable handler,
                                Value: Expr.Fun { SelfName: null } closure
                            },
                                Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable callback, Expr.Variable rows]
                            } effect,
                                Expr.Variable result
                            ]
                        }
                    }
                ] || !DistinctBindings(
                    input,
                    retained,
                    list,
                    handler
                ) || retained.Name != result.Name || handler.Name != callback.Name || list.Name != rows.Name
                || constructor is not { Module: not null, DynamicRemote: false }
                || ReadEntryCall(constructor, [input.Name]) is not { ConstructedArgument: null } first
                || values.Items.Count < 2)
            {
                return false;
            }

            var available = new Dictionary<string, Term>(StringComparer.Ordinal)
            {
                [input.Name] = Nil.Value,
                [retained.Name] = Nil.Value,
                [list.Name] = Nil.Value
            };
            var captures = ClosureCaptures.Capture(closure, available);
            if (captures.Count != 1 || !captures.ContainsKey(retained.Name))
            {
                return false;
            }

            var constructors = new List<EntryCall>();
            foreach (Expr value in values.Items)
            {
                if (value is not Expr.Call { Module: not null, DynamicRemote: false, Arguments.Count: >= 2 } call
                    || ReadEntryCall(call, [], [input.Name]) is not
                    {
                        SavedInputs: [{ Destination: 0, Source: 0 }],
                        ConstructedArgument: null
                    } child || child.Constants.Count != call.Arguments.Count - 1)
                {
                    return false;
                }

                constructors.Add(child);
            }

            int frame = constructors.Count;
            int inputSlot = frame - 1;
            int retainedSlot = frame - 2;
            EmitFrameAllocation(1, frame);
            EmitFrameInitialization(0, inputSlot);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                inputSlot
            );
            EmitEntryCall(first);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                retainedSlot
            );
            BeamSourceLocation previous = CallLocation(constructor, location);
            for (int index = 0; index < constructors.Count; index++)
            {
                var child = constructors[index];
                bool final = index == constructors.Count - 1;
                if (final)
                {
                    owner.Op(Swap, YRegisterTag, inputSlot);
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        inputSlot,
                        XRegisterTag,
                        1
                    );
                }

                foreach (var (destination, argument) in child.Constants.Skip(1))
                {
                    owner.EmitConstant(argument, destination);
                }

                if (!final)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        1,
                        XRegisterTag,
                        0
                    );
                }

                owner.EmitConstant(child.Constants[0].Value, 1);
                owner.EmitCallLine(child.Call, previous);
                owner.EmitStaticRemoteCall(child.Call);
                previous = CallLocation(child.Call, previous);
                if (index < constructors.Count - 2)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        retainedSlot - 1 - index
                    );
                }
            }

            owner.EmitClosureAllocation(ConsHeapWords * constructors.Count + captures.Count, 1, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            AppendSavedResult(inputSlot);
            for (int slot = 0; slot < retainedSlot; slot++)
            {
                AppendSavedResult(slot);
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            var saved = variables;
            var order = entryCaptureOrder;
            variables = new(StringComparer.Ordinal) { [retained.Name] = retainedSlot };
            entryCaptureOrder = new Dictionary<string, int>(StringComparer.Ordinal) { [retained.Name] = 0 };
            EmitGeneralClosure(
                closure,
                emitCapturedFailure: true,
                heapChecked: true,
                destination: 0
            );
            variables = saved;
            entryCaptureOrder = order;
            owner.MoveRegister(
                YRegisterTag,
                retainedSlot,
                YRegisterTag,
                inputSlot
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, frame - 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitCallLine(effect, previous);
            owner.EmitStaticRemoteCall(effect);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitFrameReturn(1);

            return true;

            void AppendSavedResult(int slot)
            {
                owner.Op(PutList, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
        }
    }
}

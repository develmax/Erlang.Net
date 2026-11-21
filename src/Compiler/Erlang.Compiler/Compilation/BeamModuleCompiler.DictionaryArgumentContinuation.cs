using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDictionaryArgumentContinuation(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } record, ..] patterns,
                        Body: Expr.Sequence
                        {
                            Items: [Expr.Call
                            {
                                Module: not null, DynamicRemote: false, Arguments: [Expr.Call
                                {
                                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                                    Function: AutoImportNames.Get, Arguments: [Expr.Literal { Value: var key }]
                                } lookup, .., Expr.List { Tail: null } arguments]
                            } effect, ..] items
                        }
                    }
                ]
                || patterns.Count is < 1 or > 2 || record.Items.Skip(1).Any(field => field is not Erlang.Pattern.Variable)
                || !owner.ResolvesErlangBif(lookup) || !IsOrdinaryRemote(effect)
                || effect.Arguments.Skip(1).SkipLast(1).Any(argument => !AttributeLiteral.TryNormalizeConstant(argument, out _)))
            {
                return false;
            }

            var fields = record.Items.Skip(1).Cast<Pattern.Variable>()
                .Select((field, index) => (field.Name, Index: index + 1))
                .Where(field => field.Name != VariableScopeNames.Wildcard).ToArray();
            if (fields is not [var binding] || patterns.Count == 2
                && patterns[1] is not Pattern.Variable { Name: not VariableScopeNames.Wildcard })
            {
                return false;
            }

            string fieldName = binding.Name;
            string? otherName = patterns.Count == 2 ? ((Pattern.Variable)patterns[1]).Name : null;
            if (otherName == fieldName || arguments.Items.Count != patterns.Count
                || arguments.Items[0] is not Expr.Variable first || first.Name != fieldName
                || otherName is not null && (arguments.Items[1] is not Expr.Variable second || second.Name != otherName))
            {
                return false;
            }

            var retained = new Dictionary<string, int>(StringComparer.Ordinal) { [fieldName] = 0 };
            var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
            if (otherName is not null)
            {
                incoming.Add(otherName, 1);
            }

            int frame = patterns.Count;
            var effectLocation = CallLocation(effect, location);
            Action? continuation = ReadContinuation();
            if (continuation is null || ReadCallArgumentConstructions(
                effect.Arguments,
                result: 0,
                saved: retained,
                inputs: incoming,
                preserveX0: true
            ) is not { } construction)
            {
                return false;
            }

            EmitDetailRecordTest(
                0,
                record.Items.Count,
                tag,
                owner.nextLabel - 2
            );
            EmitFrameAllocation(frame, frame, construction.Constructions.Sum(node => node.HeapWords));
            if (otherName is not null)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    1
                );
            }

            GetTupleField(
                0,
                binding.Index,
                YRegisterTag,
                0
            );
            owner.EmitDictionaryLookup(0, key);
            EmitCallArgumentConstructions(construction, 0, heapChecked: true);
            owner.EmitCallLine(effect, location);
            owner.EmitStaticRemoteCall(effect);
            continuation();

            return true;

            bool IsOrdinaryRemote(Expr.Call call) => call is { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false }
                && TryResolveOrdinaryCallTarget(call, out _);

            bool Forwards(Expr.Call call) => IsOrdinaryRemote(call) && call.Arguments.Count == frame
                && call.Arguments[0] is Expr.Variable id && id.Name == fieldName
                && (otherName is null || call.Arguments[1] is Expr.Variable input && input.Name == otherName);

            bool Fresh(string name) => name != VariableScopeNames.Wildcard && name != fieldName && name != otherName;

            void LoadRetained() => owner.EmitOrderedTailMoves(
                [],
                [],
                Enumerable.Range(0, frame).Select(index => (Destination: index, Source: index)).ToList()
            );

            void CallForwarded(Expr.Call call, BeamSourceLocation previous)
            {
                owner.EmitCallLine(call, previous);
                owner.EmitStaticRemoteCall(call);
            }

            void Trim(int count, int remaining)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, count);
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }

            Action? ReadContinuation()
            {
                switch (items)
                {
                    case [_, Expr.Call tail] when Forwards(tail):
                        return () =>
                        {
                            LoadRetained();
                            owner.EmitCallLine(tail, effectLocation);
                            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, frame);
                            owner.Operand(BeamWire.UnsignedTag, owner.Import(tail.Module!, tail.Function, frame));
                            owner.Operand(BeamWire.UnsignedTag, frame);
                        };
                    case [_, Expr.Call ignored, Expr.Literal { Value: Atom value }] when Forwards(ignored):
                        return () =>
                        {
                            LoadRetained();
                            Trim(frame, 0);
                            CallForwarded(ignored, effectLocation);
                            owner.EmitConstant(value, 0);
                            owner.EmitFrameReturn();
                        };
                    case [_, Expr.Match { Pattern: Pattern.Variable saved, Value: Expr.Call valueCall }, Expr.Call mutation,
                        Expr.Variable result] when frame == 2 && Fresh(saved.Name) && result.Name == saved.Name
                        && Forwards(valueCall) && Forwards(mutation):
                        return () =>
                        {
                            LoadRetained();
                            CallForwarded(valueCall, effectLocation);
                            Load(0, 2);
                            Load(1, 1);
                            owner.MoveRegister(
                                XRegisterTag,
                                0,
                                YRegisterTag,
                                1
                            );
                            Trim(1, 1);
                            owner.MoveToX0(XRegisterTag, 2);
                            CallForwarded(mutation, CallLocation(valueCall, effectLocation));
                            Load(0);
                            owner.EmitFrameReturn(1);
                        };
                    case [_, Expr.Case { Value: Expr.Call selected, Clauses: [var firstArm, var secondArm] }]
                        when Forwards(selected) && firstArm.Guard is null && secondArm.Guard is null:
                        return ReadResultCase(selected, firstArm, secondArm);
                    default:
                        return null;
                }
            }

            Action? ReadResultCase(Expr.Call call, Clause firstArm, Clause secondArm)
            {
                if (frame == 1 && firstArm is
                    {
                        Patterns: [Pattern.Literal { Value: Atom missing }],
                        Body: Expr.List { Items.Count: 0, Tail: null } or Expr.Literal { Value: Nil }
                    }
                    && secondArm is { Patterns: [Pattern.Variable otherwise], Body: Expr.Variable identity }
                    && Fresh(otherwise.Name) && otherwise.Name == identity.Name)
                {
                    return () =>
                    {
                        LoadRetained();
                        CallForwarded(call, effectLocation);
                        int other = Label();
                        owner.EmitAtomTest(0, missing.Name, other);
                        owner.EmitConstant(Nil.Value, 0);
                        owner.EmitFrameReturn(frame);
                        Mark(other);
                        owner.EmitFrameReturn(frame);
                    };
                }

                if (frame != 2 || firstArm is not
                    {
                        Patterns: [Pattern.List
                        {
                            Items: [Pattern.Variable head],
                            Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                        }],
                        Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom label }, Expr.Variable returned] }
                    }
                    || !Fresh(head.Name) || returned.Name != head.Name || secondArm is not
                    {
                        Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Body: Expr.Literal { Value: Atom fallback }
                    })
                {
                    return null;
                }

                return () =>
                {
                    LoadRetained();
                    EmitFrameInitialization(0, frame);
                    CallForwarded(call, effectLocation);
                    int empty = Label();
                    owner.EmitNonemptyList(0, empty);
                    owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                    owner.Operand(BeamWire.UnsignedTag, 1);
                    owner.Op(GetHead, XRegisterTag, 0);
                    owner.Operand(XRegisterTag, 0);
                    owner.EmitTupleHeader(XRegisterTag, 2);
                    owner.LiteralOperand(label);
                    owner.Operand(XRegisterTag, 0);
                    owner.EmitFrameReturn(frame);
                    Mark(empty);
                    owner.EmitConstant(fallback, 0);
                    owner.EmitFrameReturn(frame);
                };
            }
        }
    }
}

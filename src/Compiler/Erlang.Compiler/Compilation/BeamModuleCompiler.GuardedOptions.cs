using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool IsIntegerRangeCall(Expr.Call call) =>
            call is { Function: AutoImportNames.IsInteger, Arguments.Count: 3, DynamicRemote: false }
            && owner.ResolvesErlangBif(call);

        private void EmitIntegerRangeTests(int failure, long lower, long upper)
        {
            owner.Op(IsInteger, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsGreaterOrEqual, LabelTag, failure);
            owner.EmitTypedRegister(0, IntegerTypeBits);
            owner.LiteralOperand(Term.I(lower));
            owner.Op(IsGreaterOrEqual, LabelTag, failure);
            owner.LiteralOperand(Term.I(upper));
            owner.EmitTypedRegister(0, IntegerTypeBits, lower: lower);
        }

        private bool IsBinaryPartGuard(Expr.Call call) =>
            call is { Function: AutoImportNames.BinaryPart, Arguments.Count: 3, DynamicRemote: false }
            && owner.ResolvesErlangBif(call);

        private void EmitTernaryBifValue(Expr.Call call, int? failure, bool needsGc)
        {
            int operandBase = nextSlot;
            int?[] slots = call.Arguments.Select(argument => GuardSlot(argument, failure)).ToArray();
            if (failure is null)
            {
                owner.EmitCallLine(call, location);
            }

            owner.Op(needsGc ? GcBifThree : BifThree, LabelTag, failure ?? 0);
            if (needsGc)
            {
                owner.Operand(BeamWire.UnsignedTag, ReceiveGuardLiveRegisters);
            }


            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 3));
            for (int index = 0; index < slots.Length; index++)
            {
                GuardOperand(call.Arguments[index], slots[index]);
            }

            owner.Operand(XRegisterTag, 0);
            nextSlot = Math.Max(operandBase, variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1);
        }


        internal sealed record GuardedOption(
            Expr.Call Call,
            Atom First,
            Atom Second,
            Atom Tag,
            Pattern.Variable Value,
            byte Test,
            long? Lower,
            Expr Success,
            Expr Fallback
        );

        internal static GuardedOption? ReadGuardedOption(
            Expr expression,
            IReadOnlyDictionary<(string Name, int Arity), int> entries
        )
        {
            if (expression is not Expr.Case
                {
                    Value: Expr.Call
                    {
                        Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Literal { Value: Atom first }, Expr.Literal { Value: Atom second }
                    ]
                    } call,
                    Clauses: [
                    {
                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable value] }],
                        Guard: { } guard, Body: var success
                    },
                    { Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Guard: null, Body: var fallback }
                    ]
                } || !DistinctBindings(value))
            {
                return null;
            }

            long? lower = null;
            if (guard is Expr.Binary
                {
                    Operator: ErlangOperators.AndAlso, Left: var type, Right: Expr.Binary
                    {
                        Operator: ErlangOperators.Greater, Left: Expr.Variable tested, Right: Expr.Literal { Value: Integer minimum }
                    }
                } && tested.Name == value.Name && minimum.Value >= int.MinValue && minimum.Value < int.MaxValue)
            {
                guard = type;
                lower = (long)minimum.Value + 1;
            }
            if (guard is not Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Arguments: [Expr.Variable argument]
                } predicate || argument.Name != value.Name
                || predicate.Module is null && entries.ContainsKey((predicate.Function, 1)))
            {
                return null;
            }

            byte test = predicate.Function switch
            {
                AutoImportNames.IsAtom => IsAtom,
                AutoImportNames.IsList => IsList,
                AutoImportNames.IsInteger => IsInteger,
                _ => 0
            };

            return test == 0 || lower.HasValue && test != IsInteger
                ? null
                : new(
                    call,
                    first,
                    second,
                    tag,
                    value,
                    test,
                    lower,
                    success,
                    fallback
                );
        }

        private bool TryEmitGuardedOption(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Patterns: [], Body: var body }]
                || ReadGuardedOption(body, entries) is not { Success: Expr.Variable returned } option
                || returned.Name != option.Value.Name || !AttributeLiteral.TryNormalizeConstant(option.Fallback, out Term fallback))
            {
                return false;
            }

            int fail = Label();
            owner.EmitEmptyCallFrame(0);
            EmitOptionRead(
                option,
                fail,
                XRegisterTag,
                location
            );
            owner.EmitFrameReturn();
            Mark(fail);
            owner.EmitConstant(fallback, 0);
            owner.EmitFrameReturn();

            return true;
        }

        private bool TryEmitOptionList(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match { Pattern: Pattern.Variable options, Value: var first },
                    var second
                ]
                    }
                }] || !DistinctBindings(options)
                || ReadGuardedOption(first, entries) is not { Test: IsAtom, Lower: null } left
                || ReadGuardedOption(second, entries) is not { Test: IsInteger, Lower: null } right
                || options.Name == right.Value.Name
                || !AttributeLiteral.TryNormalizeConstant(left.Fallback, out Term empty) || empty is not Nil
                || left.Success is not Expr.Sequence
                {
                    Items: [
                    Expr.Match
                    {
                        Pattern: Pattern.Variable initialized,
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } init
                    },
                    Expr.List
                    {
                        Tail: null, Items: [
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom leftTag }, Expr.Variable leftValue] },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom initTag }, Expr.Variable initValue] }
                    ]
                    }
                ]
                } || !DistinctBindings(left.Value, initialized)
                || leftValue.Name != left.Value.Name || initValue.Name != initialized.Name
                || !entries.TryGetValue((init.Function, 0), out int target)
                || right.Success is not Expr.List
                {
                    Items: [Expr.Tuple { Items: [Expr.Literal { Value: Atom rightTag }, Expr.Variable rightValue] }],
                    Tail: Expr.Variable tail
                } || rightValue.Name != right.Value.Name || tail.Name != options.Name
                || right.Fallback is not Expr.Variable returned || returned.Name != options.Name)
            {
                return false;
            }

            int noLeft = Label();
            int join = Label();
            int noRight = Label();
            owner.EmitEmptyCallFrame(1);
            EmitOptionRead(
                left,
                noLeft,
                YRegisterTag,
                location
            );
            var previous = CallLocation(left.Call, location);
            owner.EmitCallLine(init, previous);
            owner.EmitFieldCall(0, target);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 10);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(initTag.Name)), (XRegisterTag, 0));
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(leftTag.Name)), (YRegisterTag, 0));
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            JumpTo(join);
            Mark(noLeft);
            owner.EmitConstant(Nil.Value, 0, YRegisterTag);
            Mark(join);
            EmitOptionRead(
                right,
                noRight,
                XRegisterTag,
                CallLocation(init, previous)
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(rightTag.Name)), (XRegisterTag, 0));
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            Mark(noRight);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitFrameReturn(1);

            return true;
        }

        private void EmitOptionRead(
            GuardedOption option,
            int fail,
            int resultTag,
            BeamSourceLocation previous
        )
        {
            owner.EmitConstant(option.Second, 1);
            owner.EmitConstant(option.First, 0);
            owner.EmitCallLine(option.Call, previous);
            owner.EmitStaticRemoteCall(option.Call);
            owner.EmitCatchTagTest(0, option.Tag.Name, fail);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(resultTag, 0);
            owner.Op(option.Test, LabelTag, fail);
            owner.Operand(resultTag, 0);
            if (option.Lower is long lower)
            {
                owner.Op(IsGreaterOrEqual, LabelTag, fail);
                owner.EmitTypedRegister(0, IntegerTypeBits, registerTag: resultTag);
                owner.Operand(BeamWire.IntegerTag, lower);
            }
        }
    }
}

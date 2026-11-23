using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedConsAccumulator(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [var empty, var cons] || function.Arity != 2
                || empty is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items.Count: 0, Tail: null }, Pattern.Variable result],
                    Body: Expr.Variable returned
                }
                || cons is not
                {
                    Guard: null,
                    Patterns: [
                        Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                        Pattern.Variable accumulator
                    ],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [
                            Expr.Variable first, Expr.Variable second,
                            Expr.List { Items.Count: 0, Tail: null }, Expr.Variable last
                        ]
                    } tail
                }
                || returned.Name != result.Name || result.Name == VariableScopeNames.Wildcard
                || !DistinctBindings(head, rest, accumulator)
                || first.Name != head.Name || second.Name != rest.Name || last.Name != accumulator.Name
                || !entries.TryGetValue((tail.Function, 4), out int target)
                || !owner.HasListEntryFactWithCallResults((function.Name, function.Arity), 0))
            {
                return false;
            }

            int exhausted = Label();
            owner.EmitNonemptyList(0, exhausted);
            owner.EmitListParts(
                0,
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                3
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.EmitConstant(Nil.Value, 2);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 4);
            owner.Operand(LabelTag, target);
            Mark(exhausted);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitSharedConsTupleEquality(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != 4 || function.Clauses is not [var empty, var compare, var equal, var keep]
                || empty is not
                {
                    Guard: null,
                    Patterns: [
                        Pattern.Variable savedHead, Pattern.List { Items.Count: 0, Tail: null },
                        Pattern.Variable reversed, Pattern.Variable result
                    ],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [
                            Expr.Call
                        {
                            Module: ListModuleNames.Module, Function: ListModuleNames.Reverse,
                            DynamicRemote: false, Arguments: [Expr.Variable reverseInput]
                        } reverse,
                            Expr.List { Items: [Expr.Variable outgoingHead], Tail: Expr.Variable outgoingTail }
                        ]
                    } finish
                }
                || !DistinctBindings(savedHead, reversed, result)
                || reverseInput.Name != reversed.Name || outgoingHead.Name != savedHead.Name || outgoingTail.Name != result.Name
                || !entries.TryGetValue((finish.Function, 2), out int continuation)
                || !entries.TryGetValue((function.Name, function.Arity), out int entry)
                || ReadConsTupleEqualityTail(compare, function.Name, preserves: false) is not { } checkedFields
                || ReadConsTupleEqualityTail(equal, function.Name, preserves: false) is not { } exact
                || ReadConsTupleEqualityTail(keep, function.Name, preserves: true) is not { } retained
                || checkedFields.Entry.Name == checkedFields.Head.Name || exact.Entry.Name != exact.Head.Name
                || retained.Entry.Name == retained.Head.Name || equal.Guard is not null || keep.Guard is not null
                || compare.Guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } guard
                || !ReadsTupleEquality(
                    guard.Left,
                    checkedFields.Entry,
                    checkedFields.Head,
                    element: false
                )
                || !ReadsTupleEquality(
                    guard.Right,
                    checkedFields.Entry,
                    checkedFields.Head,
                    element: true
                )
                || !owner.HasListEntryFactWithCallResults((function.Name, function.Arity), 1))
            {
                return false;
            }

            const int previous = 0;
            const int list = 1;
            const int reverseAccumulator = 2;
            const int head = 4;
            const int leftPart = 5;
            const int rightPart = 6;
            int fullValue = Label();
            int recurse = Label();
            int preserve = Label();
            int exhausted = Label();

            owner.EmitNonemptyList(list, exhausted);
            owner.EmitListParts(
                list,
                XRegisterTag,
                head,
                XRegisterTag,
                list
            );
            Size(previous, leftPart);
            Size(head, rightPart);
            owner.Op(ExactEqualTest, LabelTag, fullValue);
            owner.EmitTypedRegister(
                leftPart,
                IntegerTypeBits,
                lower: 0,
                upper: MaxTupleArity
            );
            owner.EmitTypedRegister(
                rightPart,
                IntegerTypeBits,
                lower: 0,
                upper: MaxTupleArity
            );
            Field(previous, leftPart);
            Field(head, rightPart);
            owner.Op(ExactNotEqualTest, LabelTag, recurse);
            owner.Operand(XRegisterTag, leftPart);
            owner.Operand(XRegisterTag, rightPart);

            Mark(fullValue);
            owner.Op(ExactEqualTest, LabelTag, preserve);
            owner.Operand(XRegisterTag, head);
            owner.Operand(XRegisterTag, previous);
            Mark(recurse);
            Repeat();

            Mark(preserve);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, head + 1);
            owner.Op(PutList, XRegisterTag, head);
            owner.Operand(XRegisterTag, reverseAccumulator);
            owner.Operand(XRegisterTag, reverseAccumulator);
            Repeat();

            Mark(exhausted);
            EmitFrameAllocation(function.Arity, 2);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                previous,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                reverseAccumulator,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(reverse, location);
            owner.EmitStaticRemoteCall(reverse);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, continuation);
            owner.Operand(BeamWire.UnsignedTag, 2);

            return true;

            void Size(int input, int output)
            {
                owner.Op(BifOne, LabelTag, fullValue);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.TupleSize, 1));
                owner.Operand(XRegisterTag, input);
                owner.Operand(XRegisterTag, output);
            }

            void Field(int input, int output)
            {
                owner.Op(BifTwo, LabelTag, fullValue);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
                owner.Operand(BeamWire.IntegerTag, 1);
                owner.EmitTypedRegister(input, TupleTypeBits);
                owner.Operand(XRegisterTag, output);
            }

            void Repeat()
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 4);
                owner.Operand(LabelTag, entry);
            }
        }

        private static (Pattern.Variable Entry, Pattern.Variable Head)? ReadConsTupleEqualityTail(
            Clause clause,
            string self,
            bool preserves
        )
        {
            if (clause.Patterns is not
                [
                    Pattern.Variable entry, Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                    Pattern.Variable reverse, Pattern.Variable result
                ]
                || clause.Body is not Expr.Call
                {
                    Module: null, DynamicRemote: false,
                    Arguments: [Expr.Variable left, Expr.Variable next, var consumed, Expr.Variable right]
                } recurse
                || recurse.Function != self || left.Name != entry.Name || next.Name != rest.Name || right.Name != result.Name
                || !DistinctBindings(
                    entry,
                    rest,
                    reverse,
                    result
                )
                || head.Name != entry.Name && !DistinctBindings(
                    entry,
                    head,
                    rest,
                    reverse,
                    result
                ))
            {
                return null;
            }

            bool carries = preserves
                ? consumed is Expr.List { Items: [Expr.Variable current], Tail: Expr.Variable old }
                    && current.Name == head.Name && old.Name == reverse.Name
                : consumed is Expr.Variable same && same.Name == reverse.Name;

            return carries ? (entry, head) : null;
        }

        private bool ReadsTupleEquality(
            Expr expression,
            Pattern.Variable first,
            Pattern.Variable second,
            bool element
        )
        {
            return expression is Expr.Binary
            { Operator: ErlangOperators.ExactEqual, Left: Expr.Call left, Right: Expr.Call right }
                && Reads(left, first.Name) && Reads(right, second.Name);

            bool Reads(Expr.Call call, string name)
            {
                if (call.DynamicRemote || call.Module is not (null or ErlangModuleNames.Module) || !owner.ResolvesErlangBif(call))
                {
                    return false;
                }

                return element
                    ? call.Function == AutoImportNames.Element && call.Arguments is
                        [Expr.Literal { Value: Integer { Value.IsOne: true } }, Expr.Variable arg] && arg.Name == name
                    : call.Function == AutoImportNames.TupleSize && call.Arguments is [Expr.Variable tuple] && tuple.Name == name;
            }
        }
    }
}

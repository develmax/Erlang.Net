using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record DelimiterClause(
            BitPattern Input,
            Pattern.Variable Current,
            Pattern.Variable Accumulator,
            Expr Body
        );
        private sealed record DelimiterContinuation(Atom Module, Atom Function, BitString Prefix);
        private sealed record DelimiterPlan(
            byte[] Prefix,
            DelimiterContinuation Continuation,
            Atom ResultTag,
            Expr.Call Accepted,
            Expr.Call Continued
        );

        private bool TryEmitPositionedDelimiters(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Arity != 3 || function.Clauses.Count != 8 || owner.OnLoadFunction is not null
                || owner.IsDeclaredNative((function.Name, function.Arity))
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            var clauses = new List<DelimiterClause>();
            foreach (Clause clause in function.Clauses)
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [BitPattern bits, Pattern.Variable current, Pattern.Variable accumulator]
                    } || !DistinctBindings(current, accumulator))
                {
                    return false;
                }

                clauses.Add(new(
                    bits,
                    current,
                    accumulator,
                    clause.Body
                ));
            }

            if (clauses[0].Input.Segments.Count != 0
                || ReadDelimiterContinuation(clauses[0], []) is not { } empty
                || ReadBinaryLiteral(clauses[1].Input) is not [var first, var second]
                || ReadBinaryLiteral(clauses[2].Input) is not [var shortByte]
                || second != shortByte || first <= second
                || ReadDelimiterPlan(
                    function,
                    [first, second],
                    clauses[1],
                    clauses[3],
                    clauses[5]
                ) is not { } longer
                || ReadDelimiterPlan(
                    function,
                    [second],
                    clauses[2],
                    clauses[4],
                    clauses[6]
                ) is not { } shorter
                || !IsDelimiterFallback(function, clauses[7])
                || !HasBinaryLocalArgument(function, binaryRecursion: null))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int select = Label();
            int longPrefix = Label();
            int longRepeat = Label();
            int longContinue = Label();
            int shortPrefix = Label();
            int shortRepeat = Label();
            int shortContinue = Label();
            int ordinary = Label();
            int invalid = Label();

            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern(BeamBinaryNames.NoFail));
            Unsigned(3);
            X(0);
            X(0);
            SavePosition(3, 3);
            ExactEnd(select, empty);

            Mark(select);
            ReadByte(3, 3, invalid);
            SavePosition(4, 4);
            owner.EmitMembershipSelect(
                3,
                ordinary,
                [((Term)Term.I(second), shortPrefix), (Term.I(first), longPrefix)],
                IntegerTypeBits,
                (byte.MinValue, byte.MaxValue)
            );

            Mark(longPrefix);
            MatchByte(second, ordinary);
            SavePosition(3, 3);
            ExactEnd(longRepeat, longer.Continuation);
            Mark(longRepeat);
            ReadByte(4, 4, longContinue);
            CompareByte(4, first, longContinue);
            MatchByte(second, longContinue);
            EmitAccepted(longer);
            Mark(longContinue);
            EmitContinued(longer, position: 3);

            Mark(shortPrefix);
            ExactEnd(shortRepeat, shorter.Continuation);
            Mark(shortRepeat);
            ReadByte(3, 5, shortContinue);
            CompareByte(3, second, shortContinue);
            EmitAccepted(shorter);
            Mark(shortContinue);
            EmitContinued(shorter, position: 4);

            Mark(ordinary);
            Heap(ConsHeapWords, 5);
            owner.Op(PutList, XRegisterTag, 3);
            X(1);
            X(1);
            RestorePosition(XRegisterTag, 0, 4);
            EmitTailLocal(entry, arity: 3, frame: null);

            Mark(invalid);
            RestorePosition(XRegisterTag, 0, 3);
            GetTail(XRegisterTag, 0, 3);
            owner.Op(Jump, LabelTag, functionInfo);

            return true;

            void ExactEnd(int failure, DelimiterContinuation continuation)
            {
                EmitBitMatchHeader(
                    0,
                    failure,
                    2,
                    0,
                    tailUnit: null
                );
                Heap(3 * ConsHeapWords + 4, 3);
                owner.Op(PutList, XRegisterTag, 2);
                owner.LiteralOperand(Nil.Value);
                X(0);
                owner.Op(PutList, XRegisterTag, 1);
                X(0);
                X(0);
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(continuation.Prefix);
                X(0);
                X(0);
                owner.EmitTupleHeader(XRegisterTag, 3);
                owner.LiteralOperand(continuation.Module);
                owner.LiteralOperand(continuation.Function);
                X(0);
                owner.instructions.WriteByte(Return);
            }

            void ReadByte(int destination, int live, int failure)
            {
                EmitBitMatchHeader(
                    0,
                    failure,
                    9,
                    BitSyntaxDefaults.BinaryUnit,
                    BitSyntaxDefaults.BitStringUnit
                );
                owner.Operand(AtomTag, owner.Intern(BitSegmentTypes.Integer));
                Unsigned(live);
                owner.LiteralOperand(Nil.Value);
                Unsigned(BitSyntaxDefaults.BinaryUnit);
                Unsigned(BitSyntaxDefaults.NumericUnit);
                X(destination);
            }

            void MatchByte(byte value, int failure)
            {
                const int ensureAtLeastOperands = 3;
                const int fixedEqualityOperands = 4;
                EmitBitMatchHeader(
                    0,
                    failure,
                    ensureAtLeastOperands + fixedEqualityOperands,
                    BitSyntaxDefaults.BinaryUnit,
                    BitSyntaxDefaults.BitStringUnit
                );
                EmitFixedBitEquality(BitSyntaxDefaults.BinaryUnit, value);
            }

            void CompareByte(int register, byte value, int failure)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.EmitTypedRegister(
                    register,
                    IntegerTypeBits,
                    byte.MinValue,
                    byte.MaxValue
                );
                owner.LiteralOperand(Term.I(value));
            }

            void EmitAccepted(DelimiterPlan plan)
            {
                EmitFrameAllocation(arity: 3, size: 2);
                EmitFrameInitialization(0, 1);
                Save(2, 1);
                GetTail(YRegisterTag, 0, 2);
                Heap(plan.Prefix.Length * ConsHeapWords, 2);
                EmitReverse(plan.Prefix, plan.Accepted);
                Heap(ConsHeapWords + 6, 1);
                PrependResult(1, 0);
                owner.EmitTupleHeader(XRegisterTag, 2);
                X(0);
                owner.Operand(YRegisterTag, 0);
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(plan.ResultTag);
                X(0);
                owner.EmitFrameReturn(2);
            }

            void EmitContinued(DelimiterPlan plan, int position)
            {
                owner.Op(AllocateHeap, BeamWire.UnsignedTag, 3);
                Unsigned(plan.Prefix.Length * ConsHeapWords);
                Unsigned(position + 1);
                Save(position, 0);
                Save(0, 1);
                Save(2, 2);
                EmitReverse(plan.Prefix, plan.Continued);
                Heap(ConsHeapWords, 1);
                PrependResult(2, 2);
                RestorePosition(YRegisterTag, 1, 0);
                owner.EmitConstant(Nil.Value, 1);
                owner.MoveToX0(YRegisterTag, 1);
                EmitTailLocal(entry, arity: 3, frame: 3);
            }

            void EmitReverse(byte[] bytes, Expr.Call reverse)
            {
                int tail = 1;
                foreach (byte value in bytes)
                {
                    owner.instructions.WriteByte(PutList);
                    owner.LiteralOperand(Term.I(value));
                    X(tail);
                    X(0);
                    tail = 0;
                }

                owner.EmitCallLine(reverse, location);
                owner.Op(ExternalCall, BeamWire.UnsignedTag, 1);
                Unsigned(owner.Import(ListModuleNames.Module, ListModuleNames.Reverse, 1));
            }

            void PrependResult(int tail, int destination)
            {
                owner.Op(PutList, XRegisterTag, 0);
                owner.Operand(YRegisterTag, tail);
                X(destination);
            }

            void SavePosition(int destination, int live)
            {
                owner.Op(GetBinaryPosition, XRegisterTag, 0);
                X(destination);
                Unsigned(live);
            }

            void RestorePosition(int tag, int context, int position)
            {
                owner.Op(SetBinaryPosition, tag, context);
                owner.Operand(tag, position);
            }

            void GetTail(int tag, int destination, int live)
            {
                owner.Op(GetBinaryTail, XRegisterTag, 0);
                owner.Operand(tag, destination);
                Unsigned(live);
            }

            void Save(int from, int to) => owner.MoveRegister(
                XRegisterTag,
                from,
                YRegisterTag,
                to
            );

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                Unsigned(live);
            }

            void X(int register) => owner.Operand(XRegisterTag, register);

            void Unsigned(int value) => owner.Operand(BeamWire.UnsignedTag, value);
        }

        private static DelimiterContinuation? ReadDelimiterContinuation(DelimiterClause clause, byte[] expected)
        {
            if (clause.Body is not Expr.Tuple
                {
                    Items:
                    [
                        Expr.Literal { Value: Atom owner }, Expr.Literal { Value: Atom target },
                        Expr.List { Items: [var literal, Expr.Variable current, Expr.Variable accumulator], Tail: null }
                    ]
                } || current.Name != clause.Current.Name || accumulator.Name != clause.Accumulator.Name
                || !AttributeLiteral.TryNormalizeConstant(literal, out var value) || value is not BitString prefix
                || !prefix.Equals(new BitString(expected)))
            {
                return null;
            }

            return new(owner, target, prefix);
        }

        private static DelimiterPlan? ReadDelimiterPlan(
            FunctionDefinition function,
            byte[] prefix,
            DelimiterClause exact,
            DelimiterClause accepted,
            DelimiterClause continued
        )
        {
            byte[] repeated = [.. prefix, .. prefix];
            if (ReadDelimiterContinuation(exact, prefix) is not { } continuation
                || ReadDelimiterTail(accepted, repeated) is not { } acceptedTail
                || ReadDelimiterTail(continued, prefix) is not { } continuedTail
                || accepted.Body is not Expr.Tuple
                {
                    Items:
                    [
                        Expr.Literal { Value: Atom tag },
                        Expr.Tuple
                    {
                        Items: [Expr.List { Items: [Expr.Call acceptedReverse], Tail: Expr.Variable remaining }, Expr.Variable result]
                    }
                    ]
                } || remaining.Name != accepted.Accumulator.Name || result.Name != acceptedTail.Name
                || continued.Body is not Expr.Call
                {
                    Module: null, DynamicRemote: false,
                    Arguments: [Expr.Variable next, var empty, Expr.List { Items: [Expr.Call continuedReverse], Tail: Expr.Variable previous }]
                } recur || recur.Function != function.Name || next.Name != continuedTail.Name
                || previous.Name != continued.Accumulator.Name || !AttributeLiteral.TryNormalizeConstant(empty, out var nil) || nil is not Nil
                || !IsDelimiterReverse(acceptedReverse, accepted.Current.Name, prefix)
                || !IsDelimiterReverse(continuedReverse, continued.Current.Name, prefix))
            {
                return null;
            }

            return new(
                prefix,
                continuation,
                tag,
                acceptedReverse,
                continuedReverse
            );
        }

        private static Pattern.Variable? ReadDelimiterTail(DelimiterClause clause, byte[] prefix)
        {
            if (clause.Input.Segments.Count != prefix.Length + 1
                || ReadBinaryTailBinding(clause.Input.Segments[^1]) is not { } tail
                || !DistinctBindings(clause.Current, clause.Accumulator, tail))
            {
                return null;
            }

            for (int index = 0; index < prefix.Length; index++)
            {
                BitPatternSegment segment = clause.Input.Segments[index];
                if (!IsUnsignedByte(segment.Specification) || segment.Value is not Pattern.Literal { Value: Integer value }
                    || value.Value != prefix[index])
                {
                    return null;
                }
            }

            return tail;
        }

        private static bool IsDelimiterReverse(Expr.Call call, string current, byte[] prefix) =>
            call is
            {
                Module: ListModuleNames.Module, Function: ListModuleNames.Reverse, DynamicRemote: false,
                Arguments: [Expr.List { Items: var bytes, Tail: Expr.Variable tail }]
            } && tail.Name == current && bytes.Count == prefix.Length
            && bytes.Zip(prefix.Reverse(), (value, expected) => value is Expr.Literal { Value: Integer known }
                && known.Value == expected).All(equal => equal);

        private static bool IsDelimiterFallback(FunctionDefinition function, DelimiterClause clause) =>
            clause.Input.Segments is [var head, var suffix]
            && head.Value is Pattern.Variable octet && IsUnsignedByte(head.Specification)
            && ReadBinaryTailBinding(suffix) is { } tail && DistinctBindings(
                clause.Current,
                clause.Accumulator,
                octet,
                tail
            )
            && clause.Body is Expr.Call
            {
                Module: null, DynamicRemote: false,
                Arguments:
                [
                    Expr.Variable rest, Expr.List { Items: [Expr.Variable value], Tail: Expr.Variable current }, Expr.Variable accumulator
                ]
            } recur && recur.Function == function.Name && value.Name == octet.Name && rest.Name == tail.Name
            && current.Name == clause.Current.Name && accumulator.Name == clause.Accumulator.Name;
    }
}

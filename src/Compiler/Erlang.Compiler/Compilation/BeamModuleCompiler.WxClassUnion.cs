using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWxClassUnionCommand(
            FunctionDefinition function,
            bool validateOnly = false,
            int? functionFailure = null,
            bool unaryFallback = false,
            (int Receiver, int Value)? preparedClasses = null
        )
        {
            int arity = function.Arity;
            bool hasReceiver = arity == 2;
            if (arity is not (1 or 2) || unaryFallback && hasReceiver || preparedClasses is not null && !hasReceiver
                || function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
                || sequence.Items.Count < 4)
            {
                return false;
            }

            var receive = sequence.Items[^1] as Expr.Call;
            bool readsResult = receive is { Module: "wxe_util", Function: "rec", DynamicRemote: false,
                Arguments: [Expr.Literal { Value: Integer }] };
            int queueIndex = sequence.Items.Count - (readsResult ? 2 : 1);
            if (sequence.Items[queueIndex] is not Expr.Call { Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                        Arguments: [.., Expr.Call { Module: "wx", Function: "typeCast", DynamicRemote: false,
                            Arguments: [Expr.Variable input, Expr.Variable castType] } cast,
                            Expr.Case environment, Expr.Literal { Value: Integer command }] } queue
                || readsResult && receive!.Arguments[0] is Expr.Literal { Value: Integer received } && !command.Equals(received)
                || queue.Arguments.Count != arity + 2
                || ReadWxClassUnionObject(clause.Patterns[^1]) is not { } valueObject
                || input.Name != valueObject.Object
                || !IsWxEnvironment(environment)
                || sequence.Items[queueIndex - 1] is not Expr.Match { Pattern: Pattern.Variable selected, Value: Expr.If
                    { Clauses: [..,
                        { Patterns: [], Guard: Expr.Literal { Value: Atom { Name: "true" } },
                            Body: Expr.Call { Function: "error", DynamicRemote: false, Arguments:
                                [Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "badarg" } }, Expr.Variable badType] }] } error }] } decision }
                || castType.Name != selected.Name || badType.Name != valueObject.Type
                || !owner.ResolvesErlangBif(error))
            {
                return false;
            }

            int prefix = hasReceiver ? 1 : 0;
            int classCount = decision.Clauses.Count - 1;
            if (classCount < 2 || queueIndex != prefix + classCount + 1)
            {
                return false;
            }

            var bindings = new List<string> { valueObject.Object, valueObject.Type, selected.Name };
            (Atom Class, Expr.Call Call)? receiverCheck = null;
            if (hasReceiver)
            {
                if (ReadWxClassUnionObject(clause.Patterns[0]) is not { } receiverObject
                    || queue.Arguments[0] is not Expr.Variable target || target.Name != receiverObject.Object
                    || ReadWxClassTest(sequence.Items[0], receiverObject.Type) is not { } check)
                {
                    return false;
                }

                bindings.Add(receiverObject.Object);
                bindings.Add(receiverObject.Type);
                receiverCheck = check;
            }

            var classes = new (Atom Class, Expr.Call Call)[classCount];
            for (int index = 0; index < classCount; index++)
            {
                if (ReadProtectedWxClassTest(sequence.Items[prefix + index], valueObject.Type) is not { } test
                    || decision.Clauses[index] is not { Patterns: [], Guard: Expr.Variable condition,
                        Body: Expr.Literal { Value: Atom selectedClass } }
                    || condition.Name != test.Result || !selectedClass.Equals(test.Class))
                {
                    return false;
                }

                bindings.Add(test.Result);
                classes[index] = (test.Class, test.Call);
            }

            if (bindings.Contains(VariableScopeNames.Wildcard) || bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Count)
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            // Each class check uses four labels. Selection uses one per class plus two error labels.
            int failure = unaryFallback ? owner.nextLabel + 5 * classCount + 2 : functionFailure ?? owner.nextLabel - 2;
            int typeSlot = classCount - 1;
            int valueSlot = classCount;
            int receiverSlot = classCount + 1;
            int token = classCount + arity;
            int frame = token + 1;
            for (int register = 0; preparedClasses is null && register < arity; register++)
            {
                EmitDetailRecordTest(register, 4, Term.A("wx_ref"), failure);
            }

            EmitFrameAllocation(preparedClasses is { } prepared ? Math.Max(prepared.Receiver, prepared.Value) + 1 : arity, frame);
            BeamSourceLocation previous = location;
            if (receiverCheck is { } receiverClass)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                int init = preparedClasses is null ? classCount : classCount - 1;
                owner.Operand(BeamWire.UnsignedTag, init + 1);
                for (int slot = 0; slot < init; slot++)
                {
                    owner.Operand(YRegisterTag, slot);
                }

                owner.Operand(YRegisterTag, token);
                if (preparedClasses is { } classesInRegisters)
                {
                    owner.MoveRegister(XRegisterTag, classesInRegisters.Value, YRegisterTag, typeSlot);
                }

                owner.MoveRegister(XRegisterTag, 1, YRegisterTag, valueSlot);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, receiverSlot);
                if (preparedClasses is null)
                {
                    owner.EmitTuplePart(0, 2, 0);
                }

                int receiverType = preparedClasses?.Receiver ?? 0;
                int checkedReceiver = Label();
                owner.Op(ExactNotEqualTest, LabelTag, checkedReceiver);
                owner.Operand(XRegisterTag, receiverType);
                owner.LiteralOperand(receiverClass.Class);
                ApplyClass(XRegisterTag, receiverType, receiverClass.Class, receiverClass.Call);
                Mark(checkedReceiver);
            }
            else
            {
                EmitFrameInitialization(0, typeSlot);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, valueSlot);
            }

            if (preparedClasses is null)
            {
                owner.Op(GetTupleElement, hasReceiver ? YRegisterTag : XRegisterTag, hasReceiver ? valueSlot : 0);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(YRegisterTag, typeSlot);
            }
            for (int index = 0; index < classCount; index++)
            {
                var (expected, call) = classes[index];
                EmitProtectedClass(expected, call, typeSlot - index - 1);
            }

            int[] nextClass = classes.SkipLast(1).Select(_ => Label()).ToArray();
            int chosen = Label();
            int missingEnvironment = Label();
            int badArgument = Label();
            for (int index = 0; index < classCount; index++)
            {
                int slot = typeSlot - index - 1;
                bool last = slot < 0;
                owner.Op(ExactEqualTest, LabelTag, last ? badArgument : nextClass[index]);
                owner.Operand(last ? XRegisterTag : YRegisterTag, last ? 0 : slot);
                owner.LiteralOperand(Term.A("true"));
                owner.EmitConstant(classes[index].Class, 1);
                if (!last)
                {
                    JumpTo(chosen);
                    Mark(nextClass[index]);
                }
            }

            Mark(chosen);
            owner.MoveRegister(YRegisterTag, valueSlot, XRegisterTag, 0);
            if (hasReceiver)
            {
                owner.MoveRegister(YRegisterTag, receiverSlot, YRegisterTag, token);
            }

            Trim(frame - prefix, prefix);
            owner.EmitCallLine(cast, previous);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wx", "typeCast", 2));
            EmitWxEnvironmentCheck(1, missingEnvironment);
            owner.EmitTuplePart(1, 1, arity);
            if (hasReceiver)
            {
                owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            }

            owner.EmitConstant(command, arity + 1);
            if (hasReceiver)
            {
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
                if (readsResult)
                {
                    Trim(1, 0);
                }
            }

            owner.EmitCallLine(queue, CallLocation(cast, previous));
            owner.Op(readsResult ? ExternalCall : TailExternalCallWithFrame, BeamWire.UnsignedTag, arity + 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", arity + 2));
            if (readsResult)
            {
                owner.EmitConstant(command, 0);
                owner.EmitCallLine(receive!, CallLocation(queue, previous));
                TailCall("wxe_util", "rec", 0);
            }
            else
            {
                owner.Operand(BeamWire.UnsignedTag, prefix);
            }

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            if (readsResult)
            {
                owner.EmitCallLine(queue, CallLocation(receive!, previous));
            }
            TailCall(ErlangModuleNames.Module, "error", frame);
            Mark(badArgument);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(Term.A("badarg"));
            owner.Operand(YRegisterTag, typeSlot);
            owner.EmitCallLine(error, CallLocation(queue, previous));
            TailCall(ErlangModuleNames.Module, "error", frame);
            return true;

            void ApplyClass(int tag, int register, Atom expected, Expr.Call call)
            {
                owner.MoveRegister(tag, register, XRegisterTag, 1);
                owner.EmitConstant(Term.A("parent_class"), 2);
                owner.EmitConstant(expected, 0);
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            }

            void EmitProtectedClass(Atom expected, Expr.Call call, int resultSlot)
            {
                int invoke = Label();
                int success = Label();
                int handler = Label();
                int done = Label();
                owner.Op(TryEnter, YRegisterTag, token);
                owner.Operand(LabelTag, handler);
                owner.Op(ExactEqualTest, LabelTag, invoke);
                owner.Operand(YRegisterTag, typeSlot);
                owner.LiteralOperand(expected);
                owner.EmitConstant(Term.A("true"), 0);
                JumpTo(success);
                Mark(invoke);
                ApplyClass(YRegisterTag, typeSlot, expected, call);
                Mark(success);
                owner.Op(TryEnd, YRegisterTag, token);
                if (resultSlot >= 0)
                {
                    owner.MoveRegister(XRegisterTag, 0, YRegisterTag, resultSlot);
                }
                JumpTo(done);
                Mark(handler);
                owner.Op(TryCase, YRegisterTag, token);
                owner.EmitConstant(Term.A("false"), Math.Max(0, resultSlot), resultSlot < 0 ? XRegisterTag : YRegisterTag);
                Mark(done);
            }

            void Trim(int count, int remaining)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, count);
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }

            void TailCall(string module, string name, int remaining)
            {
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(module, name, 1));
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }
        }

        private static (string Object, string Type)? ReadWxClassUnionObject(Pattern pattern) =>
            pattern is AliasPattern alias
            && (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is { } binding
            && (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is Pattern.Tuple
            { Items: [Pattern.Literal { Value: Atom { Name: "wx_ref" } },
                Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable type,
                Pattern.Variable { Name: VariableScopeNames.Wildcard }] }
                ? (binding.Name, type.Name)
                : null;

        private static (Atom Class, Expr.Call Call)? ReadWxClassTest(Expr expression, string type) =>
            expression is Expr.Binary { Operator: ErlangOperators.OrElse,
                Left: Expr.Binary { Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Variable tested, Right: Expr.Literal { Value: Atom expected } },
                Right: Expr.Call { DynamicRemote: true, Arguments: [Expr.Variable dispatched,
                    Expr.Literal { Value: Atom { Name: "parent_class" } },
                    Expr.List { Items: [Expr.Literal { Value: Atom passed }], Tail: null }] } call }
            && tested.Name == type && dispatched.Name == type && expected.Equals(passed)
                ? (expected, call)
                : null;

        private static (string Result, Atom Class, Expr.Call Call)? ReadProtectedWxClassTest(Expr expression, string type) =>
            expression is Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Try
                { Body: var body, After: null, Clauses: [], Catches: [{ Guard: null, Patterns:
                    [Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                    Body: Expr.Literal { Value: Atom { Name: "false" } } }] } }
            && ReadWxClassTest(body, type) is { } check ? (result.Name, check.Class, check.Call) : null;
    }
}

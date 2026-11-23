using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TaggedRecordAction(
            Atom Tag,
            Atom Reply,
            int Arity,
            int RecordArity,
            string RecordTag,
            int KeyField,
            int FirstField,
            int SecondField,
            Expr.Call? First,
            Expr.Call? Second,
            Term? Option,
            Expr.RecordUpdate? Update,
            (int Field, int Register)[] ProjectedUpdates,
            Term? ConstantUpdate
        );

        private bool TryEmitOwnedTaggedRecordActions(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 2
                || function.Clauses.Count != 3)
            {
                return false;
            }

            var actions = new List<TaggedRecordAction>();
            foreach (Clause clause in function.Clauses)
            {
                if (ReadAction(clause) is not { } action)
                {
                    return false;
                }

                actions.Add(action);
            }

            if (actions.Where(action => action.First is null).ToArray() is not [var bind]
                || actions.Where(action => action.Second is not null).ToArray() is not [var relay]
                || actions.Where(action => action.First is not null && action.Second is null).ToArray() is not [var close]
                || bind.Arity != relay.Arity || close.Arity >= relay.Arity || bind.Tag.Equals(relay.Tag)
                || bind.RecordArity != close.RecordArity || bind.RecordArity != relay.RecordArity
                || bind.RecordTag != close.RecordTag || bind.RecordTag != relay.RecordTag
                || close.KeyField != relay.KeyField)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int common = Label();
            int relayEntry = Label();
            int updateEntry = Label();
            int closeEntry = Label();
            int badRecord = Label();
            owner.EmitTupleArityDispatch(failure, [(close.Arity, closeEntry), (relay.Arity, common)]);
            Mark(common);
            for (int index = 0; index < relay.Arity; index++)
            {
                owner.EmitTuplePart(0, index, function.Arity + index);
            }

            var tags = new (Term Key, int Label)[] { (bind.Tag, updateEntry), (relay.Tag, relayEntry) };
            owner.EmitMembershipSelect(
                function.Arity,
                failure,
                tags.OrderBy(entry => entry.Key, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true)))
            );
            Mark(relayEntry);
            EmitRecordTest(relay, failure);
            CheckKey(relay, projected: true);
            EmitFrameAllocation(5, 2);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.EmitTuplePart(1, relay.SecondField, 1);
            GetTupleField(
                1,
                relay.FirstField,
                0,
                fromStack: true
            );
            owner.MoveRegister(
                XRegisterTag,
                4,
                XRegisterTag,
                2
            );
            EmitRemote(relay.First!, location);
            owner.EmitConstant(relay.Option!, 1);
            Load(0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitRemote(relay.Second!, CallLocation(relay.First!, location));
            EmitRetainedRecordReply([relay.Reply]);

            Mark(updateEntry);
            EmitRecordTest(bind, badRecord);
            owner.Op(TestHeap, BeamWire.UnsignedTag, bind.RecordArity + 4);
            owner.Operand(BeamWire.UnsignedTag, 5);
            EmitUpdate(bind, fromStack: false);
            EmitRecordUpdateWrapper([bind.Reply]);
            owner.instructions.WriteByte(Return);

            Mark(closeEntry);
            owner.EmitTuplePart(0, 0, function.Arity);
            owner.EmitAtomTest(function.Arity, close.Tag.Name, failure);
            EmitRecordTest(close, failure);
            CheckKey(close, projected: false);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            GetTupleField(
                1,
                close.SecondField,
                0,
                toStack: true
            );
            owner.EmitTuplePart(1, close.FirstField, 0);
            Load(0, 1);
            EmitRemote(close.First!, location);
            owner.Op(TestHeap, BeamWire.UnsignedTag, close.RecordArity + 5);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitUpdate(close, fromStack: true);
            TupleHeader(0, 3);
            owner.LiteralOperand(close.Reply);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);

            Mark(badRecord);
            Expr.RecordUpdate update = bind.Update!;
            if (update.FailureSourceLine > 0)
            {
                owner.EmitLine(new(update.FailureSourceFile, update.FailureSourceLine));
            }

            owner.Op(BadRecord, XRegisterTag, 1);

            return true;

            void EmitRemote(Expr.Call call, BeamSourceLocation previous)
            {
                owner.EmitCallLine(call, previous);
                owner.EmitStaticRemoteCall(call);
            }

            void EmitRecordTest(TaggedRecordAction action, int failed) =>
                EmitDetailRecordTest(
                    1,
                    action.RecordArity,
                    new Atom(action.RecordTag),
                    failed
                );

            void CheckKey(TaggedRecordAction action, bool projected)
            {
                owner.EmitTuplePart(1, action.KeyField, 2);
                if (!projected)
                {
                    owner.EmitTuplePart(0, 1, 3);
                }

                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 2);
                owner.Operand(XRegisterTag, 3);
            }

            void GetTupleField(
                int source,
                int field,
                int destination,
                bool fromStack = false,
                bool toStack = false
            )
            {
                owner.Op(GetTupleElement, fromStack ? YRegisterTag : XRegisterTag, source);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(toStack ? YRegisterTag : XRegisterTag, destination);
            }

            void EmitUpdate(TaggedRecordAction action, bool fromStack)
            {
                owner.Op(UpdateRecord, AtomTag, owner.Intern(fromStack && action.RecordArity < RecordUpdateArityLimit ? "reuse" : "copy"));
                owner.Operand(BeamWire.UnsignedTag, action.RecordArity);
                owner.EmitTypedRegister(1, TupleTypeBits, registerTag: fromStack ? YRegisterTag : XRegisterTag);
                owner.Operand(XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, action.Update!.Fields.Count * 2);
                if (fromStack)
                {
                    owner.Operand(BeamWire.UnsignedTag, action.Update.Fields[0].Index + 1);
                    owner.LiteralOperand(action.ConstantUpdate!);
                }
                else
                {
                    foreach (var (field, register) in action.ProjectedUpdates)
                    {
                        owner.Operand(BeamWire.UnsignedTag, field + 1);
                        owner.Operand(XRegisterTag, register);
                    }
                }
            }
        }

        private static TaggedRecordAction? ReadAction(Clause clause)
        {
            if (clause.Guard is not null || clause.Patterns is not [Pattern.Tuple message, var record]
                || !HasTupleTag(message, out Atom tag)
                || message.Items.Skip(1).Any(item => item is not Erlang.Pattern.Variable))
            {
                return null;
            }

            var messageFields = message.Items.Skip(1).Cast<Pattern.Variable>().ToArray();
            if (!DistinctBindings(messageFields))
            {
                return null;
            }

            if (messageFields.Length == 2 && record is Pattern.Variable original
                && DistinctBindings([original, .. messageFields])
                && clause.Body is Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom reply }, Expr.RecordUpdate { Value: Expr.Variable value, Fields.Count: 2 } update]
                } && value.Name == original.Name
                && update.Fields.All(field => field.Value is Expr.Variable variable
                    && Array.Exists(messageFields, source => source.Name == variable.Name)))
            {
                var fields = update.Fields.OrderBy(field => field.Index)
                    .Select(field => (field.Index, Register: Array.FindIndex(
                        messageFields, variable => variable.Name == ((Expr.Variable)field.Value).Name) + 3)).ToArray();

                return new TaggedRecordAction(
                    tag,
                    reply,
                    message.Items.Count,
                    update.Arity,
                    update.Name,
                    -1,
                    -1,
                    -1,
                    null,
                    null,
                    null,
                    update,
                    fields,
                    null
                );
            }

            if (messageFields.Length is not (1 or 2) || record is not AliasPattern alias
                || !TryReadPatternAlias(alias, out var state, out var shape)
                || shape is not Pattern.Tuple recordTuple || !HasTupleTag(recordTuple, out Atom recordTag)
                || recordTuple.Items.Skip(1).Any(item => item is not Erlang.Pattern.Variable)
                || clause.Body is not Expr.Sequence sequence
                || sequence.Items is not [Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable first, Expr.Variable second, ..]
                } call, ..])
            {
                return null;
            }

            var fieldsByName = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 1; index < recordTuple.Items.Count; index++)
            {
                var recordField = (Pattern.Variable)recordTuple.Items[index];
                if (recordField.Name != VariableScopeNames.Wildcard && !fieldsByName.TryAdd(recordField.Name, index))
                {
                    return null;
                }
            }

            if (fieldsByName.ContainsKey(state.Name) || messageFields.Any(field => field.Name == state.Name)
                || !fieldsByName.TryGetValue(messageFields[0].Name, out int key)
                || !fieldsByName.TryGetValue(first.Name, out int firstField) || firstField == key
                || !fieldsByName.TryGetValue(second.Name, out int secondField) || secondField == key
                || firstField == secondField)
            {
                return null;
            }

            if (messageFields.Length == 2 && !fieldsByName.ContainsKey(messageFields[1].Name)
                && call.Arguments is [_, _, Expr.Variable payload] && payload.Name == messageFields[1].Name
                && sequence.Items is
                [
                    _,
                    Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable keyArgument, var option]
                } next,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom relayed }, Expr.Variable originalState] }
                ] && keyArgument.Name == messageFields[0].Name && originalState.Name == state.Name
                && AttributeLiteral.TryNormalizeConstant(option, out Term constant))
            {
                return new TaggedRecordAction(
                    tag,
                    relayed,
                    message.Items.Count,
                    recordTuple.Items.Count,
                    recordTag.Name,
                    key,
                    firstField,
                    secondField,
                    call,
                    next,
                    constant,
                    null,
                    [],
                    null
                );
            }

            if (messageFields.Length == 1 && call.Arguments.Count == 2 && sequence.Items is
                [
                    _,
                    Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom stopped }, Expr.Variable retained,
                            Expr.RecordUpdate { Value: Expr.Variable updated, Fields: [var field] } terminal]
                }
                ] && retained.Name == second.Name && updated.Name == state.Name
                && terminal.Name == recordTag.Name && terminal.Arity == recordTuple.Items.Count
                && AttributeLiteral.TryNormalizeConstant(field.Value, out Term replacement))
            {
                return new TaggedRecordAction(
                    tag,
                    stopped,
                    message.Items.Count,
                    recordTuple.Items.Count,
                    recordTag.Name,
                    key,
                    firstField,
                    secondField,
                    call,
                    null,
                    null,
                    terminal,
                    [],
                    replacement
                );
            }

            return null;
        }
    }
}

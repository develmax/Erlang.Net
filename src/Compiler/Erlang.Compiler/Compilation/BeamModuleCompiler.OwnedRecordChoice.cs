using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitOwnedRecordChoice(FunctionDefinition function)
        {
            var signature = (function.Name, function.Arity);
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || !entries.TryGetValue(signature, out int entry)
                || function.Clauses is not [var readClause, var constantClause, var callbackClause]
                || ReadOwnedRecordTail(function.Name, readClause) is not { } checkedTail
                || ReadOwnedRecordTail(function.Name, constantClause) is not { } directTail
                || checkedTail.Read is not { } read || directTail.Read is not null
                || checkedTail.Tag.Name == directTail.Tag.Name
                || checkedTail.Update.Name != directTail.Update.Name || checkedTail.Update.Arity != directTail.Update.Arity
                || checkedTail.Update.Fields[0].Index != directTail.Update.Fields[0].Index
                || ReadOwnedRecordCallback(callbackClause, directTail.Update) is not { } callback
                || !ProveOwnedRecordInput(function, checkedTail, directTail))
            {
                return false;
            }

            int directLabel = Label();
            int readLabel = Label();
            int callbackLabel = Label();
            int retainLabel = Label();
            int recordFailure = Label();
            int matchFailure = Label();

            owner.EmitTupleCheck(0, 2, callbackLabel);
            owner.EmitTuplePart(0, 0, 2);
            owner.EmitTuplePart(0, 1, 3);
            bool readFirst = TermOrder.Compare(checkedTail.Tag, directTail.Tag, exact: true) < 0;
            if (readFirst)
            {
                owner.EmitAtomSelection(
                    2,
                    callbackLabel,
                    checkedTail.Tag.Name,
                    readLabel,
                    directTail.Tag.Name,
                    directLabel
                );
            }
            else
            {
                owner.EmitAtomSelection(
                    2,
                    callbackLabel,
                    directTail.Tag.Name,
                    directLabel,
                    checkedTail.Tag.Name,
                    readLabel
                );
            }

            int arity = checkedTail.Update.Arity;
            int field = checkedTail.Update.Fields[0].Index;
            Mark(directLabel);
            EmitOwnedRecordHeap(arity + 1, 4);
            EmitOwnedRecordUpdate(
                arity,
                XRegisterTag,
                1,
                1,
                [(field, XRegisterTag, 3, null)]
            );
            owner.EmitConstant(directTail.Constant!, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);

            Mark(readLabel);
            EmitFrameAllocation(4, 2);
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
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(read.Call, location);
            Call(read.Call.Module, read.Call.Function, 1);
            EmitDetailRecordTest(
                0,
                2,
                read.Tag,
                matchFailure
            );
            owner.EmitTuplePart(0, 1, 0);
            EmitDetailRecordTest(
                0,
                read.Member.Arity,
                Term.A(read.Member.Name),
                recordFailure
            );
            EmitOwnedRecordHeap(arity + 4, 1);
            EmitOwnedRecordUpdate(
                arity,
                YRegisterTag,
                1,
                1,
                [(field, YRegisterTag, 0, null)]
            );
            owner.EmitTuplePart(0, read.Member.Index, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(read.ResultTag.Name)), (XRegisterTag, 0));
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 2);

            Mark(callbackLabel);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.EmitTuplePart(1, field, 1);
            owner.EmitTuplePart(
                0,
                callback.ModuleIndex,
                0,
                YRegisterTag
            );
            owner.EmitTuplePart(
                0,
                callback.FunctionIndex,
                2,
                YRegisterTag
            );
            owner.EmitTuplePart(
                0,
                callback.ValueIndex,
                3,
                YRegisterTag
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                4
            );
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 3);
            owner.EmitCallLine(callback.Call, location);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 3);
            owner.Op(ExactEqualTest, LabelTag, retainLabel);
            owner.Operand(YRegisterTag, 1);
            owner.LiteralOperand(callback.Key);
            EmitOwnedRecordHeap(arity + 1, 0);
            owner.EmitTuplePart(
                0,
                callback.ResetIndex,
                0,
                YRegisterTag
            );
            EmitOwnedRecordUpdate(
                arity,
                YRegisterTag,
                0,
                0,
                [(field, 0, 0, callback.ResetFile), (callback.ValueIndex, XRegisterTag, 0, null)]
            );
            owner.EmitFrameReturn(2);

            Mark(retainLabel);
            EmitOwnedRecordHeap(arity + 1, 1);
            EmitOwnedRecordUpdate(
                arity,
                YRegisterTag,
                0,
                0,
                [(callback.ValueIndex, XRegisterTag, 0, null)]
            );
            owner.EmitFrameReturn(2);

            Mark(recordFailure);
            owner.EmitLine(read.Member.SourceLine == 0 ? location : new(read.Member.PreprocessorFile, read.Member.SourceLine));
            owner.Op(BadRecord, XRegisterTag, 0);
            Mark(matchFailure);
            owner.EmitLine(CallLocation(read.Call, location));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private static OwnedRecordTail? ReadOwnedRecordTail(string function, Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }, Pattern.Variable state]
                }
                || !DistinctBindings(payload, state))
            {
                return null;
            }

            Expr.Call? read = null;
            Pattern.Variable? saved = null;
            Atom? readTag = null;
            Expr body = clause.Body;
            if (body is Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom matchTag }, Pattern.Variable binding] },
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable input] } call
                    },
                        var tail
                    ]
                }
                && input.Name == payload.Name && DistinctBindings(payload, state, binding))
            {
                read = call;
                saved = binding;
                readTag = matchTag;
                body = tail;
            }

            if (body is not Expr.Call
                {
                    Module: null,
                    DynamicRemote: false,
                    Arguments: [var key, Expr.RecordUpdate { Value: Expr.Variable source, Fields: [var field] } update]
                } recur
                || recur.Function != function || source.Name != state.Name
                || field.Value is not Expr.Variable replacement || replacement.Name != payload.Name
                || update.Arity >= RecordUpdateArityLimit)
            {
                return null;
            }

            if (read is null && AttributeLiteral.TryNormalizeConstant(key, out Term literal))
            {
                return new(
                    tag,
                    recur,
                    update,
                    literal,
                    null
                );
            }

            if (read is null || key is not Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom resultTag }, Expr.RecordAccess { Value: Expr.Variable record } member]
                }
                || record.Name != saved!.Name)
            {
                return null;
            }

            return new(
                tag,
                recur,
                update,
                null,
                new(
                    read,
                    readTag!,
                    member,
                    resultTag
                )
            );
        }

        private static OwnedRecordCallback? ReadOwnedRecordCallback(Clause clause, Expr.RecordUpdate state)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable eventArg, AliasPattern alias],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable newValue,
                            Value: Expr.Call
                            {
                                DynamicRemote: true,
                                Arguments:
                                    [
                                        Expr.Variable module, Expr.Variable function,
                                        Expr.List { Items: [Expr.Variable value, Expr.Variable file, Expr.Variable input], Tail: null }
                                    ]
                            } call
                        },
                            Expr.Case
                        {
                            Value: Expr.Variable key,
                            Clauses:
                                [
                                { Guard: null, Patterns: [var resetPattern], Body: Expr.RecordUpdate reset },
                                {
                                    Guard: null,
                                    Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                    Body: Expr.RecordUpdate
                                    {
                                        Value: Expr.Variable retained,
                                        Fields: [var retainField]
                                    } retain
                                }
                                ]
                        }
                        ]
                    }
                }
                || !TryReadPatternAlias(alias, out var saved, out var pattern)
                || pattern is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tuple
                || tag.Name != state.Name || tuple.Items.Count != state.Arity
                || !DistinctBindings(saved, eventArg, newValue)
                || tuple.Items.Skip(1).Any(item => item is not Erlang.Pattern.Variable)
                || !DistinctBindings(tuple.Items.Skip(1).Cast<Pattern.Variable>()
                    .Where(variable => variable.Name != VariableScopeNames.Wildcard)
                    .Concat([saved, eventArg, newValue]).ToArray())
                || input.Name != eventArg.Name || key.Name != eventArg.Name
                || reset.Value is not Expr.Variable resetSource || resetSource.Name != saved.Name || retained.Name != saved.Name
                || reset.Name != state.Name || reset.Arity != state.Arity || retain.Name != state.Name || retain.Arity != state.Arity
                || reset.Fields.Count != 2 || retainField.Value is not Expr.Variable result || result.Name != newValue.Name
                || resetPattern is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom resetTag }, Pattern.Literal { Value: Integer resetCode }] })
            {
                return null;
            }

            int fileIndex = FindField(file);
            int moduleIndex = FindField(module);
            int functionIndex = FindField(function);
            int valueIndex = FindField(value);
            var resetFile = reset.Fields.SingleOrDefault(field => field.Index == fileIndex);
            var resetValue = reset.Fields.SingleOrDefault(field => field.Index == valueIndex);
            if (fileIndex != state.Fields[0].Index || moduleIndex <= fileIndex || functionIndex <= moduleIndex
                || valueIndex <= functionIndex || retainField.Index != valueIndex
                || resetFile is null || resetValue?.Value is not Expr.Variable resetBinding
                || !AttributeLiteral.TryNormalizeConstant(resetFile.Value, out Term resetTerm)
                || FindField(resetBinding) is not (>= 1 and var resetIndex) || resetIndex == valueIndex
                || resetIndex == fileIndex || resetIndex == moduleIndex || resetIndex == functionIndex)
            {
                return null;
            }

            return new(
                call,
                new TupleTerm([resetTag, resetCode]),
                moduleIndex,
                functionIndex,
                valueIndex,
                resetIndex,
                resetTerm
            );

            int FindField(Expr.Variable binding) => Array.FindIndex(tuple.Items.ToArray(),
                item => item is Pattern.Variable variable && variable.Name == binding.Name);
        }

        private bool ProveOwnedRecordInput(FunctionDefinition function, OwnedRecordTail first, OwnedRecordTail second)
        {
            var signature = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature))
            {
                return false;
            }

            var witnesses = new HashSet<Expr.Call>(ReferenceEqualityComparer.Instance) { first.Call, second.Call };
            bool seeded = false;
            foreach (var caller in localFunctions.Values)
            {
                foreach (var expression in OwnedRecordExpressions(caller))
                {
                    if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                    {
                        return false;
                    }

                    if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                        || (call.Function, call.Arguments.Count) != signature || witnesses.Contains(call))
                    {
                        continue;
                    }

                    if (!CreatesOwnedRecord(call.Arguments[1], first.Update)
                        && !ReadOwnedRecordReceive(caller, call, first.Update))
                    {
                        return false;
                    }

                    seeded = true;
                }
            }

            return seeded;
        }

        private bool ReadOwnedRecordReceive(FunctionDefinition caller, Expr.Call step, Expr.RecordUpdate state)
        {
            var signature = (caller.Name, caller.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature)
                || caller.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable initial], Body: Expr.Receive
                        {
                            Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom }, Pattern.Variable payload] }],
                                Body: Expr.Sequence
                                {
                                    Items:
                                        [
                                            Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call actualStep },
                                            Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable next] } recur
                                        ]
                                }
                            },
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom }] }
                            ],
                            Timeout: null,
                            After: null
                        }
                    }
                ]
                || !DistinctBindings(initial, payload, result) || recur.Function != caller.Name || next.Name != result.Name
                || !ReferenceEquals(step, actualStep)
                || step.Arguments[1] is not Expr.Variable carried || carried.Name != initial.Name)
            {
                return false;
            }

            bool seeded = false;
            foreach (var expression in localFunctions.Values.SelectMany(OwnedRecordExpressions))
            {
                if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                {
                    return false;
                }

                if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                    || (call.Function, call.Arguments.Count) != signature || ReferenceEquals(call, recur))
                {
                    continue;
                }

                if (!CreatesOwnedRecord(call.Arguments[0], state))
                {
                    return false;
                }

                seeded = true;
            }

            return seeded;
        }

        private static IEnumerable<Expr> OwnedRecordExpressions(FunctionDefinition source) =>
            LocalFunctionEntries.ClauseExpressions(source.Clauses).SelectMany(LocalFunctionEntries.Walk);

        private bool CreatesOwnedRecord(Expr value, Expr.RecordUpdate state)
        {
            if (value is Expr.Call { Module: null, DynamicRemote: false } call
                && localFunctions.TryGetValue((call.Function, call.Arguments.Count), out var target)
                && !owner.IsDeclaredNative((call.Function, call.Arguments.Count)))
            {
                return target.Clauses.Count != 0 && target.Clauses.All(clause => IsStateConstruction(clause.Body));
            }

            return IsStateConstruction(value);

            bool IsStateConstruction(Expr expr) => expr is Expr.RecordConstruction record
                && record.Definition.Name == state.Name && record.Definition.Fields.Count + 1 == state.Arity;
        }

        private void EmitOwnedRecordHeap(int words, int live)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, words);
            owner.Operand(BeamWire.UnsignedTag, live);
        }

        private void EmitOwnedRecordUpdate(
            int arity,
            int tag,
            int source,
            int target,
            (int Index, int Tag, int Source, Term? Literal)[] fields
        )
        {
            owner.Op(UpdateRecord, AtomTag, owner.Intern(fields.Length == 1 ? "reuse" : "copy"));
            owner.Operand(BeamWire.UnsignedTag, arity);
            owner.EmitTypedRegister(source, TupleTypeBits, registerTag: tag);
            owner.Operand(XRegisterTag, target);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Length * 2);
            foreach (var field in fields)
            {
                owner.Operand(BeamWire.UnsignedTag, field.Index + 1);
                if (field.Literal is { } literal)
                {
                    owner.LiteralOperand(literal);
                }
                else
                {
                    owner.Operand(field.Tag, field.Source);
                }
            }
        }

        private sealed record OwnedRecordTail(
            Atom Tag,
            Expr.Call Call,
            Expr.RecordUpdate Update,
            Term? Constant,
            OwnedRecordRead? Read
        );

        private sealed record OwnedRecordRead(
            Expr.Call Call,
            Atom Tag,
            Expr.RecordAccess Member,
            Atom ResultTag
        );

        private sealed record OwnedRecordCallback(
            Expr.Call Call,
            Term Key,
            int ModuleIndex,
            int FunctionIndex,
            int ValueIndex,
            int ResetIndex,
            Term ResetFile
        );
    }
}

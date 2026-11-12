using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int ReceiveMessageSlot = -1;
        private Expr.Call? preparedReceiveCall;

        private bool TryEmitLiteralTimeoutReceive(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Receive { Clauses.Count: 0,
                Timeout: { } timeout, After: Expr.Literal result } } clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            if (timeout is Expr.Variable variable)
            {
                int index = Array.FindIndex(clause.Patterns.ToArray(), pattern => pattern is Pattern.Variable parameter
                    && parameter.Name == variable.Name);
                if (index < 0 || ReadPrivateIntegerConstant(function, index, new()) is not { } value)
                {
                    return false;
                }

                timeout = new Expr.Literal(value);
            }

            if (timeout is not Expr.Literal { Value: Integer integer } || integer.Value <= 0 || integer.Value > uint.MaxValue)
            {
                return false;
            }

            EmitFrameAllocation(0, 0);
            int wait = Label();
            Mark(wait);
            owner.Op(WaitTimeout, LabelTag, wait);
            GuardOperand(timeout, null);
            owner.instructions.WriteByte(ReceiveTimeout);
            owner.instructions.WriteByte(Move);
            owner.LiteralOperand(result.Value);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(0);

            return true;
        }

        private Integer? ReadPrivateIntegerConstant(FunctionDefinition function, int index, HashSet<(string, int, int)> path)
        {
            var target = (function.Name, function.Arity);
            var key = (function.Name, function.Arity, index);
            if (owner.exportedFunctions.Contains(target) || !path.Add(key))
            {
                return null;
            }

            Integer? agreed = null;
            foreach (var caller in localFunctions.Values)
            {
                foreach (var clause in caller.Clauses)
                {
                    foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]).SelectMany(LocalFunctionEntries.Walk))
                    {
                        if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == target)
                        {
                            path.Remove(key);
                            return null;
                        }

                        if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                            || (call.Function, call.Arguments.Count) != target)
                        {
                            continue;
                        }

                        Integer? actual = (call.Arguments[index] as Expr.Literal)?.Value as Integer;
                        if (call.Arguments[index] is Expr.Variable argument)
                        {
                            if (ReferenceEquals(caller, function)
                                && clause.Patterns[index] is Pattern.Variable parameter
                                && parameter.Name == argument.Name && ContainsTimeoutCall(clause.Body, call))
                            {
                                // A recursive edge carrying the same argument preserves the entry fact.
                                continue;
                            }

                            actual = ReadBoundTimeout(clause.Body, call, argument.Name);
                        }

                        if (actual is null && ReferenceEquals(clause.Body, call)
                            && call.Arguments[index] is Expr.Variable forwarded)
                        {
                            int parameter = Array.FindIndex(clause.Patterns.ToArray(), pattern => pattern is Pattern.Variable input
                                && input.Name == forwarded.Name);
                            if (parameter >= 0)
                            {
                                actual = ReadPrivateIntegerConstant(caller, parameter, path);
                            }
                        }

                        if (actual is null || agreed is not null && !agreed.Equals(actual))
                        {
                            path.Remove(key);
                            return null;
                        }

                        agreed = actual;
                    }
                }
            }

            path.Remove(key);

            return agreed;
        }

        private static Integer? ReadBoundTimeout(Expr body, Expr.Call call, string name)
        {
            if (body is not Expr.Sequence sequence)
            {
                return null;
            }

            Integer? value = null;
            foreach (var item in sequence.Items)
            {
                if (ContainsTimeoutCall(item, call))
                {
                    return value;
                }

                if (item is Expr.Match { Pattern: Pattern.Variable variable, Value: Expr.Literal { Value: Integer literal } }
                    && variable.Name == name)
                {
                    value = literal;
                }
            }

            return null;
        }

        private static bool ContainsTimeoutCall(Expr body, Expr.Call call) =>
            ReferenceEquals(body, call) || body switch
            {
                Expr.Sequence sequence => sequence.Items.Any(item => ContainsTimeoutCall(item, call)),
                Expr.Match match => ContainsTimeoutCall(match.Value, call),
                Expr.Case selection => selection.Clauses.Any(clause => ContainsTimeoutCall(clause.Body, call)),
                Expr.Receive receive => receive.Clauses.Any(clause => ContainsTimeoutCall(clause.Body, call)),
                Expr.Try attempt => ContainsTimeoutCall(attempt.Body, call),
                _ => false
            };

        private void EmitReceive(Expr.Receive receive, IReadOnlyList<string>? exports = null, bool terminal = false)
        {
            Expr? timeout = receive.Timeout;
            if (timeout is Expr.Variable timeoutVariable && parentFunction is { Clauses: [var entryClause] } function
                && ReferenceEquals(entryClause.Body, receive) && HasUnconditionalParameters(entryClause))
            {
                int index = Array.FindIndex(entryClause.Patterns.ToArray(), pattern => pattern is Pattern.Variable parameter
                    && parameter.Name == timeoutVariable.Name);
                if (index >= 0 && ReadPrivateIntegerConstant(function, index, new()) is { } value
                    && value.Value > 0 && value.Value <= uint.MaxValue)
                {
                    timeout = new Expr.Literal(value);
                }
            }

            bool infinite = timeout is null or Expr.Literal { Value: Atom { Name: ReceiveTimeoutAtoms.Infinity } };
            int? timeoutSlot = infinite ? null : GuardSlot(timeout!, null);
            var original = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            var joined = ReserveBranchExports(exports);
            bool tupleOnly = receive.Clauses.All(clause => clause.Patterns[0] is Pattern.Tuple);
            var arityGroups = tupleOnly
                ? receive.Clauses.GroupBy(clause => ((Pattern.Tuple)clause.Patterns[0]).Items.Count)
                    .OrderBy(group => group.Key).ToArray()
                : [];
            int scan = Label();
            bool orderedReceive = terminal && arityGroups.Length > 1;
            // The arity table points forward to branches, then retry and wait.
            int[]? layoutLabels = orderedReceive
                ? Enumerable.Range(0, receive.Clauses.Count + 2).Select(_ => Label()).ToArray()
                : null;
            int wait = layoutLabels is not null ? layoutLabels[^1] : receive.Clauses.Count == 0 ? scan : Label();
            int done = terminal ? 0 : Label();

            Mark(scan);
            if (receive.Clauses.Count != 0)
            {
                owner.Op(LoopReceive, LabelTag, wait);
                owner.Operand(XRegisterTag, 0);
                var scratch = new Dictionary<int, int>();

                int message;
                if (tupleOnly)
                {
                    // No clause can bind the whole message to a variable.
                    message = ReceiveMessageSlot;
                    // Flat patterns without guards only use the message and their scratch registers.
                    bool keepMessageInX0 = receive.Clauses.All(IsFlatUnguardedReceiveTuple);
                    scratch.Add(message, keepMessageInX0 ? 0 : 1);
                    if (!keepMessageInX0)
                    {
                        owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
                    }
                }
                else
                {
                    owner.receiveScratchRegisters = scratch;
                    message = Save();
                    owner.receiveScratchRegisters = null;
                }
                int branchBase = nextSlot;
                int branchLimit = branchBase;
                bool exhaustive = false;
                // Distinct tuple arities cannot match the same message. Within an arity
                // group, keep source order so guards and overlapping patterns behave as before.
                int retry = 0;
                var groups = new List<(int Label, IReadOnlyList<Clause> Clauses, int[]? ClauseLabels)>();
                if (arityGroups.Length > 1)
                {
                    retry = layoutLabels is null ? Label() : layoutLabels[^2];
                    var cases = new (int Arity, int Label)[arityGroups.Length];
                    int offset = 0;
                    for (int index = cases.Length - 1; index >= 0; index--)
                    {
                        Clause[] clauses = arityGroups[index].ToArray();
                        int label = layoutLabels is null ? 0 : layoutLabels[offset];
                        int[]? clauseLabels = layoutLabels?[(offset + 1)..(offset + clauses.Length)];
                        groups.Add((label, clauses, clauseLabels));
                        offset += clauses.Length;
                    }
                    for (int index = 0; index < cases.Length; index++)
                    {
                        int groupIndex = cases.Length - 1 - index;
                        var group = groups[groupIndex];
                        int label = layoutLabels is null ? Label() : group.Label;
                        cases[index] = (arityGroups[index].Key, label);
                        groups[groupIndex] = (label, group.Clauses, group.ClauseLabels);
                    }
                    owner.EmitTupleArityDispatch(retry, cases);
                }
                else
                {
                    groups.Add((0, receive.Clauses, null));
                }

                foreach (var group in groups)
                {
                    if (group.Label != 0)
                    {
                        Mark(group.Label);
                    }

                    bool sharedFields = group.Label != 0 && scratch[message] == 0
                        && TryEmitSharedReceiveFields(group.Clauses, original, retry);
                    for (int index = 0; index < group.Clauses.Count; index++)
                    {
                        Clause clause = group.Clauses[index];
                        variables = new(original, StringComparer.Ordinal);
                        nextSlot = branchBase;
                        int next = group.Label != 0 && index == group.Clauses.Count - 1
                            ? retry : group.ClauseLabels?[index] ?? Label();
                        if (!IsReceivePattern(clause.Patterns[0]))
                        {
                            throw new NotSupportedException(UnsupportedReceivePattern(clause.Patterns[0]));
                        }
                        if (clause.Guard is { } guard
                            && LocalFunctionEntries.Walk(guard).FirstOrDefault(item => !IsReceiveGuardExpression(item)) is { } unsupported)
                        {
                            throw new NotSupportedException(UnsupportedReceiveGuard(unsupported));
                        }

                        int? resultField = terminal && !sharedFields ? ReadReceiveResultField(clause) : null;
                        int previousPeak = peakSlot;
                        var clauseScratch = new Dictionary<int, int>(scratch);
                        owner.receiveScratchRegisters = clauseScratch;
                        try
                        {
                            if (sharedFields)
                            {
                                BindSharedReceiveFields((Pattern.Tuple)clause.Patterns[0], clauseScratch, next);
                            }
                            else
                            {
                                Pattern(clause.Patterns[0], message, next,
                                    checkedShape: group.Label != 0 ? clause.Patterns[0] : null, checkedTupleTag: false,
                                    receiveResultField: resultField);
                            }
                            Guard(clause.Guard, next);
                        }
                        finally
                        {
                            owner.receiveScratchRegisters = null;
                        }

                        bool emittedBranch = terminal && IsFlatUnguardedReceiveTuple(clause)
                            && (TryEmitReceiveRegisterException(clause.Body, clauseScratch, branchBase, previousPeak)
                                || TryEmitReceiveRegisterListCall(clause.Body, clauseScratch, branchBase, previousPeak));
                        if (!emittedBranch)
                        {
                            owner.instructions.WriteByte(RemoveMessage);
                        }
                        if (!emittedBranch && resultField.HasValue)
                        {
                            owner.EmitFrameReturn(frameSize);
                        }
                        else if (!emittedBranch && terminal && clause.Body is Expr.Variable result
                            && variables.TryGetValue(result.Name, out int resultSlot)
                            && clauseScratch.TryGetValue(resultSlot, out int resultRegister))
                        {
                            owner.MoveRegister(XRegisterTag, resultRegister, XRegisterTag, 0);
                            owner.EmitFrameReturn(frameSize);
                        }
                        else if (!emittedBranch)
                        {
                            if (!terminal || clause.Body is not (Expr.Variable or Expr.Literal))
                            {
                                foreach (int slot in variables.Values.Distinct())
                                {
                                    if (clauseScratch.TryGetValue(slot, out int register))
                                    {
                                        owner.MoveRegister(XRegisterTag, register, YRegisterTag, slot);
                                    }
                                }
                            }
                            EmitReceiveBranch(clause.Body, joined, terminal);
                        }
                        if (terminal && clause.Body is Expr.Variable or Expr.Literal
                            && IsFlatUnguardedReceiveTuple(clause))
                        {
                            // Only the scratch registers are used by this pattern and its result.
                            peakSlot = previousPeak;
                            nextSlot = branchBase;
                        }
                        branchLimit = Math.Max(branchLimit, nextSlot);
                        if (!terminal)
                        {
                            JumpTo(done);
                        }
                        if (clause.Guard is null or Expr.Literal { Value: Atom { Name: "true" } }
                            && clause.Patterns[0] is Pattern.Variable variable
                            && (variable.Name == VariableScopeNames.Wildcard || !original.ContainsKey(variable.Name)))
                        {
                            exhaustive = true;
                            break;
                        }
                        if (next != retry)
                        {
                            Mark(next);
                        }
                    }
                }

                if (retry != 0)
                {
                    Mark(retry);
                }

                nextSlot = branchLimit;
                if (!exhaustive)
                {
                    owner.Op(LoopReceiveEnd, LabelTag, scan);
                }
                Mark(wait);
            }

            variables = new(original, StringComparer.Ordinal);
            if (infinite)
            {
                owner.Op(Wait, LabelTag, scan);
            }
            else
            {
                owner.Op(WaitTimeout, LabelTag, scan);
                GuardOperand(timeout!, timeoutSlot);
                owner.instructions.WriteByte(ReceiveTimeout);
                EmitReceiveBranch(receive.After!, joined, terminal);
            }

            if (!terminal)
            {
                Mark(done);
            }
            variables = original;
            foreach (var (name, slot) in joined)
            {
                variables.Add(name, slot);
            }
        }

        private bool TryEmitSharedReceiveFields(
            IReadOnlyList<Clause> clauses, IReadOnlyDictionary<string, int> original, int retry)
        {
            if (clauses is not [
                { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal leftTag, Pattern.Variable bound, Pattern.Variable leftValue] }] },
                { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal rightTag, Pattern.Variable key, Pattern.Variable rightValue] }] }
            ] || leftTag.Value is not Atom tag || !Equals(tag, rightTag.Value)
                || !original.ContainsKey(bound.Name) || !IsFresh(leftValue) || !IsFresh(key) || !IsFresh(rightValue)
                || key.Name == rightValue.Name)
            {
                return false;
            }

            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 2);
            owner.EmitTupleElement(2, 0);
            owner.Op(ExactEqualTest, LabelTag, retry);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(tag);
            return true;

            bool IsFresh(Pattern.Variable variable) => variable.Name != VariableScopeNames.Wildcard
                && !original.ContainsKey(variable.Name) && !deferredPairMembers.ContainsKey(variable.Name);
        }

        private void BindSharedReceiveFields(Pattern.Tuple tuple, Dictionary<int, int> scratch, int failure)
        {
            var key = (Pattern.Variable)tuple.Items[1];
            if (variables.TryGetValue(key.Name, out int bound))
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 2);
                owner.Operand(YRegisterTag, bound);
            }
            else
            {
                Bind(key, 2);
            }
            Bind((Pattern.Variable)tuple.Items[2], 0);

            void Bind(Pattern.Variable variable, int register)
            {
                int slot = Slot();
                variables.Add(variable.Name, slot);
                scratch[slot] = register;
            }
        }

        private bool TryEmitReceiveRegisterException(Expr body, IReadOnlyDictionary<int, int> scratch, int branchBase, int previousPeak)
        {
            if (body is not Expr.Call
                {
                    DynamicRemote: false, Module: null or ErlangModuleNames.Module,
                    Function: AutoImportNames.Error or AutoImportNames.Exit or AutoImportNames.Throw,
                    Arguments: [Expr.Tuple tuple]
                } call || !owner.ResolvesErlangBif(call))
            {
                return false;
            }

            int scratchRegister = -1;
            foreach (Expr item in tuple.Items)
            {
                if (item is Expr.Variable variable && variables.TryGetValue(variable.Name, out int slot)
                    && !deferredPairMembers.ContainsKey(variable.Name))
                {
                    if (scratch.TryGetValue(slot, out int register))
                    {
                        if (scratchRegister >= 0 && scratchRegister != register)
                        {
                            return false;
                        }
                        scratchRegister = register;
                    }
                }
                else if (!AttributeLiteral.TryNormalizeConstant(item, out _))
                {
                    return false;
                }
            }

            if (scratchRegister < 0)
            {
                return false;
            }

            peakSlot = previousPeak;
            nextSlot = branchBase;
            if (scratchRegister != 0)
            {
                owner.MoveRegister(XRegisterTag, scratchRegister, XRegisterTag, 0);
            }
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(RemoveMessage);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            foreach (Expr item in tuple.Items)
            {
                if (item is Expr.Variable variable)
                {
                    int slot = variables[variable.Name];
                    if (scratch.ContainsKey(slot))
                    {
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        owner.Operand(YRegisterTag, slot);
                    }
                }
                else
                {
                    GuardOperand(item, null);
                }
            }

            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, frameSize);
            return true;
        }

        private bool TryEmitReceiveRegisterListCall(Expr body, IReadOnlyDictionary<int, int> scratch, int branchBase, int previousPeak)
        {
            if (body is not Expr.Sequence { Items: [Expr.Match
                {
                    Value: Expr.Call
                    {
                        Module: not null and not ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [var constant, Expr.List { Items: [Expr.Variable first, Expr.Variable second], Tail: null }]
                    } call
                }, ..] } sequence
                || !AttributeLiteral.TryNormalizeConstant(constant, out _)
                || !variables.TryGetValue(first.Name, out int firstSlot) || !scratch.TryGetValue(firstSlot, out int firstRegister)
                || !variables.TryGetValue(second.Name, out int secondSlot) || !scratch.TryGetValue(secondSlot, out int secondRegister)
                || firstRegister == secondRegister
                || TryForwardReceiveCall(sequence) is not { } forwarded)
            {
                return false;
            }

            var scratchBindings = variables.Where(binding => scratch.ContainsKey(binding.Value))
                .Select(binding => (Pattern)new Pattern.Variable(binding.Key)).ToArray();
            var rest = sequence with { Items = sequence.Items.Skip(1).ToArray() };
            if (ReadUsedEntryParameters(new Clause(scratchBindings, null, rest)) is not { Count: 0 })
            {
                return false;
            }

            foreach (var binding in scratchBindings.Cast<Pattern.Variable>())
            {
                variables.Remove(binding.Name);
            }
            peakSlot = previousPeak;
            nextSlot = branchBase;

            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, Math.Max(firstRegister, secondRegister) + 1);
            if (firstRegister != 1 && secondRegister != 1)
            {
                owner.MoveRegister(XRegisterTag, secondRegister, XRegisterTag, 1);
                secondRegister = 1;
            }
            owner.instructions.WriteByte(RemoveMessage);
            owner.Op(PutList, XRegisterTag, secondRegister);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, secondRegister);
            owner.Op(PutList, XRegisterTag, firstRegister);
            owner.Operand(XRegisterTag, secondRegister);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(constant, 0);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 2);

            var previousCall = preparedReceiveCall;
            preparedReceiveCall = call;
            try
            {
                EmitReceiveBranch(forwarded, [], terminal: true);
            }
            finally
            {
                preparedReceiveCall = previousCall;
            }
            return true;
        }

        private void EmitReceiveBranch(Expr body, (string Name, int Slot)[] joined, bool terminal)
        {
            if (!terminal)
            {
                EmitExportingBranch(body, joined);
                StoreBranchExports(joined);
                return;
            }

            if (body is Expr.Sequence sequence && TryForwardReceiveCall(sequence) is { } forwarded)
            {
                body = forwarded;
            }

            if (TailLocal(body, reuseCallSlots: true))
            {
                return;
            }

            if (ReadEntryCaseExternalTail(body) is { } external)
            {
                EmitCaseExternalTail(external.Call, external.Module);
                return;
            }

            if (body is Expr.Call
                {
                    DynamicRemote: false, Module: null or ErlangModuleNames.Module, Arguments.Count: 1,
                    Function: AutoImportNames.Error or AutoImportNames.Exit or AutoImportNames.Throw
                } failure && owner.ResolvesErlangBif(failure))
            {
                EmitCaseExternalTail(failure, ErlangModuleNames.Module);
                return;
            }

            EmitReturnExpression(body, reuseCallSlots: true);
            owner.EmitFrameReturn(frameSize);
        }

        private static bool IsReceivePattern(Pattern pattern) => pattern switch
        {
            Pattern.Literal { } or Pattern.Variable { } => true,
            BitPattern { Segments: [.., { Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard },
                Specification: { Type: BitSegmentTypes.Binary, Size: null } }] } bits =>
                bits.Segments.SkipLast(1).All(segment =>
                    FixedSize(segment.Specification) is not null && IsReceivePattern(segment.Value)),
            Pattern.Tuple tuple => tuple.Items.All(IsReceivePattern),
            Pattern.List list => list.Items.All(IsReceivePattern) && (list.Tail is null || IsReceivePattern(list.Tail)),
            AliasPattern alias => IsReceivePattern(alias.Left) && IsReceivePattern(alias.Right),
            MapPattern map => map.Fields.All(field =>
                AttributeLiteral.TryNormalizeConstant(field.Key, out _) && IsReceivePattern(field.Value)),
            _ => false
        };

        private Expr? TryForwardReceiveCall(Expr.Sequence sequence)
        {
            if (sequence.Items is not
                [Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Call input }, var consumer, ..]
                || binding.Name == VariableScopeNames.Wildcard || variables.ContainsKey(binding.Name)
                || deferredPairMembers.ContainsKey(binding.Name)
                || ForwardReceiveCallOperand(consumer, binding.Name, input) is not { } rewritten)
            {
                return null;
            }

            Expr[] remaining = sequence.Items.Skip(2).ToArray();
            if (remaining.Length == 0)
            {
                return rewritten;
            }

            var continuation = new Expr.Sequence(remaining);
            return ReadUsedEntryParameters(new Clause([binding], null, continuation)) is { Count: 0 }
                ? sequence with { Items = [rewritten, .. remaining] }
                : null;
        }

        // Only follow a single evaluated operand through calls and constant constructors.
        private static Expr? ForwardReceiveCallOperand(Expr expression, string name, Expr.Call input) => expression switch
        {
            Expr.Variable variable when variable.Name == name => input,
            Expr.Call { Module: not null, DynamicRemote: false, Arguments: [var operand] } call
                when ForwardReceiveCallOperand(operand, name, input) is { } rewritten =>
                    call with { Arguments = [rewritten] },
            Expr.Tuple { Items: [.., var last] } tuple
                when tuple.Items.SkipLast(1).All(item => AttributeLiteral.TryNormalizeConstant(item, out _))
                    && ForwardReceiveCallOperand(last, name, input) is { } rewritten =>
                    tuple with { Items = [.. tuple.Items.SkipLast(1), rewritten] },
            Expr.List { Items: [.., var last], Tail: null } list
                when list.Items.SkipLast(1).All(item => AttributeLiteral.TryNormalizeConstant(item, out _))
                    && ForwardReceiveCallOperand(last, name, input) is { } rewritten =>
                    list with { Items = [.. list.Items.SkipLast(1), rewritten] },
            _ => null
        };

        private int? ReadReceiveResultField(Clause clause)
        {
            if (clause.Guard is not null || clause.Body is not Expr.Variable result
                || variables.ContainsKey(result.Name) || deferredPairMembers.ContainsKey(result.Name)
                || clause.Patterns[0] is not Pattern.Tuple { Items: [.., Pattern.Variable field] } tuple
                || field.Name != result.Name
                || !IsFlatUnguardedReceiveTuple(clause)
                || tuple.Items.Take(tuple.Items.Count - 1).Any(item => item is Pattern.Variable previous && previous.Name == field.Name))
            {
                return null;
            }

            // The final extraction cannot fail after the tuple shape and earlier fields matched.
            return tuple.Items.Count - 1;
        }

        private static bool IsFlatUnguardedReceiveTuple(Clause clause) =>
            clause.Guard is null && clause.Patterns[0] is Pattern.Tuple tuple
            && tuple.Items.All(item => item is Pattern.Variable { } or Pattern.Literal { } or Pattern.Any { });

        private int ReceiveGuardLiveRegisters => owner.receiveScratchRegisters is { } scratch
            ? scratch.Count + (scratch.GetValueOrDefault(ReceiveMessageSlot, 1) == 0 ? 0 : 1)
            : 0;

        private bool IsReceiveGuardExpression(Expr expression) => expression switch
        {
            Expr.Variable or Expr.Literal => true,
            Expr.GuardAlternatives => true,
            Expr.Binary { Operator: ErlangOperators.AndAlso } => true,
            Expr.Binary { Operator: ErlangOperators.OrElse } disjunction =>
                IsReceiveBooleanTest(disjunction.Left) && IsReceiveGuardExpression(disjunction.Right),
            Expr.RecordTest { Value: Expr.Variable or Expr.Literal } => true,
            Expr.Binary binary when IsReceiveComparison(binary) =>
                IsReceiveGuardValue(binary.Left) && IsReceiveGuardValue(binary.Right),
            Expr.Call call when IsReceiveTypeTest(call) => true,
            _ => IsReceiveGuardValue(expression)
        };

        private bool IsReceiveBooleanTest(Expr expression) => expression switch
        {
            Expr.Literal { Value: Atom { Name: "true" or "false" } } => true,
            Expr.Unary { Operator: ErlangOperators.Not } negation => IsReceiveBooleanTest(negation.Operand),
            Expr.Binary { Operator: ErlangOperators.AndAlso or ErlangOperators.OrElse } binary =>
                IsReceiveBooleanTest(binary.Left) && IsReceiveBooleanTest(binary.Right),
            Expr.Binary binary => IsReceiveComparison(binary)
                && binary.Left is Expr.Variable or Expr.Literal
                && binary.Right is Expr.Variable or Expr.Literal,
            Expr.RecordTest record => IsReceiveGuardExpression(record),
            Expr.Call call => IsReceiveTypeTest(call),
            _ => false
        };

        private static bool IsReceiveComparison(Expr.Binary binary) =>
            binary.Operator is ErlangOperators.ExactEqual
                or ErlangOperators.ExactNotEqual or ErlangOperators.NumericEqual or ErlangOperators.NumericNotEqual
                or ErlangOperators.Less or ErlangOperators.Greater or ErlangOperators.LessOrEqual or ErlangOperators.GreaterOrEqual;

        private bool IsReceiveGuardValue(Expr expression) => expression switch
        {
            Expr.Variable or Expr.Literal => true,
            Expr.RecordAccess access => IsReceiveGuardValue(access.Value),
            Expr.List list => AttributeLiteral.TryNormalizeConstant(list, out _),
            Expr.Tuple tuple => tuple.Items.All(IsReceiveGuardValue),
            Expr.Unary { Operator: ErlangOperators.Not } unary => IsReceiveGuardValue(unary.Operand),
            Expr.Binary { Operator: ErlangOperators.Plus } binary =>
                IsReceiveGuardValue(binary.Left) && IsReceiveGuardValue(binary.Right),
            Expr.Call { DynamicRemote: false } call when owner.ResolvesErlangBif(call) => call switch
            {
                { Function: AutoImportNames.Self, Arguments: [] } => true,
                { Function: AutoImportNames.Node or AutoImportNames.ByteSize or AutoImportNames.TupleSize or AutoImportNames.Length,
                    Arguments: [var operand] } => IsReceiveGuardValue(operand),
                { Function: AutoImportNames.IsTuple or AutoImportNames.IsList or AutoImportNames.IsPort,
                    Arguments: [var operand] } => IsReceiveGuardValue(operand),
                { Function: AutoImportNames.Element or AutoImportNames.IsMapKey, Arguments: [var index, var value] } =>
                    IsReceiveGuardValue(index) && IsReceiveGuardValue(value),
                _ => false
            },
            _ => false
        };

        private bool IsReceiveTypeTest(Expr.Call call) => !call.DynamicRemote && owner.ResolvesErlangBif(call)
            && (call is
            {
                Function: AutoImportNames.IsAtom or AutoImportNames.IsBinary or AutoImportNames.IsInteger
                    or AutoImportNames.IsList or AutoImportNames.IsTuple or AutoImportNames.IsPid
                    or AutoImportNames.IsReference or AutoImportNames.IsBoolean or AutoImportNames.IsMap
                    or AutoImportNames.IsFunction or AutoImportNames.IsPort,
                Arguments: [Expr.Variable or Expr.Literal]
            }
            || call is
            {
                Function: AutoImportNames.IsFunction,
                Arguments: [Expr.Variable or Expr.Literal, Expr.Literal { Value: Integer arity }]
            } && arity.Value >= 0 && arity.Value <= byte.MaxValue
            || owner.ReadRecordTest(call) is { Value: Expr.Variable or Expr.Literal });
    }
}

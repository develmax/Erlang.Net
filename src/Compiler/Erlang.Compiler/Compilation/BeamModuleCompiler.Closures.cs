using System.Globalization;
using System.Text;
using static Erlang.Compiler.BeamContainerWriter;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record LiftedClosure(
        string Name,
        Clause? Body,
        BeamSourceLocation Location,
        int CaptureCount,
        ClosureTail? Tail,
        int DeclaredArity = 0,
        string? GuardFailure = null,
        FunctionDefinition? General = null,
        FunctionDefinition? LexicalOwner = null,
        string? SelfName = null,
        bool EntryPatternFailure = false
    )
    {
        internal int Entry { get; set; }
        internal int Arity => General?.Arity ?? Body?.Patterns.Count ?? DeclaredArity;
    }

    private sealed record ClosureTail(
        Clause Base,
        Integer Key,
        int Step,
        int Line,
        int? AccumulatorLine = null
    );

    private readonly List<LiftedClosure> closures = [];

    private bool TryEmitReturnedClosure(FunctionDefinition function, BeamSourceLocation location)
    {
        if (function.Clauses is not [
            { Guard: null } parent
            ] || !HasUnconditionalParameters(parent))
        {
            return false;
        }

        var returnedTuple = parent.Body as Expr.Tuple;
        Expr.Fun[] functions;
        if (parent.Body is Expr.Fun direct)
        {
            functions = [direct];
        }
        else if (returnedTuple is not null
            && returnedTuple.Items.All(item => item is Expr.Fun || IsClosureTupleOperand(item)))
        {
            functions = returnedTuple.Items.OfType<Expr.Fun>().ToArray();
        }
        else
        {
            return false;
        }
        if (functions.Length == 0)
        {
            return false;
        }

        var plans = new List<(LiftedClosure Closure, int[] Registers)>(functions.Length);
        var uses = new int[parent.Patterns.Count];
        ClosureTail?[] tails = functions.Select(ReadClosureTail).ToArray();
        int anonymousOrdinal = tails.Count(tail => tail is null);
        int recursiveOrdinal = 0;
        int guardOrdinal = 0;
        int heapWords = returnedTuple is null ? 0 : returnedTuple.Items.Count + 1;
        for (int index = 0; index < functions.Length; index++)
        {
            ClosureTail? tail = tails[index];
            int ordinal = tail is null ? --anonymousOrdinal : recursiveOrdinal++;
            if (ReadReturnedClosure(
                function,
                parent,
                functions[index],
                location,
                ordinal,
                guardOrdinal,
                tail
            ) is not { } plan)
            {
                return false;
            }
            plans.Add(plan);
            if (plan.Closure.GuardFailure is not null)
            {
                guardOrdinal++;
            }
            heapWords += plan.Registers.Length;
            foreach (int register in plan.Registers)
            {
                uses[register]++;
            }
        }

        var retained = new HashSet<int>();
        if (returnedTuple is not null)
        {
            foreach (Expr.Variable variable in returnedTuple.Items.OfType<Expr.Variable>())
            {
                retained.Add(ArgumentRegister(parent, variable.Name));
            }
        }
        int live = Math.Max(Array.FindLastIndex(uses, count => count != 0), retained.DefaultIfEmpty(-1).Max()) + 1;
        var destinations = new int[plans.Count];
        for (int index = 0; index < plans.Count; index++)
        {
            foreach (int register in plans[index].Registers)
            {
                uses[register]--;
            }
            int destination = 0;
            while (retained.Contains(destination) || (destination < uses.Length && uses[destination] != 0))
            {
                destination++;
            }
            if (destination >= FirstReservedXRegister)
            {
                return false;
            }
            destinations[index] = destination;
            retained.Add(destination);
        }

        EmitClosureAllocation(heapWords, live, plans.Count);
        for (int index = 0; index < plans.Count; index++)
        {
            var plan = plans[index];
            Op(MakeFun, BeamWire.UnsignedTag, closures.Count);
            Operand(XRegisterTag, destinations[index]);
            instructions.WriteByte(ExtendedListOperand);
            Operand(BeamWire.UnsignedTag, plan.Registers.Length);
            foreach (int register in plan.Registers)
            {
                Operand(XRegisterTag, register);
            }
            closures.Add(plan.Closure);
        }
        if (returnedTuple is not null)
        {
            EmitReturnedClosureTuple(parent, returnedTuple, destinations);
        }
        instructions.WriteByte(Return);

        return true;
    }

    private (LiftedClosure Closure, int[] Registers)? ReadReturnedClosure(
        FunctionDefinition function,
        Clause parent,
        Expr.Fun fun,
        BeamSourceLocation location,
        int ordinal,
        int guardOrdinal,
        ClosureTail? tail
    )
    {
        Clause? body = tail?.Base ?? (fun.Clauses is [var single] ? single : null);
        if (body is null || !HasUnconditionalParameters(body)
                || fun.SelfName is { } selfName
                    && body.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == selfName))
        {
            return null;
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        if (body.Guard is not null)
        {
            if (fun.SelfName is not null
                || body.Guard is not Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Variable argument, Right: Expr.Variable captured
                }
                || !HasParameter(body, argument.Name) || HasParameter(body, captured.Name)
                || !HasParameter(parent, captured.Name))
            {
                return null;
            }

            used.Add(captured.Name);
        }

        if (tail?.AccumulatorLine is not null && body.Body is Expr.Binary { Right: Expr.Variable offset })
        {
            used.Add(offset.Name);
        }
        else if (body.Body is Expr.Variable variable)
        {
            used.Add(variable.Name);
        }
        else if (body.Guard is not null && ReadGuardedClosureEffect(body, parent) is { } operands)
        {
            used.UnionWith(operands.OfType<Expr.Variable>().Select(operand => operand.Name));
        }
        else if (body.Patterns.Count == 0 && (ReadFormattingClosure(body.Body, parent.Patterns) is not null
            || ReadBinaryTextClosure(body.Body, parent.Patterns) is not null
            || ReadProtectedTermClosure(body.Body, parent.Patterns) is not null))
        {
            used.UnionWith(parent.Patterns.OfType<Pattern.Variable>().Select(parameter => parameter.Name));
        }
        else if (!AttributeLiteral.TryNormalizeConstant(body.Body, out _))
        {
            if (body.Body is not Expr.Tuple tuple || !tuple.Items.All(IsClosureTupleOperand))
            {
                return null;
            }

            used.UnionWith(tuple.Items.OfType<Expr.Variable>().Select(item => item.Name));
        }

        used.ExceptWith(body.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name));
        if (fun.SelfName is { } self && used.Contains(self))
        {
            return null;
        }

        // OTP orders these free variables by the parent argument registers.
        var captures = new List<(Pattern.Variable Parameter, int Register)>();
        for (int register = 0; register < parent.Patterns.Count; register++)
        {
            var parameter = (Pattern.Variable)parent.Patterns[register];
            if (used.Remove(parameter.Name))
            {
                captures.Add((parameter, register));
            }
        }
        if (used.Count != 0 || body.Patterns.Count + captures.Count > BeamWire.MaximumArity)
        {
            return null;
        }

        string suffix = tail is null ? "fun" : fun.SelfName + "/" + body.Patterns.Count.ToString(CultureInfo.InvariantCulture);
        string liftedName = "-" + function.Name + "/" + function.Arity.ToString(CultureInfo.InvariantCulture)
            + "-" + suffix + "-" + ordinal.ToString(CultureInfo.InvariantCulture) + "-";

        string? guardFailure = body.Guard is null ? null
            : "-" + function.Name + "/" + function.Arity.ToString(CultureInfo.InvariantCulture)
                + "-inlined-" + guardOrdinal.ToString(CultureInfo.InvariantCulture) + "-";

        return (new(
            liftedName,
            body with { Patterns = [.. body.Patterns, .. captures.Select(capture => capture.Parameter)] },
            fun.ClauseLine == 0 ? location : new(fun.ClauseFile, fun.ClauseLine),
            captures.Count,
            tail,
            GuardFailure: guardFailure
        ), captures.Select(capture => capture.Register).ToArray());
    }

    private static bool HasParameter(Clause clause, string name) =>
        clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == name);

    private IReadOnlyList<Expr>? ReadGuardedClosureEffect(Clause body, Clause parent) => body.Body switch
    {
        var expression when ReadGuardedListCall(expression) is { } listCall => [listCall.Receiver],
        var expression when ReadGuardedBinaryReply(expression) is { } reply
            && !HasParameter(body, reply.Payload) && !HasParameter(parent, reply.Payload)
            && !HasParameter(body, reply.Fallback) && !HasParameter(parent, reply.Fallback) => reply.Call.Arguments,
        var expression when ReadGuardedLocalReply(expression) is { } reply
            && !HasParameter(body, reply.Payload) && !HasParameter(parent, reply.Payload)
            && !HasParameter(body, reply.Fallback) && !HasParameter(parent, reply.Fallback) => reply.Call.Arguments,
        var expression when ReadGuardedOptionFilter(expression) is { } filter
            && !HasParameter(body, filter.Rejected) && !HasParameter(parent, filter.Rejected) => filter.Apply.Arguments,
        Expr.Call { Module: not null and not ErlangModuleNames.Module, DynamicRemote: false } call
            when call.Arguments.All(IsClosureTupleOperand) => call.Arguments,
        Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Variable recipient } send
            when AttributeLiteral.TryNormalizeConstant(send.Right, out _) => [recipient, send.Right],
        _ => null
    };

    private int EmitGuardedLiftedBody(LiftedClosure closure, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        Clause body = closure.Body!;
        var guard = (Expr.Binary)body.Guard!;
        var argument = (Expr.Variable)guard.Left;
        var capture = (Expr.Variable)guard.Right;
        int argumentRegister = ArgumentRegister(body, argument.Name);
        int captureRegister = ArgumentRegister(body, capture.Name);
        var binaryReply = ReadGuardedBinaryReply(body.Body);
        var localReply = ReadGuardedLocalReply(body.Body);
        var optionFilter = ReadGuardedOptionFilter(body.Body);
        int replyFallback = binaryReply is null && localReply is null && optionFilter is null ? 0 : nextLabel++;
        int failed = nextLabel++;
        Op(ExactEqualTest, LabelTag, failed);
        Operand(XRegisterTag, argumentRegister);
        Operand(XRegisterTag, captureRegister);
        if (binaryReply is not null)
        {
            EmitGuardedBinaryReply(
                body,
                binaryReply,
                replyFallback,
                closure.Location
            );
        }
        else if (localReply is not null)
        {
            EmitGuardedLocalReply(
                body,
                localReply,
                replyFallback,
                closure.Location,
                entries
            );
        }
        else if (optionFilter is not null)
        {
            EmitGuardedOptionFilter(
                body,
                optionFilter,
                replyFallback,
                closure.Location,
                entries
            );
        }
        else if (ReadGuardedListCall(body.Body) is { } listCall)
        {
            EmitGuardedListCall(
                body,
                listCall.Outer,
                listCall.Inner,
                listCall.Receiver,
                closure.Location
            );
        }
        else if (body.Body is Expr.Call { Module: not null } call)
        {
            EmitGuardedClosureCall(
                body,
                call,
                closure.Location,
                argumentRegister,
                captureRegister
            );
        }
        else if (body.Body is Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Variable recipient } send)
        {
            int register = ArgumentRegister(body, recipient.Name);
            Op(Allocate, BeamWire.UnsignedTag, 0);
            Operand(BeamWire.UnsignedTag, register + 1);
            if (register != 0)
            {
                MoveToX0(XRegisterTag, register);
            }
            EmitConstant(send.Right, 1);
            var previous = closure.Location;
            EmitArithmeticLine(send.SourceLine, ref previous);
            instructions.WriteByte(Send);
            Op(Deallocate, BeamWire.UnsignedTag, 0);
            instructions.WriteByte(Return);
        }
        else
        {
            // The equality test can establish that x0 already holds the result.
            if (body.Body is not Expr.Variable result || result.Name != capture.Name || argumentRegister != 0)
            {
                EmitBody(body);
            }
            instructions.WriteByte(Return);
        }

        Op(Label, BeamWire.UnsignedTag, failed);
        Op(TailLocalCall, BeamWire.UnsignedTag, closure.Arity - closure.CaptureCount);
        int position = checked((int)instructions.Position);
        Operand(LabelTag, 0);

        return position;
    }

    private void EmitGuardedClosureCall(
        Clause body,
        Expr.Call call,
        BeamSourceLocation location,
        int argumentRegister,
        int captureRegister
    )
    {
        EmitGuardedCallArguments(
            body,
            call,
            argumentRegister,
            captureRegister
        );
        EmitCallLine(call, location);
        Op(TailExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
        Operand(BeamWire.UnsignedTag, Import(call.Module!, call.Function, call.Arguments.Count));
    }

    private void EmitGuardedCallArguments(
        Clause body,
        Expr.Call call,
        int argumentRegister = -1,
        int captureRegister = -1
    )
    {
        var moves = new List<(int Destination, int Source)>();
        var constants = new List<(int Destination, Term Value)>();
        for (int destination = 0; destination < call.Arguments.Count; destination++)
        {
            if (call.Arguments[destination] is Expr.Variable variable)
            {
                int source = ArgumentRegister(body, variable.Name);
                if (source != destination && !(source == captureRegister && destination == argumentRegister))
                {
                    moves.Add((destination, source));
                }
            }
            else if (AttributeLiteral.TryNormalizeConstant(call.Arguments[destination], out Term constant))
            {
                constants.Add((destination, constant));
            }
        }

        if (constants.Count == call.Arguments.Count)
        {
            EmitIndependentCallMoves(constants);
        }
        else
        {
            EmitArgumentMoves(moves, constants);
        }
    }

    private void EmitLiftedGuardFailures(Dictionary<string, int> failureOperands)
    {
        if (failureOperands.Count == 0)
        {
            return;
        }

        // OTP sorts these stubs by atom name after all lifted functions.
        var atomOrder = Comparer<byte[]>.Create((left, right) => left.AsSpan().SequenceCompareTo(right));
        foreach (LiftedClosure closure in closures.Where(closure => closure.GuardFailure is not null)
            .OrderBy(closure => Encoding.UTF8.GetBytes(closure.GuardFailure!), atomOrder))
        {
            string helper = closure.GuardFailure!;
            int info = nextLabel;
            StartFunction(
                helper,
                closure.Arity - closure.CaptureCount,
                closure.Location,
                exported: false
            );
            deferredLabelOperands.Add((failureOperands[helper], nextLabel - 1));
            Op(Jump, LabelTag, info);
        }
    }

    private void ResolveDeferredLabelOperands()
    {
        if (deferredLabelOperands.Count == 0)
        {
            return;
        }

        // Replace the compact one-byte placeholders once the helper labels are known.
        byte[] code = instructions.ToArray();
        instructions.Position = 0;
        int copied = 0;
        foreach (var (position, target) in deferredLabelOperands.OrderBy(operand => operand.Position))
        {
            instructions.Write(code, copied, position - copied);
            Operand(LabelTag, target < 0 ? deferredEntries[target] : target);
            copied = position + 1;
        }

        instructions.Write(code, copied, code.Length - copied);
    }

    private void EmitReturnedClosureTuple(Clause parent, Expr.Tuple tuple, IReadOnlyList<int> closureRegisters)
    {
        Op(PutTuple, XRegisterTag, 0);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, tuple.Items.Count);
        int closureIndex = 0;
        foreach (Expr item in tuple.Items)
        {
            switch (item)
            {
                case Expr.Variable variable:
                    Operand(XRegisterTag, ArgumentRegister(parent, variable.Name));
                    break;
                case Expr.Literal literal:
                    LiteralOperand(literal.Value);
                    break;
                case Expr.Fun:
                    Operand(XRegisterTag, closureRegisters[closureIndex++]);
                    break;
            }
        }
    }

    private static bool IsClosureTupleOperand(Expr expression) =>
        expression is Expr.Variable or Expr.Literal { Value: Atom or Nil }
            || expression is Expr.Literal { Value: Integer value } && value.Value >= int.MinValue && value.Value <= int.MaxValue;

    private static ClosureTail? ReadClosureTail(Expr.Fun fun)
    {
        if (fun.SelfName is not { } self || fun.Clauses is not [
            { Guard: null, Patterns: [Pattern.Literal { Value: Integer key }, ..] } terminal,
            {
                Guard: null, Patterns: [Pattern.Variable input, ..] parameters,
                Body: Expr.Apply
                {
                    Function: Expr.Variable target,
                    Arguments: [Expr.Binary
                    {
                        Operator: ErlangOperators.Minus,
                        Left: Expr.Variable source,
                        Right: Expr.Literal { Value: Integer step }
                    } decrement, ..] arguments
                }
            }]
            || input.Name is VariableScopeNames.Wildcard || input.Name == self
            || target.Name != self || source.Name != input.Name
            || step.Value <= 0 || step.Value > int.MaxValue)
        {
            return null;
        }

        var counter = new Pattern.Variable(VariableScopeNames.Wildcard);
        if (terminal.Patterns.Count == 1 && parameters.Count == 1 && arguments.Count == 1)
        {
            return new(
                terminal with { Patterns = [counter] },
                key,
                (int)step.Value,
                decrement.SourceLine
            );
        }

        if (terminal is not
            {
                Patterns: [_, Pattern.Variable initial],
                Body: Expr.Binary { Operator: ErlangOperators.Plus, Left: Expr.Variable sum, Right: Expr.Variable offset }
            }
            || initial.Name is VariableScopeNames.Wildcard || initial.Name == self
            || sum.Name != initial.Name || offset.Name == initial.Name
            || parameters is not [_, Pattern.Variable accumulator]
            || accumulator.Name is VariableScopeNames.Wildcard || accumulator.Name == self || accumulator.Name == input.Name
            || arguments is not [_, Expr.Binary
            {
                Operator: ErlangOperators.Plus, Left: Expr.Variable previous, Right: Expr.Variable counterArgument
            } update]
            || previous.Name != accumulator.Name || counterArgument.Name != input.Name)
        {
            return null;
        }

        return new(
            terminal with { Patterns = [counter, initial] },
            key,
            (int)step.Value,
            decrement.SourceLine,
            update.SourceLine
        );
    }

    private void EmitClosureAllocation(int words, int live, int funCount)
    {
        instructions.WriteByte(TestHeap);
        EmitClosureHeap(words, live, funCount);
    }

    private void EmitClosureHeap(int words, int live, int funCount)
    {
        Operand(ExtendedTag, AllocationList);
        Operand(BeamWire.UnsignedTag, AllocationKinds);
        Operand(BeamWire.UnsignedTag, AllocationWords);
        Operand(BeamWire.UnsignedTag, words);
        Operand(BeamWire.UnsignedTag, AllocationFloats);
        Operand(BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, AllocationFuns);
        Operand(BeamWire.UnsignedTag, funCount);
        Operand(BeamWire.UnsignedTag, live);
    }

    private void EmitLiftedClosures(
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
        Dictionary<string, int> failureOperands,
        int start = 0,
        Action<int>? emitAtBoundary = null
    )
    {
        while (start < closures.Count)
        {
            int end = closures.Count;
            for (int index = end - 1; index >= start; index--)
            {
                emitAtBoundary?.Invoke(index + 1);
                var closure = closures[index];
                if (closure.Body is null && closure.General is null)
                {
                    continue;
                }
                StartFunction(
                    closure.Name,
                    closure.Arity,
                    closure.Location,
                    exported: false
                );
                closure.Entry = nextLabel - 1;
                if (closure.General is { } general)
                {
                    EmitGeneralLiftedBody(
                        general,
                        closure,
                        index,
                        entries,
                        localFunctions,
                        failureOperands
                    );
                }
                else if (closure.GuardFailure is { } guardFailure)
                {
                    failureOperands.Add(guardFailure, EmitGuardedLiftedBody(closure, entries));
                }
                else
                {
                    EmitLiftedBody(closure, entries);
                }
            }
            emitAtBoundary?.Invoke(start);
            emitAtBoundary = null;
            start = end;
        }

        emitAtBoundary?.Invoke(start);
    }

    private void EmitGeneralLiftedBody(
        FunctionDefinition function,
        LiftedClosure closure,
        int closureIndex,
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
        Dictionary<string, int> failureOperands
    )
    {
        if (closure.SelfName is null && closure.GuardFailure is null
            && function.Clauses is [{ Guard: null } clause] && HasUnconditionalParameters(clause)
            && (AttributeLiteral.TryNormalizeConstant(clause.Body, out _)
                || clause.Body is Expr.Variable variable && clause.Patterns.OfType<Pattern.Variable>()
                    .Any(parameter => parameter.Name == variable.Name)))
        {
            EmitBody(clause);
            instructions.WriteByte(Return);

            return;
        }

        var location = closure.Location;
        var planner = new BeamModuleCompiler(name, sourceModule: sourceModule) { exportedFunctions = exportedFunctions };
        using var scratch = planner.instructions;
        planner.StartFunction(
            function.Name,
            function.Arity,
            location,
            exported: false
        );
        var probe = new GeneralEmitter(
            planner,
            entries,
            localFunctions,
            location,
            0,
            lexicalOwner: closure.LexicalOwner,
            recursiveClosure: closure,
            recursiveClosureIndex: closureIndex
        );
        probe.Function(function);
        new GeneralEmitter(
            this,
            entries,
            localFunctions,
            location,
            probe.SlotCount,
            planner.nextLabel - 1,
            probe.ListFailureCount,
            closure.LexicalOwner,
            closure,
            closureIndex,
            failureOperands
        ).Function(function);
    }

    private void EmitLiftedBody(LiftedClosure closure, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        Clause body = closure.Body!;
        if (closure.Tail is not { } tail)
        {
            if (ReadRemoteToLocalClosure(body) is { } chain && entries.TryGetValue((chain.Local.Function, 1), out int target))
            {
                Op(Allocate, BeamWire.UnsignedTag, 0);
                Operand(BeamWire.UnsignedTag, 1);
                EmitCallLine(chain.Remote, closure.Location);
                EmitStaticRemoteCall(chain.Remote);
                Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
                Operand(LabelTag, target);
                Operand(BeamWire.UnsignedTag, 0);

                return;
            }

            if (ReadProtectedTermClosure(body.Body, body.Patterns) is { } protectedTerm)
            {
                EmitProtectedTermClosure(protectedTerm, closure.Location, entries);

                return;
            }
            if (ReadFormattingClosure(body.Body, body.Patterns) is { } formatting)
            {
                EmitFormattingClosure(formatting, closure.Location, entries);

                return;
            }
            if (ReadBinaryTextClosure(body.Body, body.Patterns) is { } binaryText)
            {
                EmitBinaryTextClosure(binaryText, closure.Location, entries);

                return;
            }
            if (body.Body is Expr.Call { Module: not null, Arguments: [Expr.Variable, Expr.Literal literal] } call)
            {
                EmitConstant(literal.Value, 1);
                EmitCallLine(call, closure.Location);
                Op(TailExternalCall, BeamWire.UnsignedTag, 2);
                Operand(BeamWire.UnsignedTag, Import(call.Module, call.Function, 2));

                return;
            }

            EmitBody(body);
            instructions.WriteByte(Return);

            return;
        }

        int recurse = nextLabel++;
        Op(ExactEqualTest, LabelTag, recurse);
        Operand(XRegisterTag, 0);
        LiteralOperand(tail.Key);
        var previous = closure.Location;
        int arity = closure.Arity;
        if (tail.AccumulatorLine is not null
            && body.Body is Expr.Binary { Right: Expr.Variable offset } finish)
        {
            EmitArithmeticLine(finish.SourceLine, ref previous);
            GcArithmetic(
                GcBifTwo,
                ErlangOperators.Plus,
                arity,
                0,
                (XRegisterTag, 1, 0),
                (XRegisterTag, ArgumentRegister(body, offset.Name), 0)
            );
        }
        else
        {
            EmitBody(body);
        }
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, recurse);
        EmitArithmeticLine(tail.Line, ref previous);
        int decrementRegister = tail.AccumulatorLine.HasValue ? arity : 0;
        GcArithmetic(
            GcBifTwo,
            ErlangOperators.Minus,
            arity,
            decrementRegister,
            (XRegisterTag, 0, 0),
            (BeamWire.IntegerTag, tail.Step, 0)
        );
        if (tail.AccumulatorLine is int accumulatorLine)
        {
            EmitArithmeticLine(accumulatorLine, ref previous);
            GcArithmetic(
                GcBifTwo,
                ErlangOperators.Plus,
                arity + 1,
                1,
                (XRegisterTag, 1, 0),
                (XRegisterTag, 0, NumberTypeBits)
            );
            MoveToX0(XRegisterTag, decrementRegister);
        }
        Op(TailLocalCall, BeamWire.UnsignedTag, arity);
        Operand(LabelTag, closure.Entry);
    }

    private void EmitClosureTable(Stream output, int oldUnique)
    {
        Word(output, closures.Count);
        for (int index = 0; index < closures.Count; index++)
        {
            var closure = closures[index];
            Word(output, atoms[closure.Name]);
            Word(output, closure.Arity);
            int entry = closure.Entry < 0 ? deferredEntries[closure.Entry] : closure.Entry;
            Word(output, MapEmittedLabel(entry));
            Word(output, index);
            Word(output, closure.CaptureCount);
            Word(output, oldUnique);
        }
    }
}

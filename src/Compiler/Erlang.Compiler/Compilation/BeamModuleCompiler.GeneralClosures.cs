using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private int anonymousFailureOrdinal;
        private IReadOnlyDictionary<string, int>? entryCaptureOrder;

        private bool TryEmitClosureArgumentTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Call
                {
                    Arguments: [Expr.Fun { SelfName: null } closure]
                } call } clause]
                || !HasUnconditionalParameters(clause)
                || !TryResolveOrdinaryCall(call, out string? module, preparedArgument: 0))
            {
                return false;
            }

            var registers = clause.Patterns.Cast<Pattern.Variable>()
                .Select((parameter, register) => (parameter.Name, Register: register))
                .Where(parameter => parameter.Name != VariableScopeNames.Wildcard)
                .ToDictionary(parameter => parameter.Name, parameter => parameter.Register, StringComparer.Ordinal);
            var bindings = registers.ToDictionary(entry => entry.Key, _ => (Term)Nil.Value, StringComparer.Ordinal);
            if (closure.Clauses[0].Patterns.Count + ClosureCaptures.Capture(closure, bindings).Count
                > BeamWire.MaximumArity)
            {
                return false;
            }

            EmitGeneralClosure(closure, captureRegisters: registers);
            EmitRegisterTailTarget(call with { Module = module }, call.Arguments.Count);

            return true;
        }

        private bool TryEmitRegisterCapturedClosure(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Fun { SelfName: null } closure } clause]
                || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var registers = clause.Patterns.Cast<Pattern.Variable>()
                .Select((parameter, register) => (parameter.Name, Register: register))
                .Where(parameter => parameter.Name != VariableScopeNames.Wildcard)
                .ToDictionary(parameter => parameter.Name, parameter => parameter.Register, StringComparer.Ordinal);
            EmitGeneralClosure(closure, captureRegisters: registers);
            owner.instructions.WriteByte(Return);
            return true;
        }

        private void EmitGeneralClosure(
            Expr.Fun function,
            bool emitCapturedFailure = false,
            bool heapChecked = false,
            int destination = 0,
            IReadOnlyDictionary<string, Term>? captureConstants = null,
            string? x0Capture = null,
            int destinationTag = XRegisterTag,
            IReadOnlyDictionary<string, int>? captureRegisters = null
        )
        {
            var bindings = variables.ToDictionary(entry => entry.Key, _ => (Term)Nil.Value, StringComparer.Ordinal);
            foreach (string binding in (captureConstants?.Keys ?? []).Concat(captureRegisters?.Keys ?? []))
            {
                bindings.TryAdd(binding, Nil.Value);
            }

            if (x0Capture is not null)
            {
                bindings.TryAdd(x0Capture, Nil.Value);
            }

            var captures = ClosureCaptures.Capture(function, bindings).Keys.OrderBy(name =>
                entryCaptureOrder?.TryGetValue(name, out int order) == true
                    ? (Local: false, Order: order)
                    : (Local: entryCaptureOrder is not null, Order: captureRegisters?.GetValueOrDefault(name) ?? variables[name])).ToArray();
            int declaredArity = function.Clauses[0].Patterns.Count;
            int arity = declaredArity + captures.Length;
            if (arity > BeamWire.MaximumArity)
            {
                throw new NotSupportedException(UnsupportedExpression(function));
            }

            var lexicalOwner = parentFunction!;
            int ordinal = function.CoreOrdinal ?? anonymousOrdinal++;
            string suffix = function.SelfName is { } self
                ? FormattableString.Invariant($"{self}/{declaredArity}")
                : "fun";
            string name = FormattableString.Invariant($"-{lexicalOwner.Name}/{lexicalOwner.Arity}-{suffix}-{ordinal}-");
            var clauses = function.Clauses.Select(clause => clause with
            {
                Patterns = [.. clause.Patterns, .. captures.Select(capture => new Pattern.Variable(capture))]
            }).ToArray();
            BeamSourceLocation source = function.ClauseLine == 0 ? location : new(function.ClauseFile, function.ClauseLine);
            var lifted = new FunctionDefinition(name, arity, clauses);

            if (!heapChecked)
            {
                int live = captureRegisters is null ? 0
                    : captures.Select(name => captureRegisters[name]).DefaultIfEmpty(-1).Max() + 1;
                owner.EmitClosureAllocation(captures.Length, live, 1);
            }
            owner.Op(MakeFun, BeamWire.UnsignedTag, owner.closures.Count);
            owner.Operand(destinationTag, destination);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, captures.Length);
            foreach (string capture in captures)
            {
                if (captureConstants?.TryGetValue(capture, out Term? constant) == true)
                {
                    owner.LiteralOperand(constant);
                }
                else if (captureRegisters?.TryGetValue(capture, out int register) == true)
                {
                    owner.Operand(XRegisterTag, register);
                }
                else if (capture == x0Capture)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.Operand(YRegisterTag, variables[capture]);
                }
            }
            bool entryPatternFailure = captures.Length != 0 && function.SelfName is null
                && CanRejectClosureTupleAtEntry(function);
            string? failure = null;
            if (entryPatternFailure || emitCapturedFailure && captures.Length != 0
                && !function.Clauses.Any(clause => clause.Guard is null && HasUnconditionalParameters(clause)))
            {
                do
                {
                    failure = FormattableString.Invariant(
                        $"-{lexicalOwner.Name}/{lexicalOwner.Arity}-inlined-{anonymousFailureOrdinal++}-"
                    );
                }
                while (owner.closures.Any(closure => closure.GuardFailure == failure));
            }

            owner.closures.Add(new(
                name,
                null,
                source,
                captures.Length,
                null,
                GuardFailure: failure,
                General: lifted,
                LexicalOwner: lexicalOwner,
                SelfName: function.SelfName,
                EntryPatternFailure: entryPatternFailure
            ));
        }

        private bool TryEmitEntryClosureTail(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                        Value: Expr.Call prefix
                    },
                        Expr.Call tail
                    ]
                })
            {
                return false;
            }

            int argument = Enumerable.Range(0, tail.Arguments.Count)
                .FirstOrDefault(index => tail.Arguments[index] is Expr.Fun, -1);
            if (argument < 0 || tail.Arguments.Count >= FirstReservedXRegister
                || tail.Arguments[argument] is not Expr.Fun { SelfName: null } closure)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(result.Name, StringComparer.Ordinal)
                || ReadEntryCall(prefix, parameters) is not { } first
                || ReadEntryCall(
                    tail,
                    [result.Name],
                    parameters,
                    preparedOperand: argument
                ) is not { } continuation)
            {
                return false;
            }

            var bindings = parameters.Append(result.Name).Where(name => name != VariableScopeNames.Wildcard)
                .ToDictionary(name => name, _ => (Term)Nil.Value, StringComparer.Ordinal);
            var captures = ClosureCaptures.Capture(closure, bindings).Keys.ToHashSet(StringComparer.Ordinal);
            if (closure.Clauses[0].Patterns.Count + captures.Count > BeamWire.MaximumArity)
            {
                return false;
            }

            var constants = parentFunction is { Clauses: [var original] } function && ReferenceEquals(original, clause)
                ? ReadPrivateBranchConstants(function, parameters, captures) : null;
            var loads = continuation.SavedInputs ?? [];
            int[] saved = Enumerable.Range(0, parameters.Length).Reverse()
                .Where(index => loads.Any(load => load.Source == index)
                    || captures.Contains(parameters[index]) && constants?.ContainsKey(parameters[index]) != true)
                .ToArray();
            var slots = new Dictionary<int, int>();
            int live = Math.Max(EntryCallLiveCount(first, parameters), saved.DefaultIfEmpty(-1).Max() + 1);
            EmitFrameAllocation(live, saved.Length);
            var previousVariables = variables;
            var previousOrder = entryCaptureOrder;
            variables = new(StringComparer.Ordinal);
            entryCaptureOrder = parameters.Append(result.Name).Select((name, index) => (name, index))
                .Where(parameter => captures.Contains(parameter.name))
                .ToDictionary(parameter => parameter.name, parameter => parameter.index, StringComparer.Ordinal);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                int input = saved[slot];
                slots.Add(input, slot);
                variables.Add(parameters[input], slot);
                owner.MoveRegister(
                    XRegisterTag,
                    input,
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(first);
            bool capturesResult = captures.Contains(result.Name);
            bool keepsResult = capturesResult || tail.Arguments.OfType<Expr.Variable>().Any(variable => variable.Name == result.Name);
            int destination = argument == 0 && keepsResult ? 1 : argument;
            owner.EmitClosureAllocation(captures.Count, keepsResult ? 1 : 0, 1);
            EmitGeneralClosure(
                closure,
                heapChecked: true,
                destination: destination,
                captureConstants: constants,
                x0Capture: capturesResult ? result.Name : null
            );
            var moves = destination == argument ? continuation.Moves : [.. continuation.Moves, (argument, destination)];
            owner.EmitOrderedTailMoves(moves, continuation.Constants,
                loads.Select(load => (load.Destination, slots[load.Source])).ToList());
            EmitPreparedFrameTail(continuation, CallLocation(prefix, location), saved.Length);
            variables = previousVariables;
            entryCaptureOrder = previousOrder;

            return true;
        }

        private static bool CanRejectClosureTupleAtEntry(Expr.Fun function)
        {
            if (function.Clauses is not [{ Guard: null } clause]
                || clause.Patterns.Count(pattern => pattern is Pattern.Tuple) != 1
                || !clause.Patterns.All(pattern => pattern is Pattern.Variable { }
                    || pattern is Pattern.Tuple tuple
                    && tuple.Items.Skip(HasTupleTag(tuple, out _) ? 1 : 0).All(item => item is Pattern.Variable)))
            {
                return false;
            }

            var bindings = clause.Patterns.SelectMany(Semantics.Variables).ToArray();

            return bindings.Distinct(StringComparer.Ordinal).Count() == bindings.Length;
        }

        private bool TryEmitEmptyListClosureCallChain(FunctionDefinition function, Clause clause, int failure)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [], Tail: null }, Pattern.Variable parameter],
                    Body: Expr.Call { Arguments: [Expr.Call { Arguments: [Expr.Fun closure, Expr.Variable input] } first] } tail
                }
                || parameter.Name == VariableScopeNames.Wildcard || input.Name != parameter.Name
                || !TryResolveOrdinaryCall(first, out string? firstModule, preparedArgument: 0)
                || !TryResolveOrdinaryCall(tail, out string? tailModule, preparedArgument: 0)
                || ClosureCaptures.Capture(closure,
                    new Dictionary<string, Term>(StringComparer.Ordinal) { [parameter.Name] = Nil.Value }).Count != 0)
            {
                return false;
            }

            if (!HasProperListEntry(function, 0))
            {
                owner.Op(IsNil, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
            }
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
            owner.EmitClosureHeap(0, 2, 1);
            variables = new(StringComparer.Ordinal);
            EmitGeneralClosure(closure, heapChecked: true);
            owner.EmitCallLine(first, location);
            Call(firstModule, first.Function, 2);
            EmitPreparedFrameTail(new EntryCall(
                tail,
                tailModule,
                [],
                []
            ), CallLocation(first, location), 0);

            return true;
        }

        private int selfSlot = -1;
        private int selfEntry;
        private int[] selfCaptures = [];

        private void PrepareClosureSelf(int[] arguments, int entry)
        {
            if (recursiveClosure?.SelfName is null)
            {
                return;
            }

            selfEntry = entry;
            selfCaptures = arguments[^recursiveClosure.CaptureCount..];
            owner.EmitClosureAllocation(selfCaptures.Length, 0, 1);
            owner.Op(MakeFun, BeamWire.UnsignedTag, recursiveClosureIndex);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, selfCaptures.Length);
            foreach (int capture in selfCaptures)
            {
                owner.Operand(YRegisterTag, capture);
            }

            selfSlot = Save();
        }

        private bool IsClosureSelfCall(Expr.Apply apply) =>
            recursiveClosure?.SelfName is { } self
            && apply.Function is Expr.Variable variable && variable.Name == self
            && apply.Arguments.Count == recursiveClosure.Arity - recursiveClosure.CaptureCount
            && variables.TryGetValue(self, out int slot) && slot == selfSlot;

        private void EmitClosureSelfCall(Expr.Apply apply, bool tail)
        {
            int operandBase = nextSlot;
            LoadCallArguments(apply.Arguments, null);
            for (int index = 0; index < selfCaptures.Length; index++)
            {
                Load(selfCaptures[index], apply.Arguments.Count + index);
            }

            if (!tail)
            {
                var previous = location;
                owner.EmitArithmeticLine(apply.SourceLine, ref previous);
            }
            owner.Op(tail ? TailLocalCallWithFrame : LocalCall, BeamWire.UnsignedTag, apply.Arguments.Count + selfCaptures.Length);
            owner.Operand(LabelTag, selfEntry);
            ReleaseOperandSlots(operandBase);
            if (tail)
            {
                owner.Operand(BeamWire.UnsignedTag, frameSize);
            }
        }
    }
}

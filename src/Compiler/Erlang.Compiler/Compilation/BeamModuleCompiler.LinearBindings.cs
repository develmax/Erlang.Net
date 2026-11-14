using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool linearBindings;
        private HashSet<string>? linearCaseLive;
        private HashSet<Expr>? linearCaseBranches;

        private bool TryEmitListEntryBifTuple(Expr.Sequence body, IReadOnlyDictionary<string, int> incoming)
        {
            if (body.Items is not [
                Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call { Arguments: [Expr.Variable argument] } call },
                Expr.Tuple tuple
            ] || result.Name == VariableScopeNames.Wildcard || incoming.ContainsKey(result.Name)
                || !incoming.TryGetValue(argument.Name, out int source)
                || call is not { Module: null or ErlangModuleNames.Module, DynamicRemote: false }
                || !owner.ResolvesErlangBif(call) || ReadUnaryBifOpcode(call.Function) != GcBifOne)
            {
                return false;
            }

            var fields = new (int Register, Term? Constant)[tuple.Items.Count];
            int tupleLive = 1;
            bool usesResult = false;
            for (int index = 0; index < fields.Length; index++)
            {
                Expr item = tuple.Items[index];
                if (item is Expr.Variable variable && variable.Name == result.Name)
                {
                    fields[index] = (0, null);
                    usesResult = true;
                }
                else if (item is Expr.Variable input && incoming.TryGetValue(input.Name, out int register))
                {
                    fields[index] = (register, null);
                    tupleLive = Math.Max(tupleLive, register + 1);
                }
                else if (AttributeLiteral.TryNormalizeConstant(item, out Term literal))
                {
                    fields[index] = (-1, literal);
                }
                else
                {
                    return false;
                }
            }

            if (!usesResult)
            {
                return false;
            }

            owner.EmitCallLine(call, location);
            owner.Op(GcBifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, Math.Max(tupleLive, source + 1));
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 1));
            owner.Operand(XRegisterTag, source);
            owner.Operand(XRegisterTag, 0);

            owner.Op(TestHeap, BeamWire.UnsignedTag, fields.Length + 1);
            owner.Operand(BeamWire.UnsignedTag, tupleLive);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Length);
            foreach (var (register, constant) in fields)
            {
                if (constant is not null)
                {
                    owner.LiteralOperand(constant);
                }
                else
                {
                    owner.Operand(XRegisterTag, register);
                }
            }

            owner.instructions.WriteByte(Return);
            return true;
        }

        private bool TryEmitLinearBindings(Expr.Sequence body, IReadOnlyDictionary<string, int> incoming)
        {
            var bindings = new List<(string Name, Expr Value)>();
            var names = variables.Keys.ToHashSet(StringComparer.Ordinal);
            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            foreach (Expr item in body.Items.SkipLast(1))
            {
                if (item is not Expr.Match { Pattern: Pattern.Variable binding, Value: var value }
                    || binding.Name == VariableScopeNames.Wildcard || !names.Add(binding.Name)
                    || !HasLinearReads(value))
                {
                    return false;
                }

                if (AttributeLiteral.TryNormalizeConstant(value, out Term constant, constants))
                {
                    constants.Add(binding.Name, constant);
                }
                else
                {
                    bindings.Add((binding.Name, SubstituteConstants(value, constants)));
                }
            }

            Expr result = body.Items[^1];
            if (!HasLinearReads(result))
            {
                return false;
            }

            result = SubstituteConstants(result, constants);

            var lastReads = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index <= bindings.Count; index++)
            {
                Expr expression = index == bindings.Count ? result : bindings[index].Value;
                foreach (Expr.Variable read in LocalFunctionEntries.Walk(expression).OfType<Expr.Variable>())
                {
                    lastReads[read.Name] = index;
                }
            }

            linearBindings = true;
            foreach (string unused in variables.Keys.Where(name => !lastReads.ContainsKey(name)).ToArray())
            {
                variables.Remove(unused);
            }

            bool forwardResult = false;
            string? lastArgumentRead = null;
            for (int index = 0; index < bindings.Count; index++)
            {
                var (binding, value) = bindings[index];
                nextSlot = variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1;
                if (lastArgumentRead is not null)
                {
                    EmitLinearCallReusingArgumentSlot((Expr.Call)value, lastArgumentRead);
                    lastArgumentRead = null;
                }
                else if (forwardResult)
                {
                    EmitForwardedLinearCall((Expr.Call)value, bindings[index - 1].Name);
                    forwardResult = false;
                }
                else if (index != 0 || value is not Expr.Call call || !TryEmitFirstListCall(call, incoming))
                {
                    if (value is Expr.Case decision)
                    {
                        linearCaseLive = lastReads.Where(read => read.Value > index)
                            .Select(read => read.Key).ToHashSet(StringComparer.Ordinal);
                        linearCaseBranches = decision.Clauses.Select(clause => clause.Body).ToHashSet<Expr>(ReferenceEqualityComparer.Instance);
                    }
                    Expression(value);
                    linearCaseLive = null;
                    linearCaseBranches = null;
                }

                foreach (string expired in variables.Keys.Where(name => lastReads[name] == index).ToArray())
                {
                    variables.Remove(expired);
                }

                if (index == bindings.Count - 1 && TryEmitLinearReturnTuple(result, binding))
                {
                    return true;
                }

                if (lastReads.ContainsKey(binding))
                {
                    lastArgumentRead = index + 1 < bindings.Count && lastReads[binding] > index + 1
                        ? LastReadArgument(bindings[index + 1].Value, index + 1, lastReads)
                        : null;
                    if (lastArgumentRead is not null)
                    {
                        variables.Add(binding, variables[lastArgumentRead]);
                        continue;
                    }

                    int slot = 0;
                    while (variables.ContainsValue(DecoderSlot(slot)))
                    {
                        slot++;
                    }

                    variables.Add(binding, DecoderSlot(slot));
                    peakSlot = Math.Max(peakSlot, slot + 1);
                    forwardResult = index + 1 < bindings.Count
                        && CanForwardLinearResult(bindings[index + 1].Value, binding);
                    if (!forwardResult)
                    {
                        owner.MoveRegister(XRegisterTag, 0, YRegisterTag, DecoderSlot(slot));
                    }
                }
            }

            nextSlot = variables.Values.Select(DecoderSlot).DefaultIfEmpty(-1).Max() + 1;
            Expression(result);

            return true;
        }

        private string? LastReadArgument(Expr expression, int index, IReadOnlyDictionary<string, int> lastReads) =>
            expression is Expr.Call
            {
                Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                Arguments: [Expr.Literal, Expr.Variable argument, Expr.Literal]
            } && variables.ContainsKey(argument.Name) && lastReads[argument.Name] == index
                ? argument.Name
                : null;

        private void EmitLinearCallReusingArgumentSlot(Expr.Call call, string lastRead)
        {
            int slot = variables[lastRead];
            Load(slot, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, slot);
            owner.EmitConstant(call.Arguments[2], 2);
            owner.EmitConstant(call.Arguments[0], 0);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 3);
        }
        private bool CanForwardLinearResult(Expr expression, string result) =>
            expression is Expr.Call
            {
                Module: null, DynamicRemote: false,
                Arguments: [Expr.Variable first, Expr.Variable second, Expr.Variable last]
            } call
            && last.Name == result && first.Name != result && second.Name != result
            && variables.ContainsKey(first.Name) && variables.ContainsKey(second.Name)
            && entries.ContainsKey((call.Function, 3))
            || expression is Expr.Call
            {
                Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                Arguments: [Expr.Literal, Expr.Variable a, Expr.Variable b, Expr.Variable c]
            } && a.Name != result && b.Name != result && c.Name != result
                && variables.ContainsKey(a.Name) && variables.ContainsKey(b.Name) && variables.ContainsKey(c.Name);

        private void EmitForwardedLinearCall(Expr.Call call, string result)
        {
            if (call.Module is null)
            {
                // Stage the first argument in the last register while X0 still holds the previous result.
                Load(variables[((Expr.Variable)call.Arguments[0]).Name], 2);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, variables[result]);
                Load(variables[((Expr.Variable)call.Arguments[1]).Name], 1);
                owner.Op(Swap, XRegisterTag, 0);
                owner.Operand(XRegisterTag, 2);
            }
            else
            {
                // The first two independent moves leave X0 available for the spill.
                Load(variables[((Expr.Variable)call.Arguments[2]).Name], 2);
                Load(variables[((Expr.Variable)call.Arguments[1]).Name], 1);
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, variables[result]);
                Load(variables[((Expr.Variable)call.Arguments[3]).Name], 3);
                owner.EmitConstant(call.Arguments[0], 0);
            }

            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, call.Arguments.Count);
        }

        private bool TryEmitFirstListCall(Expr.Call call, IReadOnlyDictionary<string, int> incoming)
        {
            if (call.Module is null or ErlangModuleNames.Module
                || call.Arguments.Any(argument => argument is not Expr.Variable
                    && !AttributeLiteral.TryNormalizeConstant(argument, out _)))
            {
                return false;
            }

            int arity = call.Arguments.Count;
            foreach (int index in IndependentCallMoveOrder(arity))
            {
                Expr argument = call.Arguments[index];
                if (argument is Expr.Variable variable)
                {
                    if (incoming[variable.Name] >= arity)
                    {
                        // Argument writes cannot overwrite registers outside the destination range.
                        owner.MoveRegister(XRegisterTag, incoming[variable.Name], XRegisterTag, index);
                    }
                    else
                    {
                        Load(variables[variable.Name], index);
                    }
                }
                else
                {
                    owner.EmitConstant(argument, index);
                }
            }

            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, arity);

            return true;
        }
        private bool TryEmitLinearCallListTuple(Expr.Tuple tuple)
        {
            if (!linearBindings || tuple.Items is not [Expr.Literal { Value: Atom tag },
                    Expr.Literal { Value: Atom member }, Expr.List { Tail: null } list])
            {
                return false;
            }

            int callIndex = -1;
            for (int index = 0; index < list.Items.Count; index++)
            {
                if (list.Items[index] is Expr.Call && callIndex < 0)
                {
                    callIndex = index;
                }
                else if (list.Items[index] is not Expr.Variable variable || !variables.ContainsKey(variable.Name))
                {
                    return false;
                }
            }

            if (callIndex < 0)
            {
                return false;
            }

            var call = (Expr.Call)list.Items[callIndex];
            if (linearCaseBranches?.Contains(tuple) != true || !TryEmitLinearCaseListCall(call, list))
            {
                Expression(call);
            }
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4 + ConsHeapWords * list.Items.Count);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = list.Items.Count - 1; index >= 0; index--)
            {
                int destination = index > callIndex ? 1 : 0;
                if (index == callIndex)
                {
                    owner.Op(PutList, XRegisterTag, 0);
                }
                else
                {
                    var variable = (Expr.Variable)list.Items[index];
                    owner.Op(PutList, YRegisterTag, variables[variable.Name]);
                }

                if (index == list.Items.Count - 1)
                {
                    owner.Operand(AtomTag, NilAtomIndex);
                }
                else
                {
                    owner.Operand(XRegisterTag, index >= callIndex ? 1 : 0);
                }
                owner.Operand(XRegisterTag, destination);
            }

            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (AtomTag, owner.Intern(member.Name)), (XRegisterTag, 0));

            return true;
        }
        private bool TryEmitLinearCaseListCall(Expr.Call call, Expr.List list)
        {
            if (linearCaseLive is null || call.Module is null or ErlangModuleNames.Module
                || call.Arguments is not [Expr.Literal first, ..]
                || call.Arguments.Skip(1).Any(argument => argument is not Expr.Variable variable
                    || !variables.ContainsKey(variable.Name)))
            {
                return false;
            }

            for (int index = call.Arguments.Count - 1; index > 0; index--)
            {
                Load(variables[((Expr.Variable)call.Arguments[index]).Name], index);
            }

            var live = list.Items.OfType<Expr.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            live.UnionWith(linearCaseLive);
            int[] dead = variables.Where(binding => !live.Contains(binding.Key)).Select(binding => binding.Value).Order().ToArray();
            if (dead.Length > 0)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, dead.Length);
                foreach (int slot in dead)
                {
                    owner.Operand(YRegisterTag, slot);
                }
            }

            owner.EmitConstant(first.Value, 0);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, call.Arguments.Count);

            return true;
        }

        private bool TryEmitLinearReturnTuple(Expr expression, string result)
        {
            if (expression is not Expr.Tuple tuple
                || !tuple.Items.OfType<Expr.Variable>().Any(variable => variable.Name == result)
                || tuple.Items.Any(item => item is Expr.Variable variable
                    ? variable.Name != result && !variables.ContainsKey(variable.Name)
                    : !AttributeLiteral.TryNormalizeConstant(item, out _)))
            {
                return false;
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, tuple.Items.Count);
            foreach (Expr item in tuple.Items)
            {
                if (item is Expr.Variable variable && variable.Name == result)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    GuardOperand(item, GuardSlot(item, null));
                }
            }

            return true;
        }
        private static Expr SubstituteConstants(Expr expression, IReadOnlyDictionary<string, Term> constants) => expression switch
        {
            Expr.Variable variable when constants.TryGetValue(variable.Name, out var value) => new Expr.Literal(value),
            Expr.Tuple tuple => tuple with { Items = tuple.Items.Select(item => SubstituteConstants(item, constants)).ToArray() },
            Expr.List list => list with
            {
                Items = list.Items.Select(item => SubstituteConstants(item, constants)).ToArray(),
                Tail = list.Tail is null ? null : SubstituteConstants(list.Tail, constants)
            },
            Expr.Call call => call with { Arguments = call.Arguments.Select(item => SubstituteConstants(item, constants)).ToArray() },
            Expr.Case decision => decision with
            {
                Value = SubstituteConstants(decision.Value, constants),
                Clauses = decision.Clauses.Select(clause => clause with { Body = SubstituteConstants(clause.Body, constants) }).ToArray()
            },
            _ => expression
        };

        private FunctionDefinition FoldEntryResultCase(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Sequence sequence } clause]
                || !HasUnconditionalParameters(clause)
                || sequence.Items is not [
                    Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding, Value: Expr.Call call },
                    var second, ..])
            {
                return function;
            }

            var assignment = second as Expr.Match;
            if ((assignment?.Value ?? second) is not Expr.Case { Value: Expr.Variable selector } decision
                || selector.Name != binding.Name || assignment is not null && assignment.Pattern is not Pattern.Variable { })
            {
                return function;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || ReadEntryCall(call, parameters, allowConstructedArgument: true) is null)
            {
                return function;
            }

            var withoutSelector = decision with { Value = new Expr.Literal(Nil.Value) };
            Expr[] remainder = sequence.Items.Skip(1).ToArray();
            remainder[0] = assignment is null ? withoutSelector : assignment with { Value = withoutSelector };
            if (ReadUsedEntryParameters(new Clause([binding], null, sequence with { Items = remainder })) is not { } uses)
            {
                return function;
            }

            if (uses.Count != 0)
            {
                if (assignment?.Pattern is Pattern.Variable target && target.Name == binding.Name
                    || ReadUsedEntryParameters(new Clause([binding], null,
                        sequence with { Items = remainder.Skip(1).ToArray() })) is not { Count: 0 }
                    || BindEntryCaseSelector(decision, binding) is not { } bound)
                {
                    return function;
                }

                decision = bound;
            }

            var prepared = decision with { Value = call };
            remainder[0] = assignment is null ? prepared : assignment with { Value = prepared };
            Expr body = remainder.Length == 1 ? remainder[0] : sequence with { Items = remainder };
            return function with { Clauses = [clause with { Body = body }] };
        }

        private Expr.Case? BindEntryCaseSelector(Expr.Case decision, Pattern.Variable binding)
        {
            var clauses = new List<Clause>(decision.Clauses.Count);
            foreach (Clause clause in decision.Clauses)
            {
                if (!CanTestCasePatternInX0(clause) || Semantics.Variables(clause.Patterns[0]).Contains(binding.Name))
                {
                    return null;
                }

                if (ClosureCaptures.ReadEntryReferences(clause, [binding.Name]).Count == 0)
                {
                    clauses.Add(clause);
                    continue;
                }

                Pattern pattern = clause.Patterns[0];
                Pattern? bound = pattern switch
                {
                    Pattern.Tuple { } => new AliasPattern(binding, pattern),
                    Pattern.Variable { Name: VariableScopeNames.Wildcard } => binding,
                    _ => null
                };
                if (bound is null)
                {
                    return null;
                }

                clauses.Add(clause with { Patterns = [bound] });
            }

            return decision with { Clauses = clauses };
        }

        private static FunctionDefinition FoldLeadingCaseConstants(FunctionDefinition function)
        {
            if (function is not { Arity: 0, Clauses: [{ Guard: null, Body: Expr.Sequence sequence } clause] }
                || sequence.Items is not [Expr.Match { Pattern: Pattern.Variable binding, Value: var value }, ..]
                || binding.Name == VariableScopeNames.Wildcard
                || !AttributeLiteral.TryNormalizeConstant(value, out Term constant))
            {
                return function;
            }

            var remaining = sequence.Items.Skip(1).ToArray();
            if (!remaining.OfType<Expr.Match>().Any(match => match.Value is Expr.Case)
                || !remaining.All(item => item is Expr.Match match
                    ? match.Pattern is Pattern.Variable target && target.Name != binding.Name
                        && HasLinearReads(match.Value, allowFlatTuplePatterns: true)
                    : HasLinearReads(item, allowFlatTuplePatterns: true)))
            {
                return function;
            }

            var constants = new Dictionary<string, Term>(StringComparer.Ordinal) { [binding.Name] = constant };
            return function with
            {
                Clauses = [clause with
                {
                    Body = sequence with
                    {
                        Items = remaining.Select(item => item is Expr.Match match
                            ? match with { Value = SubstituteConstants(match.Value, constants) }
                            : SubstituteConstants(item, constants)).ToArray()
                    }
                }]
            };
        }

        private static bool HasLinearReads(Expr expression, bool allowFlatTuplePatterns = false) =>
            LocalFunctionEntries.Walk(expression).All(item => item switch
        {
            Expr.Literal or Expr.Variable or Expr.Tuple or Expr.List or Expr.Call { DynamicRemote: false } => true,
            Expr.Case decision => decision.Clauses.All(clause =>
                clause.Guard is null && clause.Patterns is [var pattern]
                && (pattern is Pattern.Literal { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    || allowFlatTuplePatterns && pattern is Pattern.Tuple tuple
                        && tuple.Items.All(field => field is Pattern.Literal { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }))),
            _ => false
        });
    }
}

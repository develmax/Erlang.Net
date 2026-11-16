using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGroupedArityDispatch(FunctionDefinition function)
        {
            if (function.Arity == 2)
            {
                return TryEmitAtomArityDispatch(function);
            }

            if (function.Clauses is not [
                ..,
                {
                    Guard: null, Patterns: [
                    Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ], Body: Expr.Literal { Value: Atom { Name: "false" } }
                }
            ])
            {
                return false;
            }

            var modules = new SortedDictionary<string, SortedDictionary<string, SortedSet<int>>>(StringComparer.Ordinal);
            var seen = new HashSet<(string Module, string Name, int Arity)>();
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [
                            Pattern.Literal { Value: Atom moduleKey },
                            Pattern.Literal { Value: Atom nameKey },
                            Pattern.Literal { Value: Integer arity }
                        ],
                        Body: Expr.Literal { Value: Atom { Name: "true" or "false" } result }
                    }
                    || arity.Value < 0 || arity.Value > BeamWire.MaximumArity)
                {
                    return false;
                }
                if (!seen.Add((moduleKey.Name, nameKey.Name, (int)arity.Value)) || result.Name == "false")
                {
                    continue;
                }
                if (!modules.TryGetValue(moduleKey.Name, out var names))
                {
                    names = new(StringComparer.Ordinal);
                    modules.Add(moduleKey.Name, names);
                }
                if (!names.TryGetValue(nameKey.Name, out var arities))
                {
                    arities = [];
                    names.Add(nameKey.Name, arities);
                }
                arities.Add((int)arity.Value);
            }
            if (modules.Count == 0 || modules.Values.Any(names => names.Count < 2))
            {
                return false;
            }

            // OTP emits module and name blocks in descending order and keeps the last copy of a shared block.
            var anchors = new Dictionary<string, (string Module, string Name, SortedSet<int> Arities)>(StringComparer.Ordinal);
            foreach (var (module, names) in modules)
            {
                foreach (var (name, arities) in names)
                {
                    anchors.TryAdd(ArityKey(arities), (module, name, arities));
                }
            }

            var moduleLabels = new Dictionary<string, int>(StringComparer.Ordinal);
            var arityLabels = new Dictionary<string, int>(StringComparer.Ordinal);
            int multiRemaining = anchors.Values.Count(anchor => anchor.Arities.Count > 1);
            int success = 0;
            var schedule = new List<(string Module, string? Key)>();
            foreach (var (module, names) in modules.Reverse())
            {
                moduleLabels.Add(module, modules.Count == 1 ? 0 : Label());
                schedule.Add((module, null));
                foreach (var (name, arities) in names.Reverse())
                {
                    string key = ArityKey(arities);
                    var anchor = anchors[key];
                    if (anchor.Module != module || anchor.Name != name)
                    {
                        continue;
                    }
                    arityLabels.Add(key, Label());
                    schedule.Add((module, key));
                    if (arities.Count > 1 && --multiRemaining == 0)
                    {
                        success = Label();
                    }
                }
            }
            int failure = Label();
            if (modules.Count == 1)
            {
                owner.EmitAtomTest(0, modules.First().Key, failure);
            }
            else
            {
                EmitAritySelect(0, modules.Count, failure);
                foreach (string module in modules.Keys)
                {
                    owner.Operand(AtomTag, owner.Intern(module));
                    owner.Operand(LabelTag, moduleLabels[module]);
                }
            }

            foreach (var (module, key) in schedule)
            {
                if (key is null)
                {
                    if (modules.Count > 1)
                    {
                        Mark(moduleLabels[module]);
                    }
                    var names = modules[module];
                    EmitAritySelect(1, names.Count, failure);
                    foreach (var (name, arities) in names)
                    {
                        owner.Operand(AtomTag, owner.Intern(name));
                        owner.Operand(LabelTag, arityLabels[ArityKey(arities)]);
                    }
                    continue;
                }

                int label = arityLabels[key];
                Mark(label);
                var values = anchors[key].Arities;
                if (values.Count == 1)
                {
                    EmitArityBooleanTest(2, values.Min);
                    continue;
                }

                EmitAritySelect(2, values.Count, failure);
                foreach (int arity in values)
                {
                    owner.Operand(BeamWire.IntegerTag, arity);
                    owner.Operand(LabelTag, success);
                }
                if (label + 1 == success)
                {
                    Mark(success);
                    owner.EmitConstant(Term.A("true"), 0);
                    owner.instructions.WriteByte(Return);
                }
            }

            Mark(failure);
            owner.EmitConstant(Term.A("false"), 0);
            owner.instructions.WriteByte(Return);

            return true;

            static string ArityKey(IEnumerable<int> arities) => string.Join(",", arities);
        }

        private bool TryEmitAtomArityDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [_, _, ..])
            {
                return false;
            }

            Clause fallback = function.Clauses[^1];
            Expr.Call? tail = null;
            bool guardedFalse = false;
            if (fallback.Patterns is [Pattern.Variable name, Pattern.Variable arity]
                && name.Name != VariableScopeNames.Wildcard && arity.Name != VariableScopeNames.Wildcard
                && name.Name != arity.Name)
            {
                tail = fallback is
                {
                    Guard: null,
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable first, Expr.Variable second] } call
                }
                    && first.Name == name.Name && second.Name == arity.Name && entries.ContainsKey((call.Function, 2))
                    ? call
                    : null;
                guardedFalse = fallback is
                {
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.AndAlso,
                        Left: Expr.Call atomGuard,
                        Right: Expr.Call integerGuard
                    },
                    Body: Expr.Literal { Value: Atom { Name: "false" } }
                }
                    && MatchesDispatchGuard(atomGuard, IsAtom, name.Name)
                    && MatchesDispatchGuard(integerGuard, IsInteger, arity.Name);
            }

            bool booleanDefault = fallback is
            {
                Guard: null,
                Patterns: [Pattern.Variable firstDefault, Pattern.Variable secondDefault],
                Body: Expr.Literal { Value: Atom { Name: "false" } }
            } && (firstDefault.Name == VariableScopeNames.Wildcard || firstDefault.Name != secondDefault.Name);
            bool hasFallback = tail is not null || guardedFalse || booleanDefault;
            var keys = new SortedDictionary<Atom, (Atom Result, SortedSet<int> Arities)>(Comparer<Atom>.Create(
                (left, right) => TermOrder.Compare(left, right, exact: true)
            ));
            foreach (Clause clause in function.Clauses.SkipLast(hasFallback ? 1 : 0))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Literal { Value: Integer value }],
                        Body: Expr.Literal { Value: Atom result }
                    }
                    || (guardedFalse || booleanDefault) && result.Name != "true"
                    || value.Value < 0 || value.Value > BeamWire.MaximumArity)
                {
                    return false;
                }

                if (!keys.TryGetValue(key, out var entry))
                {
                    entry = (result, []);
                    keys.Add(key, entry);
                }
                else if (!entry.Result.Equals(result))
                {
                    return false;
                }

                entry.Arities.Add((int)value.Value);
            }

            // Blocks are laid out in reverse atom order. Equivalent arity tests keep the final block.
            var groups = keys.GroupBy(key => (key.Value.Result, Arities: string.Join(",", key.Value.Arities)))
                .Reverse().ToArray();
            var results = groups.Where(group => !booleanDefault || group.First().Value.Arities.Count > 1)
                .GroupBy(group => group.Key.Result)
                .ToDictionary(result => result.Key, result => (Last: result.Last(), Count: result.Count()));
            int functionInfo = owner.nextLabel - 2;
            var labels = new Dictionary<(Atom Result, string Arities), int>();
            var successLabels = new Dictionary<Atom, int>();
            foreach (var group in groups)
            {
                if (keys.Count > 1)
                {
                    labels.Add(group.Key, Label());
                }

                if (results.TryGetValue(group.Key.Result, out var result)
                    && group == result.Last && (result.Count > 1 || group.First().Value.Arities.Count > 1))
                {
                    successLabels.Add(group.Key.Result, Label());
                }
            }

            int failure = hasFallback ? Label() : functionInfo;
            if (keys.Count == 1)
            {
                owner.EmitAtomTest(0, keys.First().Key.Name, failure);
            }
            else
            {
                EmitAritySelect(0, keys.Count, failure);
                foreach (var (key, entry) in keys)
                {
                    owner.Operand(AtomTag, owner.Intern(key.Name));
                    owner.Operand(LabelTag, labels[(entry.Result, string.Join(",", entry.Arities))]);
                }
            }

            foreach (var group in groups)
            {
                if (keys.Count > 1)
                {
                    Mark(labels[group.Key]);
                }

                var values = group.First().Value.Arities;
                if (booleanDefault && values.Count == 1)
                {
                    EmitArityBooleanTest(1, values.Min);
                    continue;
                }

                Atom result = group.Key.Result;
                bool returnsHere = group == results[result].Last;
                int success = successLabels.GetValueOrDefault(result);
                if (values.Count == 1)
                {
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, 1);
                    owner.Operand(BeamWire.IntegerTag, values.Min);
                    if (!returnsHere)
                    {
                        JumpTo(success);
                    }
                }
                else
                {
                    EmitAritySelect(1, values.Count, failure);
                    foreach (int value in values)
                    {
                        owner.Operand(BeamWire.IntegerTag, value);
                        owner.Operand(LabelTag, success);
                    }
                }

                if (returnsHere)
                {
                    if (success != 0)
                    {
                        Mark(success);
                    }

                    owner.EmitConstant(result, 0);
                    owner.instructions.WriteByte(Return);
                }
            }

            if (!hasFallback)
            {
                return true;
            }

            Mark(failure);
            if (tail is not null)
            {
                EmitRegisterTailTarget(tail, 2);
            }
            else
            {
                if (guardedFalse)
                {
                    owner.Op(IsAtom, LabelTag, functionInfo);
                    owner.Operand(XRegisterTag, 0);
                    owner.Op(IsInteger, LabelTag, functionInfo);
                    owner.Operand(XRegisterTag, 1);
                }

                owner.EmitConstant(Term.A("false"), 0);
                owner.instructions.WriteByte(Return);
            }

            return true;
        }

        private bool MatchesDispatchGuard(Expr.Call call, byte opcode, string variable) =>
            call is { DynamicRemote: false, Arguments: [Expr.Variable argument] }
            && argument.Name == variable && owner.ResolvesErlangBif(call) && TypeGuardOpcode(call) == opcode;

        private void EmitAritySelect(int register, int count, int failure)
        {
            owner.Op(SelectValue, XRegisterTag, register);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, count * 2);
        }

        private void EmitArityBooleanTest(int register, int value)
        {
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
            owner.Operand(XRegisterTag, register);
            owner.Operand(BeamWire.IntegerTag, value);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
        }
    }
}

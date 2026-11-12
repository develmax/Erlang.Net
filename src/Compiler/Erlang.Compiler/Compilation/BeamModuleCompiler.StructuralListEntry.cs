using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitStructuralListEntry(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses.Count < 2 || function.Arity + 2 >= FirstReservedXRegister)
            {
                return false;
            }

            for (int input = 0; input < function.Arity; input++)
            {
                int leadingNil = function.Clauses.TakeWhile(clause => ListEntryTest(clause.Patterns[input]) == IsNil).Count();
                var prefix = function.Clauses.Skip(leadingNil).TakeWhile(clause =>
                    clause.Patterns[input] is Pattern.List { Items.Count: 1 }).ToArray();
                if (!prefix.Any(clause => EntryShape(((Pattern.List)clause.Patterns[input]).Items[0]) is Pattern.Tuple))
                {
                    continue;
                }

                // Empty and nonempty inputs are disjoint. All other clauses keep their relative order.
                Clause[] ordered = [.. prefix, .. function.Clauses.Take(leadingNil),
                    .. function.Clauses.Skip(leadingNil + prefix.Length)];
                if (TryEmitStructuralListBranches(function.Arity, input, ordered, prefix.Length, functionInfo))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryEmitStructuralListBranches(int arity, int listInput, Clause[] clauses, int consCount, int functionInfo)
        {
            int head = arity;
            int tail = arity + 1;
            var branches = new List<(Clause Clause, Action<int>[] Tests, (Pattern.Variable Variable, int Register)[] Used,
                EntryCall? Tail, int ResultRegister, Term? Constant)>();

            for (int branch = 0; branch < clauses.Length; branch++)
            {
                Clause clause = clauses[branch];
                var tests = new List<Action<int>>();
                var bindings = new Dictionary<string, int>(StringComparer.Ordinal);
                int nextRegister = branch < consCount ? arity + 2 : arity;

                for (int index = 0; index < arity; index++)
                {
                    if (branch < consCount && index == listInput)
                    {
                        var list = (Pattern.List)clause.Patterns[index];
                        if (!ReadPattern(list.Items[0], head) || !ReadPattern(list.Tail ?? new Pattern.List([], null), tail))
                        {
                            return false;
                        }
                    }
                    else if (!ReadPattern(clause.Patterns[index], index))
                    {
                        return false;
                    }
                }

                var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, nextRegister).ToArray();
                foreach (var (name, register) in bindings)
                {
                    if (parameters[register] != VariableScopeNames.Wildcard)
                    {
                        // A register can represent multiple names in a nested alias. Leave those aliases to the general emitter.
                        return false;
                    }

                    parameters[register] = name;
                }

                Clause guardClause = clause with
                {
                    Patterns = parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray()
                };
                if (!CanEmitSeparateEntryGuard(guardClause, clause.Guard))
                {
                    return false;
                }

                var used = ReadUsedEntryParameters(guardClause);
                var bodyInputs = bindings.Where(binding => used is null || used.Contains(binding.Key))
                    .Select(binding => (new Pattern.Variable(binding.Key), binding.Value)).ToArray();
                EntryCall? tailCall = clause.Body is Expr.Call call ? ReadEntryCall(call, parameters, allowControlTransfer: true) : null;
                int resultRegister = clause.Body is Expr.Variable returned ? bindings.GetValueOrDefault(returned.Name, -1) : -1;
                Term? constant = AttributeLiteral.TryNormalizeConstant(clause.Body, out Term value) ? value : null;
                branches.Add((guardClause, tests.ToArray(), bodyInputs, tailCall, resultRegister, constant));

                bool ReadPattern(Pattern pattern, int register)
                {
                    switch (pattern)
                    {
                        case Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                            return true;
                        case Pattern.Variable variable:
                            if (!bindings.TryAdd(variable.Name, register) && bindings[variable.Name] != register)
                            {
                                int bound = bindings[variable.Name];
                                tests.Add(failure =>
                                {
                                    owner.Op(ExactEqualTest, LabelTag, failure);
                                    owner.Operand(XRegisterTag, register);
                                    owner.Operand(XRegisterTag, bound);
                                });
                            }
                            return true;
                        case Pattern.Literal { Value: Nil }:
                            tests.Add(failure => owner.EmitNilTest(XRegisterTag, register, failure));
                            return true;
                        case Pattern.Literal literal:
                            tests.Add(failure =>
                            {
                                owner.Op(ExactEqualTest, LabelTag, failure);
                                owner.Operand(XRegisterTag, register);
                                owner.LiteralOperand(literal.Value);
                            });
                            return true;
                        case AliasPattern alias:
                            return ReadPattern(alias.Left, register) && ReadPattern(alias.Right, register);
                        case Pattern.Tuple tuple:
                            bool tagged = HasTupleTag(tuple, out Atom tag);
                            tests.Add(failure =>
                            {
                                if (tagged)
                                {
                                    EmitDetailRecordTest(register, tuple.Items.Count, tag, failure);
                                }
                                else
                                {
                                    EmitTupleShapeTest(XRegisterTag, register, tuple.Items.Count, failure);
                                }
                            });
                            for (int field = tagged ? 1 : 0; field < tuple.Items.Count; field++)
                            {
                                Pattern member = tuple.Items[field];
                                if (IsDiscarded(member))
                                {
                                    continue;
                                }

                                if (nextRegister == FirstReservedXRegister)
                                {
                                    return false;
                                }

                                int destination = nextRegister++;
                                int position = field;
                                tests.Add(_ => owner.EmitTuplePart(register, position, destination));
                                if (!ReadPattern(member, destination))
                                {
                                    return false;
                                }
                            }
                            return true;
                        case Pattern.List list:
                            return ReadList(list, 0, register);
                        default:
                            return false;
                    }
                }

                bool ReadList(Pattern.List list, int index, int register)
                {
                    if (index == list.Items.Count)
                    {
                        return ReadPattern(list.Tail ?? new Pattern.Literal(Nil.Value), register);
                    }

                    tests.Add(failure => owner.EmitNonemptyList(register, failure));
                    bool readHead = !IsDiscarded(list.Items[index]);
                    bool readTail = index + 1 < list.Items.Count || !IsDiscarded(list.Tail);
                    if (nextRegister + (readHead ? 1 : 0) + (readTail ? 1 : 0) > FirstReservedXRegister)
                    {
                        return false;
                    }

                    int item = readHead ? nextRegister++ : -1;
                    int rest = readTail ? nextRegister++ : -1;
                    if (readHead && readTail)
                    {
                        tests.Add(_ => owner.EmitListParts(register, XRegisterTag, item, XRegisterTag, rest));
                    }
                    else if (readHead || readTail)
                    {
                        tests.Add(_ =>
                        {
                            owner.Op(readHead ? GetHead : GetTail, XRegisterTag, register);
                            owner.Operand(XRegisterTag, readHead ? item : rest);
                        });
                    }

                    return (!readHead || ReadPattern(list.Items[index], item)) && (!readTail || ReadList(list, index + 1, rest));
                }
            }

            int nonCons = consCount < branches.Count ? Label() : functionInfo;
            owner.EmitNonemptyList(listInput, nonCons);
            owner.EmitListParts(listInput, XRegisterTag, head, XRegisterTag, tail);

            var frames = new Dictionary<int, int>();
            for (int branch = 0; branch < branches.Count; branch++)
            {
                var (clause, tests, used, tailCall, resultRegister, constant) = branches[branch];
                int failure = branch + 1 == consCount ? nonCons : branch + 1 == branches.Count ? functionInfo : Label();
                foreach (var test in tests)
                {
                    test(failure);
                }

                EmitEntryGuardPrefix(clause, clause.Guard, failure);
                if (tailCall is not null)
                {
                    EmitRegisterTailCall(tailCall);
                }
                else if (resultRegister >= 0 || constant is not null)
                {
                    if (constant is not null)
                    {
                        owner.EmitConstant(constant, 0);
                    }
                    else if (resultRegister != 0)
                    {
                        owner.MoveRegister(XRegisterTag, resultRegister, XRegisterTag, 0);
                    }

                    owner.instructions.WriteByte(Return);
                }
                else
                {
                    var previousOrder = entryCaptureOrder;
                    entryCaptureOrder = used.ToDictionary(binding => binding.Variable.Name, binding => binding.Register, StringComparer.Ordinal);
                    EmitEntryClauseBody(clause, used, branch, frames, null, null, literalDispatch: false);
                    entryCaptureOrder = previousOrder;
                }

                if (branch + 1 < branches.Count)
                {
                    Mark(failure);
                }
            }

            if (frames.Count != 0)
            {
                TupleFrames = frames;
            }

            return true;

            static bool IsDiscarded(Pattern? pattern) =>
                pattern is Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard };
        }
    }
}

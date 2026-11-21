using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Atom? ReadClosedArgumentAtom(FunctionDefinition function, int index) =>
            ReadClosedArgumentFact(function, index, expression => (expression as Expr.Literal)?.Value as Atom);

        private T? ReadClosedArgumentFact<T>(FunctionDefinition function, int index, Func<Expr, T?> readFact)
            where T : class
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled)
            {
                return null;
            }

            var active = new HashSet<(string Name, int Arity, int Index)>();

            return Resolve(function, index);

            T? Resolve(FunctionDefinition target, int position)
            {
                var key = (target.Name, target.Arity, position);
                if (owner.IsDeclaredNative((target.Name, target.Arity)) || !active.Add(key))
                {
                    return null;
                }

                T? known = null;
                bool closed = HasOnlyLocalCalls(
                    target,
                    target.Arity,
                    (caller, call) =>
                    {
                        if (caller.Name == target.Name && caller.Arity == target.Arity
                            && caller.Clauses.Any(clause => ReferenceEquals(clause.Body, call)
                                && clause.Patterns[position] is Erlang.Pattern.Variable incoming
                                && call.Arguments[position] is Expr.Variable forwarded && incoming.Name == forwarded.Name))
                        {
                            // The recursive edge preserves this argument without supplying a new fact.
                            return true;
                        }

                        T? inferred = ReadArgument(caller, call, position);
                        if (inferred is null || known is not null && !EqualityComparer<T>.Default.Equals(known, inferred))
                        {
                            return false;
                        }

                        known = inferred;

                        return true;
                    }
                );
                active.Remove(key);

                return closed ? known : null;
            }

            T? ReadArgument(FunctionDefinition caller, Expr.Call call, int position)
            {
                if (readFact(call.Arguments[position]) is { } immediate)
                {
                    return immediate;
                }

                if (call.Arguments[position] is not Expr.Variable input)
                {
                    return null;
                }

                foreach (Clause clause in caller.Clauses)
                {
                    IReadOnlyList<Expr> items = clause.Body is Expr.Sequence sequence ? sequence.Items : [clause.Body];
                    var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
                    var parameters = clause.Patterns.OfType<Erlang.Pattern.Variable>().Select(parameter => parameter.Name).ToHashSet(StringComparer.Ordinal);
                    foreach (Expr item in items)
                    {
                        if (ReferenceEquals(item, call) || item is Expr.Case decision
                            && decision.Clauses.Any(branch => ReferenceEquals(branch.Body, call)))
                        {
                            if (constants.TryGetValue(input.Name, out Term? literal))
                            {
                                return readFact(new Expr.Literal(literal));
                            }

                            int parameter = Enumerable.Range(0, clause.Patterns.Count).FirstOrDefault(slot =>
                                clause.Patterns[slot] is Erlang.Pattern.Variable variable && variable.Name == input.Name, -1);

                            return parameter >= 0 ? Resolve(caller, parameter) : null;
                        }

                        if (item is Expr.Match { Pattern: Erlang.Pattern.Variable variable, Value: var value }
                            && variable.Name != VariableScopeNames.Wildcard && !parameters.Contains(variable.Name)
                            && AttributeLiteral.TryNormalizeConstant(value, out Term constant, constants))
                        {
                            constants.TryAdd(variable.Name, constant);
                        }
                    }
                }

                return null;
            }
        }

        private bool TryEmitRematerializedLookupCleanup(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable receiver, Erlang.Pattern.Variable target],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Whereis,
                            DynamicRemote: false, Arguments: [Expr.Variable lookupName]
                        } lookup,
                        Clauses:
                        [
                        { Guard: null, Patterns: [Erlang.Pattern.Literal { Value: Atom absent }], Body: Expr.Literal missing },
                        {
                            Guard: null, Patterns: [Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Sequence
                            {
                                Items:
                                    [
                                        Expr.Call
                                    {
                                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                        Arguments: [Expr.Variable cleanupReceiver, Expr.Variable cleanupTarget]
                                    } cleanup,
                                        Expr.Call
                                    {
                                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                        Arguments: [Expr.Variable releaseReceiver, Expr.Variable releaseTarget]
                                    } release
                                    ]
                            }
                        }
                        ]
                    }
                }]
                || !DistinctBindings(receiver, target) || lookupName.Name != receiver.Name
                || !owner.ResolvesErlangBif(lookup)
                || cleanupReceiver.Name != receiver.Name || cleanupTarget.Name != target.Name
                || releaseReceiver.Name != receiver.Name || releaseTarget.Name != target.Name
                || ReadClosedArgumentAtom(function, 0) is not { } address)
            {
                return false;
            }

            const int slot = 0;
            const int frame = 1;
            int present = Label();
            EmitFrameAllocation(2, frame);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                slot
            );
            owner.EmitCallLine(lookup, location);
            Call(ErlangModuleNames.Module, lookup.Function, 1);
            owner.Op(ExactEqualTest, LabelTag, present);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(absent);
            owner.EmitConstant(missing.Value, 0);
            owner.EmitFrameReturn(frame);

            Mark(present);
            LoadArguments();
            var previous = CallLocation(lookup, location);
            owner.EmitCallLine(cleanup, previous);
            owner.EmitStaticRemoteCall(cleanup);
            LoadArguments();
            owner.EmitCallLine(release, CallLocation(cleanup, previous));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(release.Module!, release.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;

            void LoadArguments()
            {
                owner.MoveRegister(
                    YRegisterTag,
                    slot,
                    XRegisterTag,
                    1
                );
                owner.EmitConstant(address, 0);
            }
        }

        private bool TryEmitRematerializedCheckedConstruction(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable receiver, Erlang.Pattern.Variable key, Erlang.Pattern.Variable module, Erlang.Pattern.Variable arguments],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                        {
                            Pattern: Erlang.Pattern.Variable readResult,
                            Value: Expr.Call
                            {
                                Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                Arguments: [Expr.Variable queryReceiver]
                            } query
                        },
                            Expr.Case
                        {
                            Value: Expr.Call
                            {
                                Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                Arguments: [Expr.Variable lookupKey, Expr.Literal { Value: Integer lookupIndex }, Expr.Variable lookupList]
                            } lookup,
                            Clauses:
                                [
                                {
                                    Guard: null,
                                    Patterns:
                                        [
                                            Erlang.Pattern.Tuple
                                        {
                                            Items: [Erlang.Pattern.Literal { Value: Atom matchedTag }, Erlang.Pattern.Tuple matchedTuple]
                                        }
                                        ],
                                    Body: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false,
                                        Arguments: [Expr.Variable cleanupReceiver, Expr.Variable cleanupKey]
                                    } cleanup
                                },
                                { Guard: null, Patterns: [Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Literal }
                                ]
                        },
                            Expr.Match { Pattern: Erlang.Pattern.Variable constructed, Value: Expr.Tuple specification },
                            Expr.Call
                        {
                            Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [Expr.Variable startReceiver, Expr.Variable startValue]
                        } invoke
                        ]
                    }
                }]
                || !DistinctBindings(
                    receiver,
                    key,
                    module,
                    arguments,
                    readResult,
                    constructed
                )
                || queryReceiver.Name != receiver.Name || startReceiver.Name != receiver.Name
                || lookupList.Name != readResult.Name || lookupKey.Name != key.Name
                || cleanupReceiver.Name != receiver.Name || cleanupKey.Name != key.Name
                || startValue.Name != constructed.Name
                || !entries.TryGetValue((cleanup.Function, 2), out int cleanupTarget)
                || ReadClosedArgumentAtom(function, 0) is not { } address
                || matchedTuple.Items.Count == 0 || !matchedTuple.Items.All(item => item is Erlang.Pattern.Variable)
                || !DistinctBindings([
                    receiver, key, module, arguments, readResult, constructed,
                    .. matchedTuple.Items.Cast<Erlang.Pattern.Variable>().Where(member => member.Name != VariableScopeNames.Wildcard)
                ])
                || specification.Items.Count < 3 || specification.Items[0] is not Expr.Variable name || name.Name != key.Name
                || specification.Items[1] is not Expr.Tuple
                {
                    Items: [Expr.Variable childModule, Expr.Literal { Value: Atom functionName }, Expr.Variable childArguments]
                }
                || specification.Items[^1] is not Expr.List
                { Items: [Expr.Variable dependencyModule], Tail: null }
                || childModule.Name != module.Name || dependencyModule.Name != module.Name || childArguments.Name != arguments.Name)
            {
                return false;
            }

            var suffix = new List<Term>();
            foreach (Expr item in specification.Items.Skip(2).SkipLast(1))
            {
                if (!AttributeLiteral.TryNormalizeConstant(item, out Term literal))
                {
                    return false;
                }

                suffix.Add(literal);
            }

            const int argumentsSlot = 0;
            const int moduleSlot = 1;
            const int keySlot = 2;
            const int frame = 3;
            int construct = Label();

            EmitFrameAllocation(4, frame);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                argumentsSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                moduleSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                keySlot
            );
            var previous = location;
            EmitCall(query);
            owner.EmitConstant(lookupIndex, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                YRegisterTag,
                keySlot,
                XRegisterTag,
                0
            );
            EmitCall(lookup);
            EmitDetailRecordTest(
                0,
                2,
                matchedTag,
                construct
            );
            owner.EmitTuplePart(0, 1, 0);
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                matchedTuple.Items.Count,
                construct
            );

            owner.MoveRegister(
                YRegisterTag,
                keySlot,
                XRegisterTag,
                1
            );
            owner.EmitConstant(address, 0);
            owner.EmitCallLine(cleanup, previous);
            previous = CallLocation(cleanup, previous);
            owner.EmitFieldCall(2, cleanupTarget);

            Mark(construct);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4 + ConsHeapWords + specification.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTagMatchTuple(
                0,
                (YRegisterTag, moduleSlot),
                (AtomTag, owner.Intern(functionName.Name)),
                (YRegisterTag, argumentsSlot)
            );
            owner.Op(PutList, YRegisterTag, moduleSlot);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutTuple, XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, specification.Items.Count);
            owner.Operand(YRegisterTag, keySlot);
            owner.Operand(XRegisterTag, 0);
            foreach (Term literal in suffix)
            {
                owner.LiteralOperand(literal);
            }
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(address, 0);
            owner.EmitCallLine(invoke, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(invoke.Module!, invoke.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;

            void EmitCall(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.EmitStaticRemoteCall(call);
            }
        }
    }
}

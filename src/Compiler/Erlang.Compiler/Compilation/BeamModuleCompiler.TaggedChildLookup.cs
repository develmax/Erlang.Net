using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedChildLookup(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Sequence body } clause]
                || function.Arity is not (3 or 4) || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Erlang.Pattern.Variable>().ToArray();
            var items = body.Items.Where(item => item is not Expr.Literal).ToArray();
            if (items is not
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
                        Arguments:
                            [
                                Expr.Tuple { Items: [var keyExpression, Expr.Variable searchKey] },
                                Expr.Literal { Value: Integer position },
                                Expr.Variable lookupList
                            ]
                    } lookup,
                    Clauses:
                        [
                        {
                            Guard: null,
                            Patterns:
                                [
                                    Erlang.Pattern.Tuple
                                {
                                    Items: [Erlang.Pattern.Literal { Value: Atom tag }, Erlang.Pattern.Tuple child]
                                }
                                ],
                            Body: var selected
                        },
                        { Guard: null, Patterns: [Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: var otherwise }
                        ]
                },
                    ..
                ]
                || queryReceiver.Name != parameters[0].Name || searchKey.Name != parameters[1].Name
                || readResult.Name != lookupList.Name || !DistinctBindings([.. parameters, readResult])
                || ReadClosedArgumentAtom(function, 0) is not { } receiver
                || !AttributeLiteral.TryNormalizeConstant(keyExpression, out Term keyTag)
                || child.Items.Count == 0 || !child.Items.All(item => item is Erlang.Pattern.Variable)
                || !DistinctBindings([.. parameters, readResult, .. child.Items.Cast<Erlang.Pattern.Variable>()
                    .Where(variable => variable.Name != VariableScopeNames.Wildcard)]))
            {
                return false;
            }

            int projection = -1;
            Term? selectedTag = null;
            int continuation = -1;
            IReadOnlyList<Expr> constructor;
            Expr.Call? cleanup = null;
            if (items.Length == 2
                && selected is Expr.Tuple { Items: [var resultTag, Expr.Variable result] }
                && AttributeLiteral.TryNormalizeConstant(resultTag, out selectedTag))
            {
                projection = Array.FindIndex(child.Items.ToArray(), pattern =>
                    pattern is Erlang.Pattern.Variable variable && variable.Name == result.Name);
                if (projection < 0 || result.Name == VariableScopeNames.Wildcard || otherwise is not Expr.Sequence fallback)
                {
                    return false;
                }

                constructor = fallback.Items;
            }
            else if (otherwise is Expr.Literal
                && selected is Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable cleanupKey] } effect
                && cleanupKey.Name == parameters[1].Name
                && entries.TryGetValue((effect.Function, 1), out continuation))
            {
                cleanup = effect;
                constructor = items.Skip(2).ToArray();
            }
            else
            {
                return false;
            }

            var elements = constructor.Where(item => item is not Expr.Literal).ToArray();
            if (elements.Length < 2 || elements[^2] is not Expr.Match
                {
                    Pattern: Erlang.Pattern.Variable resultSpec,
                    Value: Expr.Tuple specification
                }
                || !DistinctBindings([.. parameters, readResult, resultSpec])
                || elements[^1] is not Expr.Call
                {
                    Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                    Arguments: [Expr.Variable passedReceiver, Expr.Variable passedSpec]
                } invoke
                || passedReceiver.Name != parameters[0].Name || passedSpec.Name != resultSpec.Name
                || specification.Items.Count < 3
                || specification.Items[0] is not Expr.Tuple { Items: [var keyLabel, Expr.Variable constructedKey] }
                || constructedKey.Name != parameters[1].Name
                || !AttributeLiteral.TryNormalizeConstant(keyLabel, out Term constructedTag) || !keyTag.Equals(constructedTag)
                || specification.Items[1] is not Expr.Tuple
                {
                    Items: [var module, var methodName, Expr.Variable invocationArguments]
                }
                || invocationArguments.Name != parameters[^1].Name
                || specification.Items[^1] is not Expr.List { Items: [var dependency], Tail: null }
                || !AttributeLiteral.TryNormalizeConstant(methodName, out Term method))
            {
                return false;
            }

            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            foreach (Expr element in elements.Take(elements.Length - 2))
            {
                if (element is not Expr.Match { Pattern: Erlang.Pattern.Variable alias, Value: var value }
                    || !DistinctBindings([.. parameters, readResult, resultSpec, alias])
                    || !AttributeLiteral.TryNormalizeConstant(value, out Term literal, constants)
                    || !constants.TryAdd(alias.Name, literal))
                {
                    return false;
                }
            }

            Term? literalModule = null;
            Term? literalDependencies = null;
            bool dynamicModule = parameters.Length == 4;
            if (dynamicModule)
            {
                if (module is not Expr.Variable moduleVariable || moduleVariable.Name != parameters[2].Name
                    || dependency is not Expr.Variable dependencyVariable || dependencyVariable.Name != parameters[2].Name)
                {
                    return false;
                }
            }
            else if (!AttributeLiteral.TryNormalizeConstant(module, out literalModule, constants)
                || !AttributeLiteral.TryNormalizeConstant(specification.Items[^1], out literalDependencies, constants))
            {
                return false;
            }

            var suffix = new List<Term>();
            foreach (Expr operand in specification.Items.Skip(2).SkipLast(1))
            {
                if (!AttributeLiteral.TryNormalizeConstant(operand, out Term value, constants))
                {
                    return false;
                }

                suffix.Add(value);
            }

            int frame = parameters.Length - 1;
            const int argumentSlot = 0;
            const int moduleSlot = 1;
            int referenceSlot = frame - 1;
            int construct = Label();
            EmitFrameAllocation(parameters.Length, frame);
            for (int index = parameters.Length - 1; index >= 1; index--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    index,
                    YRegisterTag,
                    parameters.Length - 1 - index
                );
            }
            var previous = location;
            EmitCall(query);
            Heap(3, 1);
            Key(1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitConstant(position, 1);
            EmitCall(lookup);

            EmitDetailRecordTest(
                0,
                2,
                tag,
                construct
            );
            owner.EmitTuplePart(0, 1, 0);
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                child.Items.Count,
                construct
            );
            if (projection >= 0)
            {
                Heap(3, 1);
                owner.EmitTuplePart(0, projection, 0);
                TaggedChildTupleHeader(0, 2);
                owner.LiteralOperand(selectedTag!);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(frame);
            }
            else
            {
                owner.MoveRegister(
                    YRegisterTag,
                    referenceSlot,
                    XRegisterTag,
                    0
                );
                owner.EmitCallLine(cleanup!, previous);
                previous = CallLocation(cleanup!, previous);
                owner.EmitFieldCall(1, continuation);
            }

            Mark(construct);
            Heap(3 + 4 + specification.Items.Count + 1 + (dynamicModule ? ConsHeapWords : 0), 0);
            Key(0);
            TaggedChildTupleHeader(1, 3);
            if (dynamicModule)
            {
                owner.Operand(YRegisterTag, moduleSlot);
            }
            else
            {
                owner.LiteralOperand(literalModule!);
            }

            owner.LiteralOperand(method);
            owner.Operand(YRegisterTag, argumentSlot);
            if (dynamicModule)
            {
                owner.Op(PutList, YRegisterTag, moduleSlot);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 2);
            }

            TaggedChildTupleHeader(1, specification.Items.Count);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            foreach (Term value in suffix)
            {
                owner.LiteralOperand(value);
            }

            if (dynamicModule)
            {
                owner.Operand(XRegisterTag, 2);
            }
            else
            {
                owner.LiteralOperand(literalDependencies!);
            }

            owner.EmitConstant(receiver, 0);
            owner.EmitCallLine(invoke, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(invoke.Module!, invoke.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void Key(int register)
            {
                TaggedChildTupleHeader(register, 2);
                owner.LiteralOperand(keyTag);
                owner.Operand(YRegisterTag, referenceSlot);
            }

            void EmitCall(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.EmitStaticRemoteCall(call);
            }
        }
    }
}

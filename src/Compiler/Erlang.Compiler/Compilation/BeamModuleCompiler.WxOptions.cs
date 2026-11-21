using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record WxOption(
            Atom Tag,
            Expr.Call? Convert,
            int? TupleArity = null,
            (Atom Class, Expr.Call Call)? ClassCheck = null,
            Term? ConversionTail = null,
            NestedTupleRepack? Repack = null,
            bool ChecksCallback = false
        )
        {
            internal Atom? UniqueTag => Convert is not null || ClassCheck is not null || Repack is not null ? Tag : null;
            internal bool UsesField => TupleArity is not null || UniqueTag is not null;
        }

        private bool TryEmitWxOptionCommand(FunctionDefinition function, int? guardFailure = null, bool validateOnly = false)
        {
            if (!validateOnly && TryEmitWxScalarTupleOptionOverload(function)
                || TryEmitWxStaticScalarOptions(function, guardFailure, validateOnly))
            {
                return true;
            }

            if (!owner.ModuleOptimizationDisabled)
            {
                function = FoldWxOptionCommandNumber(function);
            }

            if (function.Clauses is not [
                {
                    Patterns: [Pattern.Variable options],
                    Guard: Expr.Call { Function: "is_list", Arguments: [Expr.Variable tested] } guard,
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match { Pattern: Pattern.Variable mapper, Value: Expr.Fun lambda },
                    Expr.Match
                        {
                            Pattern: Pattern.Variable mapped,
                            Value: Expr.Call
                            {
                                Module: "lists", Function: "map", DynamicRemote: false,
                                Arguments: [Expr.Variable mapperArgument, Expr.Variable mapInput]
                            } map
                        },
                    Expr.Call
                        {
                            Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                            Arguments: [Expr.Variable queueInput, Expr.Case environment,
                            Expr.Literal { Value: Integer command }]
                        } queue,
                    ..
                ]
                    } sequence
                }] || options.Name != tested.Name || options.Name != mapInput.Name
                || mapper.Name != mapperArgument.Name || mapped.Name != queueInput.Name
                || options.Name == mapper.Name || options.Name == mapped.Name || mapper.Name == mapped.Name
                || mapper.Name == VariableScopeNames.Wildcard || mapped.Name == VariableScopeNames.Wildcard
                || !owner.ResolvesErlangBif(guard)
                || !UsesFreshWxEnvironment(environment, [options.Name, mapper.Name, mapped.Name])
                || lambda.SelfName is not null || !ReadWxOptions(
                    lambda.Clauses,
                    out _,
                    out _,
                    allowCallback: true
                ))
            {
                return false;
            }

            Expr.Call? receive = null;
            if (sequence.Items.Count != 3)
            {
                if (sequence.Items is not [_, _, _, Expr.Call
                    {
                        Module: "wxe_util", Function: "rec", DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Integer received }]
                    } reply] || !command.Equals(received))
                {
                    return false;
                }

                receive = reply;
            }

            if (validateOnly)
            {
                return true;
            }

            int functionFailure = guardFailure ?? owner.nextLabel - 2;
            int missingEnvironment = Label();
            owner.Op(IsList, LabelTag, functionFailure);
            owner.Operand(XRegisterTag, 0);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
            owner.EmitClosureHeap(0, 1, 1);

            EmitWxOptionMapper(lambda, 1);

            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(map, location);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("lists", "map", 2));
            EmitWxEnvironmentCheck(1, missingEnvironment);
            owner.EmitTuplePart(1, 1, 1);
            owner.EmitConstant(command, 2);
            owner.EmitCallLine(queue, CallLocation(map, location));
            owner.Op(receive is null ? TailExternalCallWithFrame : ExternalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", 3));
            BeamSourceLocation previous = CallLocation(queue, location);
            if (receive is null)
            {
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            else
            {
                owner.EmitConstant(command, 0);
                owner.EmitCallLine(receive, previous);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "rec", 1));
                owner.Operand(BeamWire.UnsignedTag, 0);
                previous = CallLocation(receive, location);
            }

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            owner.EmitCallLine((Expr.Call)environment.Clauses[1].Body, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            return true;
        }

        private bool TryEmitWxStaticScalarOptions(
            FunctionDefinition function,
            int? guardFailure,
            bool validateOnly,
            bool dimensionTuple = false,
            bool scalarTupleOptionArm = false,
            int? alternateEntry = null
        )
        {
            int arity = function.Arity;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || (scalarTupleOptionArm ? arity < 3 : dimensionTuple ? arity != 2 : arity is not (3 or 4))
                || function.Clauses is not
                [
                {
                    Guard: { } guard,
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable mapper,
                            Value: Expr.Fun { SelfName: null } lambda
                        },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable mapped,
                            Value: Expr.Call
                            {
                                Module: "lists", Function: "map", DynamicRemote: false,
                                Arguments: [Expr.Variable mapperArgument, Expr.Variable mapInput]
                            } map
                        },
                            Expr.Call { Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false } queue,
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "rec", DynamicRemote: false,
                            Arguments: [Expr.Literal { Value: Integer received }]
                        } receive
                        ]
                    }
                } clause]
                || mapper.Name != mapperArgument.Name
                || queue.Arguments.Count != arity + 2
                || queue.Arguments[arity - 1] is not Expr.Variable supplied || supplied.Name != mapped.Name
                || queue.Arguments[arity] is not Expr.Case environment
                || queue.Arguments[arity + 1] is not Expr.Literal { Value: Integer command } || !command.Equals(received)
                || !ReadWxOptions(lambda.Clauses, out _, out _))
            {
                return false;
            }

            if (ReadForwardParameters(clause) is not { } inputs
                || (dimensionTuple ? inputs.Shapes is not [(0, 2)] : inputs.Shapes.Count != 0))
            {
                return false;
            }

            string[] parameters = inputs.Parameters;
            string[] sourceNames = [.. parameters, .. inputs.Fields.Keys];
            string[] bindings = [.. sourceNames, mapper.Name, mapped.Name];
            if (bindings.Contains(VariableScopeNames.Wildcard)
                || bindings.Distinct(StringComparer.Ordinal).Count() != bindings.Length
                || mapInput.Name != parameters[^1]
                || !queue.Arguments.Take(arity - 1).SequenceEqual(parameters.SkipLast(1).Select(name => new Expr.Variable(name)))
                || !UsesFreshWxEnvironment(environment, bindings)
                || ClosureCaptures.Capture(lambda, sourceNames.ToDictionary(name => name, _ => (Term)Nil.Value)).Count != 0
                || ReadForwardGuard(guard, parameters, inputs.Fields) is not { } parsed)
            {
                return false;
            }

            ForwardGuard[] expected;
            if (scalarTupleOptionArm)
            {
                ForwardGuard[] head = dimensionTuple
                    ? [new ForwardGuard.FieldTest(
                        IsInteger,
                        0,
                        0,
                        arity
                    ), new ForwardGuard.FieldTest(
                        IsInteger,
                        0,
                        1,
                        arity
                    )]
                    : [new ForwardGuard.Test(IsInteger, 0)];
                expected =
                [
                    .. head,
                    .. Enumerable.Range(1, arity - 2).Select(register => new ForwardGuard.Test(IsInteger, register)),
                    new ForwardGuard.Test(IsList, arity - 1)
                ];
            }
            else
            {
                expected = arity switch
                {
                    2 => [new ForwardGuard.FieldTest(
                        IsInteger,
                        0,
                        0,
                        2
                    ), new ForwardGuard.FieldTest(
                        IsInteger,
                        0,
                        1,
                        2
                    ), new ForwardGuard.Test(IsList, 1)],
                    3 => [new ForwardGuard.Test(IsInteger, 0), new ForwardGuard.Test(IsInteger, 1), new ForwardGuard.Test(IsList, 2)],
                    _ => [new ForwardGuard.Test(IsBinary, 0), new ForwardGuard.Test(IsInteger, 1), new ForwardGuard.Test(IsInteger, 2), new ForwardGuard.Test(IsList, 3)]
                };
            }

            if (!FlattenForwardConjunction(parsed).SequenceEqual(expected))
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            int functionFailure = guardFailure ?? owner.nextLabel - 2;
            int missingEnvironment = Label();
            for (int index = 0; index < expected.Length; index++)
            {
                EmitForwardGuard(expected[index], index == 0 ? alternateEntry ?? functionFailure : functionFailure);
            }

            int frame = arity - 1;
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, frame);
            owner.EmitClosureHeap(0, arity, 1);
            for (int register = arity - 2; register >= 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    arity - register - 2
                );
            }

            EmitWxOptionMapper(lambda, 0);
            owner.MoveRegister(
                XRegisterTag,
                arity - 1,
                XRegisterTag,
                1
            );
            owner.EmitCallLine(map, location);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("lists", "map", 2));
            EmitWxEnvironmentCheck(1, missingEnvironment);
            owner.EmitTuplePart(1, 1, arity);

            for (int register = arity - 2; register >= 2; register--)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    arity - register - 2,
                    XRegisterTag,
                    register
                );
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                arity - 1
            );
            if (arity > 2)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    arity - 3,
                    XRegisterTag,
                    1
                );
            }

            owner.EmitConstant(command, arity + 1);
            owner.MoveRegister(
                YRegisterTag,
                frame - 1,
                XRegisterTag,
                0
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(queue, CallLocation(map, location));
            owner.Op(ExternalCall, BeamWire.UnsignedTag, arity + 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", arity + 2));
            owner.EmitConstant(command, 0);
            owner.EmitCallLine(receive, CallLocation(queue, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "rec", 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            owner.EmitCallLine((Expr.Call)environment.Clauses[1].Body, CallLocation(receive, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            owner.Operand(BeamWire.UnsignedTag, frame);

            return true;
        }

        private bool TryEmitWxScalarTupleOptionOverload(FunctionDefinition function)
        {
            if (function.Clauses is not [var scalar, var dimensions])
            {
                return false;
            }

            var first = function with { Clauses = [scalar] };
            var second = function with { Clauses = [dimensions] };
            int failure = owner.nextLabel - 2;
            if (!TryEmitWxStaticScalarOptions(
                first,
                failure,
                validateOnly: true,
                scalarTupleOptionArm: true
            ) || !TryEmitWxStaticScalarOptions(
                second,
                failure,
                validateOnly: true,
                dimensionTuple: true,
                scalarTupleOptionArm: true
            ))
            {
                return false;
            }

            // The integer arm reserves its missing environment edge before the dimension arm.
            int tuple = owner.nextLabel + 1;
            TryEmitWxStaticScalarOptions(
                first,
                failure,
                validateOnly: false,
                scalarTupleOptionArm: true,
                alternateEntry: tuple
            );
            Mark(Label());
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                failure
            );

            return TryEmitWxStaticScalarOptions(
                second,
                failure,
                validateOnly: false,
                dimensionTuple: true,
                scalarTupleOptionArm: true
            );
        }

        private bool TryEmitWxMappedTextOverload(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != 2 || function.Clauses is not [var integer, var chardata]
                || integer.Guard is not { } scalarGuard || chardata.Guard is not { } textGuard
                || !HasUnconditionalParameters(integer) || !HasUnconditionalParameters(chardata)
                || integer.Body is not Expr.Sequence { Items: [Expr.Match { Value: Expr.Fun }, _, _, _] }
                || chardata.Body is not Expr.Sequence { Items: [Expr.Match, Expr.Match { Value: Expr.Fun }, _, _, _] }
                || !HasFreshWxMappedEnvironment(integer) || !HasFreshWxMappedEnvironment(chardata))
            {
                return false;
            }

            string[] scalarArguments = integer.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            string[] textArguments = chardata.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadForwardGuard(scalarGuard, scalarArguments) is not ForwardGuard.Conjunction
                {
                    Left: ForwardGuard.Test { Opcode: IsInteger, Register: 0 },
                    Right: ForwardGuard.Test { Opcode: IsList, Register: 1 }
                }
                || ReadForwardGuard(textGuard, textArguments) is not ForwardGuard.Conjunction
                {
                    Left: ForwardGuard.Disjunction
                    {
                        Left: ForwardGuard.Test { Opcode: IsList, Register: 0 },
                        Right: ForwardGuard.Test { Opcode: IsBinary, Register: 0 }
                    },
                    Right: ForwardGuard.Test { Opcode: IsList, Register: 1 }
                })
            {
                return false;
            }

            var scalarCommand = function with { Clauses = [integer] };
            var textCommand = function with { Clauses = [chardata] };
            if (!TryEmitWxObjectCommand(scalarCommand, validateOnly: true)
                || !TryEmitWxObjectCommand(textCommand, validateOnly: true))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            // The first command owns one cold environment failure before the next guard.
            int next = owner.nextLabel + 1;
            int firstMapper = owner.closures.Count;
            TryEmitWxObjectCommand(scalarCommand, guardFailure: next);
            Mark(Label());
            TryEmitWxObjectCommand(textCommand, functionFailure: failure);

            // The Core branches define both mappers in source order. Keep their
            // discovery indices for make_fun3 while ordering their definitions.
            owner.closures[firstMapper] = owner.closures[firstMapper] with { ForwardDefinitionOrder = true };
            owner.closures[firstMapper + 1] = owner.closures[firstMapper + 1] with { ForwardDefinitionOrder = true };

            return true;
        }

        private static bool HasFreshWxMappedEnvironment(Clause clause)
        {
            var items = ((Expr.Sequence)clause.Body).Items;
            if (items[^2] is not Expr.Call { Arguments: [_, _, Expr.Case environment, _] })
            {
                return false;
            }

            var bindings = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name)
                .Concat(items.Take(items.Count - 2).OfType<Expr.Match>().Select(match => match.Pattern)
                    .OfType<Pattern.Variable>().Select(variable => variable.Name));

            return UsesFreshWxEnvironment(environment, bindings);
        }

        private static bool UsesFreshWxEnvironment(Expr.Case environment, IEnumerable<string> bindings) =>
            IsWxEnvironment(environment)
            && environment.Clauses[0].Body is Expr.Variable { Name: not VariableScopeNames.Wildcard } reference
            && !bindings.Contains(reference.Name, StringComparer.Ordinal);

        private static FunctionDefinition FoldWxOptionCommandNumber(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable options],
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Literal { Value: Integer } literal },
                            Expr.Match { Pattern: Pattern.Variable mapper } defineMapper,
                            Expr.Match { Pattern: Pattern.Variable mapped } applyMapper,
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                            Arguments: [var queueOptions, Expr.Case environment, Expr.Variable command]
                        } queue,
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "rec", DynamicRemote: false,
                            Arguments: [Expr.Variable received]
                        } receive
                        ]
                    } sequence
                } clause]
                || binding.Name == VariableScopeNames.Wildcard || binding.Name == options.Name
                || binding.Name == mapper.Name || binding.Name == mapped.Name
                || command.Name != binding.Name || received.Name != binding.Name
                || !UsesFreshWxEnvironment(environment, [options.Name, mapper.Name, mapped.Name, binding.Name]))
            {
                return function;
            }

            var folded = sequence with
            {
                Items = [
                    defineMapper,
                    applyMapper,
                    queue with { Arguments = [queueOptions, environment, literal] },
                    receive with { Arguments = [literal] }
                ]
            };

            return function with { Clauses = [clause with { Body = folded }] };
        }

        private bool TryEmitWxStaticMappedOptions(
            Expr? guard,
            string[] arguments,
            Expr.Fun mapper,
            Expr.Call map,
            Expr.Call queue,
            Expr.Call receive,
            Integer command,
            Expr.Call? colour,
            bool validateOnly,
            int? guardFailure = null
        )
        {
            if (guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                || ReadForwardGuard(conjunction.Right, arguments) is not ForwardGuard.Test { Opcode: IsList, Register: 1 }
                || (colour is null
                    ? ReadForwardGuard(conjunction.Left, arguments) is not ForwardGuard.Test { Opcode: IsInteger, Register: 0 }
                    : !IsWxColourGuard(conjunction.Left, arguments[0])))
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            int failure = guardFailure ?? owner.nextLabel - 2;
            if (colour is null)
            {
                owner.Op(IsInteger, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                EmitWxColourGuard(0, 2, failure);
            }

            owner.Op(IsList, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            int missingEnvironment = Label();
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.EmitClosureHeap(0, 2, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            EmitWxOptionMapper(mapper, 0);
            owner.EmitCallLine(map, location);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("lists", "map", 2));
            BeamSourceLocation previous = CallLocation(map, location);
            if (colour is not null)
            {
                owner.Op(Swap, YRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitCallLine(colour, previous);
                previous = CallLocation(colour, previous);
                owner.Op(ExternalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "color", 1));
            }

            EmitWxEnvironmentCheck(1, missingEnvironment);
            owner.EmitTuplePart(1, 1, 2);
            if (colour is null)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
                owner.EmitConstant(command, 3);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
            }
            else
            {
                owner.EmitConstant(command, 3);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }

            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(queue, previous);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", 4));
            owner.EmitConstant(command, 0);
            owner.EmitCallLine(receive, CallLocation(queue, previous));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "rec", 1));
            owner.Operand(BeamWire.UnsignedTag, 0);
            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            owner.EmitCallLine(queue, CallLocation(receive, previous));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }

        private void EmitWxOptionMapper(Expr.Fun lambda, int destination, int arity = 1)
        {
            var lexicalOwner = parentFunction!;
            int ordinal = lambda.CoreOrdinal ?? anonymousOrdinal++;
            string name = FormattableString.Invariant($"-{lexicalOwner.Name}/{lexicalOwner.Arity}-fun-{ordinal}-");
            BeamSourceLocation source = lambda.ClauseLine == 0 ? location : new(lambda.ClauseFile, lambda.ClauseLine);
            owner.Op(MakeFun, BeamWire.UnsignedTag, owner.closures.Count);
            owner.Operand(XRegisterTag, destination);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 0);
            var lifted = new FunctionDefinition(name, arity, lambda.Clauses);
            owner.closures.Add(new(
                name,
                null,
                source,
                0,
                null,
                General: lifted,
                LexicalOwner: lexicalOwner
            ));
        }

        private bool TryEmitWxOptionClosure(FunctionDefinition function)
        {
            if (!ReadWxOptions(
                function.Clauses,
                out var options,
                out Expr.Call? error,
                allowCallback: true
            ))
            {
                return false;
            }

            bool hasTransform = options.Any(option => option.UniqueTag is not null);
            var groups = options.GroupBy(option => (option.TupleArity, option.UniqueTag));
            // Switch successors are visited in tag order and emitted in reverse postorder.
            var ordered = groups.OrderByDescending(group => group.Min(option => option.Tag))
                .Select(group => group.First()).ToArray();
            var cases = new (WxOption Option, int Entry, int Validated, bool Returns)[ordered.Length];
            int lastClassCheck = Array.FindLastIndex(ordered, option => option.ClassCheck is not null);
            int sharedValidation = 0;
            for (int index = 0; index < ordered.Length; index++)
            {
                int entry = options.Count > 1 ? Label() : 0;
                if (index == lastClassCheck)
                {
                    sharedValidation = Label();
                }

                cases[index] = (ordered[index], entry, 0, index == lastClassCheck);
            }

            for (int index = 0; index <= lastClassCheck; index++)
            {
                if (cases[index].Option.ClassCheck is not null)
                {
                    cases[index].Validated = sharedValidation;
                }
            }

            bool sharedField = hasTransform && cases.Count(branch => branch.Option.UsesField) > 1;
            int rejected = Label();
            if (options.Count == 1)
            {
                EmitDetailRecordTest(
                    0,
                    2,
                    options[0].Tag,
                    rejected
                );
            }
            else
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    rejected
                );
                owner.EmitTuplePart(0, 0, 1);
                if (sharedField)
                {
                    owner.EmitTuplePart(0, 1, 2);
                }

                owner.Op(SelectValue, XRegisterTag, 1);
                owner.Operand(LabelTag, rejected);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, options.Count * 2);
                foreach (var option in options.OrderBy(option => option.Tag))
                {
                    var branch = cases.Single(item => item.Option.UniqueTag == option.UniqueTag
                        && item.Option.TupleArity == option.TupleArity);
                    owner.LiteralOperand(option.Tag);
                    owner.Operand(LabelTag, branch.Entry);
                }
            }

            BeamSourceLocation previous = location;
            foreach (var (option, entry, validated, returns) in cases)
            {
                if (entry != 0)
                {
                    Mark(entry);
                }

                if (option.Convert is { } convert)
                {
                    int live = sharedField ? 3 : 1;
                    if (option.ChecksCallback)
                    {
                        int field = sharedField ? 2 : 1;
                        if (!sharedField)
                        {
                            owner.EmitTuplePart(0, 1, field);
                        }

                        owner.Op(IsFunction, LabelTag, rejected);
                        owner.Operand(XRegisterTag, field);
                        EmitFrameAllocation(field + 1, 0);
                        owner.MoveRegister(
                            XRegisterTag,
                            field,
                            XRegisterTag,
                            0
                        );
                    }
                    else if (option.ConversionTail is { } tail)
                    {
                        owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
                        owner.Operand(BeamWire.UnsignedTag, 2);
                        owner.Operand(BeamWire.UnsignedTag, live);
                        if (!sharedField)
                        {
                            owner.EmitTuplePart(0, 1, 0);
                        }

                        owner.Op(PutList, XRegisterTag, sharedField ? 2 : 0);
                        owner.LiteralOperand(tail);
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        EmitFrameAllocation(live, 0);
                        if (sharedField)
                        {
                            owner.MoveRegister(
                                XRegisterTag,
                                2,
                                XRegisterTag,
                                0
                            );
                        }
                        else
                        {
                            owner.EmitTuplePart(0, 1, 0);
                        }
                    }

                    owner.EmitCallLine(convert, previous);
                    previous = CallLocation(convert, previous);
                    if (convert.Module is null)
                    {
                        owner.EmitFieldCall(1, entries[(convert.Function, 1)]);
                    }
                    else
                    {
                        owner.Op(ExternalCall, BeamWire.UnsignedTag, 1);
                        owner.Operand(BeamWire.UnsignedTag, owner.Import(convert.Module, convert.Function, 1));
                    }
                    EmitWxOptionTuple(option.Tag);
                    owner.EmitFrameReturn();
                    continue;
                }

                if (option.ClassCheck is { } classCheck)
                {
                    previous = EmitWxOptionClassCheck(
                        classCheck,
                        sharedField,
                        validated,
                        rejected,
                        previous,
                        returns
                    );
                    continue;
                }

                if (option.Repack is not null)
                {
                    EmitWxOptionRepack(option, sharedField, rejected);
                    continue;
                }

                if (option.TupleArity is { } arity)
                {
                    if (!sharedField)
                    {
                        owner.EmitTuplePart(0, 1, 1);
                    }

                    EmitTupleShapeTest(
                        XRegisterTag,
                        sharedField ? 2 : 1,
                        arity,
                        rejected
                    );
                }

                owner.instructions.WriteByte(Return);
            }

            Mark(rejected);
            EmitWxOptionTuple(Term.A("badoption"));
            owner.EmitCallLine(error!, previous);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));

            return true;
        }

        private BeamSourceLocation EmitWxOptionClassCheck(
            (Atom Class, Expr.Call Call) check,
            bool sharedField,
            int validated,
            int rejected,
            BeamSourceLocation previous,
            bool returns
        )
        {
            int field = sharedField ? 2 : 1;
            if (!sharedField)
            {
                owner.EmitTuplePart(0, 1, field);
            }

            EmitDetailRecordTest(
                field,
                4,
                Term.A("wx_ref"),
                rejected
            );
            EmitFrameAllocation(field + 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(field, 2, 0);
            owner.Op(ExactNotEqualTest, LabelTag, validated);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(check.Class);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(Term.A("parent_class"), 2);
            owner.EmitConstant(check.Class, 0);
            owner.EmitCallLine(check.Call, previous);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            if (returns)
            {
                Mark(validated);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
                owner.EmitFrameReturn(1);
            }
            else
            {
                owner.Op(Jump, LabelTag, validated);
            }

            return CallLocation(check.Call, previous);
        }

        private void EmitWxOptionTuple(Atom tag)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitWxOptionTupleValue(tag);
        }

        private void EmitWxOptionTupleValue(Atom tag)
        {
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            owner.Operand(XRegisterTag, 0);
        }

        private bool ReadWxOptions(
            IReadOnlyList<Clause> clauses,
            out List<WxOption> options,
            out Expr.Call? error,
            bool allowCallback = false
        )
        {
            options = [];
            error = null;
            if (clauses.Count < 2 || clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable bad],
                    Body: Expr.Call
                    {
                        Module: ErlangModuleNames.Module, Function: "error",
                        Arguments: [Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom { Name: "badoption" } },
                            Expr.Variable rejected]
                        }]
                    } call
                } || rejected.Name != bad.Name)
            {
                return false;
            }

            foreach (var clause in clauses.Take(clauses.Count - 1))
            {
                if (clause is { Guard: null, Patterns: [AliasPattern alias], Body: Expr.Variable result }
                    && (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is { } binding
                    && (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom tag }, var field]
                    } && binding.Name == result.Name && ReadWxOptionField(field, binding.Name, out int? tupleArity))
                {
                    options.Add(new(tag, null, tupleArity));
                }
                else if (allowCallback && !owner.ModuleOptimizationDisabled && owner.SsaTypeInferenceEnabled
                    && ReadWxCallbackOption(clause) is { } callback)
                {
                    options.Add(callback);
                }
                else if (ReadWxClassOption(clause) is { } classOption)
                {
                    options.Add(classOption);
                }
                else if (clause is
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom inputTag }, var inputTuple] }],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom outputTag }, Expr.Tuple outputTuple] }
                } && inputTag.Equals(outputTag) && ReadNestedTupleRepack(inputTuple, outputTuple) is { } repack)
                {
                    options.Add(new(inputTag, null, Repack: repack));
                }
                else if (clause is
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom colourKey }, Pattern.Variable colour] }],
                    Body: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom resultKey },
                        Expr.Call
                        {
                            Module: "wxe_util", Function: "color", DynamicRemote: false,
                            Arguments: [Expr.Variable argument]
                        } colourCall]
                    }
                } && colourKey.Equals(resultKey) && colour.Name == argument.Name
                    && colour.Name != VariableScopeNames.Wildcard)
                {
                    options.Add(new(colourKey, colourCall));
                }
                else if (clause is
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom key }, Pattern.Variable input] }],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.Variable binary, Value: Expr.Call
                            { DynamicRemote: false, Arguments: [var text] } convert
                        },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom returnedKey }, Expr.Variable returned] }
                    ]
                    }
                } && (convert is { Module: "unicode", Function: "characters_to_binary" }
                    || convert.Module is null && convert.Function.EndsWith("-lc$^0/1-0-", StringComparison.Ordinal)
                    && text is Expr.Variable)
                    && ReadWxOptionConversionTail(text, input.Name, out Term? tail)
                    && binary.Name == returned.Name && input.Name != binary.Name
                    && key.Equals(returnedKey))
                {
                    options.Add(new(key, convert, ConversionTail: tail));
                }
                else
                {
                    return false;
                }
            }

            error = call;

            return options.Select(option => option.Tag).Distinct().Count() == options.Count;
        }

        private WxOption? ReadWxCallbackOption(Clause clause)
        {
            if (clause is not
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable callback] }],
                    Guard: Expr.Call { Function: "is_function", Arguments: [Expr.Variable tested] } guard,
                    Body: Expr.Tuple
                    {
                        Items: [
                            Expr.Literal { Value: Atom returnedTag },
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "get_cbId", DynamicRemote: false,
                            Arguments: [Expr.Variable argument]
                        } register
                        ]
                    }
                }
                || callback.Name == VariableScopeNames.Wildcard || callback.Name != tested.Name
                || callback.Name != argument.Name || !tag.Equals(returnedTag) || !owner.ResolvesErlangBif(guard))
            {
                return null;
            }

            return new(tag, register, ChecksCallback: true);
        }

        private static bool ReadWxOptionConversionTail(Expr expression, string input, out Term? tail)
        {
            tail = null;
            if (expression is Expr.Variable variable)
            {
                return variable.Name == input;
            }

            if (expression is not Expr.List { Items: [Expr.Variable head, ..], Tail: null } list
                || head.Name != input || list.Items.Skip(1).Any(item => item is not Expr.Literal))
            {
                return false;
            }

            tail = Cons.From(list.Items.Skip(1).Cast<Expr.Literal>().Select(item => item.Value));

            return true;
        }

        private static WxOption? ReadWxClassOption(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [AliasPattern alias],
                    Body: Expr.Sequence { Items: [Expr.Binary { Operator: ErlangOperators.OrElse } check, Expr.Variable returned] }
                }
                || (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is not { } binding
                || binding.Name != returned.Name
                || (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is not Pattern.Tuple
                {
                    Items: [Pattern.Literal { Value: Atom tag }, Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom { Name: "wx_ref" } },
                            Pattern.Variable { Name: VariableScopeNames.Wildcard },
                            Pattern.Variable type,
                            Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                    }]
                }
                || type.Name == binding.Name
                || check.Left is not Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Variable tested,
                    Right: Expr.Literal { Value: Atom expected }
                }
                || check.Right is not Expr.Call
                {
                    DynamicRemote: true,
                    Arguments: [Expr.Variable dispatched, Expr.Literal { Value: Atom { Name: "parent_class" } },
                        Expr.List { Items: [Expr.Literal { Value: Atom passed }], Tail: null }]
                } call
                || type.Name != tested.Name || type.Name != dispatched.Name || !expected.Equals(passed))
            {
                return null;
            }

            return new(tag, null, ClassCheck: (expected, call));
        }

        private static bool ReadWxOptionField(Pattern field, string alias, out int? tupleArity)
        {
            tupleArity = null;
            if (field is Pattern.Variable variable)
            {
                return variable.Name != alias;
            }

            if (field is not Pattern.Tuple tuple)
            {
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal) { alias };
            foreach (Pattern item in tuple.Items)
            {
                if (item is not Pattern.Variable member
                    || member.Name != VariableScopeNames.Wildcard && !names.Add(member.Name))
                {
                    return false;
                }
            }

            tupleArity = tuple.Items.Count;

            return true;
        }
    }
}

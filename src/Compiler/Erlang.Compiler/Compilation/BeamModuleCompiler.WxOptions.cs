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
            NestedTupleRepack? Repack = null
        )
        {
            internal Atom? UniqueTag => Convert is not null || ClassCheck is not null || Repack is not null ? Tag : null;
            internal bool UsesField => TupleArity is not null || UniqueTag is not null;
        }

        private bool TryEmitWxOptionCommand(FunctionDefinition function, int? guardFailure = null, bool validateOnly = false)
        {
            if (function.Clauses is not [{
                Patterns: [Pattern.Variable options],
                Guard: Expr.Call { Function: "is_list", Arguments: [Expr.Variable tested] } guard,
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable mapper, Value: Expr.Fun lambda },
                    Expr.Match { Pattern: Pattern.Variable mapped,
                        Value: Expr.Call { Module: "lists", Function: "map", DynamicRemote: false,
                            Arguments: [Expr.Variable mapperArgument, Expr.Variable mapInput] } map },
                    Expr.Call { Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                        Arguments: [Expr.Variable queueInput, Expr.Case environment,
                            Expr.Literal { Value: Integer command }] } queue,
                    ..
                ] } sequence
            }] || options.Name != tested.Name || options.Name != mapInput.Name
                || mapper.Name != mapperArgument.Name || mapped.Name != queueInput.Name
                || options.Name == mapper.Name || options.Name == mapped.Name || mapper.Name == mapped.Name
                || mapper.Name == VariableScopeNames.Wildcard || mapped.Name == VariableScopeNames.Wildcard
                || !owner.ResolvesErlangBif(guard) || !IsWxEnvironment(environment)
                || lambda.SelfName is not null || !ReadWxOptions(lambda.Clauses, out _, out _))
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

        private bool TryEmitWxStaticMappedOptions(Expr? guard, string[] arguments, Expr.Fun mapper, Expr.Call map,
            Expr.Call queue, Expr.Call receive, Integer command, Expr.Call? colour, bool validateOnly)
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

            int failure = owner.nextLabel - 2;
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
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
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
                owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
                owner.EmitConstant(command, 3);
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
            }
            else
            {
                owner.EmitConstant(command, 3);
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 1);
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

        private void EmitWxOptionMapper(Expr.Fun lambda, int destination)
        {
            var lexicalOwner = parentFunction!;
            int ordinal = lambda.CoreOrdinal ?? anonymousOrdinal++;
            string name = FormattableString.Invariant($"-{lexicalOwner.Name}/{lexicalOwner.Arity}-fun-{ordinal}-");
            BeamSourceLocation source = lambda.ClauseLine == 0 ? location : new(lambda.ClauseFile, lambda.ClauseLine);
            owner.Op(MakeFun, BeamWire.UnsignedTag, owner.closures.Count);
            owner.Operand(XRegisterTag, destination);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 0);
            var lifted = new FunctionDefinition(name, 1, lambda.Clauses);
            owner.closures.Add(new(name, null, source, 0, null, General: lifted, LexicalOwner: lexicalOwner));
        }

        private bool TryEmitWxOptionClosure(FunctionDefinition function)
        {
            if (!ReadWxOptions(function.Clauses, out var options, out Expr.Call? error))
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
                EmitDetailRecordTest(0, 2, options[0].Tag, rejected);
            }
            else
            {
                EmitTupleShapeTest(XRegisterTag, 0, 2, rejected);
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
                    if (option.ConversionTail is { } tail)
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
                            owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 0);
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

                    EmitTupleShapeTest(XRegisterTag, sharedField ? 2 : 1, arity, rejected);
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

            EmitDetailRecordTest(field, 4, Term.A("wx_ref"), rejected);
            EmitFrameAllocation(field + 1, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            owner.EmitTuplePart(field, 2, 0);
            owner.Op(ExactNotEqualTest, LabelTag, validated);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(check.Class);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(Term.A("parent_class"), 2);
            owner.EmitConstant(check.Class, 0);
            owner.EmitCallLine(check.Call, previous);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
            if (returns)
            {
                Mark(validated);
                owner.MoveRegister(YRegisterTag, 0, XRegisterTag, 0);
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
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            owner.Operand(XRegisterTag, 0);
        }

        private static bool ReadWxOptions(IReadOnlyList<Clause> clauses, out List<WxOption> options, out Expr.Call? error)
        {
            options = [];
            error = null;
            if (clauses.Count < 2 || clauses[^1] is not
                {
                    Guard: null, Patterns: [Pattern.Variable bad],
                    Body: Expr.Call { Module: ErlangModuleNames.Module, Function: "error",
                        Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom { Name: "badoption" } },
                            Expr.Variable rejected] }] } call
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
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom resultKey },
                        Expr.Call { Module: "wxe_util", Function: "color", DynamicRemote: false,
                            Arguments: [Expr.Variable argument] } colourCall] }
                } && colourKey.Equals(resultKey) && colour.Name == argument.Name
                    && colour.Name != VariableScopeNames.Wildcard)
                {
                    options.Add(new(colourKey, colourCall));
                }
                else if (clause is
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom key }, Pattern.Variable input] }],
                    Body: Expr.Sequence { Items: [
                        Expr.Match { Pattern: Pattern.Variable binary, Value: Expr.Call
                            { DynamicRemote: false, Arguments: [var text] } convert },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom returnedKey }, Expr.Variable returned] }
                    ] }
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

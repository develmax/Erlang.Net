using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWxObjectOverload(FunctionDefinition function)
        {
            if (TryEmitWxSharedObjectCommandPair(function) || TryEmitWxScalarOptionRecordPair(function)
                || TryEmitWxClassIndexOverload(function))
            {
                return true;
            }

            if (function.Arity >= 3 && function.Clauses is [var numeric, var reference]
                && numeric.Patterns[^1] is Pattern.Variable && reference.Patterns[^1] is AliasPattern)
            {
                int receiverRegister = function.Arity;
                var numericCommand = function with { Clauses = [numeric] };
                var referenceCommand = function with { Clauses = [reference] };
                if (TryEmitWxObjectCommand(
                    numericCommand,
                    receiverRegister,
                    0,
                    validateOnly: true,
                    numericThenObject: true
                )
                    && TryEmitWxObjectCommand(
                        referenceCommand,
                        receiverRegister,
                        validateOnly: true,
                        numericThenObject: true
                    ))
                {
                    int rejected = owner.nextLabel - 2;
                    EmitDetailRecordTest(
                        0,
                        4,
                        Term.A("wx_ref"),
                        rejected
                    );
                    owner.EmitTuplePart(0, 2, receiverRegister);
                    int referenceEntry = owner.nextLabel + 4;
                    TryEmitWxObjectCommand(
                        numericCommand,
                        receiverRegister,
                        referenceEntry,
                        functionFailure: rejected,
                        numericThenObject: true
                    );
                    Mark(Label());

                    return TryEmitWxObjectCommand(
                        referenceCommand,
                        receiverRegister,
                        functionFailure: rejected,
                        numericThenObject: true
                    );
                }
            }

            if (function.Arity is 2 or 3 && function.Clauses is [var options, var objectClause]
                && options.Patterns[^1] is Pattern.Variable list
                && options.Guard is Expr.Call { Function: "is_list", Arguments: [Expr.Variable testedList] } listGuard
                && testedList.Name == list.Name && owner.ResolvesErlangBif(listGuard) && objectClause.Guard is null
                && objectClause.Patterns[^1] is AliasPattern)
            {
                var optionsCommand = function with { Clauses = [options] };
                var objectCommand = function with { Clauses = [objectClause] };
                int receiverType = function.Arity == 2 ? 2 : 4;
                if (TryEmitWxObjectCommand(
                    optionsCommand,
                    receiverType,
                    0,
                    validateOnly: true,
                    listThenObject: true
                )
                    && TryEmitWxObjectCommand(
                        objectCommand,
                        receiverType,
                        validateOnly: true,
                        listThenObject: true
                    ))
                {
                    int rejected = owner.nextLabel - 2;
                    EmitDetailRecordTest(
                        0,
                        4,
                        Term.A("wx_ref"),
                        rejected
                    );
                    if (function.Arity == 3)
                    {
                        EmitDetailRecordTest(
                            1,
                            4,
                            Term.A("wx_ref"),
                            rejected
                        );
                        owner.EmitTuplePart(1, 2, 3);
                    }

                    owner.EmitTuplePart(0, 2, receiverType);
                    int objectEntry = owner.nextLabel + function.Arity;
                    TryEmitWxObjectCommand(
                        optionsCommand,
                        receiverType,
                        objectEntry,
                        functionFailure: rejected,
                        listThenObject: true
                    );
                    Mark(Label());

                    return TryEmitWxObjectCommand(
                        objectCommand,
                        receiverType,
                        functionFailure: rejected,
                        listThenObject: true
                    );
                }
            }

            if (function.Clauses is not [var first, var second] || function.Arity != 2
                || first.Patterns[1] is not Pattern.Variable firstArgument)
            {
                return false;
            }

            bool textGuard = IsWxCharDataGuard(first.Guard, firstArgument.Name);
            bool textThenObject = textGuard && second.Patterns[1] is AliasPattern && second.Guard is null;
            bool integerFirst = second.Patterns[1] is Pattern.Variable secondArgument
                && first.Guard is Expr.Call { Function: "is_integer", Arguments: [Expr.Variable tested] }
                && firstArgument.Name == tested.Name && IsWxCharDataGuard(second.Guard, secondArgument.Name);
            bool textFirst = textGuard && (textThenObject || second.Patterns[1] is Pattern.Variable colour
                && IsWxColourGuard(second.Guard, colour.Name));
            if (!integerFirst && !textFirst)
            {
                return false;
            }

            var firstCommand = function with { Clauses = [first] };
            var secondCommand = function with { Clauses = [second] };
            const int typeRegister = 2;
            if (!TryEmitWxObjectCommand(
                firstCommand,
                typeRegister,
                0,
                validateOnly: true,
                textThenObject: textThenObject
            )
                || !TryEmitWxObjectCommand(
                    secondCommand,
                    typeRegister,
                    validateOnly: true,
                    textThenObject: textThenObject
                ))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            EmitDetailRecordTest(
                0,
                4,
                Term.A("wx_ref"),
                failure
            );
            owner.EmitTuplePart(0, 2, typeRegister);
            // Each branch has two body labels. Chardata adds two guard labels.
            int fallback = owner.nextLabel + (textFirst ? 4 : 2);
            TryEmitWxObjectCommand(
                firstCommand,
                typeRegister,
                fallback,
                functionFailure: failure,
                textThenObject: textThenObject
            );
            Mark(Label());
            TryEmitWxObjectCommand(
                secondCommand,
                typeRegister,
                functionFailure: failure,
                textThenObject: textThenObject
            );

            return true;
        }

        private bool TryEmitWxSharedObjectCommandPair(FunctionDefinition function)
        {
            int arity = function.Arity;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || arity is not (2 or 3) || function.Clauses is not [var first, var other])
            {
                return false;
            }

            bool startsWithObject = arity == 3 && first.Patterns[^1] is AliasPattern && first.Guard is null
                && other.Patterns[^1] is Pattern.Variable colour && IsWxColourGuard(other.Guard, colour.Name);
            bool startsWithInteger = first.Patterns[^1] is Pattern.Variable index
                && first.Guard is Expr.Call { Function: "is_integer", Arguments: [Expr.Variable tested] } guard
                && tested.Name == index.Name && owner.ResolvesErlangBif(guard);
            if (!startsWithObject && !startsWithInteger)
            {
                return false;
            }

            var firstCommand = function with { Clauses = [first] };
            var secondCommand = function with { Clauses = [other] };
            int receiver = arity == 2 ? 2 : 4;
            int? secondReceiver = arity == 2 ? null : 3;
            if (!TryEmitWxObjectCommand(
                firstCommand,
                receiver,
                guardFailure: 0,
                validateOnly: true,
                preparedSecondClass: secondReceiver,
                sharedObjectPair: true
            )
                || !TryEmitWxObjectCommand(
                    secondCommand,
                    receiver,
                    validateOnly: true,
                    preparedSecondClass: secondReceiver,
                    sharedObjectPair: true
                ))
            {
                return false;
            }

            int rejected = owner.nextLabel - 2;
            for (int register = 0; register < arity - 1; register++)
            {
                EmitDetailRecordTest(
                    register,
                    4,
                    Term.A("wx_ref"),
                    rejected
                );
            }

            if (secondReceiver is int secondType)
            {
                owner.EmitTuplePart(1, 2, secondType);
            }

            owner.EmitTuplePart(0, 2, receiver);
            // The first branch allocates one label per class check and an environment failure.
            int fallback = owner.nextLabel + arity + (startsWithObject ? 1 : 0);
            TryEmitWxObjectCommand(
                firstCommand,
                receiver,
                fallback,
                functionFailure: rejected,
                preparedSecondClass: secondReceiver,
                sharedObjectPair: true
            );
            Mark(Label());
            TryEmitWxObjectCommand(
                secondCommand,
                receiver,
                functionFailure: rejected,
                preparedSecondClass: secondReceiver,
                sharedObjectPair: true
            );

            return true;
        }

        private bool TryEmitWxScalarOptionRecordPair(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 4
                || function.Clauses is not [var options, var other] || options.Guard is null || other.Guard is null
                || options.Patterns[^1] is not Pattern.Variable { } || other.Patterns[^1] is not AliasPattern)
            {
                return false;
            }

            var optionCommand = function with { Clauses = [options] };
            var objectCommand = function with { Clauses = [other] };
            const int receiverType = 4;
            if (!TryEmitWxObjectCommand(
                optionCommand,
                receiverType,
                0,
                validateOnly: true,
                scalarOptionPair: true
            )
                || !TryEmitWxObjectCommand(
                    objectCommand,
                    receiverType,
                    validateOnly: true,
                    scalarOptionPair: true
                ))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            EmitDetailRecordTest(
                0,
                4,
                Term.A("wx_ref"),
                failure
            );
            for (int register = 1; register <= 2; register++)
            {
                EmitForwardGuard(new ForwardGuard.Test(IsInteger, register), failure);
            }

            owner.EmitTuplePart(0, 2, receiverType);
            int fallback = owner.nextLabel + 2;
            TryEmitWxObjectCommand(
                optionCommand,
                receiverType,
                fallback,
                functionFailure: failure,
                scalarOptionPair: true
            );
            Mark(Label());
            TryEmitWxObjectCommand(
                objectCommand,
                receiverType,
                functionFailure: failure,
                scalarOptionPair: true
            );

            return true;
        }

        private bool TryEmitWxGuardedForwardOverload(FunctionDefinition function)
        {
            if (function.Clauses.Count < 2 || ReadGuardedLocalForward(function.Clauses[0]) is not { } forward)
            {
                return false;
            }

            var continuation = function with { Clauses = function.Clauses.Skip(1).ToArray() };
            int failure = owner.nextLabel - 2;
            if (TryEmitWxTupleArityForward(forward, continuation, failure)
                || TryEmitWxMappedTupleUnionForward(forward, continuation, failure)
                || TryEmitWxTupleConstructorForward(function, forward, failure)
                || TryEmitWxClassedConstructorOverload(function, forward, failure)
                || TryEmitWxSharedReceiverForward(forward, continuation, failure)
                || TryEmitWxGuardedScalarOptionForward(forward, continuation, failure))
            {
                return true;
            }

            bool classUnion = TryEmitWxUnaryClassUnionOverload(continuation, failure, validateOnly: true);
            if (!classUnion && !TryEmitWxObjectCommand(continuation, validateOnly: true))
            {
                return false;
            }

            int next = owner.nextLabel + ForwardGuardLabelCount(forward.Tests);
            EmitGuardedLocalForward(forward, next);
            Mark(Label());

            return classUnion ? TryEmitWxUnaryClassUnionOverload(continuation, failure)
                : TryEmitWxObjectCommand(continuation, functionFailure: failure);
        }

        private bool TryEmitWxGuardedScalarOptionForward(
            GuardedLocalForward forward,
            FunctionDefinition command,
            int failure
        )
        {
            if (forward.Arity is not (3 or 4) || !forward.AddsEmptyList || forward.Shapes.Count != 0
                || command.Clauses is not [{ Guard: { } guard } clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            var tests = FlattenForwardConjunction(forward.Tests).ToArray();
            string[] names = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (tests.Length != forward.Arity || tests.Any(test => test is not ForwardGuard.Test { Opcode: IsBinary or IsInteger })
                || ReadForwardGuard(guard, names) is not { } parsed
                || !TryEmitWxStaticScalarOptions(command, failure, validateOnly: true))
            {
                return false;
            }

            var fallbackTests = FlattenForwardConjunction(parsed).ToHashSet();
            int fallback = Label();
            foreach (var test in tests)
            {
                EmitForwardGuard(test, fallbackTests.Contains(test) ? failure : fallback);
            }

            owner.EmitConstant(Nil.Value, forward.Arity);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, forward.Arity + 1);
            owner.Operand(LabelTag, forward.Entry);
            Mark(fallback);
            TryEmitWxStaticScalarOptions(command, failure, validateOnly: false);

            return true;
        }

        private bool TryEmitWxSharedReceiverForward(
            GuardedLocalForward forward,
            FunctionDefinition command,
            int failure
        )
        {
            int arity = forward.Arity;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || arity is not (2 or 3) || !forward.AddsEmptyList || forward.Shapes.Count != 0)
            {
                return false;
            }

            var tests = FlattenForwardConjunction(forward.Tests).ToArray();
            if (tests is not [
                ForwardGuard.Test { Opcode: IsTaggedTuple, Register: 0, Tag.Name: "wx_ref", Arity: 4 },
                .. var indexTests,
                var choice
            ] || !indexTests.SequenceEqual(Enumerable.Range(1, arity - 2)
                    .Select(register => new ForwardGuard.Test(IsInteger, register)))
                || !(choice is ForwardGuard.Test { Opcode: IsInteger, Register: var scalar } && scalar == arity - 1
                    || choice is ForwardGuard.Disjunction
                    {
                        Left: ForwardGuard.Test { Opcode: IsList, Register: var list },
                        Right: ForwardGuard.Test { Opcode: IsBinary, Register: var binary }
                    } && list == arity - 1 && binary == arity - 1)
                || !TryEmitWxObjectCommand(command, validateOnly: true, sharedReceiverForward: true))
            {
                return false;
            }

            foreach (var shared in tests.SkipLast(1))
            {
                EmitForwardGuard(shared, failure);
            }

            int next = owner.nextLabel + ForwardGuardLabelCount(choice);
            EmitForwardGuard(choice, next);
            owner.EmitConstant(Nil.Value, arity);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity + 1);
            owner.Operand(LabelTag, forward.Entry);
            Mark(Label());
            TryEmitWxObjectCommand(command, functionFailure: failure, sharedReceiverForward: true);

            return true;
        }

        private bool TryEmitWxUnaryClassUnionOverload(FunctionDefinition function, int failure, bool validateOnly = false)
        {
            if (function.Arity != 1 || function.Clauses is not [var first, var second])
            {
                return false;
            }

            var fallback = function with { Clauses = [second] };
            var command = function with { Clauses = [first] };
            if (!TryEmitWxStaticCommand(fallback, validateOnly: true)
                || !TryEmitWxClassUnionCommand(command, validateOnly: true, unaryFallback: true))
            {
                return false;
            }

            if (!validateOnly)
            {
                TryEmitWxClassUnionCommand(command, unaryFallback: true);
                Mark(Label());
                TryEmitWxStaticCommand(fallback, failure);
            }

            return true;
        }

        private bool TryEmitWxUnaryTextObjectOverload(FunctionDefinition function, Clause text, Clause reference)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || text.Patterns is not [Pattern.Variable input] || text.Guard is not { } guard
                || ReadForwardGuard(guard, [input.Name]) is not { } tests || !IsWxChardataTest(tests)
                || ReadWxClassUnionObject(reference.Patterns[0]) is null || reference.Guard is not null
                || text.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable output },
                        Expr.Call { DynamicRemote: false, Arguments: [_, Expr.Case environment, _] },
                        Expr.Call { Module: "wxe_util", Function: "rec", DynamicRemote: false }
                    ]
                }
                || !UsesFreshWxEnvironment(environment, [input.Name, output.Name]))
            {
                return false;
            }

            var textCommand = function with { Clauses = [text] };
            var referenceCommand = function with { Clauses = [reference] };
            if (!TryEmitWxStaticCommand(textCommand, validateOnly: true)
                || !TryEmitWxObjectCommand(referenceCommand, validateOnly: true))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            // Two guard edges and the converted command's missing environment edge precede the record arm.
            int record = owner.nextLabel + 3;
            TryEmitWxStaticCommand(textCommand, record);
            Mark(Label());

            return TryEmitWxObjectCommand(referenceCommand, functionFailure: failure);
        }

        private bool TryEmitWxUnaryObjectOverload(FunctionDefinition function)
        {
            if (function.Arity != 1 || function.Clauses is not [var first, var second])
            {
                return false;
            }

            var fallbackCommand = function with { Clauses = [second] };
            if (TryEmitWxUnaryTextObjectOverload(function, first, second)
                || TryEmitWxUnaryClassUnionOverload(function, owner.nextLabel - 2))
            {
                return true;
            }

            bool fallbackOptions = TryEmitWxOptionCommand(fallbackCommand, validateOnly: true);
            bool fallbackUnion = TryEmitWxClassUnionCommand(fallbackCommand, validateOnly: true);
            if (!fallbackOptions && !fallbackUnion && !TryEmitWxObjectCommand(fallbackCommand, validateOnly: true))
            {
                return false;
            }

            var optionCommand = function with { Clauses = [first] };
            bool mapsOptions = TryEmitWxOptionCommand(optionCommand, validateOnly: true);
            bool staticGuard = !mapsOptions && first.Guard is not null
                && first.Body is Expr.Call or Expr.Sequence { Items: [Expr.Call, _] }
                && TryEmitWxStaticCommand(optionCommand, validateOnly: true);
            ForwardGuard? tests = null;
            bool colourGuard = false;
            int entry = 0;
            if (!mapsOptions && !staticGuard)
            {
                if (first.Patterns is not [Pattern.Variable parameter] || parameter.Name == VariableScopeNames.Wildcard
                    || first.Guard is not { } guard
                    || first.Body is not Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments:
                        [Expr.Variable input, Expr.List { Items: [], Tail: null } or Expr.Literal { Value: Nil }]
                    } forward
                    || input.Name != parameter.Name || !entries.TryGetValue((forward.Function, 2), out entry))
                {
                    return false;
                }

                colourGuard = IsWxColourGuard(guard, parameter.Name);
                tests = ReadForwardGuard(guard, [parameter.Name]);
                if (!colourGuard && tests is not (ForwardGuard.Test or ForwardGuard.Disjunction
                    { Left: ForwardGuard.Test, Right: ForwardGuard.Test }))
                {
                    return false;
                }
            }

            int failure = owner.nextLabel - 2;
            if (mapsOptions)
            {
                // The option command emits its missing-environment label before the fallback.
                TryEmitWxOptionCommand(optionCommand, guardFailure: owner.nextLabel + 1);
            }
            else if (staticGuard)
            {
                // The environment failure block is placed before the next clause.
                TryEmitWxStaticCommand(optionCommand, guardFailure: owner.nextLabel + 1);
            }
            else
            {
                int fallback = owner.nextLabel + (colourGuard || tests is ForwardGuard.Disjunction ? 2 : 0);
                if (colourGuard)
                {
                    EmitWxColourGuard(0, 1, fallback);
                }
                else
                {
                    EmitForwardGuard(tests!, fallback);
                }
                owner.EmitConstant(Nil.Value, 1);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entry);
            }

            Mark(Label());
            if (fallbackOptions)
            {
                TryEmitWxOptionCommand(fallbackCommand, guardFailure: failure);
            }
            else if (fallbackUnion)
            {
                TryEmitWxClassUnionCommand(fallbackCommand, functionFailure: failure);
            }
            else
            {
                TryEmitWxObjectCommand(fallbackCommand, functionFailure: failure);
            }

            return true;
        }

        private bool TryEmitWxObjectOptionOverload(FunctionDefinition function)
        {
            int arity = function.Arity;
            if (arity < 2 || function.Clauses is not [var forward, var options] || forward.Guard is not { } guard
                || forward.Body is not Expr.Call { Module: null, DynamicRemote: false } call
                || call.Arguments.Count != arity + 1
                || call.Arguments[^1] is not (Expr.List { Items: [], Tail: null } or Expr.Literal { Value: Nil })
                || !entries.TryGetValue((call.Function, arity + 1), out int entry))
            {
                return false;
            }

            if (ReadForwardParameters(forward) is not { } signature)
            {
                return false;
            }

            var (parameters, shapes, fields) = signature;
            if (parameters.Length != arity
                || !call.Arguments.Take(arity).SequenceEqual(parameters.Select(name => new Expr.Variable(name)))
                || ReadForwardGuard(guard, parameters, fields) is not { } tests)
            {
                return false;
            }

            var ordered = FlattenForwardConjunction(tests).ToArray();
            if (shapes.Count == 0 && !owner.ModuleOptimizationDisabled && owner.SsaTypeInferenceEnabled
                && ReadSparseWxTextForward(ordered, arity) is { } sparse
                && TryEmitWxSparseTextOptionOverload(
                    function,
                    options,
                    entry,
                    sparse
                ))
            {
                return true;
            }

            int sharedRecords = 0;
            while (sharedRecords < Math.Min(ordered.Length, arity - 1)
                && ordered[sharedRecords] is ForwardGuard.Test
                { Opcode: IsTaggedTuple, Tag.Name: "wx_ref", Arity: 4, Register: var checkedRegister }
                && checkedRegister == sharedRecords)
            {
                sharedRecords++;
            }

            var branchTests = ordered[sharedRecords..];
            bool validBranch = shapes.Count == 0
                ? branchTests.Length > 0 && branchTests.All(test => test is ForwardGuard.Test
                    or ForwardGuard.Disjunction { Left: ForwardGuard.Test, Right: ForwardGuard.Test })
                : shapes is [{ Register: var tupleRegister }] && tupleRegister == arity - 1
                    && branchTests.All(test => test is ForwardGuard.FieldTest
                    { Register: var fieldRegister } && fieldRegister == tupleRegister);
            if (sharedRecords == 0 || !validBranch)
            {
                return false;
            }

            var command = function with { Clauses = [options] };
            if (!TryEmitWxObjectCommand(command, validateOnly: true, checkedRecordPrefix: sharedRecords))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            for (int register = 0; register < sharedRecords; register++)
            {
                EmitForwardGuard(ordered[register], failure);
            }

            int fallback = owner.nextLabel + branchTests.Count(test => test is ForwardGuard.Disjunction) * 2;
            foreach (var (register, tupleArity) in shapes)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    register,
                    tupleArity,
                    fallback
                );
            }

            foreach (var test in branchTests)
            {
                EmitForwardGuard(test, fallback);
            }
            owner.EmitConstant(Nil.Value, arity);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity + 1);
            owner.Operand(LabelTag, entry);
            Mark(Label());
            TryEmitWxObjectCommand(command, functionFailure: failure, checkedRecordPrefix: sharedRecords);

            return true;
        }

        private bool TryEmitWxSparseTextOptionOverload(
            FunctionDefinition function,
            Clause options,
            int entry,
            SparseWxTextForward forward
        )
        {
            var command = function with { Clauses = [options] };
            if (!TryEmitWxObjectCommand(command, validateOnly: true, sparseForward: forward))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int fallback = owner.nextLabel + 2;
            foreach (var record in forward.Records)
            {
                EmitForwardGuard(record, record.Register == function.Arity - 1 ? fallback : failure);
            }

            foreach (ForwardGuard test in forward.Values)
            {
                EmitForwardGuard(test, test is ForwardGuard.Disjunction ? fallback : failure);
            }

            owner.EmitConstant(Nil.Value, function.Arity);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, function.Arity + 1);
            owner.Operand(LabelTag, entry);
            Mark(Label());
            TryEmitWxObjectCommand(command, functionFailure: failure, sparseForward: forward);

            return true;
        }

        private bool TryEmitWxObjectCommand(
            FunctionDefinition function,
            int? sharedType = null,
            int? guardFailure = null,
            bool validateOnly = false,
            int? functionFailure = null,
            int checkedRecordPrefix = 0,
            bool textThenObject = false,
            bool listThenObject = false,
            bool numericThenObject = false,
            int? preparedSecondClass = null,
            bool preparedTupleOverload = false,
            bool sharedObjectPair = false,
            SparseWxTextForward? sparseForward = null,
            bool sharedReceiverForward = false,
            bool scalarOptionPair = false,
            bool tupleConstructorArm = false,
            bool preparedTupleSetter = false,
            bool scalarAfterUnion = false
        )
        {
            if (function.Clauses is not [var clause] || clause.Body is not Expr.Sequence sequence)
            {
                return false;
            }

            int arity = function.Arity;
            var arguments = new string[arity];
            var objectTypes = new Dictionary<string, int>(StringComparer.Ordinal);
            var tupleArities = new int?[arity];
            var variables = new HashSet<string>(StringComparer.Ordinal);
            var guardNames = new List<string>();
            var guardSources = new List<(int Register, int? Field)>();
            for (int register = 0; register < arity; register++)
            {
                Pattern pattern = clause.Patterns[register];
                if (pattern is Pattern.Variable plain)
                {
                    arguments[register] = plain.Name;
                    guardNames.Add(plain.Name);
                    guardSources.Add((register, null));
                }
                else if (pattern is AliasPattern alias
                    && (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is { } binding
                    && (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is Pattern.Tuple tuple)
                {
                    arguments[register] = binding.Name;
                    if (tuple.Items is [
                        Pattern.Literal { Value: Atom { Name: "wx_ref" } },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable type,
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }])
                    {
                        if (!variables.Add(type.Name) || !objectTypes.TryAdd(type.Name, register))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        tupleArities[register] = tuple.Items.Count;
                        for (int field = 0; field < tuple.Items.Count; field++)
                        {
                            if (tuple.Items[field] is not Pattern.Variable element
                                || element.Name == VariableScopeNames.Wildcard || !variables.Add(element.Name))
                            {
                                return false;
                            }
                            guardNames.Add(element.Name);
                            guardSources.Add((register, field));
                        }
                    }
                }
                else
                {
                    return false;
                }

                if (arguments[register] == VariableScopeNames.Wildcard || !variables.Add(arguments[register]))
                {
                    return false;
                }
            }

            int classCount = objectTypes.Count;
            if (sequence.Items.Count <= classCount)
            {
                return false;
            }

            int queueIndex = classCount;
            bool interleavesTextConversion = classCount >= 2
                && sequence.Items[1] is Expr.Match
                {
                    Value: Expr.Call { Module: "unicode", Function: "characters_to_binary" }
                };
            int conversionIndex = interleavesTextConversion ? 1 : queueIndex;
            Expr.Call? convert = null;
            string? convertedName = null;
            int convertedRegister = -1;
            if (sequence.Items[conversionIndex] is Expr.Match
                {
                    Pattern: Pattern.Variable converted,
                    Value: Expr.Call
                    {
                        DynamicRemote: false, Arguments: [Expr.Variable input]
                    } conversion
                } && !variables.Contains(converted.Name) && converted.Name != VariableScopeNames.Wildcard
                && (conversion is { Module: "unicode", Function: "characters_to_binary" }
                    || conversion.Module is null && conversion.Function == MappingName(function.Name, arity)
                    && localFunctions.TryGetValue((conversion.Function, 1), out var helper)
                    && ReadClosedLocalMapping(helper) is { Mapped.Module: "unicode" }))
            {
                convertedRegister = Array.IndexOf(arguments, input.Name);
                if (convertedRegister < 0 || clause.Patterns[convertedRegister] is not Pattern.Variable { })
                {
                    return false;
                }

                convert = conversion;
                convertedName = converted.Name;
                queueIndex++;
            }

            var preliminaryConversions = new List<(Expr.Call Call, string Name, int Register)>();
            while (convert is { Module: "unicode" }
                && queueIndex < sequence.Items.Count
                && sequence.Items[queueIndex] is Expr.Match
                {
                    Pattern: Pattern.Variable nextOutput,
                    Value: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable nextInput] } nextConversion
                }
                && (nextConversion is { Module: "unicode", Function: "characters_to_binary" }
                    || nextConversion.Module is null && nextConversion.Function == MappingName(function.Name, arity)
                    && localFunctions.TryGetValue((nextConversion.Function, 1), out var listHelper)
                    && ReadClosedLocalMapping(listHelper) is { Mapped.Module: "unicode" }))
            {
                int nextRegister = Array.IndexOf(arguments, nextInput.Name);
                if (convertedRegister <= 0 || nextRegister <= convertedRegister
                    || clause.Patterns[nextRegister] is not Pattern.Variable { }
                    || nextOutput.Name == convertedName || nextOutput.Name == VariableScopeNames.Wildcard
                    || variables.Contains(nextOutput.Name) || preliminaryConversions.Any(item => item.Name == nextOutput.Name))
                {
                    return false;
                }

                preliminaryConversions.Add((convert, convertedName!, convertedRegister));
                convert = nextConversion;
                convertedName = nextOutput.Name;
                convertedRegister = nextRegister;
                queueIndex++;
            }

            Expr.Fun? optionMapper = null;
            Expr.Call? optionMap = null;
            string? mappedOptions = null;
            if (arity >= 2 && clause.Patterns[^1] is Pattern.Variable
                && sequence.Items.Count > queueIndex + 2
                && sequence.Items[queueIndex] is Expr.Match
                {
                    Pattern: Pattern.Variable mapper,
                    Value: Expr.Fun lambda
                }
                && sequence.Items[queueIndex + 1] is Expr.Match
                {
                    Pattern: Pattern.Variable mapped,
                    Value: Expr.Call
                    {
                        Module: "lists", Function: "map",
                        Arguments: [Expr.Variable mapperArgument, Expr.Variable mapInput]
                    } map
                }
                && mapInput.Name == arguments[^1] && mapperArgument.Name == mapper.Name
                && mapper.Name != mapped.Name && mapper.Name != convertedName && mapped.Name != convertedName
                && preliminaryConversions.All(item => item.Name != mapper.Name && item.Name != mapped.Name)
                && !variables.Contains(mapper.Name) && !variables.Contains(mapped.Name)
                && mapper.Name != VariableScopeNames.Wildcard && mapped.Name != VariableScopeNames.Wildcard
                && lambda.SelfName is null && ReadWxOptions(lambda.Clauses, out _, out _))
            {
                optionMapper = lambda;
                optionMap = map;
                mappedOptions = mapped.Name;
                queueIndex += 2;
            }
            bool convertsBeforeMapping = convert is not null && optionMapper is not null;
            bool staticTextOptions = classCount == 0 && arity == 2 && convertsBeforeMapping;
            bool staticMappedOptions = classCount == 0 && arity == 2 && optionMapper is not null
                && convert is null && clause.Patterns.All(pattern => pattern is Pattern.Variable { });
            if (preliminaryConversions.Count > 0 && optionMapper is null
                && (convertedRegister != arity - 1 || convert is not { Module: "unicode" })
                || !staticTextOptions && !staticMappedOptions && (classCount == 0 || convertedRegister == 0)
                || convertsBeforeMapping && convertedRegister >= arity - 1)
            {
                return false;
            }

            (TaggedCallMatch Check, EntryCall Call)? mappedOptionsCheck = null;
            if (arity == 2 && classCount == 1 && optionMap is not null && convert is null && sharedType is null
                && ReadDiscardedTaggedCallMatch(sequence.Items.ElementAtOrDefault(queueIndex)) is { } check
                && ReadEntryCall(check.Call, []) is { Call.Arguments.Count: 0 } checkedCall)
            {
                mappedOptionsCheck = (check, checkedCall);
                queueIndex++;
            }

            if (sequence.Items.Count <= queueIndex || sequence.Items.Count > queueIndex + 2
                || sequence.Items[queueIndex] is not Expr.Call { Module: "wxe_util", Function: "queue_cmd" } queue
                || queue.Arguments.Count != arity + 2
                || queue.Arguments[arity] is not Expr.Case environment || !IsWxEnvironment(environment)
                || queue.Arguments[arity + 1] is not Expr.Literal { Value: Integer command })
            {
                return false;
            }

            if (optionMapper is not null)
            {
                var bindings = variables.Concat(preliminaryConversions.Select(item => item.Name));
                if (convertedName is not null)
                {
                    bindings = bindings.Append(convertedName);
                }

                if (optionMap is not { Arguments: [Expr.Variable mapperBinding, _] } || mappedOptions is null
                    || !UsesFreshWxEnvironment(environment, bindings.Concat([mapperBinding.Name, mappedOptions])))
                {
                    return false;
                }
            }

            if (convert is null && optionMapper is null && !UsesFreshWxEnvironment(environment, variables))
            {
                return false;
            }

            int firstColourRegister = -1;
            if (convert is null)
            {
                for (int register = 0; register < arity; register++)
                {
                    if (ReadWxColourArgument(queue.Arguments[register], arguments[register]) is not { } colour)
                    {
                        continue;
                    }

                    int next = register + 1;
                    if (!staticMappedOptions && next < arity
                        && ReadWxColourArgument(queue.Arguments[next], arguments[next]) is { } second)
                    {
                        firstColourRegister = register;
                        preliminaryConversions.Add((colour, arguments[register], register));
                        convert = second;
                        convertedRegister = next;
                    }
                    else if (register > 0 || staticMappedOptions)
                    {
                        convert = colour;
                        convertedRegister = register;
                    }

                    if (convert is not null)
                    {
                        break;
                    }
                }
            }

            bool inlineColour = convert is { Module: "wxe_util", Function: "color" };
            bool colourAfterOptions = inlineColour && optionMapper is not null && !staticMappedOptions;
            if (mappedOptionsCheck is not null && convert is not null
                || colourAfterOptions && (convertedRegister != arity - 2 || convertedRegister == 0
                    || sharedType is not null || interleavesTextConversion))
            {
                return false;
            }

            for (int register = 0; register < arity; register++)
            {
                if (inlineColour && (register == convertedRegister || register == firstColourRegister))
                {
                    continue;
                }

                string expected = register == convertedRegister ? convertedName!
                    : register == arity - 1 ? mappedOptions ?? arguments[register]
                    : preliminaryConversions.Find(item => item.Register == register).Name ?? arguments[register];
                if (queue.Arguments[register] is not Expr.Variable forwarded || forwarded.Name != expected)
                {
                    return false;
                }
            }

            Expr? result = sequence.Items.Count == queueIndex + 2 ? sequence.Items[^1] : null;
            Expr.Call? receive = result as Expr.Call;
            if (result is not null && result is not Expr.Literal { Value: Atom { Name: "ok" } }
                && (receive is not
                {
                    Module: "wxe_util", Function: "rec",
                    Arguments: [Expr.Literal { Value: Integer received }]
                } || !received.Equals(command)))
            {
                return false;
            }

            if (staticMappedOptions)
            {
                return receive is not null && (convert is null || convertedRegister == 0)
                    && TryEmitWxStaticMappedOptions(
                        clause.Guard,
                        arguments,
                        optionMapper!,
                        optionMap!,
                        queue,
                        receive,
                        command,
                        convert,
                        validateOnly,
                        guardFailure ?? functionFailure
                    );
            }

            var classChecks = new List<(int Register, Atom Class, Expr.Call Call)>();
            var checkedObjects = new HashSet<int>();
            for (int index = 0; index < classCount; index++)
            {
                Expr item = sequence.Items[interleavesTextConversion && index > 0 ? index + 1 : index];
                if (item is not Expr.Binary
                    {
                        Operator: ErlangOperators.OrElse,
                        Left: Expr.Binary
                        {
                            Operator: ErlangOperators.ExactEqual,
                            Left: Expr.Variable testedType, Right: Expr.Literal { Value: Atom expectedClass }
                        },
                        Right: Expr.Call
                        {
                            DynamicRemote: true,
                            Arguments: [Expr.Variable dispatchedType, Expr.Literal { Value: Atom { Name: "parent_class" } },
                            Expr.List { Items: [Expr.Literal { Value: Atom passedClass }], Tail: null }]
                        } classCall
                    } || testedType.Name != dispatchedType.Name || !expectedClass.Equals(passedClass)
                    || !objectTypes.TryGetValue(testedType.Name, out int register) || !checkedObjects.Add(register))
                {
                    return false;
                }

                classChecks.Add((register, expectedClass, classCall));
            }

            bool interleavesMappedText = interleavesTextConversion && optionMapper is not null
                && !owner.ModuleOptimizationDisabled && owner.SsaTypeInferenceEnabled
                && arity is >= 4 and <= 6 && classChecks[0].Register == 0
                && convertedRegister >= 1 && convertedRegister < arity - 2
                && classChecks.Skip(1).Select(check => check.Register)
                    .SequenceEqual(Enumerable.Range(convertedRegister + 1, arity - convertedRegister - 2))
                && preliminaryConversions.Count == 0 && sharedType is null
                && convert is { Module: "unicode", Function: "characters_to_binary" }
                && receive is { DynamicRemote: false } && !queue.DynamicRemote
                && optionMap is { DynamicRemote: false, Arguments: [Expr.Variable mapperInput, _] }
                && convertedName is not null && mappedOptions is not null
                && UsesFreshWxEnvironment(environment, variables.Concat([convertedName, mapperInput.Name, mappedOptions]))
                && ClosureCaptures.Capture(optionMapper, variables.Append(convertedName)
                    .ToDictionary(name => name, _ => (Term)Nil.Value)).Count == 0;
            if (interleavesTextConversion && !interleavesMappedText
                && (convert is null || convertedRegister != 1 || classChecks[0].Register != 0
                    || clause.Guard is null || result is not null || optionMapper is not null
                    || preliminaryConversions.Count != 0 || sharedType is not null && !numericThenObject))
            {
                return false;
            }

            Expr? guard = clause.Guard;
            ForwardGuard? combinedGuard = null;
            bool convertsMiddleArgument = convert is not null && !convertsBeforeMapping && convertedRegister < arity - 1;
            var guardFields = new Dictionary<string, (int Register, int Index)>(StringComparer.Ordinal);
            for (int index = 0; index < guardSources.Count; index++)
            {
                var (register, field) = guardSources[index];
                if (field is int tupleField)
                {
                    guardFields.Add(guardNames[index], (register, tupleField));
                }
            }

            if (firstColourRegister >= 0 || colourAfterOptions)
            {
                int[] colourInputs = firstColourRegister >= 0 ? [firstColourRegister, convertedRegister] : [convertedRegister];
                if (guard is null || ReadForwardGuard(
                    guard,
                    arguments,
                    guardFields,
                    allowColour: true
                ) is not { } colourGuard)
                {
                    return false;
                }

                var tests = FlattenForwardConjunction(colourGuard).ToArray();
                if (!tests.OfType<ForwardGuard.ColourTest>()
                        .Select(test => test.Register).SequenceEqual(colourInputs)
                    || colourAfterOptions && (tests[^1] is not ForwardGuard.Test { Opcode: IsList } list
                        || list.Register != arity - 1))
                {
                    return false;
                }

                combinedGuard = colourGuard;
                guard = null;
            }
            else if (convertsBeforeMapping && (convert is { Module: not null } || preliminaryConversions.Count > 0))
            {
                if (guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                    || conjunction.Right is not Expr.Call { Function: "is_list", Arguments: [Expr.Variable options] }
                    || options.Name != arguments[^1]
                    || ReadForwardGuard(guard, arguments, guardFields) is not { } entryGuard)
                {
                    return false;
                }

                combinedGuard = entryGuard;
                guard = null;
            }
            else if (inlineColour && convertsMiddleArgument)
            {
                if (guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                    || !IsWxColourGuard(conjunction.Left, arguments[convertedRegister])
                    || ReadForwardGuard(conjunction.Right, arguments, guardFields) is not { } suffix)
                {
                    return false;
                }

                combinedGuard = new ForwardGuard.Conjunction(
                    new ForwardGuard.ColourTest(convertedRegister, arity), suffix);
                guard = null;
            }
            else if (!convertsBeforeMapping
                && (preliminaryConversions.Count > 0 || convertsMiddleArgument || convert is { Module: null }))
            {
                combinedGuard = guard is null ? null : ReadForwardGuard(guard, arguments, guardFields);
                if (guard is not null && combinedGuard is null)
                {
                    return false;
                }

                guard = null;
            }
            else if (convert is { Module: not null })
            {
                Expr candidate = guard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                    ? conjunction.Right : guard!;

                bool Matches(Expr? value) => inlineColour
                    ? IsWxColourGuard(value, arguments[^1])
                    : IsWxCharDataGuard(value, arguments[^1]);
                if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso } prefix && Matches(candidate))
                {
                    guard = prefix.Left;
                }
                else if (Matches(guard))
                {
                    guard = null;
                }
                else
                {
                    return false;
                }
            }
            var guards = new List<(byte Opcode, int Register)>();
            if (guard is not null && !ReadWxStaticGuards(guard, guardNames.ToArray(), guards))
            {
                return false;
            }

            if (optionMapper is not null && combinedGuard is null && (guards.Count == 0 || guards[^1].Opcode != IsList
                || guardSources[guards[^1].Register] != (arity - 1, null)))
            {
                return false;
            }

            bool hasSharedObjectArm = sharedObjectPair && classChecks.Count == arity && convert is null;
            bool scalarObjectFallback = scalarOptionPair && guardFailure is null;
            bool objectFallback = hasSharedObjectArm || scalarObjectFallback
                || (textThenObject || listThenObject || numericThenObject) && guardFailure is null;
            if (scalarOptionPair)
            {
                var expected = scalarObjectFallback
                    ? new[] { (IsInteger, 1), (IsInteger, 2) }
                    : new[] { (IsInteger, 1), (IsInteger, 2), (IsList, 3) };
                if (receive is not { DynamicRemote: false } || queue.DynamicRemote || convert is not null
                    || combinedGuard is not null || (optionMapper is null) != scalarObjectFallback
                    || classChecks.Count != (scalarObjectFallback ? 2 : 1)
                    || classChecks[0].Register != 0 || scalarObjectFallback && classChecks[1].Register != 3
                    || !guards.Select(test => (test.Opcode, guardSources[test.Register]))
                        .SequenceEqual(expected.Select(test => (test.Item1, (test.Item2, (int?)null))))
                    || optionMapper is not null && ClosureCaptures.Capture(
                        optionMapper,
                        variables.ToDictionary(name => name, _ => (Term)Nil.Value)).Count != 0)
                {
                    return false;
                }
            }
            if (sharedObjectPair)
            {
                if (receive is null || optionMapper is not null || preliminaryConversions.Count != 0
                    || interleavesTextConversion || combinedGuard is not null
                    || classChecks.Count != (hasSharedObjectArm ? arity : arity - 1)
                    || classChecks.Where((check, index) => check.Register != index).Any()
                    || !UsesFreshWxEnvironment(environment, convertedName is null ? variables : variables.Append(convertedName))
                    || (guardFailure is not null
                        ? convert is not null || (hasSharedObjectArm
                            ? guards.Count != 0
                            : guards is not [(IsInteger, _)] || guardSources[guards[0].Register] != (arity - 1, null))
                        : guards.Count != 0 || !hasSharedObjectArm && (convert is null || convertedRegister != arity - 1)))
                {
                    return false;
                }
            }

            if (numericThenObject && (result is not null || optionMapper is not null || preliminaryConversions.Count != 0
                || convert is not { Module: "unicode", Function: "characters_to_binary" } || convertedRegister != 1
                || classChecks.Count != (objectFallback ? 2 : 1) || classChecks[0].Register != 0
                || objectFallback && (!interleavesTextConversion || classChecks[1].Register != arity - 1)
                || !objectFallback && (interleavesTextConversion || combinedGuard is null
                    || FlattenForwardConjunction(combinedGuard).ToArray() is not
                    [
                        ForwardGuard.Disjunction
                    {
                        Left: ForwardGuard.Test { Opcode: IsList, Register: 1 },
                        Right: ForwardGuard.Test { Opcode: IsBinary, Register: 1 }
                    },
                        .. var scalarTests
                    ] || scalarTests.Length == 0
                    || scalarTests.Any(test => test is not ForwardGuard.Test { Opcode: IsNumber }))))
            {
                return false;
            }
            if (listThenObject && (receive is null || convert is not null
                || classChecks.Count != (objectFallback ? arity : arity - 1)
                || classChecks.Where((check, index) => check.Register != index).Any()
                || (objectFallback ? optionMapper is not null : optionMapper is null || guards is not [(IsList, _)])))
            {
                return false;
            }

            if (listThenObject && arity == 2 || mappedOptionsCheck is not null)
            {
                string reference = ((Expr.Variable)environment.Clauses[0].Body).Name;
                if (variables.Contains(reference) || reference == mappedOptions
                    || optionMap?.Arguments[0] is Expr.Variable mapperVariable && mapperVariable.Name == reference
                    || optionMapper is not null && ClosureCaptures.Capture(optionMapper,
                        variables.ToDictionary(name => name, _ => (Term)Nil.Value)).Count != 0)
                {
                    return false;
                }
            }

            if (textThenObject && (classChecks.Count != (objectFallback ? 2 : 1)
                || classChecks[0].Register != 0 || optionMapper is not null
                || (objectFallback ? classChecks[1].Register != 1 || convert is not null
                    : convert is not { Module: "unicode", Function: "characters_to_binary" } || guards.Count != 0)))
            {
                return false;
            }

            if (preparedTupleOverload && (classChecks.Count != (preparedSecondClass is null ? 1 : 2)
                || classChecks.Where((check, index) => check.Register != index).Any()
                || convert is not null || optionMapper is not null || guards.Count != 0
                || (preparedTupleSetter ? result is not null || arity != 2 || queue.DynamicRemote : receive is null)))
            {
                return false;
            }

            if (scalarAfterUnion && (sharedType is null || arity is < 2 or > 4 || classChecks is not [{ Register: 0 }]
                || (optionMapper is not null
                    ? arity != 3 || guards is not [{ Opcode: IsInteger }, { Opcode: IsList }]
                    : guards.Count != (arity == 2 ? 1 : 3) || guards.Any(guard => guard.Opcode != IsInteger))
                || combinedGuard is not null || convert is not null || receive is null))
            {
                return false;
            }

            if (!scalarAfterUnion && !scalarOptionPair && !sharedObjectPair && !preparedTupleOverload && !textThenObject && !listThenObject
                && !numericThenObject && sharedType is not null && (classChecks.Count != 1 || classChecks[0].Register != 0
                || optionMapper is not null || result is not null || convertedRegister == 0
                || (convert is null
                    ? guardFailure is null || guards.Count != 1 || guards[0].Opcode != IsInteger
                    : guards.Count != 0 || (inlineColour ? guardFailure is not null
                        : convert is not { Module: "unicode", Function: "characters_to_binary" }))))
            {
                return false;
            }

            if (sharedReceiverForward
                && (receive is not { DynamicRemote: false } || queue.DynamicRemote
                    || convert is not null || optionMapper is not null || combinedGuard is not null
                    || classChecks is not [{ Register: 0 }, { Register: var other }] || other != arity - 1
                    || !guards.Select(test => (test.Opcode, guardSources[test.Register])).SequenceEqual(
                        Enumerable.Range(1, arity - 2).Select(register => (IsInteger, (register, (int?)null))))))
            {
                return false;
            }

            if (sparseForward is not null
                && (combinedGuard is null || !convertsBeforeMapping
                    || convert is not { Module: "unicode", Function: "characters_to_binary" }
                    || preliminaryConversions.Count != 0 || !checkedObjects.SetEquals(sparseForward.Records
                        .Take(sparseForward.Records.Length - 1).Select(test => test.Register))
                    || !FlattenForwardConjunction(combinedGuard).SequenceEqual(
                        sparseForward.Values.Append(new ForwardGuard.Test(IsList, arity - 1)))
                    || optionMap is not { DynamicRemote: false, Arguments: [Expr.Variable forwardMapperInput, _] }
                    || convertedName is null || mappedOptions is null || receive is not { DynamicRemote: false }
                    || queue.DynamicRemote || !UsesFreshWxEnvironment(
                        environment,
                        variables.Concat([convertedName, forwardMapperInput.Name, mappedOptions]))
                    || ClosureCaptures.Capture(optionMapper!, variables.Append(convertedName)
                        .ToDictionary(name => name, _ => (Term)Nil.Value)).Count != 0))
            {
                return false;
            }

            if (tupleConstructorArm && (receive is not { DynamicRemote: false } || optionMapper is null
                || mappedOptionsCheck is not null || preliminaryConversions.Count != 0
                || (checkedRecordPrefix == 0 ? !staticTextOptions || convertedRegister != 0
                    || combinedGuard is not ForwardGuard.Conjunction
                    {
                        Right: ForwardGuard.Test { Opcode: IsList, Register: 1 }
                    } textGuard || !IsWxChardataTest(textGuard.Left)
                    : classChecks is not [{ Register: 0 }] || combinedGuard is not null
                        || guards.Count != 1 || guards[0].Opcode != IsList)))
            {
                return false;
            }

            if (checkedRecordPrefix > 0 && (Enumerable.Range(0, checkedRecordPrefix).Any(register => !checkedObjects.Contains(register))
                || convert is not null || guards.Count == 0
                || guards[^1].Opcode is not (IsList or IsInteger)
                || guardSources[guards[^1].Register] != (arity - 1, null)
                || guards.Count > 1 && (optionMapper is null || guards[^1].Opcode != IsList
                    || guards.Take(guards.Count - 1).Any(test => guardSources[test.Register].Field is null))))
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            int failureLabel = functionFailure ?? owner.nextLabel - 2;
            if (objectFallback)
            {
                EmitDetailRecordTest(
                    arity - 1,
                    4,
                    Term.A("wx_ref"),
                    hasSharedObjectArm ? guardFailure ?? failureLabel : failureLabel
                );
            }

            for (int register = 0; sharedType is null && register < arity; register++)
            {
                if (checkedObjects.Contains(register) && register >= (sharedReceiverForward ? 1 : checkedRecordPrefix)
                    && (sparseForward is null || !sparseForward.Records.Any(record => record.Register == register)))
                {
                    EmitDetailRecordTest(
                        register,
                        4,
                        Term.A("wx_ref"),
                        failureLabel
                    );
                }
                else if (tupleArities[register] is int tupleArity)
                {
                    owner.Op(IsTuple, LabelTag, failureLabel);
                    owner.Operand(XRegisterTag, register);
                    owner.Op(TestArity, LabelTag, failureLabel);
                    owner.Operand(XRegisterTag, register);
                    owner.Operand(BeamWire.UnsignedTag, tupleArity);
                }
            }
            foreach (var (opcode, guardIndex) in guards.Skip(scalarOptionPair ? 2 : sharedReceiverForward || scalarAfterUnion ? guards.Count : 0))
            {
                var (register, field) = guardSources[guardIndex];
                if (field is int tupleField)
                {
                    owner.EmitTuplePart(register, tupleField, arity);
                    register = arity;
                }
                owner.Op(opcode, LabelTag, guardFailure ?? failureLabel);
                owner.Operand(XRegisterTag, register);
            }

            if (combinedGuard is not null)
            {
                EmitForwardGuard(combinedGuard, guardFailure ?? failureLabel);
            }
            else if (inlineColour)
            {
                int scratch = sharedObjectPair ? sharedType!.Value + 1 : sharedType is null ? arity : arity + 1;
                EmitWxColourGuard(arity - 1, scratch, failureLabel);
            }
            else if (convert is { Module: not null } && !convertsMiddleArgument)
            {
                int binary = Label();
                int valid = Label();
                owner.Op(IsList, LabelTag, binary);
                owner.Operand(XRegisterTag, arity - 1);
                owner.Op(Jump, LabelTag, valid);
                Mark(binary);
                owner.Op(IsBinary, LabelTag, guardFailure ?? failureLabel);
                owner.Operand(XRegisterTag, arity - 1);
                Mark(valid);
            }

            bool preservesSecondClass = preparedSecondClass is not null || listThenObject && arity == 3;
            int extraSlot = preservesSecondClass ? 1 : 0;
            int initialFrame = staticTextOptions ? 1 : arity + extraSlot;
            int live = sharedObjectPair ? sharedType!.Value + 1
                : preparedSecondClass is int secondType ? secondType + 1 : sharedType is null ? arity : sharedType.Value + 1;
            EmitFrameAllocation(live, initialFrame);
            if (preservesSecondClass)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    preparedSecondClass ?? arity,
                    YRegisterTag,
                    0
                );
            }
            for (int register = arity - 1; register >= (staticTextOptions ? 1 : 0); register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    arity - register - 1 + extraSlot
                );
            }

            BeamSourceLocation previous = location;
            for (int index = 0; index < classChecks.Count; index++)
            {
                var (register, expectedClass, classCall) = classChecks[index];
                int validated = Label();
                bool followsConversion = interleavesTextConversion && index == 1;
                if (followsConversion)
                {
                    int textSlot = arity - convertedRegister - 1;
                    owner.MoveRegister(
                        YRegisterTag,
                        textSlot,
                        XRegisterTag,
                        0
                    );
                    EmitFrameInitialization(textSlot, textSlot + 1);
                    owner.EmitCallLine(convert!, previous);
                    previous = CallLocation(convert!, previous);
                    owner.Op(ExternalCall, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, owner.Import("unicode", "characters_to_binary", 1));
                }

                bool savedClass = preservesSecondClass && index == 1;
                int type = followsConversion ? 1 : index == 0 ? sharedType ?? 0 : 0;
                if (!savedClass && (sharedType is null || index > 0))
                {
                    owner.Op(GetTupleElement, index == 0 ? XRegisterTag : YRegisterTag,
                        index == 0 ? register : arity - register - 1 + extraSlot);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                    owner.Operand(XRegisterTag, type);
                }

                if (followsConversion)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        arity - convertedRegister - 1
                    );
                }
                owner.Op(ExactNotEqualTest, LabelTag, validated);
                int typeTag = savedClass ? YRegisterTag : XRegisterTag;
                owner.Operand(typeTag, type);
                owner.LiteralOperand(expectedClass);
                if (!followsConversion)
                {
                    owner.MoveRegister(
                        typeTag,
                        type,
                        XRegisterTag,
                        1
                    );
                }
                owner.EmitConstant(Term.A("parent_class"), 2);
                if (savedClass)
                {
                    EmitFrameInitialization(0, 1);
                }
                owner.EmitConstant(expectedClass, 0);
                owner.EmitCallLine(classCall, previous);
                previous = CallLocation(classCall, previous);
                owner.Op(DynamicApply, BeamWire.UnsignedTag, 1);
                Mark(validated);
            }

            foreach (var (call, _, register) in colourAfterOptions ? [] : preliminaryConversions)
            {
                int slot = arity - register - 1;
                owner.MoveRegister(
                    YRegisterTag,
                    slot,
                    XRegisterTag,
                    0
                );
                EmitFrameInitialization(slot, slot + 1);
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(ExternalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, 1));
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
            }

            int missingEnvironment = Label();
            int matchFailure = mappedOptionsCheck is null ? 0 : Label();
            if (optionMapper is not null)
            {
                if (convert is not null && !colourAfterOptions && !interleavesMappedText)
                {
                    int convertedSlot = arity - convertedRegister - 1;
                    if (!staticTextOptions)
                    {
                        owner.MoveRegister(
                            YRegisterTag,
                            convertedSlot,
                            XRegisterTag,
                            0
                        );
                        EmitFrameInitialization(convertedSlot, convertedSlot + 1);
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

                    owner.EmitClosureAllocation(0, 1, 1);
                    EmitWxOptionMapper(optionMapper, 1);
                    if (staticTextOptions)
                    {
                        owner.MoveRegister(
                            YRegisterTag,
                            0,
                            XRegisterTag,
                            2
                        );
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            0
                        );
                    }
                    else
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            YRegisterTag,
                            convertedSlot
                        );
                        owner.MoveRegister(
                            YRegisterTag,
                            0,
                            XRegisterTag,
                            2
                        );
                    }


                    owner.MoveRegister(
                        XRegisterTag,
                        1,
                        XRegisterTag,
                        0
                    );
                    owner.MoveRegister(
                        XRegisterTag,
                        2,
                        XRegisterTag,
                        1
                    );
                }
                else
                {
                    owner.EmitClosureAllocation(0, 0, 1);
                    EmitWxOptionMapper(optionMapper, 0);
                    owner.MoveRegister(
                        YRegisterTag,
                        extraSlot,
                        XRegisterTag,
                        1
                    );
                }
                if (mappedOptionsCheck is not null)
                {
                    EmitFrameInitialization(0, 1);
                }
                else if (!staticTextOptions)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1 + extraSlot);
                    owner.Operand(BeamWire.UnsignedTag, arity - 1);
                }

                owner.EmitCallLine(optionMap!, previous);
                previous = CallLocation(optionMap!, previous);
                owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("lists", "map", 2));
                if (mappedOptionsCheck is { } checkedOptions)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        0
                    );
                    EmitEntryCall(checkedOptions.Call, previous);
                    previous = CallLocation(checkedOptions.Check.Call, previous);
                    EmitDetailRecordTest(
                        0,
                        checkedOptions.Check.Arity,
                        checkedOptions.Check.Tag,
                        matchFailure
                    );
                    EmitWxEnvironmentCheck(0, missingEnvironment);
                    owner.EmitTuplePart(0, 1, arity);
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        XRegisterTag,
                        1
                    );
                }
                else
                {
                    if (colourAfterOptions)
                    {
                        if (firstColourRegister >= 0)
                        {
                            EmitColourSwap(1, preliminaryConversions[0].Call);
                        }

                        EmitColourSwap(0, convert!);
                    }

                    EmitWxEnvironmentCheck(1, missingEnvironment);
                    owner.EmitTuplePart(1, 1, arity);
                    if (colourAfterOptions)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            XRegisterTag,
                            convertedRegister
                        );
                        if (firstColourRegister >= 0)
                        {
                            owner.MoveRegister(
                                YRegisterTag,
                                0,
                                XRegisterTag,
                                firstColourRegister
                            );
                        }

                        owner.MoveRegister(
                            YRegisterTag,
                            firstColourRegister >= 0 ? 1 : 0,
                            XRegisterTag,
                            arity - 1
                        );
                    }

                    int highestStackArgument = colourAfterOptions
                        ? (firstColourRegister >= 0 ? firstColourRegister : convertedRegister) - 1 : arity - 2;
                    for (int register = highestStackArgument; register >= 2; register--)
                    {
                        owner.MoveRegister(
                            YRegisterTag,
                            arity - register - 2,
                            XRegisterTag,
                            register
                        );
                    }

                    if (!colourAfterOptions)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            XRegisterTag,
                            arity - 1
                        );
                    }

                    if (highestStackArgument >= 1)
                    {
                        owner.MoveRegister(
                            YRegisterTag,
                            arity - 3,
                            XRegisterTag,
                            1
                        );
                    }
                }
            }
            else if (convert is null || interleavesTextConversion)
            {
                EmitWxEnvironmentCheck(0, missingEnvironment);
                owner.EmitTuplePart(0, 1, arity);
                for (int register = arity - 1; register > 0; register--)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        arity - register - 1 + extraSlot,
                        XRegisterTag,
                        register
                    );
                }
            }
            else
            {
                int droppedSlots = sharedObjectPair ? 1 + extraSlot : 1;
                int convertedSlot = arity - convertedRegister - 1 + droppedSlots - 1;
                owner.MoveRegister(
                    YRegisterTag,
                    convertedSlot,
                    XRegisterTag,
                    0
                );
                if (convertsMiddleArgument)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        YRegisterTag,
                        convertedSlot
                    );
                }
                owner.Op(TrimFrame, BeamWire.UnsignedTag, droppedSlots);
                owner.Operand(BeamWire.UnsignedTag, arity - 1);
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
                EmitWxEnvironmentCheck(1, missingEnvironment);
                owner.EmitTuplePart(1, 1, arity);
                if (convertsMiddleArgument)
                {
                    bool convertsTextBeforeScalarPair = convert.Module == "unicode"
                        && convertedRegister == arity - 3 && arity is 5 or 6
                        && checkedObjects.Count == 2;
                    int[] moveOrder = convertsTextBeforeScalarPair
                        ? (arity == 5 ? [3, 2, 4, 1] : [3, 5, 4, 2, 1])
                        : Enumerable.Range(1, arity - 1).Reverse().ToArray();
                    bool moveColourBeforeTrailingArgument = firstColourRegister > 0 && convertedRegister == arity - 2;
                    if (moveColourBeforeTrailingArgument)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            XRegisterTag,
                            convertedRegister
                        );
                    }

                    foreach (int register in moveOrder)
                    {
                        if (register == convertedRegister)
                        {
                            if (!moveColourBeforeTrailingArgument)
                            {
                                owner.MoveRegister(
                                    XRegisterTag,
                                    0,
                                    XRegisterTag,
                                    register
                                );
                            }
                        }
                        else
                        {
                            int slot = register == arity - 1 ? convertedSlot - 1 : arity - register - 2;
                            owner.MoveRegister(
                                YRegisterTag,
                                slot,
                                XRegisterTag,
                                register
                            );
                        }
                    }
                }
                else if (firstColourRegister >= 0)
                {
                    for (int register = arity - 2; register > 1; register--)
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
                    owner.MoveRegister(
                        YRegisterTag,
                        arity - 3,
                        XRegisterTag,
                        1
                    );
                }
                else
                {
                    bool moveConvertedFirst = arity == 3
                        && (inlineColour && (sharedObjectPair || result is null)
                            || convert.Module == "unicode"
                                && (result is null || checkedObjects.Contains(1) || preliminaryConversions.Count > 0
                                    || guards is [(IsInteger, var indexGuard)] && guardSources[indexGuard] == (1, null)));
                    if (moveConvertedFirst)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            XRegisterTag,
                            2
                        );
                    }

                    if (arity > 2)
                    {
                        owner.MoveRegister(
                            YRegisterTag,
                            0,
                            XRegisterTag,
                            arity - 2
                        );
                    }

                    if (!moveConvertedFirst)
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            0,
                            XRegisterTag,
                            arity - 1
                        );
                    }

                    for (int register = arity - 3; register > 0; register--)
                    {
                        owner.MoveRegister(
                            YRegisterTag,
                            arity - register - 2,
                            XRegisterTag,
                            register
                        );
                    }
                }
            }
            owner.EmitConstant(command, arity + 1);
            int frame = mappedOptionsCheck is not null || (convert is null || interleavesTextConversion) && optionMapper is null
                ? initialFrame
                : arity - 1;
            owner.MoveRegister(
                YRegisterTag,
                frame - 1,
                XRegisterTag,
                0
            );
            if (result is not null)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            owner.EmitCallLine(queue, previous);
            owner.Op(result is null ? TailExternalCallWithFrame : ExternalCall, BeamWire.UnsignedTag, arity + 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", arity + 2));

            if (result is null)
            {
                owner.Operand(BeamWire.UnsignedTag, frame);
            }
            else if (receive is null)
            {
                owner.EmitConstant(Term.A("ok"), 0);
                owner.EmitFrameReturn();
            }
            else
            {
                owner.EmitConstant(command, 0);
                owner.EmitCallLine(receive, CallLocation(queue, location));
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "rec", 1));
                owner.Operand(BeamWire.UnsignedTag, 0);
            }

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            var beforeMatchFailure = receive is null ? CallLocation(queue, previous) : CallLocation(receive, location);
            if (receive is not null || mappedOptionsCheck is not null)
            {
                var errorLocation = listThenObject && arity == 2 || mappedOptionsCheck is not null
                    ? (Expr.Call)environment.Clauses[1].Body
                    : queue;
                owner.EmitCallLine(errorLocation, beforeMatchFailure);
                beforeMatchFailure = CallLocation(errorLocation, beforeMatchFailure);
            }
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            owner.Operand(BeamWire.UnsignedTag, initialFrame);

            if (mappedOptionsCheck is { } failedCheck)
            {
                EmitTaggedCallMatchFailure(
                    failedCheck.Check.Match,
                    matchFailure,
                    previous,
                    beforeMatchFailure
                );
            }

            return true;

            void EmitColourSwap(int slot, Expr.Call call)
            {
                owner.Op(Swap, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 0);
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                Call(call.Module, call.Function, 1);
            }
        }

        private static Expr.Call? ReadWxColourArgument(Expr expression, string input) =>
            expression is Expr.Call
            {
                Module: "wxe_util", Function: "color", DynamicRemote: false,
                Arguments: [Expr.Variable colour]
            } call && colour.Name == input ? call : null;

        private bool IsWxColourGuard(Expr? guard, string argument)
        {
            if (guard is not Expr.Binary
                {
                    Operator: ErlangOperators.AndAlso,
                    Left: Expr.Binary { Operator: ErlangOperators.OrElse } sizes,
                    Right: Expr.Call
                    {
                        Function: "is_integer", Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Arguments: [Expr.Call
                        {
                            Function: "element", Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Arguments: [Expr.Literal { Value: Integer field }, Expr.Variable colour]
                        } element]
                    } test
                }
                || !field.Value.IsOne || colour.Name != argument
                || !owner.ResolvesErlangBif(test) || !owner.ResolvesErlangBif(element))
            {
                return false;
            }

            return ReadTupleArityGuard(sizes.Left) is { Arity: 3, Value: Expr.Variable rgb } && rgb.Name == argument
                && ReadTupleArityGuard(sizes.Right) is { Arity: 4, Value: Expr.Variable rgba } && rgba.Name == argument;
        }

        private void EmitWxColourGuard(int register, int scratch, int failure)
        {
            int rgba = Label();
            int valid = Label();
            owner.Op(IsTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
            owner.Op(TestArity, LabelTag, rgba);
            owner.Operand(XRegisterTag, register);
            owner.Operand(BeamWire.UnsignedTag, 3);
            JumpTo(valid);
            Mark(rgba);
            owner.Op(TestArity, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
            owner.Operand(BeamWire.UnsignedTag, 4);
            Mark(valid);
            owner.EmitTuplePart(register, 0, scratch);
            owner.Op(IsInteger, LabelTag, failure);
            owner.Operand(XRegisterTag, scratch);
        }

        private static bool IsWxCharDataGuard(Expr? guard, string argument) =>
            guard is Expr.Binary
            {
                Operator: ErlangOperators.OrElse,
                Left: Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, Function: "is_list",
                    Arguments: [Expr.Variable list]
                },
                Right: Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, Function: "is_binary",
                    Arguments: [Expr.Variable binary]
                }
            } && list.Name == argument && binary.Name == argument;

        private bool TryEmitWxScalarTupleOverload(FunctionDefinition function)
        {
            if (function.Arity < 2 || function.Clauses is not [var scalar, var tuple]
                || !HasUnconditionalParameters(scalar)
                || tuple.Patterns[^1] is not AliasPattern alias
                || scalar.Guard is null || tuple.Guard is null)
            {
                return false;
            }

            var pair = alias.Left as Pattern.Tuple ?? alias.Right as Pattern.Tuple;
            var whole = alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable;
            if (pair?.Items is not [Pattern.Variable first, Pattern.Variable second]
                || whole is null || tuple.Patterns.Take(function.Arity - 1).Any(pattern => pattern is not Pattern.Variable { }))
            {
                return false;
            }

            var scalarNames = scalar.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var tuplePatterns = tuple.Patterns.Take(function.Arity - 1).Append(whole).ToArray();
            var tupleNames = tuplePatterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            var tupleGuardNames = tupleNames.SkipLast(1).Append(first.Name).Append(second.Name).ToArray();
            if (tupleNames.Concat([first.Name, second.Name]).Distinct(StringComparer.Ordinal).Count() != function.Arity + 2
                || tupleGuardNames.Contains(VariableScopeNames.Wildcard))
            {
                return false;
            }

            var scalarTests = new List<(byte Opcode, int Register)>();
            var tupleTests = new List<(byte Opcode, int Register)>();
            if (!ReadWxStaticGuards(scalar.Guard, scalarNames, scalarTests)
                || !ReadWxStaticGuards(tuple.Guard, tupleGuardNames, tupleTests)
                || scalarTests.Count != function.Arity || tupleTests.Count != function.Arity + 1
                || scalarTests.Where((test, index) => test != (IsInteger, index)).Any()
                || tupleTests.Where((test, index) => test != (IsInteger, index)).Any())
            {
                return false;
            }

            var scalarCommand = function with { Clauses = [scalar with { Guard = null }] };
            var tupleCommand = function with { Clauses = [tuple with { Guard = null, Patterns = tuplePatterns }] };
            if (!TryEmitWxStaticCommand(scalarCommand, validateOnly: true)
                || !TryEmitWxStaticCommand(tupleCommand, validateOnly: true))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int tupleEntry = owner.nextLabel + 1;
            int last = function.Arity - 1;
            foreach (var (opcode, register) in scalarTests)
            {
                owner.Op(opcode, LabelTag, register == last ? tupleEntry : failure);
                owner.Operand(XRegisterTag, register);
            }

            TryEmitWxStaticCommand(scalarCommand);
            Mark(Label());
            owner.Op(IsTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, last);
            owner.Op(TestArity, LabelTag, failure);
            owner.Operand(XRegisterTag, last);
            owner.Operand(BeamWire.UnsignedTag, 2);
            for (int field = 0; field < 2; field++)
            {
                owner.EmitTuplePart(last, field, function.Arity);
                owner.Op(IsInteger, LabelTag, failure);
                owner.Operand(XRegisterTag, function.Arity);
            }

            return TryEmitWxStaticCommand(tupleCommand);
        }


        private bool TryEmitWxStaticCommand(FunctionDefinition function, int? guardFailure = null, bool validateOnly = false)
        {
            if (function.Clauses is not [var clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            Expr.Call queue;
            Expr? result = null;
            Expr.Match? conversion = null;
            switch (clause.Body)
            {
                case Expr.Call direct:
                    queue = direct;
                    break;
                case Expr.Sequence { Items: [Expr.Call first, var second] }:
                    queue = first;
                    result = second;
                    break;
                case Expr.Sequence { Items: [Expr.Match convert, Expr.Call call, ..] } converted
                    when converted.Items.Count <= 3:
                    conversion = convert;
                    queue = call;
                    result = converted.Items.Count == 3 ? converted.Items[2] : null;
                    break;
                default:
                    return false;
            }

            int arity = function.Arity;
            if (queue is not { Module: "wxe_util", Function: "queue_cmd" }
                || queue.Arguments.Count != arity + 2
                || queue.Arguments[arity] is not Expr.Case environment || !IsWxEnvironment(environment)
                || queue.Arguments[arity + 1] is not Expr.Literal { Value: Integer command })
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            Expr.Call? convertCall = null;
            Expr? scalarGuard = null;
            if (conversion is not null)
            {
                Expr? textGuard = clause.Guard;
                if (arity == 2 && textGuard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
                {
                    textGuard = conjunction.Left;
                    scalarGuard = conjunction.Right;
                }

                if (arity is not (1 or 2) || arity == 2 && scalarGuard is null || conversion is not
                    {
                        Pattern: Pattern.Variable output,
                        Value: Expr.Call
                        {
                            Module: "unicode", Function: "characters_to_binary",
                            Arguments: [Expr.Variable input]
                        } call
                    } || input.Name != parameters[0] || parameters.Contains(output.Name)
                    || output.Name == VariableScopeNames.Wildcard || !IsWxCharDataGuard(textGuard, input.Name))
                {
                    return false;
                }

                convertCall = call;
                parameters[0] = output.Name;
            }
            for (int index = 0; index < arity; index++)
            {
                if (queue.Arguments[index] is not Expr.Variable forwarded || forwarded.Name != parameters[index])
                {
                    return false;
                }
            }

            Expr.Call? receive = result as Expr.Call;
            if (result is not null && result is not Expr.Literal { Value: Atom { Name: "ok" } }
                && (receive is not
                {
                    Module: "wxe_util", Function: "rec",
                    Arguments: [Expr.Literal { Value: Integer received }]
                } || !received.Equals(command)))
            {
                return false;
            }

            var guards = new List<(byte Opcode, int Register)>();
            if (convertCall is null && clause.Guard is { } guard && !ReadWxStaticGuards(guard, parameters, guards))
            {
                return false;
            }

            bool preservesScalar = scalarGuard is not null;
            if (preservesScalar && (result is not null && receive is null || !ReadWxStaticGuards(scalarGuard!, parameters, guards)
                || guards is not [(IsInteger, 1)]))
            {
                return false;
            }

            if (preservesScalar && result is null)
            {
                string reference = ((Expr.Variable)environment.Clauses[0].Body).Name;
                var inputNames = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
                if (inputNames.Contains(reference) || parameters.Contains(reference)
                    || ReadForwardGuard(clause.Guard!, inputNames) is null)
                {
                    return false;
                }
            }

            if (validateOnly)
            {
                return true;
            }

            int functionFailure = guardFailure ?? owner.nextLabel - 2;

            foreach (var (opcode, register) in guards)
            {
                if (preservesScalar)
                {
                    break;
                }
                owner.Op(opcode, LabelTag, functionFailure);
                owner.Operand(XRegisterTag, register);
            }
            BeamSourceLocation previous = location;
            if (convertCall is not null)
            {
                int binary = Label();
                int valid = Label();
                owner.Op(IsList, LabelTag, binary);
                owner.Operand(XRegisterTag, 0);
                owner.Op(Jump, LabelTag, valid);
                Mark(binary);
                owner.Op(IsBinary, LabelTag, functionFailure);
                owner.Operand(XRegisterTag, 0);
                Mark(valid);
                if (preservesScalar)
                {
                    owner.Op(IsInteger, LabelTag, functionFailure);
                    owner.Operand(XRegisterTag, 1);
                }

                EmitFrameAllocation(arity, arity - 1);
                if (preservesScalar)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        1,
                        YRegisterTag,
                        0
                    );
                }
                owner.EmitCallLine(convertCall, previous);
                previous = CallLocation(convertCall, previous);
                owner.Op(ExternalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("unicode", "characters_to_binary", 1));
            }

            int missingEnvironment = Label();
            int environmentRegister = preservesScalar ? 1 : arity;
            EmitWxEnvironmentCheck(environmentRegister, missingEnvironment);
            if (convertCall is null && result is not null)
            {
                owner.Op(Allocate, BeamWire.UnsignedTag, 0);
                owner.Operand(BeamWire.UnsignedTag, arity + 1);
            }
            owner.EmitTuplePart(environmentRegister, 1, arity);
            owner.EmitConstant(command, arity + 1);
            if (preservesScalar)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
                if (receive is not null)
                {
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                    owner.Operand(BeamWire.UnsignedTag, 0);
                }
            }
            owner.EmitCallLine(queue, previous);
            owner.Op(result is not null ? ExternalCall : convertCall is null ? TailExternalCall : TailExternalCallWithFrame,
                BeamWire.UnsignedTag, arity + 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "queue_cmd", arity + 2));

            if (result is null && convertCall is not null)
            {
                owner.Operand(BeamWire.UnsignedTag, arity - 1);
            }

            if (receive is not null)
            {
                owner.EmitConstant(command, 0);
                owner.EmitCallLine(receive, CallLocation(queue, location));
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import("wxe_util", "rec", 1));
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
            else if (result is not null)
            {
                owner.EmitConstant(Term.A("ok"), 0);
                owner.EmitFrameReturn();
            }

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            if (receive is not null)
            {
                owner.EmitCallLine(queue, CallLocation(receive, location));
            }
            else if (preservesScalar)
            {
                owner.EmitCallLine((Expr.Call)environment.Clauses[1].Body, CallLocation(queue, previous));
            }
            owner.Op(convertCall is null ? TailExternalCall : TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            if (convertCall is not null)
            {
                owner.Operand(BeamWire.UnsignedTag, arity - 1);
            }

            return true;
        }

        private bool ReadWxStaticGuards(Expr guard, string[] parameters, List<(byte Opcode, int Register)> guards)
        {
            if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
            {
                return ReadWxStaticGuards(conjunction.Left, parameters, guards)
                    && ReadWxStaticGuards(conjunction.Right, parameters, guards);
            }
            if (guard is not Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Arguments: [Expr.Variable variable]
                } call
                || call.Module is null && entries.ContainsKey((call.Function, 1)))
            {
                return false;
            }
            byte opcode = call.Function switch
            {
                "is_atom" => IsAtom,
                "is_binary" => IsBinary,
                "is_boolean" => IsBoolean,
                "is_float" => IsFloat,
                "is_integer" => IsInteger,
                "is_list" => IsList,
                "is_number" => IsNumber,
                "is_tuple" => IsTuple,
                _ => 0
            };
            int register = Array.IndexOf(parameters, variable.Name);
            if (opcode == 0 || register < 0 || guards.Any(test => test.Register == register))
            {
                return false;
            }
            guards.Add((opcode, register));

            return true;
        }

        private void EmitWxEnvironmentCheck(int register, int failure)
        {
            owner.Op(BifOne, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Get, 1));
            owner.LiteralOperand(Term.A("wx_env"));
            owner.Operand(XRegisterTag, register);
            EmitDetailRecordTest(
                register,
                4,
                Term.A("wx_env"),
                failure
            );
        }

        private static bool IsWxEnvironment(Expr.Case environment) =>
            environment is
            {
                Value: Expr.Call
                {
                    Module: ErlangModuleNames.Module, Function: AutoImportNames.Get,
                    Arguments: [Expr.Literal { Value: Atom { Name: "wx_env" } }]
                },
                Clauses: [
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom { Name: "wx_env" } },
                                Pattern.Variable reference, Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                    }], Body: Expr.Variable result
                },
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Call
                    {
                        Module: ErlangModuleNames.Module, Function: "error",
                        Arguments: [Expr.Tuple
                        {
                            Items: [
                                Expr.Literal { Value: Atom { Name: "wx" } },
                                Expr.Literal { Value: Atom { Name: "unknown_env" } }
                            ]
                        }]
                    }
                }
                ]
            } && reference.Name == result.Name;
    }
}

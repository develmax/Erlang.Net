using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (Expr Value, Atom Name, int Arity)? ReadRecordTest(Expr expression) => expression switch
    {
        Expr.RecordTest record when record.Arity > 0 => (record.Value, Term.A(record.Name), record.Arity),
        Expr.Call
        {
            Module: null or ErlangModuleNames.Module, Function: RecordPredicateNames.IsRecord, DynamicRemote: false,
            Arguments: [var value, Expr.Literal { Value: Atom name }, Expr.Literal { Value: Integer arity }]
        } call when arity.Value > 0 && arity.Value <= int.MaxValue && ResolvesErlangBif(call) => (value, name, (int)arity.Value),
        _ => null
    };

    private void EmitRecordTest(int failure, int tag, int register, Atom name, int arity)
    {
        Op(IsTaggedTuple, LabelTag, failure);
        Operand(tag, register);
        Operand(BeamWire.UnsignedTag, arity);
        LiteralOperand(name);
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedLocalForward(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause] || ReadGuardedLocalForward(clause) is not { } plan)
            {
                return false;
            }

            EmitGuardedLocalForward(plan, owner.nextLabel - 2);

            return true;
        }

        private sealed record GuardedLocalForward(
            int Entry,
            int Arity,
            bool AddsEmptyList,
            IReadOnlyList<(int Register, int Arity)> Shapes,
            ForwardGuard Tests
        );

        private GuardedLocalForward? ReadGuardedLocalForward(Clause clause)
        {
            int arity = clause.Patterns.Count;
            if (clause is not { Guard: { } guard, Body: Expr.Call { Module: null, DynamicRemote: false } call }
                || call.Arguments.Count < arity || call.Arguments.Count > arity + 1
                || !entries.TryGetValue((call.Function, call.Arguments.Count), out int entry))
            {
                return null;
            }

            if (ReadForwardParameters(clause) is not { } forward)
            {
                return null;
            }

            var (parameters, shapes, fields) = forward;
            bool addsEmptyList = call.Arguments.Count > arity;
            if (!call.Arguments.Take(arity).SequenceEqual(parameters.Select(name => new Expr.Variable(name)))
                || addsEmptyList && call.Arguments[^1] is not (Expr.List { Items: [], Tail: null }
                    or Expr.Literal { Value: Nil })
                || ReadForwardGuard(guard, parameters, fields, allowColour: true) is not { } tests)
            {
                return null;
            }

            return new GuardedLocalForward(
                entry,
                arity,
                addsEmptyList,
                shapes,
                tests
            );
        }

        private void EmitGuardedLocalForward(GuardedLocalForward plan, int failure)
        {
            var (entry, arity, addsEmptyList, shapes, tests) = plan;
            var ordered = FlattenForwardConjunction(tests).ToArray();
            if (shapes.Count == 0)
            {
                if (addsEmptyList)
                {
                    var leadingTests = ordered.TakeWhile(test => test is
                        ForwardGuard.Test { Tag: not null } or ForwardGuard.Test { Opcode: IsInteger }).ToArray();
                    ordered = leadingTests.OrderBy(test => test is ForwardGuard.Test { Tag: null })
                        .Concat(ordered.Skip(leadingTests.Length)).ToArray();
                }

                foreach (var test in ordered)
                {
                    EmitForwardGuard(test, failure);
                }
            }
            else
            {
                if (addsEmptyList && ordered.SkipWhile(test => test is ForwardGuard.Test { Tag: not null })
                    .Any(test => test is ForwardGuard.Test { Tag: not null }))
                {
                    foreach (int register in Enumerable.Range(0, arity))
                    {
                        foreach (var shape in shapes.Where(shape => shape.Register == register))
                        {
                            EmitTupleShapeTest(
                                XRegisterTag,
                                register,
                                shape.Arity,
                                failure
                            );
                        }

                        foreach (var record in ordered.OfType<ForwardGuard.Test>().Where(test => test.Tag is not null && test.Register == register))
                        {
                            EmitForwardGuard(record, failure);
                        }
                    }

                    ordered = ordered.Where(test => test is not ForwardGuard.Test { Tag: not null }).ToArray();
                    shapes = [];
                }

                int prefix = 0;
                while (prefix < ordered.Length && ordered[prefix] is ForwardGuard.Test { Tag: not null })
                {
                    EmitForwardGuard(ordered[prefix++], failure);
                }

                foreach (var (register, width) in shapes)
                {
                    EmitTupleShapeTest(
                        XRegisterTag,
                        register,
                        width,
                        failure
                    );
                }

                foreach (var test in ordered.Skip(prefix))
                {
                    EmitForwardGuard(test, failure);
                }
            }

            if (addsEmptyList)
            {
                owner.EmitConstant(Nil.Value, arity);
            }
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity + (addsEmptyList ? 1 : 0));
            owner.Operand(LabelTag, entry);
        }

        private static int ForwardGuardLabelCount(ForwardGuard guard) => guard switch
        {
            ForwardGuard.Disjunction disjunction =>
                2 + ForwardGuardLabelCount(disjunction.Left) + ForwardGuardLabelCount(disjunction.Right),
            ForwardGuard.Conjunction conjunction => ForwardGuardLabelCount(conjunction.Left) + ForwardGuardLabelCount(conjunction.Right),
            ForwardGuard.ColourTest => 2,
            _ => 0
        };

        private static (string[] Parameters, List<(int Register, int Arity)> Shapes,
            Dictionary<string, (int Register, int Index)> Fields)? ReadForwardParameters(Clause clause)
        {
            var parameters = new string[clause.Patterns.Count];
            var shapes = new List<(int Register, int Arity)>();
            var fields = new Dictionary<string, (int Register, int Index)>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < parameters.Length; index++)
            {
                Pattern pattern = clause.Patterns[index];
                if (pattern is AliasPattern alias
                    && (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is { } binding
                    && (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is Pattern.Tuple tuple)
                {
                    shapes.Add((index, tuple.Items.Count));
                    for (int field = 0; field < tuple.Items.Count; field++)
                    {
                        if (tuple.Items[field] is not Pattern.Variable variable
                            || variable.Name != VariableScopeNames.Wildcard && !names.Add(variable.Name))
                        {
                            return null;
                        }

                        if (variable.Name != VariableScopeNames.Wildcard)
                        {
                            fields.Add(variable.Name, (index, field));
                        }
                    }

                    pattern = binding;
                }

                if (pattern is not Pattern.Variable parameter || parameter.Name == VariableScopeNames.Wildcard
                    || !names.Add(parameter.Name))
                {
                    return null;
                }

                parameters[index] = parameter.Name;
            }
            return (parameters, shapes, fields);
        }

        private abstract record ForwardGuard
        {
            internal sealed record Test(byte Opcode, int Register, Atom? Tag = null, int Arity = 0) : ForwardGuard;
            internal sealed record FieldTest(byte Opcode, int Register, int Index, int Scratch) : ForwardGuard;
            internal sealed record ColourTest(int Register, int Scratch) : ForwardGuard;
            internal sealed record Conjunction(ForwardGuard Left, ForwardGuard Right) : ForwardGuard;
            internal sealed record Disjunction(ForwardGuard Left, ForwardGuard Right) : ForwardGuard;
        }

        private static IEnumerable<ForwardGuard> FlattenForwardConjunction(ForwardGuard guard) =>
            guard is ForwardGuard.Conjunction conjunction
                ? FlattenForwardConjunction(conjunction.Left).Concat(FlattenForwardConjunction(conjunction.Right))
                : [guard];

        private ForwardGuard? ReadForwardGuard(
            Expr guard,
            string[] parameters,
            IReadOnlyDictionary<string, (int Register, int Index)>? fields = null,
            bool allowColour = false
        )
        {
            if (allowColour && guard is Expr.Binary { Operator: ErlangOperators.AndAlso,
                Left: Expr.Binary { Operator: ErlangOperators.OrElse } })
            {
                int colour = Array.FindIndex(parameters, argument => IsWxColourGuard(guard, argument));
                if (colour >= 0)
                {
                    return new ForwardGuard.ColourTest(colour, parameters.Length);
                }
            }

            if (guard is Expr.Binary { Operator: ErlangOperators.AndAlso or ErlangOperators.OrElse } binary)
            {
                if (ReadForwardGuard(binary.Left, parameters, fields, allowColour) is not { } left
                    || ReadForwardGuard(binary.Right, parameters, fields, allowColour) is not { } right)
                {
                    return null;
                }

                return binary.Operator == ErlangOperators.AndAlso
                    ? new ForwardGuard.Conjunction(left, right)
                    : new ForwardGuard.Disjunction(left, right);
            }

            if (owner.ReadRecordTest(guard) is { Value: Expr.Variable tested } record)
            {
                int register = Array.IndexOf(parameters, tested.Name);
                return register >= 0 ? new ForwardGuard.Test(IsTaggedTuple, register, record.Name, record.Arity) : null;
            }

            if (guard is Expr.Call
                {
                    Module: null or ErlangModuleNames.Module,
                    DynamicRemote: false,
                    Arguments: [Expr.Variable argument]
                } call && owner.ResolvesErlangBif(call))
            {
                int register = Array.IndexOf(parameters, argument.Name);
                byte opcode = call.Function == AutoImportNames.IsBoolean ? IsBoolean : TypeGuardOpcode(call);
                if (register >= 0 && opcode != 0)
                {
                    return new ForwardGuard.Test(opcode, register);
                }

                if (opcode != 0 && fields?.TryGetValue(argument.Name, out var field) == true)
                {
                    return new ForwardGuard.FieldTest(opcode, field.Register, field.Index, parameters.Length);
                }
            }

            return null;
        }

        private void EmitForwardGuard(ForwardGuard guard, int failure)
        {
            switch (guard)
            {
                case ForwardGuard.Conjunction conjunction:
                    EmitForwardGuard(conjunction.Left, failure);
                    EmitForwardGuard(conjunction.Right, failure);
                    break;
                case ForwardGuard.Disjunction disjunction:
                    int retry = Label();
                    int accepted = Label();
                    EmitForwardGuard(disjunction.Left, retry);
                    JumpTo(accepted);
                    Mark(retry);
                    EmitForwardGuard(disjunction.Right, failure);
                    Mark(accepted);
                    break;
                case ForwardGuard.Test { Tag: { } tag } test:
                    owner.EmitRecordTest(failure, XRegisterTag, test.Register, tag, test.Arity);
                    break;
                case ForwardGuard.FieldTest field:
                    owner.EmitTuplePart(field.Register, field.Index, field.Scratch);
                    owner.Op(field.Opcode, LabelTag, failure);
                    owner.Operand(XRegisterTag, field.Scratch);
                    break;
                case ForwardGuard.ColourTest colour:
                    EmitWxColourGuard(colour.Register, colour.Scratch, failure);
                    break;
                case ForwardGuard.Test test:
                    owner.Op(test.Opcode, LabelTag, failure);
                    owner.Operand(XRegisterTag, test.Register);
                    break;
            }
        }

        private bool TryEmitRecordPredicateFunction(FunctionDefinition function)
        {
            if (function.Clauses is [{ Guard: null, Patterns: [Pattern.Variable parameter], Body: var body }]
                && owner.ReadRecordTest(body) is { Value: Expr.Variable argument } value
                && parameter.Name == argument.Name)
            {
                int failure = Label();
                owner.EmitRecordTest(failure, XRegisterTag, 0, value.Name, value.Arity);
                owner.EmitConstant(Term.A("true"), 0);
                owner.instructions.WriteByte(Return);
                Mark(failure);
                owner.EmitConstant(Term.A("false"), 0);
                owner.instructions.WriteByte(Return);
                return true;
            }

            if (function.Clauses is not [
                { Patterns: [Pattern.Variable tested], Guard: { } guard, Body: Expr.Literal success },
                { Patterns: [Pattern.Variable { }], Guard: null, Body: Expr.Literal fallback }
            ])
            {
                return false;
            }

            Expr testedExpression = guard;
            bool integerAlternative = false;
            if (guard is Expr.Binary { Operator: ErlangOperators.OrElse } alternative
                && alternative.Right is Expr.Call { Function: AutoImportNames.IsInteger, Arguments: [Expr.Variable operand] } call
                && operand.Name == tested.Name && owner.ResolvesErlangBif(call))
            {
                integerAlternative = true;
                testedExpression = alternative.Left;
            }
            if (owner.ReadRecordTest(testedExpression) is not { Value: Expr.Variable candidate } record
                || candidate.Name != tested.Name)
            {
                return false;
            }

            int failed = Label();
            if (integerAlternative)
            {
                int accepted = Label();
                int rejected = Label();
                owner.EmitRecordTest(failed, XRegisterTag, 0, record.Name, record.Arity);
                JumpTo(accepted);
                Mark(failed);
                owner.Op(IsInteger, LabelTag, rejected);
                owner.Operand(XRegisterTag, 0);
                Mark(accepted);
                failed = rejected;
            }
            else
            {
                owner.EmitRecordTest(failed, XRegisterTag, 0, record.Name, record.Arity);
            }
            owner.EmitConstant(success.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(failed);
            owner.EmitConstant(fallback.Value, 0);
            owner.instructions.WriteByte(Return);
            return true;
        }
    }
}

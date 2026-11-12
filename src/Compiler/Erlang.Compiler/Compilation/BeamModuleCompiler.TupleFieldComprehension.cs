using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TupleFieldComprehension(
        Expr.Variable Source,
        Atom? Tag,
        int Arity,
        int Field,
        ComprehensionQualifier.Generator Generator
    );

    private static TupleFieldComprehension? ReadTupleFieldComprehension(Expr expression)
    {
        if (expression is not Expr.ListComprehension
            {
                Items: [Expr.Variable selected],
                Qualifiers: [ComprehensionQualifier.Generator
                {
                    Strict: false, Pattern: Pattern.Tuple tuple, Source: Expr.Variable source
                } generator]
            }
            || selected.Name == VariableScopeNames.Wildcard)
        {
            return null;
        }

        Atom? tag = tuple.Items.FirstOrDefault() is Pattern.Literal { Value: Atom atom } ? atom : null;
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        int field = -1;
        for (int index = tag is null ? 0 : 1; index < tuple.Items.Count; index++)
        {
            if (tuple.Items[index] is not Pattern.Variable variable
                || variable.Name != VariableScopeNames.Wildcard && !bindings.Add(variable.Name))
            {
                return null;
            }

            if (variable.Name == selected.Name)
            {
                field = index;
            }
        }

        return field < 0 ? null : new(
            source,
            tag,
            tuple.Items.Count,
            field,
            generator
        );
    }

    private (Expr Body, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractListComprehensionHelper(
        FunctionDefinition function,
        Clause clause,
        BeamSourceLocation location,
        HashSet<(string, int)> declared
    )
    {
        var expressions = LocalFunctionEntries.Walk(clause.Body);
        if (expressions.Count(expression => expression is Expr.ListComprehension) != 1
            || expressions.Any(expression => expression is Expr.Fun or Expr.Receive or Expr.Try { After: not null }
                or Expr.BinaryComprehension or Expr.MapComprehension))
        {
            return null;
        }

        string name = MappingName(function.Name, function.Arity);
        if (declared.Contains((name, 1)))
        {
            return null;
        }

        FunctionDefinition? helper = null;
        BeamSourceLocation helperLocation = location;
        Expr body = Rewrite(clause.Body);
        if (helper is null)
        {
            return null;
        }

        declared.Add((name, 1));

        return (body, helper, helperLocation);

        Expr Rewrite(Expr expression)
        {
            if (expression is Expr.ListComprehension comprehension)
            {
                var projection = ReadTupleFieldComprehension(expression);
                var mapper = projection is null ? ReadClosedListMapper(expression) : null;
                if (projection is null
                    && (mapper is null || !CanExtractClosedListMapper(expression, mapper, clause, declared)))
                {
                    return expression;
                }

                var generator = projection?.Generator ?? mapper!.Generator;
                var source = projection?.Source ?? mapper!.Source;
                int line = generator.GeneratorLine != 0 ? generator.GeneratorLine
                    : comprehension.PreprocessorLine != 0 ? comprehension.PreprocessorLine
                    : comprehension.Source?.Line ?? location.Line;
                helperLocation = new(generator.GeneratorFile ?? comprehension.PreprocessorFile ?? location.File, line);
                helper = new(name, 1, [new([new Pattern.Variable(source.Name)], null, expression)])
                {
                    IsClosedListMapper = mapper is not null
                };

                return new Expr.Call(null, name, [source])
                {
                    SourceLine = line,
                    PreprocessorFile = helperLocation.File
                };
            }

            return expression switch
            {
                Expr.Block block => block with { Body = Rewrite(block.Body) },
                Expr.Match { Pattern: not Pattern.Variable { Name: VariableScopeNames.Wildcard } } match =>
                    match with { Value = Rewrite(match.Value) },
                Expr.Sequence sequence => sequence with
                {
                    Items = sequence.Items.Select((item, index) =>
                        index == sequence.Items.Count - 1 || item is Expr.Match ? Rewrite(item) : item).ToArray()
                },
                Expr.Case decision => decision with { Value = Rewrite(decision.Value) },
                _ => expression
            };
        }
    }

    private static TupleFieldComprehension? ReadTupleFieldComprehensionHelper(FunctionDefinition function) =>
        function.Clauses is [{ Guard: null, Patterns: [Pattern.Variable input], Body: var body }]
        && ReadTupleFieldComprehension(body) is { } flow && flow.Source.Name == input.Name ? flow : null;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleFieldComprehension(FunctionDefinition function)
        {
            if (ReadTupleFieldComprehensionHelper(function) is not { } flow)
            {
                return false;
            }

            int entry = owner.nextLabel - 1;
            int skip = Label();
            int empty = Label();
            int invalid = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            if (flow.Tag is { } tag)
            {
                owner.EmitRecordTest(
                    skip,
                    XRegisterTag,
                    1,
                    tag,
                    flow.Arity
                );
            }
            else
            {
                owner.EmitTupleCheck(1, flow.Arity, skip);
            }

            EmitFrameAllocation(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitFieldCall(1, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitSavedRecordField(0, flow.Field, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            Mark(skip);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.EmitMappingEnd(empty, invalid);

            return true;
        }
    }
}

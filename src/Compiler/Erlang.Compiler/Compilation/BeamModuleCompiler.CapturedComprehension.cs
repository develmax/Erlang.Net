using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (Expr Body, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractCapturedComprehension(
        FunctionDefinition function,
        Clause clause,
        BeamSourceLocation location,
        HashSet<(string, int)> declared
    )
    {
        if (clause.Guard is not null || !HasUnconditionalParameters(clause)
            || clause.Body is not Expr.Sequence
            {
                Items: [
                    Expr.Match { Pattern: Pattern.Variable bound, Value: Expr.Call { Module: null, DynamicRemote: false } producer } binding,
                    Expr.ListComprehension
                {
                    Items: [var mapped],
                    Qualifiers: [ComprehensionQualifier.Generator
                    {
                        Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable source
                    } generator]
                } comprehension
                ]
            } sequence
            || bound.Name != source.Name || item.Name == VariableScopeNames.Wildcard
            || clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == bound.Name)
            || !declared.Contains((producer.Function, producer.Arguments.Count))
            || !producer.Arguments.All(argument => argument is Expr.Variable
                || AttributeLiteral.TryNormalizeConstant(argument, out _))
            || LocalFunctionEntries.Walk(mapped).Any(expression =>
                expression is Expr.Fun or Expr.Receive or Expr.Try { After: not null }
                    or Expr.ListComprehension or Expr.BinaryComprehension or Expr.MapComprehension))
        {
            return null;
        }

        var outer = clause.Patterns.OfType<Pattern.Variable>()
            .Select(parameter => parameter.Name)
            .Where(parameter => parameter != item.Name && parameter != VariableScopeNames.Wildcard)
            .Append(bound.Name);
        var captures = ClosureCaptures.ReadEntryReferences(new Clause([], null, mapped), outer);
        if (captures.Count != 1 || captures.Contains(bound.Name))
        {
            return null;
        }

        string capture = captures.Single();
        string helper = MappingName(function.Name, function.Arity);
        if (!declared.Add((helper, 2)))
        {
            return null;
        }

        int line = generator.GeneratorLine != 0 ? generator.GeneratorLine
            : comprehension.PreprocessorLine != 0 ? comprehension.PreprocessorLine
            : comprehension.Source?.Line ?? location.Line;
        var helperLocation = new BeamSourceLocation(generator.GeneratorFile ?? comprehension.PreprocessorFile ?? location.File, line);
        var definition = BuildClosedMapping(
            helper,
            item,
            mapped,
            new Pattern.Variable(capture)
        );

        return (sequence with { Items = [binding, new Expr.Call(null, helper, [source, new Expr.Variable(capture)])] },
            definition, helperLocation);
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCapturedComprehensionParent(FunctionDefinition function)
        {
            if (function.Clauses is not [var clause] || clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable bound, Value: Expr.Call { Module: null, DynamicRemote: false } producer },
                        Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input, Expr.Variable capture] } helper
                    ]
                }
                || bound.Name != input.Name || helper.Function != MappingName(function.Name, function.Arity)
                || !entries.TryGetValue((producer.Function, producer.Arguments.Count), out int sourceEntry)
                || !entries.TryGetValue((helper.Function, 2), out int helperEntry)
                || producer.Arguments.Count > clause.Patterns.Count
                || !producer.Arguments.Select((argument, index) => (argument, clause.Patterns[index]))
                    .All(pair => pair is (Expr.Variable actual, Pattern.Variable parameter) && actual.Name == parameter.Name))
            {
                return false;
            }

            int captureIndex = Array.FindIndex(clause.Patterns.ToArray(),
                pattern => pattern is Pattern.Variable parameter && parameter.Name == capture.Name);
            if (captureIndex < 0)
            {
                return false;
            }

            EmitFrameAllocation(function.Arity, 1);
            owner.MoveRegister(
                XRegisterTag,
                captureIndex,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(producer, location);
            owner.EmitFieldCall(producer.Arguments.Count, sourceEntry);
            Load(0, 1);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, helperEntry);
            owner.Operand(BeamWire.UnsignedTag, 1);
            peakSlot = Math.Max(peakSlot, 1);

            return true;
        }
    }
}

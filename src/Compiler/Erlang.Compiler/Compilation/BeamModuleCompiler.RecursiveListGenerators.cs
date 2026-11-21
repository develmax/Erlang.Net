using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (FunctionDefinition Parent, IReadOnlyList<(FunctionDefinition Function, BeamSourceLocation Location)> Helpers)?
        ExtractRecursiveListGenerators(
            FunctionDefinition function,
            BeamSourceLocation fallback,
            HashSet<(string Name, int Arity)> declared,
            IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
        )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || function.Clauses is not [var clause] || clause.Guard is not null || !HasUnconditionalParameters(clause))
        {
            return null;
        }

        var expressions = LocalFunctionEntries.Walk(clause.Body).ToArray();
        if (expressions.Any(expression => expression is Expr.Fun or Expr.Try or Expr.Catch or Expr.Receive
                or Expr.Maybe or Expr.MapComprehension or Expr.BinaryComprehension)
            || expressions.OfType<Expr.ListComprehension>().ToArray() is not [var comprehension]
            || !HasRetainedRecursiveListResult(comprehension, expressions)
            || PlanRecursiveListGenerators(
                function,
                comprehension,
                clause,
                fallback,
                MappingName(function.Name, function.Arity),
                MappingName(
                    function.Name,
                    function.Arity,
                    1,
                    1
                ),
                declared,
                localFunctions,
                recordCursorLayout: true
            ) is not { } plan)
        {
            return null;
        }

        Expr parentBody = BeamTargetedInlining.Rewrite(
            clause.Body,
            expression => expression,
            rewriteScope: expression => ReferenceEquals(expression, comprehension) ? plan.Call : null
        );
        declared.Add((plan.Outer.Function.Name, plan.Outer.Function.Arity));
        declared.Add((plan.Inner.Function.Name, plan.Inner.Function.Arity));

        // The module emits the collected local definitions in reverse.
        recursiveDefinitionOrder.Add(
            (function.Name, function.Arity),
            [(plan.Inner.Function.Name, plan.Inner.Function.Arity), (plan.Outer.Function.Name, plan.Outer.Function.Arity)]
        );

        return (function with { Clauses = [clause with { Body = parentBody }] }, [plan.Inner, plan.Outer]);
    }

    private (Expr.Call Call, (FunctionDefinition Function, BeamSourceLocation Location) Outer,
        (FunctionDefinition Function, BeamSourceLocation Location) Inner)? PlanRecursiveListGenerators(
            FunctionDefinition function,
            Expr.ListComprehension comprehension,
            Clause clause,
            BeamSourceLocation fallback,
            string outerName,
            string innerName,
            HashSet<(string Name, int Arity)> declared,
            IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
            bool recordCursorLayout = false
        )
    {
        if (comprehension is not
            {
                Items: [var mapped],
                Qualifiers:
                    [
                        ComprehensionQualifier.Generator
                    {
                        Strict: false,
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } or Pattern.Tuple,
                        Source: Expr.Variable or Expr.Call { DynamicRemote: false }
                    } first,
                        ComprehensionQualifier.Generator
                    {
                        Strict: false,
                        Pattern: var inner,
                        Source: Expr.Variable or Expr.Call { DynamicRemote: false } or Expr.RecordAccess
                    } second
                    ]
            }
            || !IsRecursiveListPattern(inner)
            || ClosureCaptures.ReadListComprehensionBindings(clause, comprehension) is not { } visible)
        {
            return null;
        }

        Pattern outer = first.Pattern;
        var outerBindings = Semantics.Variables(outer).ToArray();
        var innerBindings = Semantics.Variables(inner).ToHashSet(StringComparer.Ordinal);
        bool tupleOuter = outer is Pattern.Tuple;
        if (visible.Overlaps(outerBindings) || visible.Overlaps(innerBindings) || innerBindings.Overlaps(outerBindings)
            || outer is Pattern.Tuple tuple
                && (tuple.Items is not [Pattern.Variable, Pattern.Variable]
                    || outerBindings.Distinct(StringComparer.Ordinal).Count() != outerBindings.Length
                    || inner is not Pattern.Variable { Name: not VariableScopeNames.Wildcard }
                    || second.Source is not (Expr.Variable or Expr.Call { DynamicRemote: false })
                    || second.Source is Expr.Variable collection && !outerBindings.Contains(collection.Name, StringComparer.Ordinal)))
        {
            return null;
        }

        Expr innerSource = second.Source;
        Expr source = first.Source;
        if (LocalFunctionEntries.Walk(source).Any(operand =>
            operand is Expr.Variable variable && !visible.Contains(variable.Name)
            || !CanUseRecursiveListOperand(operand, localFunctions)))
        {
            return null;
        }
        var constants = ReadNestedListGeneratorConstants(clause, comprehension);
        if (constants.Count > 0)
        {
            // A call through a lexical variable retains its callable operand.
            foreach (Expr.Apply apply in LocalFunctionEntries.Walk(source).Concat(LocalFunctionEntries.Walk(innerSource))
                .Concat(LocalFunctionEntries.Walk(mapped)).OfType<Expr.Apply>())
            {
                if (apply.Function is Expr.Variable variable)
                {
                    constants.Remove(variable.Name);
                }
            }

            source = SubstituteHelperOperandConstants(source, constants);
            innerSource = SubstituteHelperOperandConstants(innerSource, constants);
            mapped = SubstituteHelperOperandConstants(mapped, constants);
        }

        var sourceOperands = LocalFunctionEntries.Walk(innerSource).ToArray();
        var valueOperands = LocalFunctionEntries.Walk(mapped).ToArray();
        if (sourceOperands.Concat(valueOperands).Any(expression => !CanUseRecursiveListOperand(expression, localFunctions))
            || innerSource is Expr.Literal { Value: not Cons { Tail: Cons } })
        {
            return null;
        }

        var sourceNames = sourceOperands.OfType<Expr.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
        var valueNames = valueOperands.OfType<Expr.Variable>().Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
        valueNames.ExceptWith(innerBindings);

        // Only reads in the mapped value keep outer fields live in the inner loop.
        // The outer tail precedes these fields in Core's local-function captures.
        var keptFields = outerBindings.Where(valueNames.Remove).Select(name => new Pattern.Variable(name)).ToArray();
        if (tupleOuter && keptFields.Length > 1)
        {
            return null;
        }

        sourceNames.ExceptWith(outerBindings);
        sourceNames.UnionWith(valueNames);
        if (!sourceNames.IsSubsetOf(visible))
        {
            return null;
        }

        var captures = clause.Patterns.OfType<Pattern.Variable>().Select(parameter => parameter.Name)
            .Where(sourceNames.Remove).Concat(sourceNames.Order(StringComparer.Ordinal))
            .Select(name => new Pattern.Variable(name)).ToArray();
        const string outerTail = "@BeamOuterListRemainder";
        var remainder = new Pattern.Variable(outerTail);
        Pattern.Variable[] innerCaptures = [.. captures, remainder, .. keptFields];
        int outerWidth = captures.Length + 1;
        int innerWidth = innerCaptures.Length + 1;
        if (innerWidth >= FirstReservedXRegister || declared.Contains((outerName, outerWidth))
            || declared.Contains((innerName, innerWidth)))
        {
            return null;
        }

        var outerLocation = new BeamSourceLocation(
            first.GeneratorFile ?? comprehension.PreprocessorFile ?? fallback.File,
            first.GeneratorLine == 0 ? fallback.Line : first.GeneratorLine
        );
        var innerLocation = new BeamSourceLocation(
            second.GeneratorFile ?? outerLocation.File,
            second.GeneratorLine == 0 ? outerLocation.Line : second.GeneratorLine
        );
        var outerFailure = first.InputLine == 0
            ? outerLocation : new BeamSourceLocation(first.InputFile ?? outerLocation.File, first.InputLine);
        var innerFailure = second.InputLine == 0
            ? innerLocation : new BeamSourceLocation(second.InputFile ?? innerLocation.File, second.InputLine);
        var outerLoop = BuildRecursiveMapping(
            outerName,
            outer,
            mapped,
            captures,
            invalidSourceLocation: outerFailure
        );
        var outerClauses = outerLoop.Clauses.ToArray();
        var currentTail = ((Pattern.List)outerClauses[0].Patterns[0]).Tail as Pattern.Variable
            ?? throw new System.Diagnostics.UnreachableException();
        var outerArguments = captures.Select(capture => new Expr.Variable(capture.Name)).ToArray();
        // Evaluate the inner source in the successful outer head. Even an independent
        // call is evaluated for each accepted outer element, never for empty outer input.
        var nextArguments = innerCaptures.Select(capture => new Expr.Variable(
            capture.Name == outerTail ? currentTail.Name : capture.Name
        ));
        outerClauses[0] = outerClauses[0] with
        {
            Body = new Expr.Call(null, innerName, [innerSource, .. nextArguments])
            {
                SourceLine = tupleOuter ? innerLocation.Line : 0,
                PreprocessorFile = tupleOuter ? innerLocation.File : null
            }
        };
        outerLoop = outerLoop with { Clauses = outerClauses };

        // Core's letrec lifting fixes the helper captures before SSA infers their values.
        // Preserve those parameters and the forwarding slots in recursive calls.
        var entryAtoms = ReadNestedCaptureAtoms(
            function,
            clause,
            comprehension,
            mapped,
            captures
        );
        Expr innerValue = mapped;
        Expr[] parentArguments = outerArguments;
        if (entryAtoms.Count != 0 && mapped is Expr.Tuple tupleValue)
        {
            innerValue = tupleValue with { Items = tupleValue.Items.Select(ReplaceCapture).ToArray() };
            parentArguments = outerArguments.Select((argument, index) =>
                index + 1 < clause.Patterns.Count && clause.Patterns[index + 1] is Pattern.Variable parameter
                    && parameter.Name == captures[index].Name
                    ? argument
                    : ReplaceCapture(argument)).ToArray();
        }

        var innerLoop = BuildRecursiveMapping(
            innerName,
            inner,
            innerValue,
            innerCaptures,
            invalidSourceLocation: innerFailure
        );
        var innerClauses = innerLoop.Clauses.ToArray();
        var empty = innerClauses[^2];
        Pattern[] finishPatterns = [empty.Patterns[0], .. captures, remainder,
            .. empty.Patterns.Skip(captures.Length + 2)];
        innerClauses[^2] = empty with
        {
            Patterns = finishPatterns,
            Body = new Expr.Call(null, outerName, [new Expr.Variable(outerTail), .. outerArguments])
            {
                SourceLine = tupleOuter ? outerLocation.Line : 0,
                PreprocessorFile = tupleOuter ? outerLocation.File : null
            }
        };
        innerLoop = innerLoop with { Clauses = innerClauses };

        // The outer input is evaluated once before entering either recursive helper.
        var parentCall = new Expr.Call(null, outerName, [source, .. parentArguments])
        {
            SourceLine = outerLocation.Line,
            PreprocessorFile = outerLocation.File
        };
        if (recordCursorLayout && captures.Length == 0 && outer is Pattern.Variable outerItem
            && inner is Pattern.Variable innerItem)
        {
            RecordNestedListCursorLayout(
                outerName,
                innerName,
                outerItem,
                innerItem,
                source,
                innerSource,
                mapped
            );
        }

        return (parentCall, (outerLoop, outerLocation), (innerLoop, innerLocation));

        Expr ReplaceCapture(Expr expression) =>
            expression is Expr.Variable variable && entryAtoms.TryGetValue(variable.Name, out Atom? known)
                ? new Expr.Literal(known)
                : expression;
    }

    private Dictionary<string, Atom> ReadNestedCaptureAtoms(
        FunctionDefinition function,
        Clause clause,
        Expr.ListComprehension comprehension,
        Expr mapped,
        Pattern.Variable[] captures
    )
    {
        var result = new Dictionary<string, Atom>(StringComparer.Ordinal);
        if (!localSingletonArguments.TryGetValue((function.Name, function.Arity), out var arguments)
            || !ReferenceEquals(ReadSingleNestedListMapper(clause.Body), comprehension)
            || mapped is not Expr.Tuple tuple
            || tuple.Items.Any(field => field is not (Expr.Variable or Expr.Literal)))
        {
            return result;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] is Atom atom && clause.Patterns[index] is Pattern.Variable parameter
                && captures.Any(capture => capture.Name == parameter.Name))
            {
                result.Add(parameter.Name, atom);
            }
        }

        return result;
    }

    private static bool HasRetainedRecursiveListResult(Expr.ListComprehension comprehension, IReadOnlyList<Expr> expressions) =>
        !expressions.OfType<Expr.Sequence>().Any(sequence =>
            sequence.Items.SkipLast(1).Any(item => ReferenceEquals(item, comprehension)))
        && !expressions.OfType<Expr.Match>().Any(match =>
            match.Pattern is Pattern.Variable { Name: VariableScopeNames.Wildcard }
            && ReferenceEquals(match.Value, comprehension))
        && !expressions.OfType<Expr.Binary>().Any(binary =>
            binary.Operator == ErlangOperators.Append && ReferenceEquals(binary.Left, comprehension));

    private static Dictionary<string, Term> ReadNestedListGeneratorConstants(Clause clause, Expr.ListComprehension comprehension)
    {
        Expr entry = comprehension;
        if (clause.Body is Expr.Sequence sequence
            && sequence.Items.FirstOrDefault(statement => LocalFunctionEntries.Walk(statement)
                .Any(expression => ReferenceEquals(expression, comprehension))) is { } containing)
        {
            // Clause-local constants still dominate when the sole comprehension is in a case arm.
            entry = ReadDirectHelperExpression(containing);
        }

        return ReadDominatingHelperConstants(clause, entry);
    }
}

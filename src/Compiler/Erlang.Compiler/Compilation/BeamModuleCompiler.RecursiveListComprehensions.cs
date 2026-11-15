using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (FunctionDefinition Parent, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractRecursiveListComprehension(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        var expressions = function.Clauses.SelectMany(clause => LocalFunctionEntries.Walk(clause.Body)).ToArray();
        if (expressions.OfType<Expr.ListComprehension>().ToArray() is not
            [
            {
                Items: [var mapped], Qualifiers: [ComprehensionQualifier.Generator
                { Strict: false, Pattern: var item, Source: var source } generator]
            } comprehension]
            || source is not (Expr.Variable or Expr.Call { DynamicRemote: false })
            || !(item is Pattern.Variable && (source is Expr.Call || mapped is Expr.Call { DynamicRemote: false })
                || item is Pattern.Tuple && IsRecursiveListPattern(item))
            || expressions.Any(expression => expression is Expr.Fun or Expr.BinaryComprehension or Expr.MapComprehension)
            || expressions.OfType<Expr.Sequence>().Any(sequence => sequence.Items.SkipLast(1)
                .Any(expression => ReferenceEquals(expression, comprehension)))
            || expressions.OfType<Expr.Match>().Any(match => match.Pattern is Pattern.Variable { Name: VariableScopeNames.Wildcard }
                && ReferenceEquals(match.Value, comprehension))
            || ReadListMappingPlan(function.Clauses) is not null
            || ReadDiscardedListCalls(function, localFunctions) is not null
            || ReadPageComprehension(function, localFunctions) is not null
            || ReadCapturedTableList(function, localFunctions) is not null)
        {
            return null;
        }

        var output = LocalFunctionEntries.Walk(mapped).ToArray();
        // The source stays in the parent and runs once, before the helper can bind a generator pattern.
        if (!output.All(CanUseExpression) || !LocalFunctionEntries.Walk(source).All(CanUseExpression))
        {
            return null;
        }

        var bound = Semantics.Variables(item).ToHashSet(StringComparer.Ordinal);
        var free = output.OfType<Expr.Variable>()
            .Select(variable => variable.Name)
            .Where(name => !bound.Contains(name))
            .ToHashSet(StringComparer.Ordinal);
        var parentClause = function.Clauses.First(clause => LocalFunctionEntries.Walk(clause.Body)
            .Any(expression => ReferenceEquals(expression, comprehension)));
        // Core names incoming arguments before variables defined in the clause body.
        var captures = parentClause.Patterns.OfType<Pattern.Variable>()
            .Select(parameter => parameter.Name)
            .Where(free.Remove)
            .Concat(free.Order(StringComparer.Ordinal))
            .ToArray();
        string helperName = MappingName(function.Name, function.Arity);
        int arity = captures.Length + 1;
        if (arity >= FirstReservedXRegister || !declared.Add((helperName, arity)))
        {
            return null;
        }

        int line = generator.GeneratorLine != 0 ? generator.GeneratorLine
            : comprehension.PreprocessorLine != 0 ? comprehension.PreprocessorLine
            : comprehension.Source?.Line ?? location.Line;
        var helperLocation = new BeamSourceLocation(generator.GeneratorFile ?? comprehension.PreprocessorFile ?? location.File, line);
        var recursiveCall = new Expr.Call(null, helperName, [source, .. captures.Select(name => new Expr.Variable(name))])
        {
            SourceLine = line,
            PreprocessorFile = helperLocation.File
        };
        var parent = function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Body = BeamTargetedInlining.Rewrite(
                    clause.Body,
                    expression => expression,
                    rewriteScope: expression => ReferenceEquals(expression, comprehension) ? recursiveCall : null
                )
            }).ToArray()
        };
        var helper = BuildRecursiveMapping(
            helperName,
            item,
            mapped,
            captures.Select(name => new Pattern.Variable(name)).ToArray(),
            properInput: IsProperListGeneratorCall(source)
        ) with { IsRecursiveListMapper = true };

        return (parent, helper, helperLocation);

        bool CanUseExpression(Expr expression) => expression switch
        {
            Expr.Call { DynamicRemote: false } call => call.Module is not null
                || localFunctions.ContainsKey((call.Function, call.Arguments.Count))
                || ResolveExternalModule(call.Function, call.Arguments.Count) is not null,
            Expr.Variable or Expr.Literal or Expr.Tuple or Expr.List or Expr.Unary or Expr.Binary
                or Expr.RecordAccess or Expr.RecordTest => true,
            _ => false
        };
    }

    private static bool IsProperListGeneratorCall(Expr expression) => expression is Expr.Call
    {
        DynamicRemote: false,
        Arguments.Count: 1
    } call && (call is { Module: "lists", Function: "reverse" or "sort" }
        || call is { Module: "maps", Function: "keys" or "values" or "to_list" });

    private static bool IsRecursiveListPattern(Pattern pattern) => pattern switch
    {
        Pattern.Variable or Pattern.Literal => true,
        Pattern.Tuple tuple => tuple.Items.All(IsRecursiveListPattern),
        Pattern.List list => list.Items.All(IsRecursiveListPattern) && (list.Tail is null || IsRecursiveListPattern(list.Tail)),
        _ => false
    };

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitIdentityListComprehension(FunctionDefinition function)
        {
            if (!function.IsRecursiveListMapper || function.Clauses[0] is not
                {
                    Patterns: [Pattern.List { Items: [Pattern.Variable item] }],
                    Body: Expr.List { Items: [Expr.Variable value] }
                }
                || item.Name != value.Name || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(1, 1);
            owner.EmitListParts(
                0,
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitRecursiveCons(entry, 1);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }

        private void EmitRecursiveListMappingEnd(FunctionDefinition function, int empty)
        {
            if (function.IsRecursiveListMapper && function.Clauses[^1].Body is Expr.Variable)
            {
                // On the non-cons arm of a proper list, x0 already contains nil.
                Mark(empty);
                owner.instructions.WriteByte(Return);
            }
            else
            {
                owner.EmitMappingEnd(empty, Label());
            }
        }

        private bool TryEmitCapturedListMapping(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item], Tail: Pattern.Variable { Name: "@BeamComprehensionTail" } },
                        Pattern.Variable capture],
                    Body: Expr.List
                    {
                        Items: [Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable environment, Expr.Variable element]
                        } mapped]
                    }
                }, ..]
                || environment.Name != capture.Name || element.Name != item.Name
                || !entries.TryGetValue((mapped.Function, 2), out int mapper)
                || !entries.TryGetValue((function.Name, 2), out int recurse))
            {
                return false;
            }

            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            Load(1, 0);
            owner.EmitCallLine(mapped, location);
            owner.EmitFieldCall(2, mapper);

            Load(0, 2);
            Load(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            if (CallLocation(mapped, location) != location)
            {
                owner.EmitLine(location);
            }
            owner.EmitRecursiveCons(recurse, 2);
            EmitRecursiveListMappingEnd(function, empty);

            return true;
        }
    }
}

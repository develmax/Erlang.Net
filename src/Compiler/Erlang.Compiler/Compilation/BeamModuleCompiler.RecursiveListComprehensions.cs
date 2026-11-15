using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (FunctionDefinition Parent, IReadOnlyList<(FunctionDefinition Function, BeamSourceLocation Location)> Helpers)? ExtractRecursiveListComprehensions(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        var expressions = function.Clauses.SelectMany(clause => LocalFunctionEntries.Walk(clause.Body)).ToArray();
        var comprehensions = expressions.OfType<Expr.ListComprehension>().ToArray();
        if (comprehensions.Length == 0
            || expressions.Any(expression => expression is Expr.Fun or Expr.BinaryComprehension or Expr.MapComprehension)
            || ReadListMappingPlan(function.Clauses) is not null
            || ReadDiscardedListCalls(function, localFunctions) is not null
            || ReadPageComprehension(function, localFunctions) is not null
            || ReadCapturedTableList(function, localFunctions) is not null)
        {
            return null;
        }

        var core = comprehensions;
        var ssa = comprehensions;
        var definitions = comprehensions;
        if (comprehensions.Length > 1)
        {
            if (LocalFunctionEntries.ClauseExpressions(function.Clauses).SelectMany(LocalFunctionEntries.Walk)
                .Any(expression => expression is Expr.Try or Expr.Receive or Expr.Maybe or Expr.Fun
                    or Expr.BinaryComprehension or Expr.MapComprehension))
            {
                return null;
            }

            core = OrderedListComprehensions(function.Clauses, ListComprehensionOrder.Core).ToArray();
            ssa = OrderedListComprehensions(function.Clauses, ListComprehensionOrder.Ssa).ToArray();
            definitions = OrderedListComprehensions(function.Clauses, ListComprehensionOrder.Definition).ToArray();
            if (core.Length != comprehensions.Length || ssa.Length != core.Length || definitions.Length != core.Length)
            {
                return null;
            }
        }

        var names = new Dictionary<Expr.ListComprehension, int>(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < core.Length; index++)
        {
            if (!names.TryAdd(core[index], index))
            {
                return null;
            }
        }

        var helpers = new Dictionary<Expr.ListComprehension, (FunctionDefinition Function, BeamSourceLocation Location)>(ReferenceEqualityComparer.Instance);
        var calls = new Dictionary<Expr, Expr>(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < ssa.Length; index++)
        {
            var comprehension = ssa[index];
            if (expressions.OfType<Expr.Sequence>().Any(sequence => sequence.Items.SkipLast(1)
                    .Any(expression => ReferenceEquals(expression, comprehension)))
                || expressions.OfType<Expr.Match>().Any(match => match.Pattern is Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    && ReferenceEquals(match.Value, comprehension)))
            {
                return null;
            }

            var clause = function.Clauses.First(candidate => LocalFunctionEntries.Walk(candidate.Body)
                .Any(expression => ReferenceEquals(expression, comprehension)));
            string name = MappingName(function.Name, function.Arity, names[comprehension], index);
            var plan = PlanRecursiveListComprehension(
                comprehension,
                clause,
                location,
                name,
                localFunctions,
                comprehensions.Length > 1
            );
            if (plan is not { } mapping || declared.Contains((name, mapping.Helper.Arity)))
            {
                return null;
            }

            helpers.Add(comprehension, (mapping.Helper, mapping.Location));
            calls.Add(comprehension, mapping.Call);
        }

        var ordered = definitions.Select(comprehension => helpers[comprehension]).ToArray();
        foreach (var helper in ordered)
        {
            declared.Add((helper.Function.Name, helper.Function.Arity));
        }

        var parent = function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Body = BeamTargetedInlining.Rewrite(
                    clause.Body,
                    expression => expression,
                    rewriteScope: expression => calls.GetValueOrDefault(expression)
                )
            }).ToArray()
        };

        return (parent, ordered);
    }

    private (Expr.Call Call, FunctionDefinition Helper, BeamSourceLocation Location)? PlanRecursiveListComprehension(
        Expr.ListComprehension comprehension,
        Clause parentClause,
        BeamSourceLocation location,
        string helperName,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions,
        bool independent
    )
    {
        if (comprehension is not
            {
                Items: [var mapped], Qualifiers: [ComprehensionQualifier.Generator
                { Strict: false, Pattern: var item, Source: var source } generator, ..]
            }
            || comprehension.Qualifiers is not ([_] or [_, ComprehensionQualifier.Filter])
            || source is not (Expr.Variable or Expr.Call { DynamicRemote: false })
            || !(item is Pattern.Variable && (independent || comprehension.Qualifiers.Count == 2
                    || source is Expr.Call || mapped is Expr.Call { DynamicRemote: false })
                || item is Pattern.Tuple && IsRecursiveListPattern(item)))
        {
            return null;
        }

        Expr? filter = comprehension.Qualifiers.LastOrDefault() is ComprehensionQualifier.Filter condition
            ? condition.Expression : null;
        var output = LocalFunctionEntries.Walk(mapped)
            .Concat(filter is null ? [] : LocalFunctionEntries.Walk(filter)).ToArray();
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
        // Core names incoming arguments before variables defined in the clause body.
        var captures = parentClause.Patterns.OfType<Pattern.Variable>()
            .Select(parameter => parameter.Name)
            .Where(free.Remove)
            .Concat(free.Order(StringComparer.Ordinal))
            .ToArray();
        if (captures.Length + 1 >= FirstReservedXRegister)
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
        var helper = BuildRecursiveMapping(
            helperName,
            item,
            mapped,
            captures.Select(name => new Pattern.Variable(name)).ToArray(),
            properInput: IsProperListGeneratorCall(source),
            filter: filter is null ? null : (filter, ComprehensionGuard.IsTest(filter, sourceModule))
        ) with { IsRecursiveListMapper = true };

        return (recursiveCall, helper, helperLocation);

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
                } match
                || item.Name != value.Name || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            var registerGuard = match with { Patterns = [new Pattern.Variable(VariableScopeNames.Wildcard), item] };
            if (!CanEmitSeparateEntryGuard(registerGuard, match.Guard))
            {
                return false;
            }

            int skip = match.Guard is null ? 0 : Label();
            int empty = Label();
            owner.EmitNonemptyList(0, empty);
            if (match.Guard is null)
            {
                EmitFrameAllocation(1, 1);
                owner.EmitListParts(
                    0,
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
            }
            else
            {
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                EmitEntryGuardPrefix(registerGuard, match.Guard, skip);
                EmitFrameAllocation(2, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
            }

            owner.EmitRecursiveCons(entry, 1);
            if (skip != 0)
            {
                Mark(skip);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, entry);
            }

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

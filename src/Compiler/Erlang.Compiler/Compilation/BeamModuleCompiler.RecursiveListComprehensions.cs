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
                Items: [Expr.Call { DynamicRemote: false } mapped], Qualifiers: [ComprehensionQualifier.Generator
                { Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable list } generator]
            } comprehension]
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
        if (output.Any(expression => expression switch
            {
                Expr.Call { DynamicRemote: false } call => call.Module is null
                    && !localFunctions.ContainsKey((call.Function, call.Arguments.Count))
                    && ResolveExternalModule(call.Function, call.Arguments.Count) is null,
                Expr.Variable or Expr.Literal or Expr.Tuple or Expr.List or Expr.Unary or Expr.Binary
                    or Expr.RecordAccess or Expr.RecordTest => false,
                _ => true
            }))
        {
            return null;
        }

        var free = output.OfType<Expr.Variable>()
            .Select(variable => variable.Name)
            .Where(name => name != item.Name)
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
        var recursiveCall = new Expr.Call(null, helperName, [list, .. captures.Select(name => new Expr.Variable(name))])
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
            captures.Select(name => new Pattern.Variable(name)).ToArray()
        );

        return (parent, helper, helperLocation);
    }

    private sealed partial class GeneralEmitter
    {
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
            int invalid = Label();
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
            owner.EmitFieldCall(2, recurse);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            owner.EmitMappingEnd(empty, invalid);

            return true;
        }
    }
}

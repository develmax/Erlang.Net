using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly Dictionary<(string Name, int Arity), string> zippedMapOwners = [];
    private readonly Dictionary<(string Name, int Arity), bool> zippedMapHelpers = [];

    private (FunctionDefinition Parent, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractZippedMapPairs(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || IsDeclaredNative((function.Name, function.Arity)))
        {
            return null;
        }

        var expressions = LocalFunctionEntries.ClauseExpressions(function.Clauses)
            .SelectMany(LocalFunctionEntries.Walk).ToArray();
        if (expressions.Any(expression => expression is Expr.Fun or Expr.ListComprehension or Expr.BinaryComprehension
                or Expr.Try or Expr.Receive or Expr.Maybe or Expr.Catch))
        {
            return null;
        }

        var maps = expressions.OfType<Expr.MapComprehension>().ToArray();
        if (maps.Length == 0)
        {
            return null;
        }

        Expr[] core = OrderedLocalDefinitions(function.Clauses, ListComprehensionOrder.Core).ToArray();
        Expr[] ssa = OrderedLocalDefinitions(function.Clauses, ListComprehensionOrder.Ssa).ToArray();
        if (core.Length != maps.Length || ssa.Length != maps.Length
            || core.FirstOrDefault() is not Expr.MapComprehension
            {
                SourceLine: > 0,
                Fields: [{ Key: Expr.Variable key, Value: Expr.Variable value, Exact: false }],
                Qualifiers: [
                    ComprehensionQualifier.Zip
                {
                    Generators: [
                            ComprehensionQualifier.Generator
                    {
                        Strict: false,
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } firstPattern,
                        Source: Expr.Variable firstSource
                    },
                            ComprehensionQualifier.Generator
                    {
                        Strict: false,
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } secondPattern,
                        Source: Expr.Variable secondSource
                    }
                        ]
                }
                ]
            } map || !ReferenceEquals(map, ssa[0]) || firstPattern.Name == secondPattern.Name)
        {
            return null;
        }

        bool reversed = key.Name == secondPattern.Name && value.Name == firstPattern.Name;
        if (!reversed && (key.Name != firstPattern.Name || value.Name != secondPattern.Name))
        {
            return null;
        }

        // mc_pairs selects from_list when a field value depends on the row.
        string helper = GeneratedComprehensionNames.Function(
            function.Name,
            function.Arity,
            GeneratedComprehensionNames.ZipList,
            0,
            ZippedListArity,
            0
        );
        var signature = (helper, ZippedListArity);
        if (!declared.Add(signature))
        {
            return null;
        }

        var helperCall = new Expr.Call(null, helper, [firstSource, secondSource]);
        var fromPairs = new Expr.Call(MapModuleNames.Module, MapModuleNames.FromList, [helperCall])
        {
            SourceLine = map.SourceLine,
            PreprocessorFile = map.PreprocessorFile
        };
        var parent = function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Body = BeamTargetedInlining.Rewrite(
                    clause.Body,
                    expression => expression,
                    rewriteScope: expression => ReferenceEquals(expression, map) ? fromPairs : null
                )
            }).ToArray()
        };

        zippedMapOwners.Add((function.Name, function.Arity), helper);
        zippedMapHelpers.Add(signature, reversed);

        return (
            parent,
            BuildZippedListHelper(
                helper,
                firstPattern,
                secondPattern,
                new Expr.Tuple([key, value])
            ),
            new BeamSourceLocation(map.PreprocessorFile ?? location.File, map.SourceLine)
        );
    }

    private void EmitZippedMapPairHelper(
        int entry,
        bool reversed,
        bool firstProperList,
        bool secondProperList
    )
    {
        const int listFrame = ZippedListArity;
        const int firstCursor = 0;
        const int secondCursor = 1;
        const int firstHead = 2;
        const int firstTail = 3;
        const int firstSavedHead = 1;
        const int secondSavedHead = 0;
        const int pairRegister = 1;
        const int pairHeapWords = 3;
        int firstEmpty = nextLabel++;
        int invalid = nextLabel++;

        EmitNonemptyList(firstCursor, firstEmpty);
        EmitListParts(
            firstCursor,
            XRegisterTag,
            firstHead,
            XRegisterTag,
            firstTail
        );
        EmitNonemptyList(secondCursor, invalid);
        Op(Allocate, BeamWire.UnsignedTag, listFrame);
        Operand(BeamWire.UnsignedTag, firstTail + 1);
        MoveRegister(
            XRegisterTag,
            firstHead,
            YRegisterTag,
            firstSavedHead
        );
        EmitListParts(
            secondCursor,
            YRegisterTag,
            secondSavedHead,
            XRegisterTag,
            secondCursor
        );
        MoveRegister(
            XRegisterTag,
            firstTail,
            XRegisterTag,
            firstCursor
        );
        EmitFieldCall(ZippedListArity, entry);

        // Keys and values are already evaluated. Build the pair and cons only after recursion.
        Op(TestHeap, BeamWire.UnsignedTag, pairHeapWords + ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleHeader(XRegisterTag, ZippedListArity, pairRegister);
        Operand(YRegisterTag, reversed ? secondSavedHead : firstSavedHead);
        Operand(YRegisterTag, reversed ? firstSavedHead : secondSavedHead);
        Op(PutList, XRegisterTag, pairRegister);
        Operand(XRegisterTag, firstCursor);
        Operand(XRegisterTag, firstCursor);
        EmitFrameReturn(listFrame);

        EmitZippedListEnd(
            firstEmpty,
            invalid,
            firstProperList,
            secondProperList
        );
    }
}

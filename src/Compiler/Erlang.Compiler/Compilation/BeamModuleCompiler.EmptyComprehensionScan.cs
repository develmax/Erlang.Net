using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly HashSet<(string, int)> emptyComprehensionScans = [];

    private (FunctionDefinition Parent, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractEmptyComprehensionScan(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> locals
    )
    {
        var signature = (function.Name, function.Arity);
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || function.Arity != 2
            || exportedFunctions.Contains(signature) || IsDeclaredNative(signature)
            || function.Clauses is not
            [
            {
                Guard: null,
                Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }, Pattern.Variable { }],
                Body: var empty
            } initial,
            {
                Guard: null,
                Patterns: [AliasPattern alias, Pattern.Variable tags],
                Body: Expr.Case
                {
                    Value: Expr.ListComprehension
                    {
                        Items: [Expr.Variable item],
                        Qualifiers:
                            [
                                ComprehensionQualifier.Generator
                            {
                                Strict: false, Pattern: AliasPattern itemAlias, Source: Expr.Variable collection
                            } generator,
                                ComprehensionQualifier.Filter
                            {
                                Expression: Expr.Binary
                                {
                                    Operator: ErlangOperators.ExactEqual,
                                    Left: Expr.Variable tested, Right: Expr.Variable captured
                                }
                            }
                            ]
                    },
                    Clauses:
                        [
                        {
                            Guard: null,
                            Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }],
                            Body: Expr.Call
                            {
                                Module: null, DynamicRemote: false,
                                Arguments: [Expr.Variable forwardedTail, Expr.Variable forwardedTags]
                            } tailCall
                        },
                        {
                            Guard: null,
                            Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Variable retained
                        }
                        ]
                }
            } step
            ]
            || (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is not { } full
            || (alias.Left as Pattern.List ?? alias.Right as Pattern.List) is not
            {
                Items: [Pattern.Tuple { Items: [Pattern.Variable tag, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                Tail: Pattern.Variable tail
            }
            || itemAlias is not { Left: Pattern.Variable a, Right: Pattern.Variable b }
            || !DistinctScanBindings(
                full,
                tags,
                tag,
                tail,
                a,
                b
            )
            || item.Name != a.Name || tested.Name != b.Name
            || collection.Name != tags.Name || forwardedTags.Name != tags.Name
            || captured.Name != tag.Name || retained.Name != full.Name || forwardedTail.Name != tail.Name
            || tailCall.Function != function.Name
            || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyValue) || emptyValue is not Nil
            || !HasClosedEmptyArgument(locals, signature, tailCall))
        {
            return null;
        }

        string helperName = MappingName(
            function.Name,
            function.Arity,
            0,
            0
        );
        if (!declared.Add((helperName, 2)))
        {
            return null;
        }

        int line = generator.GeneratorLine == 0 ? location.Line : generator.GeneratorLine;
        var source = new BeamSourceLocation(generator.GeneratorFile ?? location.File, line);
        Expr.Literal nil = new(Nil.Value);
        var helperCall = new Expr.Call(null, helperName, [nil, new Expr.Variable(tag.Name)])
        {
            SourceLine = line,
            PreprocessorFile = source.File
        };
        var parent = function with
        {
            Clauses =
            [
                initial,
                step with
                {
                    Body = new Expr.Sequence([helperCall, tailCall with { Arguments = [forwardedTail, nil] }])
                }
            ]
        };
        Pattern.Variable wildcard = new(VariableScopeNames.Wildcard);
        var helper = new FunctionDefinition(helperName, 2, [new Clause([wildcard, wildcard], null, nil)]);
        emptyComprehensionScans.Add(signature);

        return (parent, helper, source);
    }

    private static bool DistinctScanBindings(params Pattern.Variable[] variables)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        return variables.All(variable => variable.Name != VariableScopeNames.Wildcard && names.Add(variable.Name));
    }

    private static bool HasClosedEmptyArgument(
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> locals,
        (string Name, int Arity) signature,
        Expr.Call recursion
    )
    {
        bool hasSeed = false;
        foreach (var function in locals.Values)
        {
            foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(function.Clauses).SelectMany(LocalFunctionEntries.Walk))
            {
                if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                {
                    return false;
                }

                if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                    || (call.Function, call.Arguments.Count) != signature || ReferenceEquals(call, recursion))
                {
                    continue;
                }

                if (ReadLocalSingletonArgument(call.Arguments[1]) is not Nil)
                {
                    return false;
                }

                hasSeed = true;
            }
        }

        return hasSeed;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEmptyComprehensionScan(FunctionDefinition function)
        {
            if (!owner.emptyComprehensionScans.Contains((function.Name, function.Arity))
                || function.Clauses[1].Body is not Expr.Sequence
                {
                    Items: [Expr.Call helper, Expr.Call recur]
                })
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int end = Label();
            owner.Op(IsNonemptyList, LabelTag, end);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 3);
            owner.EmitTupleCheck(2, 2, functionInfo);
            EmitFrameAllocation(arity: 4, size: 1);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                0
            );
            owner.Op(GetTupleElement, XRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(Nil.Value, 0);
            owner.EmitCallLine(helper, location);
            owner.EmitFieldCall(2, entries[(helper.Function, 2)]);
            owner.EmitConstant(Nil.Value, 1);
            owner.MoveToX0(YRegisterTag, 0);
            EmitTailLocal(entries[(recur.Function, 2)], 2, 1);
            Mark(end);
            owner.EmitNilTest(XRegisterTag, 0, functionInfo);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}

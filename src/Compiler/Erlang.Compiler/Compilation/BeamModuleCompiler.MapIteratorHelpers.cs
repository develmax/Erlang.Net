using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int MapIteratorHelperArity = 1;
    private const int MapIteratorHelperLabels = 5;
    private const int FilteredMapIteratorHelperLabels = 6;
    private const string MapIteratorModule = "erts_internal";
    private const string MapIteratorStart = "mc_iterator";
    private const string MapIteratorRefill = "mc_refill";
    private const string MapIteratorEnd = "none";
    private const string MapIteratorCursor = "$map_iterator_cursor";

    private readonly Dictionary<(string Name, int Arity), Atom?> mapIteratorHelpers = [];

    private (FunctionDefinition Parent, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractMapIteratorPairs(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || IsDeclaredNative((function.Name, function.Arity)))
        {
            return null;
        }

        Expr[] expressions = LocalFunctionEntries.ClauseExpressions(function.Clauses).SelectMany(LocalFunctionEntries.Walk).ToArray();
        if (expressions.Any(expression => expression is Expr.Fun or Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe)
            || expressions.Where(expression => expression is Expr.MapComprehension or Expr.ListComprehension
                or Expr.BinaryComprehension).ToArray() is not
            [
                Expr.MapComprehension
            {
                Fields: [{ Exact: false, Key: Expr.Variable key, Value: Expr.Variable value }],
                Qualifiers: [
                        ComprehensionQualifier.MapGenerator
                {
                    Strict: false,
                    Pattern: Erlang.Pattern.Tuple { Items: [Erlang.Pattern.Variable pairKey, Erlang.Pattern.Variable pairValue] },
                    Source: Expr.Variable source
                } generator, ..]
            } map
            ]
            || map.SourceLine == 0 || generator.SourceLine == 0 || pairKey.Name != key.Name || pairValue.Name != value.Name
            || key.Name == value.Name || key.Name == VariableScopeNames.Wildcard || value.Name == VariableScopeNames.Wildcard
            || source.Name == key.Name || source.Name == value.Name)
        {
            return null;
        }

        Atom? excluded = null;
        Expr? filter = null;
        if (map.Qualifiers is
            [
                _,
                ComprehensionQualifier.Filter
            {
                Expression: Expr.Binary
                {
                    Operator: ErlangOperators.ExactNotEqual,
                    Left: Expr.Variable compared,
                    Right: Expr.Literal { Value: Atom omitted }
                } comparison
            }
            ] && compared.Name == value.Name)
        {
            excluded = omitted;
            filter = comparison;
        }
        else if (map.Qualifiers.Count != 1)
        {
            return null;
        }

        string name = MappingName(function.Name, function.Arity);
        var signature = (name, MapIteratorHelperArity);
        if (!declared.Add(signature))
        {
            return null;
        }

        var cursor = new Expr.Variable(MapIteratorCursor);
        var collect = new Expr.Call(null, name, [cursor]);
        var build = new Expr.Call(MapModuleNames.Module, MapModuleNames.FromList, [collect])
        {
            SourceLine = map.SourceLine,
            PreprocessorFile = map.PreprocessorFile
        };
        var parent = ReplaceMapIteratorSite(
            function,
            map,
            source,
            build,
            generator.SourceLine,
            map.PreprocessorFile
        );
        mapIteratorHelpers.Add(signature, excluded);
        var helperLocation = new BeamSourceLocation(map.PreprocessorFile ?? location.File, generator.SourceLine);

        return (parent, BuildMapIteratorHelper(
            name,
            pairKey,
            pairValue,
            new Expr.Tuple([new Expr.Variable(pairKey.Name), new Expr.Variable(pairValue.Name)]),
            filter
        ), helperLocation);
    }

    private static FunctionDefinition ReplaceMapIteratorSite(
        FunctionDefinition function,
        Expr site,
        Expr source,
        Expr success,
        int sourceLine,
        string? file
    )
    {
        var start = new Expr.Call(MapIteratorModule, MapIteratorStart, [source])
        {
            SourceLine = sourceLine,
            PreprocessorFile = file
        };
        var error = MapIteratorGeneratorError(source) with
        {
            SourceLine = sourceLine,
            PreprocessorFile = file
        };
        var replacement = new Expr.Case(
            start,
            [
                new Clause([new Pattern.Literal(Nil.Value)], null, error),
                new Clause([new Pattern.Variable(MapIteratorCursor)], null, success)
            ]
        );

        return function with
        {
            Clauses = function.Clauses.Select(clause => clause with
            {
                Body = BeamTargetedInlining.Rewrite(
                    clause.Body,
                    expression => expression,
                    rewriteScope: expression => ReferenceEquals(expression, site) ? replacement : null
                )
            }).ToArray()
        };
    }

    private static FunctionDefinition BuildMapIteratorHelper(
        string name,
        Pattern key,
        Pattern value,
        Expr element,
        Expr? filter,
        bool skipRejectedPair = false
    )
    {
        var cursorPattern = new Pattern.Variable(MapIteratorCursor);
        var cursor = new Expr.Variable(cursorPattern.Name);
        var tuple = new Pattern.Tuple([key, value, cursorPattern]);
        var recurse = new Expr.Call(null, name, [cursor]);
        var refill = new Expr.Call(MapIteratorModule, MapIteratorRefill, [cursor]);
        var clauses = new List<Clause>
        {
            new(
                [new AliasPattern(cursorPattern, new Pattern.List([new Pattern.Any()], new Pattern.Any()))],
                null,
                new Expr.Call(null, name, [refill])
            ),
            new([tuple], filter, new Expr.List([element], recurse))
        };
        if (filter is not null)
        {
            clauses.Add(new Clause([tuple], null, recurse));
        }
        else if (skipRejectedPair)
        {
            var ignored = new Pattern.Tuple([new Pattern.Any(), new Pattern.Any(), cursorPattern]);
            clauses.Add(new Clause([ignored], null, recurse));
        }

        clauses.Add(new Clause([new Pattern.Literal(Term.A(MapIteratorEnd))], null, new Expr.Literal(Nil.Value)));
        clauses.Add(new Clause([cursorPattern], null, MapIteratorGeneratorError(cursor)));

        return new FunctionDefinition(name, MapIteratorHelperArity, clauses);
    }

    private static Expr.Call MapIteratorGeneratorError(Expr payload) => new(
        ErlangModuleNames.Module,
        AutoImportNames.Error,
        [new Expr.Tuple([new Expr.Literal(Term.A(ComprehensionErrorReasons.BadGenerator)), payload])]
    );

    private void EmitMapIteratorHelper(
        int entry,
        Atom? excluded,
        MapListIteratorProjection? projection = null
    )
    {
        const int cursor = 0;
        const int cursorArity = 3;
        const int pairHeapWords = 3;
        int tuple = nextLabel++;
        int branch = excluded is null && projection is null ? 0 : nextLabel++;
        int end = nextLabel++;
        int invalid = nextLabel++;

        EmitNonemptyList(cursor, tuple);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, MapIteratorHelperArity);
        Op(ExternalCall, BeamWire.UnsignedTag, MapIteratorHelperArity);
        Operand(BeamWire.UnsignedTag, Import(MapIteratorModule, MapIteratorRefill, MapIteratorHelperArity));
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, MapIteratorHelperArity);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, 0);

        Op(Label, BeamWire.UnsignedTag, tuple);
        Op(IsTuple, LabelTag, end);
        Operand(XRegisterTag, cursor);
        Op(TestArity, LabelTag, invalid);
        Operand(XRegisterTag, cursor);
        Operand(BeamWire.UnsignedTag, cursorArity);

        if (projection is null)
        {
            EmitMapIteratorPair(
                entry,
                excluded,
                branch,
                cursorArity
            );
        }
        else
        {
            EmitMapListIteratorProjection(entry, branch, projection);
        }

        Op(Label, BeamWire.UnsignedTag, end);
        EmitAtomTest(cursor, MapIteratorEnd, invalid);
        EmitConstant(Nil.Value, cursor);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, invalid);
        Op(TestHeap, BeamWire.UnsignedTag, pairHeapWords);
        Operand(BeamWire.UnsignedTag, MapIteratorHelperArity);
        EmitProtectedPair(AtomTag, Intern(ComprehensionErrorReasons.BadGenerator));
        EmitLine(default);
        Op(TailExternalCall, BeamWire.UnsignedTag, MapIteratorHelperArity);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Error, MapIteratorHelperArity));
    }

    private void EmitMapIteratorPair(
        int entry,
        Atom? excluded,
        int branch,
        int cursorArity
    )
    {
        const int cursor = 0;
        const int key = 1;
        const int value = 2;
        const int keySlot = 0;
        const int valueSlot = 1;
        const int pairArity = 2;
        const int continuationField = 2;
        const int pairHeapWords = pairArity + 1;
        const int tupleSlot = 0;
        int frame;
        if (excluded is null)
        {
            // The source does not need either field before recursion.
            frame = 1;
            Op(Allocate, BeamWire.UnsignedTag, frame);
            Operand(BeamWire.UnsignedTag, MapIteratorHelperArity);
            MoveRegister(
                XRegisterTag,
                cursor,
                YRegisterTag,
                tupleSlot
            );
            EmitTuplePart(cursor, continuationField, cursor);
        }
        else
        {
            // The exact filter consumes the value before recursion and rejects with no frame.
            frame = 2;
            EmitTuplePart(cursor, 0, key);
            EmitTuplePart(cursor, 1, value);
            EmitTuplePart(cursor, continuationField, cursor);
            EmitAtomTest(value, excluded.Name, branch);
            Op(TailLocalCall, BeamWire.UnsignedTag, MapIteratorHelperArity);
            Operand(LabelTag, entry);

            Op(Label, BeamWire.UnsignedTag, branch);
            Op(Allocate, BeamWire.UnsignedTag, frame);
            Operand(BeamWire.UnsignedTag, cursorArity);
            MoveRegister(
                XRegisterTag,
                key,
                YRegisterTag,
                keySlot
            );
            MoveRegister(
                XRegisterTag,
                value,
                YRegisterTag,
                valueSlot
            );
        }

        EmitFieldCall(MapIteratorHelperArity, entry);
        Op(TestHeap, BeamWire.UnsignedTag, pairHeapWords + ConsHeapWords);
        Operand(BeamWire.UnsignedTag, MapIteratorHelperArity);
        if (excluded is null)
        {
            EmitTuplePart(
                tupleSlot,
                0,
                key,
                YRegisterTag
            );
            EmitTuplePart(
                tupleSlot,
                1,
                value,
                YRegisterTag
            );
        }

        EmitTupleHeader(XRegisterTag, pairArity, key);
        Operand(excluded is null ? XRegisterTag : YRegisterTag, excluded is null ? key : keySlot);
        Operand(excluded is null ? XRegisterTag : YRegisterTag, excluded is null ? value : valueSlot);
        Op(PutList, XRegisterTag, key);
        Operand(XRegisterTag, cursor);
        Operand(XRegisterTag, cursor);
        EmitFrameReturn(frame);
    }
}

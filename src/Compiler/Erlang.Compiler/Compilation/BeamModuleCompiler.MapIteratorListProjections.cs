using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record MapListIteratorProjection(Atom? RequiredValue, int TupleArity, int Field);

    private readonly Dictionary<(string Name, int Arity), MapListIteratorProjection> mapListIteratorHelpers = [];

    private (FunctionDefinition Parent, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractMapIteratorListProjection(
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
            || expressions.Where(expression => expression is Expr.ListComprehension or Expr.MapComprehension
                or Expr.BinaryComprehension).ToArray() is not
            [
                Expr.ListComprehension
            {
                Items: [var element],
                Qualifiers: [ComprehensionQualifier.MapGenerator
                {
                    Strict: false,
                    Pattern: Pattern.Tuple { Items: [Pattern.Variable key, var value] },
                    Source: Expr.Variable source
                } generator]
            } site
            ]
            || generator.SourceLine == 0 || key.Name == VariableScopeNames.Wildcard
            || source.Name == key.Name
            || ReadMapListIteratorProjection(key, value, element) is not { } projection)
        {
            return null;
        }

        string name = MappingName(function.Name, function.Arity);
        var signature = (name, MapIteratorHelperArity);
        if (!declared.Add(signature))
        {
            return null;
        }

        var collect = new Expr.Call(null, name, [new Expr.Variable(MapIteratorCursor)]);
        var parent = ReplaceMapIteratorSite(
            function,
            site,
            source,
            collect,
            generator.SourceLine,
            site.PreprocessorFile
        );
        mapListIteratorHelpers.Add(signature, projection);

        return (parent, BuildMapIteratorHelper(
            name,
            key,
            value,
            element,
            null,
            skipRejectedPair: true
        ), new BeamSourceLocation(site.PreprocessorFile ?? location.File, generator.SourceLine));
    }

    private static MapListIteratorProjection? ReadMapListIteratorProjection(
        Pattern.Variable key,
        Pattern value,
        Expr element
    )
    {
        if (value is Pattern.Literal { Value: Atom required }
            && element is Expr.Variable keyResult && keyResult.Name == key.Name)
        {
            return new(required, 0, 0);
        }

        if (value is not Pattern.Tuple tuple
            || tuple.Items.Any(field => field is not (Pattern.Any or Pattern.Variable))
            || element is not Expr.Tuple { Items: [Expr.Variable first, Expr.Variable second] }
            || first.Name != key.Name || second.Name == key.Name || second.Name == VariableScopeNames.Wildcard)
        {
            return null;
        }

        var bound = new HashSet<string>(StringComparer.Ordinal) { key.Name };
        int projectedField = -1;
        for (int index = 0; index < tuple.Items.Count; index++)
        {
            if (tuple.Items[index] is not Pattern.Variable field || field.Name == VariableScopeNames.Wildcard)
            {
                continue;
            }

            if (!bound.Add(field.Name))
            {
                return null;
            }

            if (field.Name == second.Name)
            {
                projectedField = index;
            }
        }

        return projectedField < 0 ? null : new(null, tuple.Items.Count, projectedField);
    }

    private bool HasMapIteratorHelper((string, int) signature) =>
        mapIteratorHelpers.ContainsKey(signature) || mapListIteratorHelpers.ContainsKey(signature);

    private void EmitMapListIteratorProjection(
        int entry,
        int rejected,
        MapListIteratorProjection projection
    )
    {
        const int cursor = 0;
        const int key = 1;
        const int value = 2;
        const int keySlot = 0;
        const int valueSlot = 1;
        const int pairArity = 2;
        const int continuation = 2;
        EmitTuplePart(cursor, 0, key);
        EmitTuplePart(cursor, 1, value);
        EmitTuplePart(cursor, continuation, cursor);

        bool onlyKey = projection.RequiredValue is not null;
        int frame = onlyKey ? 1 : 2;
        if (projection.RequiredValue is { } atom)
        {
            EmitAtomTest(value, atom.Name, rejected);
        }
        else
        {
            Op(IsTuple, LabelTag, rejected);
            Operand(XRegisterTag, value);
            Op(TestArity, LabelTag, rejected);
            Operand(XRegisterTag, value);
            Operand(BeamWire.UnsignedTag, projection.TupleArity);
        }

        Op(Allocate, BeamWire.UnsignedTag, frame);
        Operand(BeamWire.UnsignedTag, frame + MapIteratorHelperArity);
        MoveRegister(
            XRegisterTag,
            key,
            YRegisterTag,
            keySlot
        );
        if (!onlyKey)
        {
            MoveRegister(
                XRegisterTag,
                value,
                YRegisterTag,
                valueSlot
            );
        }

        EmitFieldCall(MapIteratorHelperArity, entry);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + (onlyKey ? 0 : pairArity + 1));
        Operand(BeamWire.UnsignedTag, MapIteratorHelperArity);
        if (onlyKey)
        {
            Op(PutList, YRegisterTag, keySlot);
        }
        else
        {
            EmitTuplePart(
                valueSlot,
                projection.Field,
                key,
                YRegisterTag
            );
            EmitTupleHeader(XRegisterTag, pairArity, key);
            Operand(YRegisterTag, keySlot);
            Operand(XRegisterTag, key);
            Op(PutList, XRegisterTag, key);
        }

        Operand(XRegisterTag, cursor);
        Operand(XRegisterTag, cursor);
        EmitFrameReturn(frame);

        Op(Label, BeamWire.UnsignedTag, rejected);
        Op(TailLocalCall, BeamWire.UnsignedTag, MapIteratorHelperArity);
        Operand(LabelTag, entry);
    }
}

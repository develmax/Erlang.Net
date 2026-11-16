using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record CapturedListFilter(Expr.Call Map, int MinimumLength);
    private readonly Dictionary<(string Name, int Arity), CapturedListFilter> capturedListFilters = [];

    private (Expr.Sequence Body, FunctionDefinition Helper, BeamSourceLocation Location)? ExtractCapturedListFilter(
        FunctionDefinition function,
        Clause clause,
        BeamSourceLocation location,
        HashSet<(string, int)> declared
    )
    {
        if (!HasUnconditionalParameters(clause)
            || clause.Body is not Expr.Sequence
            {
                Items: [.., Expr.Case
                {
                    Value: Expr.ListComprehension
                    {
                        Items: [Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable capture, Expr.Variable keyArgument, Expr.Variable listArgument]
                        } map],
                        Qualifiers: [ComprehensionQualifier.Generator
                        {
                            Strict: false, Source: Expr.Variable source,
                            Pattern: Pattern.Tuple { Items: [Pattern.Variable key, AliasPattern listAlias] }
                        } generator]
                    } comprehension
                } decision]
            } sequence
            || !clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == capture.Name)
            || !declared.Contains((map.Function, 3))
            || key.Name != keyArgument.Name || key.Name == source.Name
            || (listAlias.Left as Pattern.Variable ?? listAlias.Right as Pattern.Variable) is not { } list
            || (listAlias.Left is Pattern.Variable ? listAlias.Right : listAlias.Left) is not Pattern.List
            {
                Items.Count: > 0,
                Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
            } prefix
            || prefix.Items.Any(item => item is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
            || list.Name != listArgument.Name || list.Name == source.Name || source.Name == capture.Name
            || !DistinctMappingBindings(key, list, new Pattern.Variable(capture.Name)))
        {
            return null;
        }

        string helper = MappingName(function.Name, function.Arity);
        if (!declared.Add((helper, 2)))
        {
            return null;
        }

        var sourceLocation = generator.GeneratorLine == 0 ? location
            : new BeamSourceLocation(generator.GeneratorFile ?? location.File, generator.GeneratorLine);
        var call = new Expr.Call(null, helper, [source, capture])
        {
            SourceLine = sourceLocation.Line,
            PreprocessorFile = sourceLocation.File
        };
        var definition = new FunctionDefinition(helper, 2,
            [new Clause([new Pattern.Variable(source.Name), new Pattern.Variable(capture.Name)], null, comprehension)]);
        capturedListFilters.Add((helper, 2), new CapturedListFilter(map, prefix.Items.Count));

        return (sequence with { Items = [.. sequence.Items.SkipLast(1), decision with { Value = call }] }, definition, sourceLocation);
    }

    private void EmitCapturedListFilter(
        CapturedListFilter filter,
        int entry,
        IReadOnlyDictionary<(string Name, int Arity), int> entries,
        BeamSourceLocation location
    )
    {
        int skipped = nextLabel++;
        int empty = nextLabel++;
        int badGenerator = nextLabel++;
        EmitNonemptyList(0, empty);
        EmitListParts(
            0,
            XRegisterTag,
            2,
            XRegisterTag,
            0
        );
        Op(IsTuple, LabelTag, skipped);
        Operand(XRegisterTag, 2);
        Op(TestArity, LabelTag, skipped);
        Operand(XRegisterTag, 2);
        Operand(BeamWire.UnsignedTag, 2);
        EmitTuplePart(2, 1, 3);
        EmitNonemptyList(3, skipped);
        for (int index = 1; index < filter.MinimumLength; index++)
        {
            Op(GetTail, XRegisterTag, index == 1 ? 3 : 4);
            Operand(XRegisterTag, 4);
            EmitNonemptyList(4, skipped);
        }

        Op(Allocate, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 4);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        MoveRegister(
            XRegisterTag,
            1,
            YRegisterTag,
            1
        );
        EmitTuplePart(2, 0, 1);
        MoveRegister(
            XRegisterTag,
            3,
            XRegisterTag,
            2
        );
        MoveRegister(
            YRegisterTag,
            1,
            XRegisterTag,
            0
        );
        EmitCallLine(filter.Map, location);
        EmitFieldCall(3, entries[(filter.Map.Function, 3)]);
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            2
        );
        MoveRegister(
            YRegisterTag,
            1,
            XRegisterTag,
            1
        );
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            1
        );
        Op(TrimFrame, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            2,
            XRegisterTag,
            0
        );
        if (CallLocation(filter.Map, location) != location)
        {
            EmitLine(location);
        }

        EmitFieldCall(2, entry);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);

        Op(Label, BeamWire.UnsignedTag, skipped);
        Op(TailLocalCall, BeamWire.UnsignedTag, 2);
        Operand(LabelTag, entry);
        Op(Label, BeamWire.UnsignedTag, empty);
        Op(IsNil, LabelTag, badGenerator);
        Operand(XRegisterTag, 0);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, badGenerator);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTupleHeader(XRegisterTag, 2, 0);
        LiteralOperand(Term.A("bad_generator"));
        Operand(XRegisterTag, 0);
        Op(TailExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, "error", 1));
    }
}

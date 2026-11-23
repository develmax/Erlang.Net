using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int ZippedListArity = 2;
    private const int ZippedListHelperLabelCount = 4;

    private sealed record ZippedListPlan(string Helper, string Mapper);

    private readonly Dictionary<(string Name, int Arity), ZippedListPlan> zippedListOwners = [];
    private readonly Dictionary<(string Name, int Arity), ZippedListPlan> zippedListHelpers = [];

    private (FunctionDefinition Function, BeamSourceLocation Location)? ExtractZippedListMapper(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || function.Arity != ZippedListArity
            || IsDeclaredNative((function.Name, function.Arity))
            || function.Clauses is not [
                {
                    Patterns: [
                        Pattern.Variable { Name: not VariableScopeNames.Wildcard } firstParameter,
                        Pattern.Variable { Name: not VariableScopeNames.Wildcard } secondParameter
                    ],
                    Guard: null,
                    Body: Expr.ListComprehension
                    {
                        Items: [
                            Expr.Call
                        {
                            Module: null,
                            DynamicRemote: false,
                            Arguments: [Expr.Variable firstItem, Expr.Variable secondItem]
                        } mapper
                        ],
                        Qualifiers: [
                            ComprehensionQualifier.Zip
                        {
                            Generators: [
                                    ComprehensionQualifier.Generator
                            {
                                Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } firstPattern,
                                Source: Expr.Variable firstSource,
                                Strict: false
                            } firstGenerator,
                                    ComprehensionQualifier.Generator
                            {
                                Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } secondPattern,
                                Source: Expr.Variable secondSource,
                                Strict: false
                            } secondGenerator
                                ]
                        }
                        ]
                    } comprehension
                }
            ]
            || firstParameter.Name == secondParameter.Name
            || firstParameter.Name != firstSource.Name || secondParameter.Name != secondSource.Name
            || firstPattern.Name == secondPattern.Name
            || firstItem.Name != firstPattern.Name || secondItem.Name != secondPattern.Name
            || firstPattern.Name == firstParameter.Name || firstPattern.Name == secondParameter.Name
            || secondPattern.Name == firstParameter.Name || secondPattern.Name == secondParameter.Name
            || !localFunctions.ContainsKey((mapper.Function, ZippedListArity))
            || IsDeclaredNative((mapper.Function, ZippedListArity)))
        {
            return null;
        }

        int line = comprehension.PreprocessorLine != 0
            ? comprehension.PreprocessorLine
            : comprehension.Source?.Line ?? location.Line;
        var helperLocation = new BeamSourceLocation(comprehension.PreprocessorFile ?? location.File, line);
        if ((mapper.SourceLine != 0 && mapper.SourceLine != line)
            || (mapper.PreprocessorFile is not null && mapper.PreprocessorFile != helperLocation.File)
            || (firstGenerator.GeneratorLine != 0 && firstGenerator.GeneratorLine != line)
            || (secondGenerator.GeneratorLine != 0 && secondGenerator.GeneratorLine != line)
            || (firstGenerator.GeneratorFile is not null && firstGenerator.GeneratorFile != helperLocation.File)
            || (secondGenerator.GeneratorFile is not null && secondGenerator.GeneratorFile != helperLocation.File))
        {
            return null;
        }

        // This top-level comprehension introduces one Core letrec and one lifted function.
        string helper = GeneratedComprehensionNames.Function(
            function.Name,
            function.Arity,
            GeneratedComprehensionNames.ZipList,
            0,
            ZippedListArity,
            0
        );
        var key = (helper, ZippedListArity);
        if (!declared.Add(key))
        {
            return null;
        }

        var plan = new ZippedListPlan(helper, mapper.Function);
        zippedListOwners.Add((function.Name, function.Arity), plan);
        zippedListHelpers.Add(key, plan);

        return (BuildZippedListHelper(
            helper,
            firstPattern,
            secondPattern,
            mapper
        ), helperLocation);
    }

    private static FunctionDefinition BuildZippedListHelper(
        string helper,
        Pattern firstPattern,
        Pattern secondPattern,
        Expr element
    )
    {
        const string left = "$zipped_left_cursor";
        const string right = "$zipped_right_cursor";
        var recur = new Expr.Call(null, helper, [new Expr.Variable(left), new Expr.Variable(right)]);
        var suffixes = new Expr.Tuple([new Expr.Variable(left), new Expr.Variable(right)]);
        var failure = new Expr.Tuple([new Expr.Literal(Term.A(ComprehensionErrorReasons.BadGenerators)), suffixes]);
        Clause[] clauses =
        [
            new(
                [
                    new Pattern.List([firstPattern], new Pattern.Variable(left)),
                    new Pattern.List([secondPattern], new Pattern.Variable(right))
                ],
                null,
                new Expr.List([element], recur)
            ),
            new([new Pattern.Literal(Nil.Value), new Pattern.Literal(Nil.Value)], null, new Expr.Literal(Nil.Value)),
            new(
                [new Pattern.Variable(left), new Pattern.Variable(right)],
                null,
                new Expr.Call(ErlangModuleNames.Module, AutoImportNames.Error, [failure])
            )
        ];

        return new FunctionDefinition(helper, ZippedListArity, clauses);
    }

    private void EmitZippedListHelper(int entry, int mapper)
    {
        const int firstCursor = 0;
        const int secondCursor = 1;
        const int firstHead = 2;
        const int firstTail = 3;
        const int firstSavedTail = 1;
        const int secondSavedTail = 0;
        const int initialFrame = ZippedListArity;
        const int retainedFrame = 1;
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

        // Both tails live across the mapper call. The cold edge still owns the original two cursors.
        Op(Allocate, BeamWire.UnsignedTag, initialFrame);
        Operand(BeamWire.UnsignedTag, firstTail + 1);
        MoveRegister(
            XRegisterTag,
            firstTail,
            YRegisterTag,
            firstSavedTail
        );
        EmitListParts(
            secondCursor,
            XRegisterTag,
            secondCursor,
            YRegisterTag,
            secondSavedTail
        );
        MoveRegister(
            XRegisterTag,
            firstHead,
            XRegisterTag,
            firstCursor
        );
        EmitFieldCall(ZippedListArity, mapper);

        // Recycle the first tail's slot for the mapped head, which is the only root across recursion.
        Op(Swap, YRegisterTag, firstSavedTail);
        Operand(XRegisterTag, firstCursor);
        MoveRegister(
            YRegisterTag,
            secondSavedTail,
            XRegisterTag,
            secondCursor
        );
        Op(TrimFrame, BeamWire.UnsignedTag, initialFrame - retainedFrame);
        Operand(BeamWire.UnsignedTag, retainedFrame);
        EmitFieldCall(ZippedListArity, entry);
        Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
        Operand(BeamWire.UnsignedTag, retainedFrame);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, firstCursor);
        Operand(XRegisterTag, firstCursor);
        EmitFrameReturn(retainedFrame);

        EmitZippedListEnd(
            firstEmpty,
            invalid,
            firstProperList: false,
            secondProperList: false
        );
    }

    private void EmitZippedListEnd(
        int firstEmpty,
        int invalid,
        bool firstProperList,
        bool secondProperList
    )
    {
        const int firstCursor = 0;
        const int secondCursor = 1;
        const int pairHeapWords = 3;
        Op(Label, BeamWire.UnsignedTag, firstEmpty);
        if (!firstProperList)
        {
            EmitNilTest(XRegisterTag, firstCursor, invalid);
        }

        Op(IsNil, LabelTag, invalid);
        if (secondProperList)
        {
            EmitTypedRegister(secondCursor, ProperListTypeBits);
        }
        else
        {
            Operand(XRegisterTag, secondCursor);
        }

        EmitConstant(Nil.Value, firstCursor);
        instructions.WriteByte(Return);

        Op(Label, BeamWire.UnsignedTag, invalid);
        Op(TestHeap, BeamWire.UnsignedTag, pairHeapWords * 2);
        Operand(BeamWire.UnsignedTag, ZippedListArity);
        EmitTupleHeader(XRegisterTag, ZippedListArity, firstCursor);
        Operand(XRegisterTag, firstCursor);
        Operand(XRegisterTag, secondCursor);
        EmitProtectedPair(AtomTag, Intern(ComprehensionErrorReasons.BadGenerators));
        Op(TailExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Error, 1));
    }
}

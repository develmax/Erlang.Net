using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int OneBitSegmentWidth = 1;
    private const int ByteCapacityShift = 3;
    private const long ListLengthTypeMaximum = (1L << 58) - 1;
    private const int WritableBinaryListLabelCount = 4;
    private const int WritableBinaryListHelperArity = 2;

    private sealed record WritableBinaryListPlan(string HelperName, Integer? Subtract);

    private readonly Dictionary<(string Name, int Arity), WritableBinaryListPlan> writableBinaryListOwners = [];
    private readonly Dictionary<(string Name, int Arity), WritableBinaryListPlan> writableBinaryListHelpers = [];

    private (FunctionDefinition Function, BeamSourceLocation Location)? ExtractWritableBinaryListBuilder(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || function.Arity != 1
            || IsDeclaredNative((function.Name, function.Arity))
            || function.Clauses is not
            [
            {
                Guard: null, Patterns: [Pattern.Variable parameter],
                Body: Expr.BinaryComprehension
                {
                    Body: Expr.Bits
                    {
                        Segments: [
                        {
                            Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
                            Endian: BitByteOrders.Big, Signed: false, IsStringLiteral: false,
                            Size: Expr.Literal { Value: Integer width }
                        } segment
                            ]
                    } bits,
                    Qualifiers: [
                            ComprehensionQualifier.Generator
                    {
                        Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable source
                    } generator
                        ]
                }
            }
            ] || parameter.Name != source.Name || item.Name == VariableScopeNames.Wildcard
            || width.Value != OneBitSegmentWidth)
        {
            return null;
        }

        Integer? subtract = null;
        Expr value = segment.Value;
        if (value is Expr.Binary
            {
                Operator: ErlangOperators.Minus, Left: Expr.Variable left,
                Right: Expr.Literal { Value: Integer offset }
            } && left.Name == item.Name)
        {
            subtract = offset;
        }
        else if (value is not Expr.Variable variable || variable.Name != item.Name)
        {
            return null;
        }

        int line = generator.GeneratorLine != 0 ? generator.GeneratorLine : location.Line;
        string? file = generator.GeneratorFile ?? location.File;
        if (bits.SourceLine != 0 && bits.SourceLine != line
            || value is Expr.Binary { SourceLine: > 0 } arithmetic && arithmetic.SourceLine != line)
        {
            return null;
        }

        // A standalone, single generator gives v3_core and SSA one letrec each.
        string helper = GeneratedComprehensionNames.Function(
            function.Name,
            function.Arity,
            GeneratedComprehensionNames.Binary,
            0,
            WritableBinaryListHelperArity,
            0
        );
        var helperKey = (helper, WritableBinaryListHelperArity);
        if (!declared.Add(helperKey))
        {
            return null;
        }

        var plan = new WritableBinaryListPlan(helper, subtract);
        writableBinaryListOwners.Add((function.Name, function.Arity), plan);
        writableBinaryListHelpers.Add(helperKey, plan);

        const string tail = "$writable_binary_list_tail";
        const string accumulator = "$writable_binary_list_accumulator";
        const string invalid = "$writable_binary_list_invalid";
        var prefix = new BitSegment(
            new Expr.Variable(accumulator),
            null,
            BitSegmentTypes.Binary,
            BitSyntaxDefaults.BitStringUnit
        );
        var definition = new FunctionDefinition(
            helper,
            WritableBinaryListHelperArity,
            [
            new Clause(
                [
                    new Pattern.List([item], new Pattern.Variable(tail)),
                    new Pattern.Variable(accumulator)
                ],
                null,
                new Expr.Call(null, helper, [new Expr.Variable(tail), bits with { Segments = [prefix, segment] }])
            ),
            new Clause(
                [new Pattern.Literal(Nil.Value), new Pattern.Variable(accumulator)],
                null,
                new Expr.Variable(accumulator)
            ),
            new Clause(
                [new Pattern.Variable(invalid), new Pattern.Variable(VariableScopeNames.Wildcard)],
                null,
                new Expr.Call(
                    ErlangModuleNames.Module,
                    AutoImportNames.Error,
                    [new Expr.Tuple([new Expr.Literal(Term.A(ComprehensionErrorReasons.BadGenerator)), new Expr.Variable(invalid)])]
                )
            )
        ]
        );

        return (definition, new(file, line));
    }

    private void EmitWritableBinaryListOwner(int entry)
    {
        int failedSize = nextLabel++;
        int initialize = nextLabel++;
        int bitRounding = BitSyntaxDefaults.BinaryUnit - 1;

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        Op(GcBifOne, LabelTag, failedSize);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Length, 1));
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitLine(default);
        EmitWritableCapacityOperation(
            ErlangOperators.Plus,
            bitRounding,
            0,
            ListLengthTypeMaximum
        );
        EmitWritableCapacityOperation(
            ErlangOperators.ShiftRight,
            ByteCapacityShift,
            bitRounding,
            ListLengthTypeMaximum + bitRounding
        );
        Op(Jump, LabelTag, initialize);
        Op(Label, BeamWire.UnsignedTag, failedSize);
        EmitConstant(Term.I(BinaryComprehensionInitialCapacity), 0);
        Op(Label, BeamWire.UnsignedTag, initialize);
        instructions.WriteByte(InitializeWritableBinary);
        MoveRegister(
            XRegisterTag,
            0,
            XRegisterTag,
            1
        );
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, WritableBinaryListHelperArity);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, 1);
    }

    private void EmitWritableCapacityOperation(
        string operation,
        int argument,
        long lower,
        long upper
    )
    {
        Op(GcBifTwo, LabelTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, operation, 2));
        EmitTypedRegister(
            0,
            IntegerTypeBits,
            lower,
            upper
        );
        LiteralOperand(Term.I(argument));
        Operand(XRegisterTag, 0);
    }

    private void EmitWritableBinaryListHelper(int entry, WritableBinaryListPlan plan)
    {
        int empty = nextLabel++;
        int invalid = nextLabel++;
        Op(IsNonemptyList, LabelTag, empty);
        Operand(XRegisterTag, 0);
        Op(GetList, XRegisterTag, 0);
        Operand(XRegisterTag, 2);
        Operand(XRegisterTag, 0);
        if (plan.Subtract is { } subtract)
        {
            Op(GcBifTwo, LabelTag, 0);
            Operand(BeamWire.UnsignedTag, 3);
            Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
            Operand(XRegisterTag, 2);
            LiteralOperand(subtract);
            Operand(XRegisterTag, 2);
        }

        // Only the initial writable buffer or the immediately preceding private
        // append reaches X1. Both the remaining list and that buffer are roots.
        BeginBinaryConstruction(
            failure: 0,
            live: 3,
            unit: BitSyntaxDefaults.BitStringUnit,
            destination: 1,
            segmentCount: 2
        );
        EmitBinarySegmentPrefix(
            BinaryPrivateAppendSegment,
            1,
            BitSyntaxDefaults.BitStringUnit,
            BitByteOrders.Big
        );
        EmitTypedRegister(1, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
        LiteralOperand(Term.A(BitSizeAtoms.All));
        EmitBinarySegmentPrefix(
            BitSegmentTypes.Integer,
            2,
            BitSyntaxDefaults.NumericUnit,
            BitByteOrders.Big
        );
        if (plan.Subtract is null)
        {
            Operand(XRegisterTag, 2);
        }
        else
        {
            EmitTypedRegister(2, NumberTypeBits);
        }

        LiteralOperand(Term.I(OneBitSegmentWidth));
        Op(TailLocalCall, BeamWire.UnsignedTag, WritableBinaryListHelperArity);
        Operand(LabelTag, entry);
        EmitMappingEnd(empty, invalid, nilResultRegister: 1);
    }
}

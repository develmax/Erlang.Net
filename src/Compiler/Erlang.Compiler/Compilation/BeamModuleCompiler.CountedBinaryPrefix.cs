using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCountedBinaryPrefix(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Arity != 2 || owner.OnLoadFunction is not null
                || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [BitPattern { Segments.Count: 0 }, Pattern.Variable emptyCount],
                    Body: Expr.Variable value
                },
                {
                    Guard: null, Patterns: [BitPattern { Segments: [var stopByte, var ignoredTail] }, Pattern.Variable stopCount],
                    Body: var stop
                },
                {
                    Guard: null, Patterns: [BitPattern { Segments: [var octet, var tail] }, Pattern.Variable count],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable next, _] } recur
                }
                ]
                || emptyCount.Name == VariableScopeNames.Wildcard || value.Name != emptyCount.Name
                || stopByte.Value is not Pattern.Literal { Value: Integer key }
                || !IsUnsignedByte(stopByte.Specification) || key.Value < byte.MinValue || key.Value > byte.MaxValue
                || ReadBinaryTailBinding(ignoredTail) is not { } ignored
                || ignored.Name != VariableScopeNames.Wildcard && ignored.Name == stopCount.Name
                || octet.Value is not Pattern.Variable octetBinding || !IsUnsignedByte(octet.Specification)
                || ReadBinaryTailBinding(tail) is not { } rest || rest.Name != next.Name || next.Name == VariableScopeNames.Wildcard
                || !DistinctBindings(rest, count)
                || octetBinding.Name != VariableScopeNames.Wildcard && !DistinctBindings(octetBinding, rest, count)
                || ReadScannerAdvance(
                    function,
                    recur,
                    rest.Name,
                    count.Name
                ) is not { } advance
                || advance.Right is not Expr.Literal { Value: Integer increment } || !increment.Value.IsOne
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            bool returnsCount = stop is Expr.Variable selected && selected.Name == stopCount.Name
                && selected.Name != VariableScopeNames.Wildcard;
            EntryCall? tailThrow = null;
            if (!returnsCount)
            {
                if (stop is not Expr.Call raise || ReadStackFreeThrow(raise) is not { } stackFree
                    || stackFree.Arguments.Any(argument => !AttributeLiteral.TryNormalizeConstant(argument, out _))
                    || ReadEntryCall(raise, [], allowControlTransfer: true) is not { } ordinaryThrow)
                {
                    return false;
                }

                tailThrow = PrepareStackFreeTailThrow(ordinaryThrow);
            }

            bool seeded = false;
            if (!HasOnlyLocalCalls(
                function,
                function.Arity,
                (_, call) =>
                {
                    if (ReferenceEquals(call, recur))
                    {
                        return true;
                    }

                    if (call.Arguments[1] is not Expr.Literal { Value: Integer seed } || !seed.Value.IsZero)
                    {
                        return false;
                    }

                    seeded = true;

                    return true;
                }
            ) || !seeded || !HasBinaryLocalArgument(function, recur, trackGuardedBinaryInputs: true))
            {
                return false;
            }

            int mismatch = owner.nextLabel - 2;
            int readByte = Label();
            int continueCounting = Label();
            int restore = Label();
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern(BeamBinaryNames.NoFail));
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetBinaryPosition, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            EmitBitMatchHeader(
                0,
                readByte,
                2,
                0,
                tailUnit: null
            );
            EmitCount();

            Mark(readByte);
            EmitBitMatchHeader(
                0,
                restore,
                9,
                BitSyntaxDefaults.BinaryUnit,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                octet.Specification,
                BitSyntaxDefaults.BinaryUnit,
                2,
                XRegisterTag,
                2
            );
            owner.Op(ExactEqualTest, LabelTag, continueCounting);
            owner.EmitTypedRegister(
                2,
                IntegerTypeBits,
                byte.MinValue,
                byte.MaxValue
            );
            owner.LiteralOperand(key);
            if (returnsCount)
            {
                EmitCount();
            }
            else
            {
                EmitRegisterTailCall(tailThrow!);
            }

            Mark(continueCounting);
            BeamSourceLocation previous = location;
            owner.EmitArithmeticLine(advance.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
            owner.EmitTypedRegister(1, IntegerTypeBits, 0);
            owner.LiteralOperand(increment);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);

            Mark(restore);
            owner.Op(SetBinaryPosition, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.Op(GetBinaryTail, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Op(Jump, LabelTag, mismatch);

            return true;

            void EmitCount()
            {
                owner.MoveToX0(XRegisterTag, 1);
                owner.instructions.WriteByte(Return);
            }
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool IsClosedGuardedComprehension(Clause clause, Expr.ListComprehension comprehension)
    {
        if (clause is not
            {
                Guard: null,
                Body: Expr.Sequence
                {
                    Items: [Expr.Match
                    {
                        Pattern: Pattern.Variable prepared,
                        Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments.Count: 2
                        }
                    }, var result]
                }
            }
            || !ReferenceEquals(result, comprehension) || !HasUnconditionalParameters(clause)
            || comprehension is not
            {
                Items: [Expr.Block { Body: Expr.Sequence { Items: var body } }],
                Qualifiers: [ComprehensionQualifier.Generator
                {
                    Strict: false, Pattern: Pattern.Variable item, Source: Expr.Variable input
                }]
            }
            || prepared.Name != input.Name || body.Count is not (2 or 3)
            || body[0] is not Expr.Match
            {
                Pattern: Pattern.Variable decoded,
                Value: Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable selected, var tag]
                }
            }
            || selected.Name != item.Name || !AttributeLiteral.TryNormalizeConstant(tag, out _)
            || body[^1] is not Expr.If
            {
                Clauses: [
                { Guard: Expr.Binary { Operator: ErlangOperators.AndAlso } guard, Body: var accepted },
                {
                    Guard: Expr.Literal { Value: Atom { Name: "true" } },
                    Body: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Exit,
                        DynamicRemote: false, Arguments: [var reason]
                    }
                }]
            }
            || !AttributeLiteral.TryNormalizeConstant(reason, out _))
        {
            return false;
        }

        var bindings = clause.Patterns.Cast<Pattern.Variable>().Concat([prepared, item, decoded]).ToList();
        string count = decoded.Name;
        if (body.Count == 3)
        {
            if (body[1] is not Expr.Match
                {
                    Pattern: Pattern.Variable size,
                    Value: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, Function: AutoImportNames.ByteSize,
                        DynamicRemote: false, Arguments: [Expr.Variable measuredInput]
                    }
                } || measuredInput.Name != decoded.Name)
            {
                return false;
            }

            bindings.Add(size);
            count = size.Name;
        }

        return DistinctBindings(bindings.ToArray())
            && guard is
            {
                Left: Expr.Binary
                {
                    Operator: ErlangOperators.LessOrEqual,
                    Left: Expr.Literal { Value: Integer },
                    Right: Expr.Variable lower
                },
                Right: Expr.Binary
                {
                    Operator: ErlangOperators.LessOrEqual,
                    Left: Expr.Variable upper,
                    Right: Expr.Literal { Value: Integer }
                }
            }
            && lower.Name == count && upper.Name == count
            && (accepted is Expr.Variable value && value.Name == decoded.Name
                || accepted is Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, Function: AutoImportNames.BinaryToList,
                    DynamicRemote: false, Arguments: [Expr.Variable bytes]
                } && bytes.Name == decoded.Name);
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedComprehensionParent(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable first, Pattern.Variable second],
                        Body: Expr.Sequence
                        {
                            Items: [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable prepared,
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable left, Expr.Variable right]
                                } producer
                            },
                                Expr.Call
                            {
                                Module: null, DynamicRemote: false, Arguments: [Expr.Variable input]
                            } helper]
                        }
                    }]
                || !DistinctBindings(first, second, prepared)
                || first.Name != left.Name || second.Name != right.Name || prepared.Name != input.Name
                || !entries.TryGetValue((producer.Function, 2), out int prepare)
                || !entries.TryGetValue((helper.Function, 1), out int tail)
                || !localFunctions.TryGetValue((helper.Function, 1), out var definition)
                || definition.Clauses[0].Body is not Expr.List { Items: [Expr.Block { Body: Expr.Sequence }] }
                || !definition.IsRecursiveListMapper)
            {
                return false;
            }

            EmitFrameAllocation(2, 0);
            owner.EmitCallLine(producer, location);
            owner.EmitFieldCall(2, prepare);
            EmitTailLocal(tail, 1, 0);

            return true;
        }

        private bool TryEmitGuardedComprehensionHelper(FunctionDefinition function)
        {
            if (!function.IsRecursiveListMapper || function.Arity != 1
                || !owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.List { Items: [Pattern.Variable item], Tail: Pattern.Variable suffix }],
                    Body: Expr.List
                    {
                        Items: [Expr.Block { Body: Expr.Sequence { Items: var body } } element],
                        Tail: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable next]
                        } recur
                    }
                },
                { Guard: null, Patterns: [Pattern.List { Items.Count: 0, Tail: null }] },
                    _]
                || recur.Function != function.Name || next.Name != suffix.Name
                || !entries.TryGetValue((function.Name, 1), out int entry)
                || body.Count is not (2 or 3) || body[0] is not Expr.Match { Pattern: Pattern.Variable decoded, Value: var input }
                || body[^1] is not Expr.If test || ReadDecodeCall(input, item.Name) is not { Convert: null } decode)
            {
                return false;
            }

            var range = body.Count == 2 ? ReadTaggedBranchRange(test, decoded.Name) : null;
            var size = body.Count == 3 ? ReadForwardedChoiceMeasurement(element, item.Name, suffix.Name) : null;
            if (range is null && size is not { Convert: not null } || range is not null && !IsDecoderIntrinsic(range.Exit))
            {
                return false;
            }

            bool integer = range is not null && ((InferPairFacts(decode.Decode) & PairFacts.Integer) != 0
                || localFunctions.TryGetValue((decode.Decode.Function, 2), out var definition)
                    && definition.Clauses is [var signed] && ReadSignedInteger(signed) is not null);
            int frame = range is null ? 1 : 2;
            int rejected = Label();
            int exhausted = Label();
            int badGenerator = Label();
            owner.EmitNonemptyList(0, exhausted);
            EmitFrameAllocation(1, frame);
            if (frame == 2)
            {
                owner.InitStackSlot(0);
            }

            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, frame - 1);
            EmitDecodeCall(decode, location);
            BeamSourceLocation previous = CallLocation(decode.Decode, location);
            if (range is not null)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    0
                );
                owner.Op(IsGreaterOrEqual, LabelTag, rejected);
                EmitRangeResultOperand(integer);
                owner.LiteralOperand(range.Lower);
                if (range.Upper is { } upper)
                {
                    owner.Op(IsGreaterOrEqual, LabelTag, rejected);
                    owner.LiteralOperand(upper);
                    long? lower = range.Lower.Value >= long.MinValue && range.Lower.Value <= long.MaxValue
                        ? (long)range.Lower.Value : null;
                    EmitRangeResultOperand(integer, lower);
                }

                owner.MoveRegister(
                    YRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    YRegisterTag,
                    1
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }
            else if (size is not null)
            {
                previous = EmitForwardedChoiceMeasurementCheck(size, rejected, previous);
                owner.Op(Swap, YRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitLine(location);
            owner.EmitFieldCall(1, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(rejected);
            Expr.Call exit = range?.Exit ?? size!.Exit;
            owner.EmitConstant(range?.Reason ?? size!.Reason, 0);
            owner.EmitCallLine(exit, previous);
            EmitCheckedDecoderExit(exit, frame);
            Mark(exhausted);
            owner.EmitNilTest(XRegisterTag, 0, badGenerator);
            owner.instructions.WriteByte(Return);
            Mark(badGenerator);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(Term.A(ErlangErrorReasons.BadGenerator));
            owner.Operand(XRegisterTag, 0);
            owner.EmitLine(location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Error, 1));

            return true;
        }
    }
}

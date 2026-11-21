using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedForeignFieldPairs(
            Expr[] items,
            Expr.Match extract,
            int arity,
            IReadOnlyDictionary<string, int> projections,
            string input,
            string tags
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || items is not [Expr.Match first, Expr.Match second, .. var rest]
                || first.Pattern is not Pattern.Tuple
                { Items: [Pattern.Variable firstBytes, Pattern.Variable firstLength] }
                || second.Pattern is not Pattern.Tuple
                { Items: [Pattern.Variable secondBytes, Pattern.Variable secondLength] }
                || !DistinctBindings(
                    firstBytes,
                    firstLength,
                    secondBytes,
                    secondLength
                )
                || ReadForeignFieldPair(first.Value, projections) is not { } left
                || ReadForeignFieldPair(second.Value, projections) is not { } right
                || left.Module is null && right.Module is null)
            {
                return false;
            }

            if (left.Module is not null && TryEmitRetainedForeignTriple(
                rest,
                extract,
                arity,
                projections,
                input,
                tags,
                first,
                second,
                left,
                right
            ))
            {
                return true;
            }

            string[] pairNames = [firstBytes.Name, firstLength.Name, secondBytes.Name, secondLength.Name];
            if (pairNames.Any(name => name == input || name == tags || projections.ContainsKey(name)))
            {
                return false;
            }

            var pairs = pairNames.ToHashSet(StringComparer.Ordinal);
            var bound = new HashSet<string>(pairs, StringComparer.Ordinal) { input, tags };
            if (ReadDeferredPairTail(
                new Expr.Sequence(rest),
                tags,
                bound,
                pairs
            ) is not
            {
                List.Items: [Expr.Variable firstPart, Expr.Variable secondPart],
                Operands: [var firstCount, var secondCount]
            } tail
                || firstPart.Name != firstBytes.Name || secondPart.Name != secondBytes.Name
                || firstCount != firstLength.Name || secondCount != secondLength.Name)
            {
                return false;
            }

            int badSecond = right.Module is null ? 0 : Label();
            int badFirst = left.Module is null ? 0 : Label();
            int badTuple = Label();
            bool saveFirstPair = badFirst != 0;
            int frame = saveFirstPair ? 3 : 2;
            owner.EmitTupleCheck(0, arity, badTuple);
            EmitGeneralFrame(2, initializedByMoves: 2, slots: frame);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                frame - 1
            );
            owner.EmitTuplePart(0, left.Field, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, right.Field);
            owner.Operand(YRegisterTag, frame - 2);
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitConstant(left.Option, 1);
            owner.EmitCallLine(left.Call, location);
            Call(left.Module, left.Call.Function, 2);
            if (saveFirstPair)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    0
                );
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    badFirst
                );
                owner.EmitConstant(right.Option, 1);
                Load(1, 0);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    YRegisterTag,
                    1
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 2);
            }
            else
            {
                owner.Op(Swap, YRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitConstant(right.Option, 1);
            }

            owner.EmitCallLine(right.Call, CallLocation(left.Call, location));
            Call(right.Module, right.Call.Function, 2);
            if (badSecond != 0)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    badSecond
                );
            }

            EmitTwoDeferredPairResult(
                tail.Sum,
                tail.Entry,
                2,
                1,
                left.Module is null ? InferPairAdditionType(left.Call) : default,
                right.Module is null ? InferPairAdditionType(right.Call) : default
            );
            if (badSecond != 0)
            {
                EmitFailure(badSecond, second);
            }

            if (badFirst != 0)
            {
                EmitFailure(badFirst, first);
            }

            EmitFailure(badTuple, extract);

            return true;

            void EmitFailure(int entry, Expr.Match match)
            {
                Mark(entry);
                var previous = location;
                owner.EmitArithmeticLine(match.SourceLine, ref previous);
                owner.Op(BadMatch, XRegisterTag, 0);
            }
        }

        private bool TryEmitRetainedForeignTriple(
            Expr[] rest,
            Expr.Match extract,
            int arity,
            IReadOnlyDictionary<string, int> projections,
            string input,
            string tags,
            Expr.Match first,
            Expr.Match second,
            (Expr.Call Call, string? Module, Term Option, int Field) left,
            (Expr.Call Call, string? Module, Term Option, int Field) middle
        )
        {
            if (arity != 4 || projections.Count != 3 || rest is not
                [
                    Expr.Match
                {
                    Pattern: Pattern.Tuple { Items: [Pattern.Variable { }, Pattern.Variable { }] },
                    Value: var last
                } third,
                    .. var result
                ])
            {
                return false;
            }

            var optional = ReadDeferredOptional(last);
            if (optional is { Second: not null } || optional is not null && optional.Default is not TupleTerm { Items.Count: 2 }
                || ReadForeignFieldPair(optional?.Call ?? last, projections) is not { Module: null } right
                || middle.Module is not null && optional is null)
            {
                return false;
            }

            var names = new[] { first, second, third }
                .SelectMany(binding => ((Pattern.Tuple)binding.Pattern).Items)
                .Cast<Pattern.Variable>().Select(variable => variable.Name).ToArray();
            var pairs = names.ToHashSet(StringComparer.Ordinal);
            if (pairs.Count != names.Length || pairs.Contains(VariableScopeNames.Wildcard)
                || names.Any(name => name == input || name == tags || projections.ContainsKey(name)))
            {
                return false;
            }

            var bound = new HashSet<string>(pairs, StringComparer.Ordinal) { input, tags };
            bound.UnionWith(projections.Keys);
            if (ReadDeferredPairTail(
                new Expr.Sequence(result),
                tags,
                bound,
                pairs
            ) is not
            {
                List.Items: [Expr.Variable firstBytes, Expr.Variable secondBytes, Expr.Variable thirdBytes],
                Operands: [var firstLength, var secondLength, var thirdLength]
            } tail
                || firstBytes.Name != names[0] || secondBytes.Name != names[2] || thirdBytes.Name != names[4]
                || firstLength != names[1] || secondLength != names[3] || thirdLength != names[5])
            {
                return false;
            }

            bool checkMiddle = middle.Module is not null;
            int frame = checkMiddle ? 4 : 3;
            int record = frame - 1;
            int tagSlot = frame - 2;
            int firstPair = frame - 3;
            int secondPair = checkMiddle ? 0 : record;
            int nextFailure = owner.nextLabel + (optional is null ? 0 : 2);
            int rejectedMiddle = checkMiddle ? nextFailure++ : 0;
            int rejectedFirst = nextFailure++;
            int rejectedInput = nextFailure;
            owner.EmitTupleCheck(0, arity, rejectedInput);
            EmitGeneralFrame(2, initializedByMoves: 2, slots: frame);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                tagSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                record
            );
            Project(
                XRegisterTag,
                0,
                left.Field,
                0
            );
            owner.EmitConstant(left.Option, 1);
            owner.EmitCallLine(left.Call, location);
            Call(left.Module, left.Call.Function, 2);
            SaveAndCheck(firstPair, rejectedFirst);
            Project(
                YRegisterTag,
                record,
                middle.Field,
                0
            );
            owner.EmitConstant(middle.Option, 1);
            BeamSourceLocation previous = CallLocation(left.Call, location);
            owner.EmitCallLine(middle.Call, previous);
            Call(middle.Module, middle.Call.Function, 2);

            if (checkMiddle)
            {
                SaveAndCheck(secondPair, rejectedMiddle);
                Project(
                    YRegisterTag,
                    record,
                    right.Field,
                    0
                );
            }
            else
            {
                Project(
                    YRegisterTag,
                    record,
                    right.Field,
                    1
                );
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    secondPair
                );
            }

            if (optional is not null)
            {
                EmitDeferredOptional(
                    optional,
                    -1,
                    checkMiddle ? record : null,
                    inputRegister: checkMiddle ? 0 : 1
                );
            }
            else
            {
                owner.MoveToX0(XRegisterTag, 1);
                owner.EmitConstant(right.Option, 1);
                owner.EmitCallLine(right.Call, CallLocation(middle.Call, previous));
                Call(null, right.Call.Function, 2);
            }

            deferredPairMembers.Add(names[0], (firstPair, 0));
            deferredPairMembers.Add(names[1], (firstPair, 1));
            deferredPairMembers.Add(names[2], (secondPair, 0));
            deferredPairMembers.Add(names[3], (secondPair, 1));
            deferredPairMembers.Add(names[4], (-1, 0));
            deferredPairMembers.Add(names[5], (-1, 1));
            EmitDeferredPairTail(
                tail,
                tagSlot,
                new Dictionary<string, PairAdditionType>(StringComparer.Ordinal),
                frame
            );
            deferredPairMembers.Clear();

            if (checkMiddle)
            {
                EmitFailure(second);
            }

            EmitFailure(first);
            EmitFailure(extract);

            return true;

            void Project(
                int sourceTag,
                int source,
                int field,
                int output
            )
            {
                owner.Op(GetTupleElement, sourceTag, source);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, output);
            }

            void SaveAndCheck(int slot, int rejected)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    rejected
                );
            }

            void EmitFailure(Expr.Match match)
            {
                Mark(Label());
                var at = location;
                owner.EmitArithmeticLine(match.SourceLine, ref at);
                owner.Op(BadMatch, XRegisterTag, 0);
            }
        }

        private (Expr.Call Call, string? Module, Term Option, int Field)? ReadForeignFieldPair(
            Expr value,
            IReadOnlyDictionary<string, int> fields
        )
        {
            if (value is not Expr.Call
                { DynamicRemote: false, Arguments: [Expr.Variable field, var option] } call
                || !TryResolveOrdinaryCallTarget(call, out string? module) || module == ErlangModuleNames.Module
                || module is null && !ReturnsTuple(
                    call,
                    2,
                    [],
                    exact: true
                )
                || !fields.TryGetValue(field.Name, out int index)
                || !AttributeLiteral.TryNormalizeConstant(option, out Term constant))
            {
                return null;
            }

            return (call, module, constant, index);
        }
    }
}

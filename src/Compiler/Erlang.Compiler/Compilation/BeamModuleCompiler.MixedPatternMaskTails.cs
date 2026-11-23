using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record MixedMaskArm(
            Clause Clause,
            Pattern Shape,
            Pattern.Variable Carrier,
            Pattern.Variable[] Inputs,
            Expr.Call Tail,
            string? Module
        );

        private bool TryEmitMixedPatternMaskTails(FunctionDefinition function)
        {
            const int arity = 4;
            if (recursiveClosure is not null || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != arity || function.Clauses is not [var flat, var map, var pair, var tagged, var literal]
                || ReadMixedMaskArm(flat) is not { Shape: Pattern.Tuple { Items: var bareFields } } bare
                || ReadMixedMaskArm(map) is not { Shape: MapPattern { Fields.Count: 2 } fields } mapped
                || ReadMixedMaskArm(pair) is not
                { Shape: Pattern.Tuple { Items: [Pattern.Tuple directFields, Pattern.Variable directScalar] } } direct
                || ReadMixedMaskArm(tagged) is not
                {
                    Shape: Pattern.Tuple
                    {
                        Items:
                            [
                                Pattern.Literal { Value: Atom tag },
                                Pattern.Tuple { Items: [Pattern.Tuple nestedFields, Pattern.Variable nestedScalar] }
                            ]
                    }
                } nested
                || ReadMixedMaskArm(literal) is not
                {
                    Shape: Pattern.Tuple
                    {
                        Items:
                            [
                                Pattern.Literal { Value: Atom otherTag },
                                Pattern.Tuple
                            {
                                Items: [Pattern.Literal { Value: Atom literalValue }, Pattern.Variable literalScalar]
                            }
                            ]
                    }
                } atom
                || !tag.Equals(otherTag) || bareFields.Count is <= 2 or >= CoreAliasDepthLimit
                || directFields.Items.Count != bareFields.Count || nestedFields.Items.Count != bareFields.Count
                || fields.Fields.Where(field => field.Value is Pattern.Tuple).ToArray() is not
                    [{ Key: Expr.Literal { Value: Atom tupleKey }, Value: Pattern.Tuple { Items: var mapFields } }]
                || fields.Fields.Where(field => field.Value is Pattern.Variable).ToArray() is not
                    [{ Key: Expr.Literal { Value: Atom scalarKey }, Value: Pattern.Variable mapScalar }]
                || tupleKey.Equals(scalarKey) || mapFields.Count != bareFields.Count
                || bare.Tail.Arguments is not
                    [
                        Expr.Variable,
                        Expr.Tuple { Items: [Expr.Variable bareCarrier, Expr.Variable bareScalar] },
                        Expr.List { Items.Count: 0, Tail: null },
                        Expr.Variable
                    ]
                || bareCarrier.Name != bare.Carrier.Name || bareScalar.Name != bare.Inputs[1].Name
                || !IsForwarded(mapped, bare) || !IsForwarded(direct, bare)
                || !IsForwarded(nested, bare) || !IsForwarded(atom, bare)
                || ReadFoldGuard(
                    bare,
                    bareFields,
                    bare.Inputs[1],
                    nestedField: false
                ) is not { } flatGuard
                || ReadFoldGuard(
                    mapped,
                    mapFields,
                    mapScalar,
                    nestedField: true
                ) is not { } mapGuard
                || ReadFoldGuard(
                    direct,
                    directFields.Items,
                    directScalar,
                    nestedField: true
                ) is not { } pairGuard
                || ReadFoldGuard(
                    nested,
                    nestedFields.Items,
                    nestedScalar,
                    nestedField: true
                ) is not { } nestedGuard
                || atom.Clause.Guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } atomGuard
                || !DistinctBindings([.. atom.Inputs, atom.Carrier, literalScalar])
                || !IsMixedMaskListGuard(atomGuard.Right, atom.Inputs[1])
                || ReadZeroMask(atomGuard.Left) is not { Input: Expr.Variable atomScalar } constantGuard
                || atomScalar.Name != literalScalar.Name)
            {
                return false;
            }

            static bool IsForwarded(MixedMaskArm multi, MixedMaskArm bare) =>
                multi.Module == bare.Module && multi.Tail.Function == bare.Tail.Function
                && multi.Tail.Arguments is [Expr.Variable, Expr.Variable whole, Expr.Variable ancillary, Expr.Variable]
                && whole.Name == multi.Carrier.Name && ancillary.Name == multi.Inputs[1].Name;

            const int source = 1;
            const int incomingScalar = 2;
            const int tupleValue = arity;
            const int scalarValue = arity + 1;
            const int accumulator = arity + 2;
            const int nextField = arity + 3;
            int failure = owner.nextLabel - 2;
            int bareEntry = Label();
            int pairEntry = Label();
            int taggedEntry = Label();
            int atomEntry = Label();
            int mapEntry = Label();
            int union = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseOr, 2);
            int mask = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2);

            // These source shapes are disjoint by type at each shared test.
            // A guard rejection cannot match a later arm of the same type.
            owner.EmitTupleArityDispatch(
                failure,
                [(2, pairEntry), (bareFields.Count, bareEntry)],
                source,
                typeFailure: mapEntry
            );
            Mark(bareEntry);
            EmitFold(
                source,
                tupleValue,
                scalarValue,
                usesParent: true
            );
            var flatLocation = EmitTupleMask(flatGuard.TupleMask, flatGuard.Location, tupleValue + 1);
            EmitMask(
                failure,
                arity,
                incomingScalar,
                flatGuard.ScalarMask,
                typed: false
            );
            TestMask();
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, arity);
            owner.Op(PutTuple, XRegisterTag, source);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, source);
            owner.Operand(XRegisterTag, incomingScalar);
            owner.EmitConstant(Nil.Value, incomingScalar);
            EmitTail(bare, flatLocation);

            Mark(pairEntry);
            ProjectPair(source);
            TestTupleValue(taggedEntry);
            EmitNestedGuard(direct, pairGuard);

            Mark(taggedEntry);
            owner.EmitAtomTest(tupleValue, tag.Name, failure);
            owner.EmitTupleCheck(scalarValue, 2, failure);
            ProjectPair(scalarValue);
            TestTupleValue(atomEntry);
            EmitNestedGuard(nested, nestedGuard);

            Mark(atomEntry);
            owner.EmitAtomTest(tupleValue, literalValue.Name, failure);
            EmitListAndScalarGuard(constantGuard.Mask);
            EmitTail(atom, location);

            Mark(mapEntry);
            EmitMapTest(source, failure);
            owner.Op(GetMapElements, LabelTag, failure);
            owner.EmitTypedRegister(source, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            foreach (var (key, output) in new[] { (tupleKey, tupleValue), (scalarKey, scalarValue) }
                .OrderByDescending(field => field.Item1, MapKeyOrder))
            {
                owner.LiteralOperand(key);
                owner.Operand(XRegisterTag, output);
            }

            TestTupleValue(failure);
            EmitNestedGuard(mapped, mapGuard);

            return true;

            void ProjectPair(int input)
            {
                owner.EmitTuplePart(input, 0, tupleValue);
                owner.EmitTuplePart(input, 1, scalarValue);
            }

            void TestTupleValue(int other)
            {
                owner.Op(IsTuple, LabelTag, other);
                owner.Operand(XRegisterTag, tupleValue);
                owner.Op(TestArity, LabelTag, failure);
                owner.Operand(XRegisterTag, tupleValue);
                owner.Operand(BeamWire.UnsignedTag, bareFields.Count);
            }

            void EmitFold(
                int input,
                int running,
                int scratch,
                bool usesParent
            )
            {
                owner.EmitTuplePart(input, 0, running);
                for (int index = 1; index < bareFields.Count; index++)
                {
                    bool lastChild = !usesParent && index == bareFields.Count - 1;
                    int field = lastChild ? input : scratch;
                    owner.EmitTuplePart(input, index, field);
                    BeginBif(
                        failure,
                        lastChild ? running + 1 : scratch + 1,
                        union
                    );
                    if (index == 1)
                    {
                        owner.Operand(XRegisterTag, running);
                    }
                    else
                    {
                        owner.EmitTypedRegister(running, IntegerTypeBits);
                    }

                    owner.Operand(XRegisterTag, field);
                    owner.Operand(XRegisterTag, lastChild ? input : running);
                }
            }

            BeamSourceLocation EmitTupleMask(Integer bits, BeamSourceLocation guardLocation, int live)
            {
                if (guardLocation.Line != 0 && guardLocation != location)
                {
                    owner.EmitLine(guardLocation);
                }

                EmitMask(
                    0,
                    live,
                    tupleValue,
                    bits,
                    typed: true
                );
                TestMask();

                return guardLocation.Line == 0 ? location : guardLocation;
            }

            void EmitListAndScalarGuard(Integer bits)
            {
                EmitMask(
                    failure,
                    scalarValue + 1,
                    scalarValue,
                    bits,
                    typed: false
                );
                TestMask();
                owner.Op(IsList, LabelTag, failure);
                owner.Operand(XRegisterTag, incomingScalar);
            }

            void EmitNestedGuard(
                MixedMaskArm arm,
                (Integer TupleMask, Integer ScalarMask, BeamSourceLocation Location) guard
            )
            {
                EmitFold(
                    tupleValue,
                    accumulator,
                    nextField,
                    usesParent: false
                );
                var guardLocation = EmitTupleMask(guard.TupleMask, guard.Location, scalarValue + 1);
                EmitListAndScalarGuard(guard.ScalarMask);
                EmitTail(arm, guardLocation);
            }

            void BeginBif(int rejected, int live, int import)
            {
                owner.Op(GcBifTwo, LabelTag, rejected);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, import);
            }

            void EmitMask(
                int rejected,
                int live,
                int input,
                Integer bits,
                bool typed
            )
            {
                BeginBif(rejected, live, mask);
                if (typed)
                {
                    owner.EmitTypedRegister(input, IntegerTypeBits);
                }
                else
                {
                    owner.Operand(XRegisterTag, input);
                }

                owner.LiteralOperand(bits);
                owner.Operand(XRegisterTag, tupleValue);
            }

            void TestMask()
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.EmitTypedRegister(tupleValue, IntegerTypeBits);
                owner.Operand(BeamWire.IntegerTag, 0);
            }

            void EmitTail(MixedMaskArm arm, BeamSourceLocation previous)
            {
                if (arm.Module is null)
                {
                    owner.Op(TailLocalCall, BeamWire.UnsignedTag, arity);
                    owner.Operand(LabelTag, entries[(arm.Tail.Function, arity)]);

                    return;
                }

                owner.EmitCallLine(arm.Tail, previous);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(arm.Module, arm.Tail.Function, arity));
            }
        }

        private MixedMaskArm? ReadMixedMaskArm(Clause clause)
        {
            if (clause.Patterns is not [Pattern.Variable first, AliasPattern alias, Pattern.Variable third, Pattern.Variable last]
                || clause.Body is not Expr.Call { Arguments: [Expr.Variable firstArg, _, _, Expr.Variable lastArg] } tail
                || firstArg.Name != first.Name || lastArg.Name != last.Name
                || !TryReadPatternAlias(alias, out Pattern.Variable carrier, out Pattern shape)
                || !TryResolveOrdinaryCallTarget(tail, out string? module) || module == ErlangModuleNames.Module)
            {
                return null;
            }

            return new MixedMaskArm(
                clause,
                shape,
                carrier,
                [first, third, last],
                tail,
                module
            );
        }

        private (Integer TupleMask, Integer ScalarMask, BeamSourceLocation Location)? ReadFoldGuard(
            MixedMaskArm arm,
            IReadOnlyList<Pattern> members,
            Pattern.Variable scalar,
            bool nestedField
        )
        {
            if (arm.Clause.Guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
            {
                return null;
            }

            if (nestedField)
            {
                if (!IsMixedMaskListGuard(conjunction.Right, arm.Inputs[1])
                    || conjunction.Left is not Expr.Binary { Operator: ErlangOperators.AndAlso } maskConjunction)
                {
                    return null;
                }

                conjunction = maskConjunction;
            }

            var elements = members.OfType<Pattern.Variable>().ToArray();
            Pattern.Variable[] names = nestedField
                ? [.. arm.Inputs, arm.Carrier, scalar, .. elements]
                : [.. arm.Inputs, arm.Carrier, .. elements];

            if (elements.Length != members.Count || !DistinctBindings(names)
                || ReadZeroMask(conjunction.Left) is not { } first || !IsOrderedFold(first.Input, elements, elements.Length - 1)
                || ReadZeroMask(conjunction.Right) is not { Input: Expr.Variable input } second
                || input.Name != scalar.Name)
            {
                return null;
            }

            var firstMask = (Expr.Binary)conjunction.Left;

            return (first.Mask, second.Mask, new BeamSourceLocation(location.File, firstMask.SourceLine));
        }

        private bool IsMixedMaskListGuard(Expr expression, Pattern.Variable expected) =>
            expression is Expr.Call
            {
                DynamicRemote: false, Function: AutoImportNames.IsList, Arguments: [Expr.Variable argument]
            } call && argument.Name == expected.Name && owner.ResolvesErlangBif(call);
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedMapBitwiseTail(FunctionDefinition function)
        {
            if (recursiveClosure is not null || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [var pair, var literal]
                || ReadEntry(pair) is not { } first || ReadEntry(literal) is not { } second
                || first.Tail.Function != second.Tail.Function
                || !entries.TryGetValue((first.Tail.Function, function.Arity), out int continuation)
                || pair.Guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } pairGuard
                || literal.Guard is not Expr.Binary { Operator: ErlangOperators.AndAlso } atomGuard
                || first.Fields.Where(field => field.Value is Pattern.Tuple).ToArray() is not
                    [{ Value: Pattern.Tuple { Items: var members }, Key: Expr.Literal { Value: Atom selected } }]
                || members.Count < 2 || members.Count >= FirstReservedXRegister || members.Any(member => member is not global::Erlang.Pattern.Variable))
            {
                return false;
            }

            Pattern.Variable[] elements = members.Cast<Pattern.Variable>().ToArray();
            if (first.Fields.Where(field => field.Value is Pattern.Variable).ToArray() is not
                [{ Key: Expr.Literal { Value: Atom scalar }, Value: Pattern.Variable scalarValue }]
                || selected.Equals(scalar)
                || ReadAtomMapFields(second.Map) is not { Length: 2 } alternatives
                || alternatives.FirstOrDefault(field => field.Key.Equals(selected)).Variable is not { } atomValue
                || alternatives.FirstOrDefault(field => field.Key.Equals(scalar)).Variable is not { } otherScalar
                || !DistinctBindings([first.Carrier, .. first.Arguments, scalarValue, .. elements])
                || !DistinctBindings([second.Carrier, .. second.Arguments, atomValue, otherScalar])
                || ReadZeroMask(pairGuard.Left) is not { } tupleMask
                || !IsOrderedFold(tupleMask.Input, elements, elements.Length - 1)
                || ReadZeroMask(pairGuard.Right) is not { Input: Expr.Variable pairScalar } firstMask
                || pairScalar.Name != scalarValue.Name
                || ReadZeroMask(atomGuard.Right) is not { Input: Expr.Variable atomScalar } secondMask
                || atomScalar.Name != otherScalar.Name || !firstMask.Mask.Equals(secondMask.Mask)
                || ReadSelectorAtom(atomGuard.Left, atomValue) is not { } choice)
            {
                return false;
            }

            const int sourceRegister = 0;
            const int valueRegister = 3;
            const int scalarRegister = 4;
            const int foldedRegister = 5;
            const int fieldRegister = 6;
            int entryFailure = owner.nextLabel - 2;
            int alternative = Label();
            int next = Label();

            // A typed get_map_elements assumes the source is a map. Public
            // or incompletely indexed entry points need the explicit check.
            if (!HasClosedMapEntry(function))
            {
                EmitMapTest(sourceRegister, entryFailure);
            }

            owner.Op(GetMapElements, LabelTag, entryFailure);
            owner.EmitTypedRegister(sourceRegister, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            foreach (var (key, output) in new[] { (selected, valueRegister), (scalar, scalarRegister) }
                .OrderByDescending(field => field.Item1, MapKeyOrder))
            {
                owner.LiteralOperand(key);
                owner.Operand(XRegisterTag, output);
            }

            owner.EmitTupleCheck(valueRegister, elements.Length, alternative);
            owner.EmitTuplePart(valueRegister, 0, foldedRegister);
            int union = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseOr, 2);
            for (int field = 1; field < elements.Length; field++)
            {
                owner.EmitTuplePart(valueRegister, field, fieldRegister);
                BeginBif(alternative, fieldRegister + 1, union);
                if (field == 1)
                {
                    owner.Operand(XRegisterTag, foldedRegister);
                }
                else
                {
                    owner.EmitTypedRegister(foldedRegister, IntegerTypeBits);
                }

                owner.Operand(XRegisterTag, fieldRegister);
                owner.Operand(XRegisterTag, foldedRegister);
            }

            var testLocation = new BeamSourceLocation(location.File, pairGuard.SourceLine);
            if (testLocation.Line != 0 && testLocation != location)
            {
                owner.EmitLine(testLocation);
            }

            int mask = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2);
            EmitMask(
                0,
                foldedRegister + 1,
                foldedRegister,
                tupleMask.Mask,
                foldedRegister,
                typed: true
            );
            TestZero(ExactEqualTest, alternative, foldedRegister);
            EmitMask(
                alternative,
                scalarRegister + 1,
                scalarRegister,
                firstMask.Mask,
                foldedRegister,
                typed: false
            );
            TestZero(ExactNotEqualTest, next, foldedRegister);

            Mark(alternative);
            owner.Op(ExactEqualTest, LabelTag, entryFailure);
            owner.Operand(XRegisterTag, valueRegister);
            owner.LiteralOperand(choice);
            EmitMask(
                entryFailure,
                scalarRegister + 1,
                scalarRegister,
                secondMask.Mask,
                valueRegister,
                typed: false
            );
            TestZero(ExactEqualTest, entryFailure, valueRegister);
            Mark(next);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, function.Arity);
            owner.Operand(LabelTag, continuation);

            return true;

            void BeginBif(int failure, int live, int import)
            {
                owner.Op(GcBifTwo, LabelTag, failure);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, import);
            }

            void EmitMask(
                int failure,
                int live,
                int input,
                Integer constant,
                int output,
                bool typed
            )
            {
                BeginBif(failure, live, mask);
                if (typed)
                {
                    owner.EmitTypedRegister(input, IntegerTypeBits);
                }
                else
                {
                    owner.Operand(XRegisterTag, input);
                }

                owner.LiteralOperand(constant);
                owner.Operand(XRegisterTag, output);
            }

            void TestZero(byte opcode, int failure, int input)
            {
                owner.Op(opcode, LabelTag, failure);
                owner.EmitTypedRegister(input, IntegerTypeBits);
                owner.Operand(BeamWire.IntegerTag, 0);
            }
        }

        private bool HasClosedMapEntry(FunctionDefinition function)
        {
            var signature = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature))
            {
                return false;
            }

            var sites = owner.atomCallSites ??= IndexAtomCallSites();

            return !sites.Indirect.Contains(signature) && sites.Direct.TryGetValue(signature, out var calls)
                && calls.All(site => !site.InNestedScope && ReferenceEquals(site.Clause.Body, site.Call)
                    && site.Call.Arguments[0] is Expr.Variable input && ProvesMap(site.Clause.Guard, input.Name));

            bool ProvesMap(Expr? guard, string value) => guard switch
            {
                Expr.Binary { Operator: ErlangOperators.AndAlso, Left: var left, Right: var right } =>
                    ProvesMap(left, value) || ProvesMap(right, value),
                Expr.Call { Function: AutoImportNames.IsMap, DynamicRemote: false, Arguments: [Expr.Variable tested] } call =>
                    tested.Name == value && owner.ResolvesErlangBif(call),
                _ => false
            };
        }

        private static (
            MapPattern Map,
            IReadOnlyList<MapPatternField> Fields,
            Pattern.Variable Carrier,
            Pattern.Variable[] Arguments,
            Expr.Call Tail
        )? ReadEntry(Clause clause)
        {
            if (clause.Patterns is not [AliasPattern aliased, Pattern.Variable second, Pattern.Variable third]
                || clause.Body is not Expr.Call
                {
                    Module: null, DynamicRemote: false,
                    Arguments: [Expr.Variable firstArg, Expr.Variable secondArg, Expr.Variable thirdArg]
                } tail)
            {
                return null;
            }

            MapPattern? map = aliased.Left as MapPattern ?? aliased.Right as MapPattern;
            Pattern.Variable? carrier = aliased.Left as Pattern.Variable ?? aliased.Right as Pattern.Variable;

            return map is { Fields.Count: 2 } && carrier is not null
                && firstArg.Name == carrier.Name && secondArg.Name == second.Name && thirdArg.Name == third.Name
                ? (map, map.Fields, carrier, [second, third], tail) : null;
        }

        private static (Expr Input, Integer Mask)? ReadZeroMask(Expr guard)
        {
            if (guard is not Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual,
                    Left: Expr.Binary { Operator: ErlangOperators.BitwiseAnd, Left: var input, Right: var operand },
                    Right: Expr.Literal { Value: Integer { Value.IsZero: true } }
                } || !AttributeLiteral.TryNormalizeConstant(operand, out Term value) || value is not Integer mask)
            {
                return null;
            }

            return (input, mask);
        }

        private static bool IsOrderedFold(Expr expression, Pattern.Variable[] elements, int index) =>
            index == 0
                ? expression is Expr.Variable first && first.Name == elements[0].Name
                : expression is Expr.Binary
                {
                    Operator: ErlangOperators.BitwiseOr, Left: var rest, Right: Expr.Variable field
                } && field.Name == elements[index].Name && IsOrderedFold(rest, elements, index - 1);
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int MapTypeBits = 1 << 6;
        private static readonly IComparer<Atom> MapKeyOrder =
            Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));

        private bool TryEmitCheckedMapCalls(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [AliasPattern { Left: MapPattern map, Right: Pattern.Variable state }],
                    Body: Expr.Sequence
                    {
                        Items: [_, .., Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned]
                        }]
                    } sequence
                }] || !DistinctBindings(state) || returned.Name != state.Name
                || ReadAtomMapFields(map) is not { Length: > 0 } fields
                || fields.Any(field => field.Variable.Name != VariableScopeNames.Wildcard)
                || ReadStateCalls(sequence) is not { } calls || calls.Any(call => call.Expected is null))
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            EmitMapTest(0, info);
            owner.Op(HasMapFields, LabelTag, info);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Length);
            foreach (var field in fields)
            {
                owner.LiteralOperand(field.Key);
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            int[] failures = calls.Select(_ => Label()).Reverse().ToArray();
            var previous = location;
            var checkLocations = new BeamSourceLocation[calls.Count];
            for (int index = 0; index < calls.Count; index++)
            {
                var (call, arguments, check, expected) = calls[index];
                owner.EmitIndependentCallMoves(arguments);
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                Call(call.Module, call.Function, arguments.Count);
                owner.EmitAtomTest(0, expected!.Name, failures[index]);
                checkLocations[index] = check!.SourceLine == 0 ? previous : new(check.PreprocessorFile, check.SourceLine);
            }

            EmitStateResult(
                tag,
                YRegisterTag,
                0,
                0
            );
            owner.EmitFrameReturn(1);
            for (int index = failures.Length - 1; index >= 0; index--)
            {
                Mark(failures[index]);
                if (checkLocations[index] != previous)
                {
                    owner.EmitLine(checkLocations[index]);
                    previous = checkLocations[index];
                }
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            return true;
        }

        private bool TryEmitMapGuardedSend(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Patterns: [Pattern.Variable selector, AliasPattern { Left: MapPattern map, Right: Pattern.Variable state }],
                    Guard: Expr.Binary { Operator: ErlangOperators.OrElse, Left: var first, Right: var second },
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Binary
                        {
                            Operator: ErlangOperators.Send, Left: Expr.Variable recipient, Right: Expr.Tuple
                            {
                                Items: [Expr.Variable payload, Expr.Tuple
                                {
                                    Items: [Expr.Literal { Value: Atom messageTag }, Expr.Variable selected]
                                }]
                            }
                        } send,
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned] }
                    ]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable ignored, Pattern.Variable fallbackState],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom fallbackTag }, Expr.Variable fallbackReturn] }
                }
            ] || !DistinctBindings(selector, state) || selected.Name != selector.Name || returned.Name != state.Name
                || !DistinctBindings(fallbackState) || ignored.Name == fallbackState.Name || fallbackReturn.Name != fallbackState.Name
                || !tag.Equals(fallbackTag) || ReadAtomMapFields(map) is not { Length: 2 } fields
                || !DistinctBindings(
                    selector,
                    state,
                    fields[0].Variable,
                    fields[1].Variable
                )
                || recipient.Name == payload.Name || !fields.Any(field => field.Variable.Name == recipient.Name)
                || !fields.Any(field => field.Variable.Name == payload.Name)
                || ReadSelectorAtom(first, selector) is not { } firstAtom
                || ReadSelectorAtom(second, selector) is not { } secondAtom || firstAtom.Equals(secondAtom))
            {
                return false;
            }

            int selectedLabel = Label();
            int fallback = Label();
            EmitMapTest(1, fallback);
            owner.Op(GetMapElements, LabelTag, fallback);
            owner.EmitTypedRegister(1, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, fields.Length * 2);
            foreach (var field in fields.Reverse())
            {
                owner.LiteralOperand(field.Key);
                owner.Operand(XRegisterTag, field.Variable.Name == recipient.Name ? 2 : 3);
            }

            Atom[] keys = [firstAtom, secondAtom];
            Array.Sort(keys, MapKeyOrder);
            owner.EmitAtomSelection(
                0,
                fallback,
                keys[0].Name,
                selectedLabel,
                keys[1].Name,
                selectedLabel
            );
            Mark(selectedLabel);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(messageTag.Name)), (XRegisterTag, 0));
            owner.EmitTagMatchTuple(1, (XRegisterTag, 3), (XRegisterTag, 0));
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            var previous = location;
            owner.EmitArithmeticLine(send.SourceLine, ref previous);
            owner.instructions.WriteByte(Send);
            EmitStateResult(
                tag,
                YRegisterTag,
                0,
                0
            );
            owner.EmitFrameReturn(1);

            Mark(fallback);
            EmitStateResult(
                tag,
                XRegisterTag,
                1,
                2
            );
            owner.instructions.WriteByte(Return);

            return true;
        }

        private void EmitMapTest(int register, int failure)
        {
            owner.Op(IsMap, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
        }

        private static (Atom Key, Pattern.Variable Variable)[]? ReadAtomMapFields(MapPattern map)
        {
            var fields = new Dictionary<Atom, Pattern.Variable>();
            foreach (MapPatternField field in map.Fields)
            {
                if (field is not { Key: Expr.Literal { Value: Atom key }, Value: Pattern.Variable variable }
                    || !fields.TryAdd(key, variable))
                {
                    return null;
                }
            }

            return fields.OrderBy(pair => pair.Key, MapKeyOrder).Select(pair => (pair.Key, pair.Value)).ToArray();
        }

        private static Atom? ReadSelectorAtom(Expr guard, Pattern.Variable selector) =>
            guard is Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual,
                Left: Expr.Variable tested, Right: Expr.Literal { Value: Atom atom }
            } && tested.Name == selector.Name
                ? atom
                : null;
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDecodedIntegerPrefix(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable input, Pattern.Variable options],
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable bytes,
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false,
                                        Arguments: [Expr.Variable source, Expr.Variable settings]
                                    } extract
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest },
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable binary]
                                    } decode
                                } match,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable key, Pattern.Variable value] },
                                    Value: Expr.If { Clauses: [var first, var second, var third] }
                                },
                                    Expr.Call
                                {
                                    Function: AutoImportNames.ListToTuple,
                                    DynamicRemote: false,
                                    Arguments:
                                            [
                                                Expr.List
                                            {
                                                Items: [Expr.Variable tag, Expr.Variable field], Tail: Expr.Variable tail
                                            }
                                            ]
                                } convert
                                ]
                        }
                    }
                ]
                || !DistinctBindings(
                    input,
                    options,
                    bytes,
                    head,
                    rest,
                    key,
                    value
                )
                || source.Name != input.Name || settings.Name != options.Name || binary.Name != bytes.Name
                || tag.Name != key.Name || field.Name != value.Name || tail.Name != rest.Name
                || !owner.ResolvesErlangBif(convert) || !entries.ContainsKey((extract.Function, 2))
                || !entries.ContainsKey((decode.Function, 1))
                || !ProducesIntegerList(decode)
                || !ReadPrefixLimit(first.Guard, head.Name, out int limit1)
                || !ReadPrefixLimit(second.Guard, head.Name, out int limit2) || limit2 <= limit1
                || third.Guard is not Expr.Literal { Value: Atom { Name: "true" } }
                || first.Body is not Expr.Tuple { Items: [Expr.Literal { Value: Integer tag1 }, Expr.Variable unchanged] }
                || unchanged.Name != head.Name
                || ReadPrefixShift(second.Body, head.Name, limit1) is not { } middle
                || ReadPrefixShift(third.Body, head.Name, limit2) is not { } last)
            {
                return false;
            }

            // A successful closed binary accumulator returns a proper list of
            // integers. Test for the empty list before splitting its first
            // value, then merge both tuple fields into one register pair.
            int next = Label();
            int final = Label();
            int joined = Label();
            int empty = Label();
            EmitFrameAllocation(arity: 2, size: 0);
            owner.EmitCallLine(extract, location);
            Call(null, extract.Function, 2);
            owner.EmitCallLine(decode, CallLocation(extract, location));
            Call(null, decode.Function, 1);
            owner.Op(IsNonemptyList, LabelTag, empty);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitLimit(next, limit1);
            EmitPrefix(tag1);
            owner.Op(Jump, LabelTag, joined);

            Mark(next);
            EmitLimit(final, limit2, lower: limit1);
            EmitSubtract(middle.Subtract, limit1, limit2 - 1);
            EmitPrefix(middle.Tag);
            owner.Op(Jump, LabelTag, joined);

            Mark(final);
            EmitSubtract(last.Subtract, limit2);
            EmitPrefix(last.Tag);

            Mark(joined);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 3);
            Prepend(2);
            Prepend(1);
            owner.EmitCallLine(convert, location);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, convert.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(empty);
            BeamSourceLocation previous = location;
            owner.EmitArithmeticLine(match.SourceLine, ref previous);
            owner.Op(BadMatch, AtomTag, NilAtomIndex);

            return true;

            void EmitLimit(int failure, int bound, int? lower = null)
            {
                owner.Op(IsGreaterOrEqual, LabelTag, failure);
                owner.LiteralOperand(Term.I(bound - 1));
                owner.EmitTypedRegister(1, IntegerTypeBits, lower);
            }

            void EmitPrefix(Integer keyValue)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    2
                );
                owner.EmitConstant(keyValue, 1);
            }

            void EmitSubtract(Expr.Binary operation, int lower, int? upper = null)
            {
                BeamSourceLocation before = location;
                owner.EmitArithmeticLine(operation.SourceLine, ref before);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Operator, 2));
                owner.EmitTypedRegister(
                    1,
                    IntegerTypeBits,
                    lower,
                    upper
                );
                owner.LiteralOperand(Term.I(lower));
                owner.Operand(XRegisterTag, 1);
            }

            void Prepend(int register)
            {
                owner.Op(PutList, XRegisterTag, register);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
        }

        private bool ProducesIntegerList(Expr.Call decode) =>
            !owner.IsDeclaredNative((decode.Function, 1))
            && localFunctions.TryGetValue((decode.Function, 1), out var wrapper)
            && ReadBinaryAccumulatorEntry(wrapper) is { } loopCall
            && !owner.IsDeclaredNative((loopCall.Function, 3))
            && localFunctions.TryGetValue((loopCall.Function, 3), out var loop)
            && ReadBinaryAccumulatorLoop(loop) is not null;

        private static bool ReadPrefixLimit(Expr? guard, string head, out int limit)
        {
            limit = 0;
            if (guard is not Expr.Binary
                {
                    Operator: ErlangOperators.Less, Left: Expr.Variable input, Right: Expr.Literal { Value: Integer bound }
                }
                || input.Name != head || bound.Value <= 0 || bound.Value > int.MaxValue)
            {
                return false;
            }

            limit = (int)bound.Value;

            return true;
        }

        private static (Integer Tag, Expr.Binary Subtract)? ReadPrefixShift(Expr body, string head, int lower) =>
            body is Expr.Tuple
            {
                Items:
                    [
                        Expr.Literal { Value: Integer tag },
                        Expr.Binary
                    {
                        Operator: ErlangOperators.Minus, Left: Expr.Variable input, Right: Expr.Literal { Value: Integer amount }
                    } subtract
                    ]
            } && input.Name == head && amount.Value == lower
                ? (tag, subtract)
                : null;
    }
}

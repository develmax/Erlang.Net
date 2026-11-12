using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLiteralRedirectAndIntegerTable(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Literal { Value: Integer redirect }] } call },
                _, _, ..
            ] || call.Function != function.Name || !entries.TryGetValue((call.Function, 1), out int target))
            {
                return false;
            }

            var table = function.Clauses.Skip(1).ToArray();
            if (!HasLiteralDispatchKeys(table) || table.Any(clause =>
                    clause.Patterns is not [Pattern.Literal { Value: Integer integer }]
                    || integer.Value < int.MinValue || integer.Value > int.MaxValue))
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            int integers = Label();
            owner.Op(IsAtom, LabelTag, integers);
            owner.Operand(XRegisterTag, 0);
            owner.Op(ExactEqualTest, LabelTag, info);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(key);
            owner.EmitConstant(redirect, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, target);
            Mark(integers);
            owner.EmitLiteralDispatch(
                table,
                location,
                info,
                reuseMatchingInput: true
            );

            return true;
        }

        private bool TryEmitLiteralRedirectAndAddition(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom key }],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Literal { Value: Integer redirected }] } call
                },
                {
                    Guard: null, Patterns: [Pattern.Variable value],
                    Body: Expr.Binary
                    {
                        Operator: ErlangOperators.Plus, Left: Expr.Variable input,
                        Right: Expr.Literal { Value: Integer increment }
                    } addition
                }
            ] || !DistinctBindings(value) || input.Name != value.Name
                || call.Function != function.Name || !entries.TryGetValue((call.Function, 1), out int target))
            {
                return false;
            }

            int fallback = Label();
            owner.EmitAtomTest(0, key.Name, fallback);
            owner.EmitConstant(redirected, 0);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, target);
            Mark(fallback);
            var previous = location;
            owner.EmitArithmeticLine(addition.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(increment);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitTaggedPairProjection(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple
                            {
                                Items: [
                        Pattern.Literal { Value: Atom tag }, Pattern.Variable first, Pattern.Variable second
                    ]
                            }, Value: Expr.Call { Module: not null, DynamicRemote: false } call
                        } match,
                    Expr.Tuple { Items: [Expr.Variable left, Expr.Variable right] }
                ]
                    }
                } clause] || !HasUnconditionalParameters(clause) || !DistinctBindings(first, second)
                || !IsFresh(clause, first) || !IsFresh(clause, second)
                || left.Name != first.Name || right.Name != second.Name
                || call.Arguments.Count != 0 && (call.Arguments is not [Expr.Variable passed]
                    || clause.Patterns is not [Pattern.Variable parameter] || parameter.Name != passed.Name))
            {
                return false;
            }

            int failure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, call.Arguments.Count);
            owner.EmitCallLine(call, location);
            owner.EmitStaticRemoteCall(call);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.LiteralOperand(tag);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTuplePart(0, 2, 0);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 1), (XRegisterTag, 0));
            owner.EmitFrameReturn();
            Mark(failure);
            var previous = CallLocation(call, location);
            var failedAt = match.SourceLine == 0 ? previous : new(match.PreprocessorFile, match.SourceLine);
            if (failedAt != previous)
            {
                owner.EmitLine(failedAt);
            }
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private sealed record BinaryFormat(
            Atom Key,
            Term? Format,
            Expr.Call Render,
            Expr.Call? Flatten,
            Integer? Prefix
        );

        private bool TryEmitTaggedBinaryFormatting(FunctionDefinition function)
        {
            if (function.Clauses is not [
                var first, var second,
                { Guard: null, Patterns: [Pattern.Variable fallbackValue], Body: Expr.Variable returned }
            ] || !DistinctBindings(fallbackValue) || returned.Name != fallbackValue.Name
                || ReadBinaryFormat(first) is not { } left || ReadBinaryFormat(second) is not { } right
                || left.Key.Equals(right.Key))
            {
                return false;
            }

            var comparer = Comparer<Atom>.Create((a, b) => TermOrder.Compare(a, b, exact: true));
            var branches = new[] { left, right }.OrderByDescending(branch => branch.Key, comparer)
                .Select(branch => (Branch: branch, Label: Label())).ToArray();
            int fallback = Label();
            owner.EmitTupleCheck(0, 2, fallback);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            owner.EmitAtomSelection(
                2,
                fallback,
                branches[1].Branch.Key.Name,
                branches[1].Label,
                branches[0].Branch.Key.Name,
                branches[0].Label
            );
            foreach (var (branch, label) in branches)
            {
                Mark(label);
                owner.Op(IsBinary, LabelTag, fallback);
                owner.Operand(XRegisterTag, 1);
                if (branch.Flatten is null)
                {
                    owner.Op(Allocate, BeamWire.UnsignedTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, 2);
                    owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
                    owner.EmitCallLine(branch.Render, location);
                    owner.EmitStaticRemoteCall(branch.Render with { Module = ErlangModuleNames.Module });
                    EmitBinaryListPrefix(branch.Prefix!);

                    continue;
                }
                owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
                owner.Operand(BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Op(PutList, XRegisterTag, 1);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 1);
                owner.EmitConstant(branch.Format!, 0);
                owner.EmitCallLine(branch.Render, location);
                owner.EmitStaticRemoteCall(branch.Render);
                owner.EmitCallLine(branch.Flatten, CallLocation(branch.Render, location));
                if (branch.Prefix is { } prefix)
                {
                    owner.EmitStaticRemoteCall(branch.Flatten);
                    EmitBinaryListPrefix(prefix);
                }
                else
                {
                    owner.EmitTailRemote(branch.Flatten.Module!, branch.Flatten.Function, framed: true);
                }
            }
            Mark(fallback);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private void EmitBinaryListPrefix(Integer prefix)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(PutList);
            owner.LiteralOperand(prefix);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn();
        }

        private BinaryFormat? ReadBinaryFormat(Clause clause)
        {
            if (clause is not
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Variable payload, Pattern.Literal { Value: Atom key }] }],
                    Guard: Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.IsBinary, Arguments: [Expr.Variable guarded]
                    }
                } || !DistinctBindings(payload) || guarded.Name != payload.Name
                || ((Expr.Call)clause.Guard).Module is null && entries.ContainsKey((AutoImportNames.IsBinary, 1)))
            {
                return null;
            }

            Expr body = clause.Body;
            Integer? prefix = null;
            if (body is Expr.List { Items: [Expr.Literal { Value: Integer head }], Tail: { } tail })
            {
                prefix = head;
                body = tail;
            }

            if (prefix is not null && body is Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: AutoImportNames.BinaryToList,
                    Arguments: [Expr.Variable converted]
                } conversion && converted.Name == payload.Name && owner.ResolvesErlangBif(conversion))
            {
                return new(key, null, conversion, null, prefix);
            }

            return body is Expr.Call
            {
                Module: not null, DynamicRemote: false, Arguments: [
                Expr.Call
                {
                    Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Literal format, Expr.List { Tail: null, Items: [Expr.Variable input] }
                ]
                } render
            ]
            } flatten && input.Name == payload.Name
                ? new(
                    key,
                    format.Value,
                    render,
                    flatten,
                    prefix
                )
                : null;
        }
    }
}

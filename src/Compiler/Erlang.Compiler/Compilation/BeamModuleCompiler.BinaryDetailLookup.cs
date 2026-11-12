using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTermDetailLookup(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Variable type, Pattern.Tuple
                        {
                            Items: [
                    Pattern.Variable table, Pattern.Variable key]
                        }]
                    }, Pattern.Variable ignored], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.List
                            {
                                Items: [Pattern.Tuple
                                {
                                    Items: [
                        Pattern.Variable expected, Pattern.Variable item]
                                }], Tail: null
                            },
                            Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Variable source, Expr.Variable index]
                            } lookup
                        },
                    Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom result }, Expr.Tuple
                            {
                                Items: [
                        Expr.Literal title, Expr.Tuple
                                {
                                    Items: [Expr.Variable kind, Expr.List
                                    {
                                        Tail: null, Items: [
                            Expr.Variable value, Expr.Variable saved]
                                    }]
                                }, var extra]
                            }]
                        }]
                    }
                }
            ] || !DistinctBindings(
                type,
                table,
                key,
                item
            )
                || new[] { type, table, key, item }.Any(variable => variable.Name == ignored.Name)
                || expected.Name != key.Name || index.Name != key.Name || source.Name != table.Name
                || kind.Name != type.Name || value.Name != item.Name || saved.Name != table.Name
                || !AttributeLiteral.TryNormalizeConstant(extra, out Term extraValue))
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            int badMatch = Label();
            owner.EmitTupleCheck(0, 2, info);
            owner.EmitTuplePart(0, 0, 2);
            owner.EmitTuplePart(0, 1, 3);
            owner.EmitTupleCheck(3, 2, info);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                2
            );
            GetTupleField(
                3,
                0,
                YRegisterTag,
                1
            );
            GetTupleField(
                3,
                1,
                YRegisterTag,
                0
            );
            Load(0, 1);
            Load(1);
            owner.EmitCallLine(lookup, location);
            owner.EmitStaticRemoteCall(lookup);
            owner.EmitNonemptyList(0, badMatch);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitTupleCheck(1, 2, badMatch);
            owner.EmitNilTest(XRegisterTag, 2, badMatch);
            owner.EmitTuplePart(1, 0, 2);
            owner.Op(ExactEqualTest, LabelTag, badMatch);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 14);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(1, 1, 0);
            owner.Op(PutList, YRegisterTag, 1);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 2), (XRegisterTag, 0));
            EmitBinaryDetailReply(
                title.Value,
                extraValue,
                result,
                3
            );
            Mark(badMatch);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private bool TryEmitBinaryDetailLookup(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Variable type, Pattern.Tuple
                        {
                            Items: [
                    Pattern.Variable table, Pattern.Variable key]
                        }]
                    }, Pattern.Variable ignored], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.List { Items: [Pattern.Tuple { Items: [Pattern.Variable expected, Pattern.Variable item] }], Tail: null },
                            Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable source, Expr.Variable index] } lookup
                        } firstMatch,
                    var firstReply]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable id] },
                    Pattern.Variable fallbackIgnored], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom found }, Pattern.Variable value] },
                            Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable argument] } expand
                        } secondMatch,
                    var secondReply]
                    }
                }
            ] || !DistinctBindings(
                type,
                table,
                key,
                item
            ) || !DistinctBindings(id, value)
                || new[] { type, table, key, item }.Any(variable => variable.Name == ignored.Name)
                || id.Name == fallbackIgnored.Name || value.Name == fallbackIgnored.Name
                || expected.Name != key.Name || source.Name != table.Name || index.Name != key.Name || argument.Name != id.Name
                || ReadBinaryDetailReply(firstReply, item) is not { Kind: Expr.Variable firstType } first || firstType.Name != type.Name
                || ReadBinaryDetailReply(secondReply, value) is not { Kind: Expr.Literal secondType } second)
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            int fallback = Label();
            int badLookup = Label();
            int badExpand = Label();
            owner.EmitTupleCheck(0, 2, info);
            owner.EmitTuplePart(0, 0, 2);
            owner.EmitTuplePart(0, 1, 3);
            owner.EmitTupleCheck(3, 2, fallback);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            owner.EmitTuplePart(3, 0, 0);
            GetTupleField(
                3,
                1,
                YRegisterTag,
                0
            );
            Load(0, 1);
            owner.EmitCallLine(lookup, location);
            owner.EmitStaticRemoteCall(lookup);
            owner.EmitNonemptyList(0, badLookup);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitTupleCheck(1, 2, badLookup);
            owner.EmitNilTest(XRegisterTag, 2, badLookup);
            owner.EmitTuplePart(1, 0, 2);
            owner.Op(ExactEqualTest, LabelTag, badLookup);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 10);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitBinaryDetailReply(
                first.Title,
                first.Extra,
                first.Result,
                2
            );

            Mark(fallback);
            owner.EmitAtomTest(2, tag.Name, info);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(expand, CallLocation(lookup, location));
            owner.EmitStaticRemoteCall(expand);
            owner.Op(IsTaggedTuple, LabelTag, badExpand);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(found);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 10);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(secondType.Value);
            owner.Operand(XRegisterTag, 0);
            EmitBinaryDetailReply(
                second.Title,
                second.Extra,
                second.Result,
                0
            );

            EmitBinaryDetailBadMatch(badLookup, firstMatch, lookup);
            EmitBinaryDetailBadMatch(badExpand, secondMatch, expand);

            return true;
        }

        private static (Expr Kind, Term Title, Term Extra, Atom Result)? ReadBinaryDetailReply(Expr reply, Pattern.Variable value)
        {
            if (reply is Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom result }, Expr.Tuple
                    {
                        Items: [
                Expr.Literal title, Expr.Tuple { Items: [var kind, Expr.Variable data] }, var extra]
                    }]
                }
                && data.Name == value.Name && AttributeLiteral.TryNormalizeConstant(extra, out Term constant))
            {
                return (kind, title.Value, constant, result);
            }

            return null;
        }

        private void EmitBinaryDetailReply(
            Term title,
            Term extra,
            Atom result,
            int frame
        )
        {
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(title);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(extra);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(result.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(frame);
        }

        private void EmitBinaryDetailBadMatch(int label, Expr.Match match, Expr.Call call)
        {
            Mark(label);
            owner.EmitLine(match.SourceLine == 0 ? CallLocation(call, location) : new(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);
        }
    }
}

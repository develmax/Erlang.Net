using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitMemoryInfoFields(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple
                            {
                                Items: [Pattern.Literal { Value: Atom tag },
                        Pattern.Variable data, Pattern.Variable warning]
                            },
                            Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } read
                        } match,
                    Expr.Tuple
                        {
                            Items: [
                        Expr.List
                            {
                                Tail: null, Items: [Expr.Tuple
                                {
                                    Items: [Expr.Literal caption,
                            Expr.Call { Module: null, Arguments: [Expr.Variable selected] } fields]
                                }]
                            },
                        Expr.Variable original, Expr.Variable returned]
                        }]
                    }
                }
            ] || !DistinctBindings(data, warning) || selected.Name != data.Name
                || original.Name != data.Name || returned.Name != warning.Name
                || !entries.TryGetValue((fields.Function, 1), out int target))
            {
                return false;
            }

            int failure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.LiteralOperand(tag);
            GetTupleField(
                0,
                1,
                YRegisterTag,
                1
            );
            GetTupleField(
                0,
                2,
                YRegisterTag,
                0
            );
            Load(1);
            var readLocation = CallLocation(read, location);
            owner.EmitCallLine(fields, readLocation);
            owner.EmitFieldCall(1, target);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(caption.Value);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTagMatchTuple(
                0,
                (XRegisterTag, 0),
                (YRegisterTag, 1),
                (YRegisterTag, 0)
            );
            owner.EmitFrameReturn(2);
            EmitBinaryDetailBadMatch(failure, match, read);

            return true;
        }

        private bool TryEmitRecursiveFieldTitles(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.List
                    {
                        Items: [Pattern.Tuple
                        {
                            Items: [
                    Pattern.Variable key, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                        }],
                        Tail: Pattern.Variable rest
                    }], Body: Expr.List
                    {
                        Items: [Expr.Tuple
                        {
                            Items: [
                        Expr.Call
                            {
                                Module: null, Arguments: [Expr.Call
                                {
                                    Module: null or ErlangModuleNames.Module,
                                    Function: AutoImportNames.AtomToList, DynamicRemote: false,
                                    Arguments: [Expr.Variable input]
                                } atomToList]
                            } title,
                        Expr.Tuple { Items: [Expr.Literal tag, Expr.Variable value] }]
                        }],
                        Tail: Expr.Call { Module: null, Arguments: [Expr.Variable tail] } recur
                    }
                },
                { Guard: null, Patterns: [var nil], Body: var empty }
            ] || !DistinctBindings(key, rest) || input.Name != key.Name || value.Name != key.Name || tail.Name != rest.Name
                || !IsEmptyListPattern(nil) || !AttributeLiteral.TryNormalizeConstant(empty, out Term emptyList) || emptyList is not Nil
                || !owner.ResolvesErlangBif(atomToList) || recur.Function != function.Name
                || !entries.TryGetValue((title.Function, 1), out int titleEntry)
                || !entries.TryGetValue((function.Name, 1), out int entry))
            {
                return false;
            }

            int info = owner.nextLabel - 2;
            int fallback = Label();
            owner.EmitNonemptyList(0, fallback);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitTupleCheck(1, 2, info);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            GetTupleField(
                1,
                0,
                YRegisterTag,
                0
            );
            Load(0);
            owner.EmitCallLine(atomToList, location);
            owner.EmitStaticRemoteCall(atomToList with { Module = ErlangModuleNames.Module });
            var conversion = CallLocation(atomToList, location);
            owner.EmitCallLine(title, conversion);
            owner.EmitFieldCall(1, titleEntry);
            owner.Op(Swap, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(recur, CallLocation(title, conversion));
            owner.EmitFieldCall(1, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 8);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2, 1);
            owner.LiteralOperand(tag.Value);
            owner.Operand(YRegisterTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 2, 1);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(2);
            Mark(fallback);
            owner.EmitNilTest(XRegisterTag, 0, info);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}

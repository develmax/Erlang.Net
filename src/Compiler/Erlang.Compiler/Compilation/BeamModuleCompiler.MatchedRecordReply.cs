using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLocalPageLookup(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable source],
                    Body: Expr.Call
                    {
                        Module: null, Arguments: [Expr.Variable receiver,
                        Expr.Call
                        {
                            Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Literal key, Expr.Variable input]
                        } lookup]
                    } finish
                }
            ] || !DistinctBindings(parent, source) || receiver.Name != parent.Name || input.Name != source.Name
                || !entries.TryGetValue((finish.Function, 2), out int entry))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitConstant(key.Value, 0);
            owner.EmitCallLine(lookup, location);
            owner.EmitStaticRemoteCall(lookup);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Load(0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }

        private bool TryEmitPageWithDefault(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable forwarded, Pattern.Literal { Value: Atom missing }],
                    Body: Expr.Call { Module: null, Arguments: [Expr.Variable argument, Expr.Literal fallback] } redirect
                },
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable source], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable colors, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable receiver]
                            } prepare
                        },
                    Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [Expr.Variable passed,
                        Expr.Call
                            {
                                Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Variable data, Expr.Variable theme]
                            } render]
                        } finish]
                    }
                }
            ] || !DistinctBindings(forwarded) || !DistinctBindings(parent, source, colors)
                || forwarded.Name != argument.Name || parent.Name != receiver.Name || parent.Name != passed.Name
                || source.Name != data.Name || colors.Name != theme.Name || missing.Equals(fallback.Value)
                || redirect.Function != function.Name || !entries.TryGetValue((redirect.Function, 2), out int entry))
            {
                return false;
            }

            int present = Label();
            owner.EmitAtomTest(1, missing.Name, present);
            owner.EmitConstant(fallback.Value, 1);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            Mark(present);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.EmitCallLine(prepare, location);
            owner.EmitStaticRemoteCall(prepare);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Load(0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            var prepareLocation = CallLocation(prepare, location);
            owner.EmitCallLine(render, prepareLocation);
            owner.EmitStaticRemoteCall(render);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Load(0);
            owner.EmitCallLine(finish, CallLocation(render, prepareLocation));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finish.Module, finish.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }

        private bool TryEmitMatchedRecordReply(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable id, Pattern.Variable ignored], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom ok }, Pattern.Variable record, Pattern.Variable extra] },
                            Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key] } read
                        } match,
                    Expr.Match
                        {
                            Pattern: Pattern.Variable props, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Literal keys, Expr.Variable input]
                            } convert
                        },
                    Expr.Match
                        {
                            Pattern: Pattern.Variable title, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Literal format, Expr.List
                                {
                                    Tail: null, Items: [
                            Expr.RecordAccess { Value: Expr.Variable accessed } field]
                                }]
                            } render
                        },
                    Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom result }, Expr.Tuple
                            {
                                Items: [
                        Expr.Variable rendered, Expr.Variable properties, Expr.Variable warning]
                            }]
                        }]
                    }
                }
            ] || !DistinctBindings(
                id,
                record,
                extra,
                props,
                title
            )
                || new[] { id, record, extra, props, title }.Any(variable => variable.Name == ignored.Name)
                || key.Name != id.Name || input.Name != record.Name || accessed.Name != record.Name
                || rendered.Name != title.Name || properties.Name != props.Name || warning.Name != extra.Name)
            {
                return false;
            }

            int badRecord = Label();
            int badMatch = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(YRegisterTag, 1);
            owner.Operand(YRegisterTag, 2);
            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            owner.Op(IsTaggedTuple, LabelTag, badMatch);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(AtomTag, owner.Intern(ok.Name));
            GetTupleField(
                0,
                1,
                YRegisterTag,
                2
            );
            GetTupleField(
                0,
                2,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                2,
                XRegisterTag,
                1
            );
            owner.EmitConstant(keys.Value, 0);
            var readLocation = CallLocation(read, location);
            owner.EmitCallLine(convert, readLocation);
            owner.EmitStaticRemoteCall(convert);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.Op(IsTaggedTuple, LabelTag, badRecord);
            owner.Operand(YRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, field.Arity);
            owner.Operand(AtomTag, owner.Intern(field.Name));
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(GetTupleElement, YRegisterTag, 2);
            owner.Operand(BeamWire.UnsignedTag, field.Index);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitConstant(format.Value, 0);
            owner.EmitCallLine(render, CallLocation(convert, readLocation));
            owner.EmitStaticRemoteCall(render);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(
                0,
                (XRegisterTag, 0),
                (YRegisterTag, 1),
                (YRegisterTag, 0)
            );
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(result.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(2);
            Mark(badRecord);
            owner.Op(BadRecord, YRegisterTag, 2);
            Mark(badMatch);
            owner.EmitLine(match.SourceLine == 0 ? readLocation : new(match.PreprocessorFile, match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}

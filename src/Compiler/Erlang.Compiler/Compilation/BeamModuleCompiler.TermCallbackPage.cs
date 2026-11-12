using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TermPage(
            Term Title,
            Atom View,
            Term Format,
            Expr.Call Create,
            int Entry
        );

        private bool TryEmitTermCallbackPage(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Tuple
                    {
                        Items: [Pattern.Variable kind,
                    Pattern.List { Tail: null, Items: [Pattern.Variable term, Pattern.Variable table] }]
                    }],
                    Body: Expr.Sequence
                    {
                        Items: [
                        var announce, var begin,
                        Expr.Match
                        {
                            Pattern: Pattern.Variable expanded, Value: Expr.Call
                            {
                                Module: null,
                                Arguments: [Expr.Variable firstTerm, Expr.Literal mode]
                            } expand
                        },
                        Expr.Match
                        {
                            Pattern: Pattern.Variable stored, Value: Expr.Call
                            {
                                Module: null,
                                Arguments: [Expr.Variable secondTerm, Expr.Variable storage]
                            } save
                        },
                        var end, Expr.Match
                        {
                            Pattern: Pattern.Variable colors, Value: Expr.Call
                            { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable input] } prepare
                        },
                        Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Variable receiver, Expr.List { Tail: null, Items.Count: 6 } pages]
                        } finish]
                    }
                }
            ] || !DistinctBindings(
                parent,
                kind,
                term,
                table,
                expanded,
                stored,
                colors
            )
                || parent.Name != input.Name || parent.Name != receiver.Name
                || term.Name != firstTerm.Name || term.Name != secondTerm.Name || storage.Name != table.Name
                || !entries.TryGetValue((expand.Function, 2), out int expandEntry)
                || !entries.TryGetValue((save.Function, 2), out int saveEntry)
                || ReadClosureProgress(announce) is not { } first || ReadClosureProgress(begin) is not { } second
                || ReadClosureProgress(end) is not { } last)
            {
                return false;
            }

            var formats = new List<TermPage>(pages.Items.Count);
            for (int index = 0; index < pages.Items.Count; index++)
            {
                if (pages.Items[index] is not Expr.Tuple
                    {
                        Items: [Expr.Literal title, Expr.Literal { Value: Atom view },
                    Expr.Tuple
                        {
                            Items: [Expr.Variable type, Expr.Call
                            {
                                Module: null, Arguments: [
                        Expr.Literal template, Expr.Variable data, Expr.Variable tab, Expr.Variable theme]
                            } create]
                        }]
                    }
                    || type.Name != kind.Name || tab.Name != table.Name || theme.Name != colors.Name
                    || data.Name != (index < 4 ? stored.Name : expanded.Name)
                    || !entries.TryGetValue((create.Function, 4), out int entry))
                {
                    return false;
                }
                formats.Add(new(
                    title.Value,
                    view,
                    template.Value,
                    create,
                    entry
                ));
            }

            int info = owner.nextLabel - 2;
            owner.EmitTupleCheck(1, 2, info);
            owner.EmitTuplePart(1, 0, 2);
            owner.EmitTuplePart(1, 1, 3);
            for (int register = 4; register <= 5; register++)
            {
                owner.EmitNonemptyList(3, info);
                owner.EmitListParts(
                    3,
                    XRegisterTag,
                    register,
                    XRegisterTag,
                    3
                );
            }
            owner.EmitNilTest(XRegisterTag, 3, info);
            owner.Op(Allocate, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 5);
            for (int slot = 0; slot < 5; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            owner.MoveRegister(
                XRegisterTag,
                5,
                YRegisterTag,
                5
            );
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                6
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                7
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                8
            );
            owner.EmitClosureProgress(first, location);
            owner.EmitClosureProgress(second, CallLocation(first.Call, location));
            owner.EmitConstant(mode.Value, 1);
            Load(6);
            owner.EmitCallLine(expand, CallLocation(second.Call, location));
            owner.EmitFieldCall(2, expandEntry);
            owner.Op(Swap, YRegisterTag, 6);
            owner.Operand(XRegisterTag, 0);
            Load(5, 1);
            owner.EmitCallLine(save, CallLocation(expand, location));
            owner.EmitFieldCall(2, saveEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                4
            );
            owner.EmitClosureProgress(last, CallLocation(save, location));
            Load(8);
            owner.EmitCallLine(prepare, CallLocation(last.Call, location));
            owner.EmitStaticRemoteCall(prepare);
            Load(4, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );
            Load(5, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            var previous = CallLocation(prepare, location);
            EmitTermPageCall(formats[0], ref previous);

            int[] resultSlots = [2, 1, 4, 0, 6];
            for (int index = 1; index < formats.Count; index++)
            {
                if (index < 4)
                {
                    Load(4, 1);
                    Load(5, 2);
                }
                else
                {
                    Load(5, 2);
                    Load(6, 1);
                }
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    resultSlots[index - 1]
                );
                Load(3, 3);
                if (index == formats.Count - 1)
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        0,
                        YRegisterTag,
                        5
                    );
                    owner.MoveRegister(
                        YRegisterTag,
                        1,
                        YRegisterTag,
                        3
                    );
                    owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
                    owner.Operand(BeamWire.UnsignedTag, 7);
                }
                EmitTermPageCall(formats[index], ref previous);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, formats.Count * (3 + 4 + ConsHeapWords));
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = 0; index < formats.Count - 1; index++)
            {
                owner.EmitTagMatchTuple(index + 1, (YRegisterTag, 5), (YRegisterTag, index));
            }
            owner.EmitTagMatchTuple(0, (YRegisterTag, 5), (XRegisterTag, 0));
            for (int index = formats.Count - 1; index >= 0; index--)
            {
                int register = index == formats.Count - 1 ? 0 : index + 1;
                owner.EmitTupleHeader(XRegisterTag, 3, register);
                owner.LiteralOperand(formats[index].Title);
                owner.LiteralOperand(formats[index].View);
                owner.Operand(XRegisterTag, register);
                owner.Op(PutList, XRegisterTag, register);
                if (index == formats.Count - 1)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }
                owner.Operand(XRegisterTag, index == 0 ? 1 : 0);
            }
            Load(6);
            owner.EmitCallLine(finish, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finish.Module, finish.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, 7);

            return true;
        }

        private void EmitTermPageCall(TermPage page, ref BeamSourceLocation previous)
        {
            owner.EmitConstant(page.Format, 0);
            owner.EmitCallLine(page.Create, previous);
            owner.EmitFieldCall(4, page.Entry);
            previous = CallLocation(page.Create, previous);
        }
    }
}

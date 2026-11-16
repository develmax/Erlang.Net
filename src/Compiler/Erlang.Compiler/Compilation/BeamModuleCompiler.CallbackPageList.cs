using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CallbackPage(
            Term Title,
            Atom View,
            Expr.Call Create,
            int Entry,
            Term? Format
        );

        private bool TryEmitCallbackPageList(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Tuple { Items: [Pattern.Variable kind, Pattern.Variable data] }],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.Variable colors, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable input]
                            } prepare
                        },
                        Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                            Expr.Variable receiver, Expr.List { Tail: null } list]
                        } finish]
                    }
                }
            ] || !DistinctBindings(
                parent,
                kind,
                data,
                colors
            ) || input.Name != parent.Name || receiver.Name != parent.Name
                || list.Items.Count < 3 || list.Items.Count > FirstReservedXRegister)
            {
                return false;
            }

            int count = list.Items.Count;
            int formatted = count - 2;
            var pages = new List<CallbackPage>();
            for (int index = 0; index < count; index++)
            {
                if (list.Items[index] is not Expr.Tuple
                    {
                        Items: [Expr.Literal title, Expr.Literal { Value: Atom view },
                    Expr.Tuple { Items: [Expr.Variable type, Expr.Call { Module: null } create] }]
                    }
                    || type.Name != kind.Name || !entries.TryGetValue((create.Function, create.Arguments.Count), out int target))
                {
                    return false;
                }
                Term? format = null;
                if (index < formatted)
                {
                    if (create.Arguments is not [Expr.Literal template, Expr.Variable body, Expr.Variable theme]
                        || body.Name != data.Name || theme.Name != colors.Name)
                    {
                        return false;
                    }
                    format = template.Value;
                }
                else if (create.Arguments is not [Expr.Variable body, Expr.Variable theme]
                    || body.Name != data.Name || theme.Name != colors.Name)
                {
                    return false;
                }
                pages.Add(new(
                    title.Value,
                    view,
                    create,
                    target,
                    format
                ));
            }

            int info = owner.nextLabel - 2;
            owner.EmitTupleCheck(1, 2, info);
            owner.Op(Allocate, BeamWire.UnsignedTag, count + 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, count - 1);
            for (int slot = 0; slot < count - 1; slot++)
            {
                owner.Operand(YRegisterTag, slot);
            }
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                count
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                count + 1
            );
            GetTupleField(
                1,
                0,
                YRegisterTag,
                count - 1
            );
            owner.EmitCallLine(prepare, location);
            owner.EmitStaticRemoteCall(prepare);
            owner.Op(GetTupleElement, YRegisterTag, count);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, count);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                formatted
            );

            var previous = CallLocation(prepare, location);
            for (int index = 0; index < formatted; index++)
            {
                CallbackPage page = pages[index];
                Load(count, 1);
                if (index == 0)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        XRegisterTag,
                        2
                    );
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        formatted - index
                    );
                    Load(formatted, 2);
                }
                owner.EmitConstant(page.Format!, 0);
                owner.EmitCallLine(page.Create, previous);
                owner.EmitFieldCall(3, page.Entry);
                previous = CallLocation(page.Create, previous);
            }

            Load(count, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Load(formatted, 1);
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            CallbackPage penultimate = pages[^2];
            owner.EmitCallLine(penultimate.Create, previous);
            owner.EmitFieldCall(2, penultimate.Entry);
            previous = CallLocation(penultimate.Create, previous);
            owner.Op(Swap, YRegisterTag, count);
            owner.Operand(XRegisterTag, 0);
            Load(formatted, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                formatted
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, count + 1);
            CallbackPage last = pages[^1];
            owner.EmitCallLine(last.Create, previous);
            owner.EmitFieldCall(2, last.Entry);

            owner.Op(TestHeap, BeamWire.UnsignedTag, count * (3 + 4 + ConsHeapWords));
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int index = 0; index < count - 1; index++)
            {
                int slot = index < formatted - 1 ? formatted - index - 2
                    : index == formatted - 1 ? formatted - 1 : count - 1;
                owner.EmitTupleHeader(XRegisterTag, 2, index + 1);
                owner.Operand(YRegisterTag, formatted);
                owner.Operand(YRegisterTag, slot);
            }
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(YRegisterTag, formatted);
            owner.Operand(XRegisterTag, 0);
            for (int index = count - 1; index >= 0; index--)
            {
                int register = index == count - 1 ? 0 : index + 1;
                owner.EmitTupleHeader(XRegisterTag, 3, register);
                owner.LiteralOperand(pages[index].Title);
                owner.LiteralOperand(pages[index].View);
                owner.Operand(XRegisterTag, register);
                owner.Op(PutList, XRegisterTag, register);
                if (index == count - 1)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }
                owner.Operand(XRegisterTag, index == 0 ? 1 : 0);
            }
            Load(count);
            owner.EmitCallLine(finish, CallLocation(last.Create, previous));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finish.Module, finish.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, count + 1);

            return true;
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitMemoryDetailPage(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable info, Pattern.Variable tag, Pattern.Variable heading],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.Variable data, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable key, Expr.Variable source]
                            } read
                        },
                        Expr.Match
                        {
                            Pattern: Pattern.Variable table, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Literal tableKey, Expr.Variable tableSource]
                            } readTable
                        },
                        Expr.Match
                        {
                            Pattern: Pattern.Variable html, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Variable title, Expr.Variable content, Expr.Variable expansion,
                                Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable styled] } colors]
                            } render
                        },
                        Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [Expr.Variable receiver,
                            Expr.Tuple { Items: [Expr.Literal kind, Expr.Variable markup, Expr.Variable shared] }]
                        } finish]
                    }
                }
            ] || !DistinctBindings(
                parent,
                info,
                tag,
                heading,
                data,
                table,
                html
            )
                || tag.Name != key.Name || info.Name != source.Name || info.Name != tableSource.Name
                || heading.Name != title.Name || data.Name != content.Name || table.Name != expansion.Name
                || parent.Name != styled.Name || parent.Name != receiver.Name || html.Name != markup.Name || table.Name != shared.Name)
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 4);
            InitPageSlot(0);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                3
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            Load(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.EmitConstant(tableKey.Value, 0);
            var readLocation = CallLocation(read, location);
            owner.EmitCallLine(readTable, readLocation);
            owner.EmitStaticRemoteCall(readTable);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Load(3);
            var tableLocation = CallLocation(readTable, readLocation);
            owner.EmitCallLine(colors, tableLocation);
            owner.EmitStaticRemoteCall(colors);
            Load(0, 2);
            Load(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            Load(1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                2
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 2);
            var colorLocation = CallLocation(colors, tableLocation);
            owner.EmitCallLine(render, colorLocation);
            owner.EmitStaticRemoteCall(render);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3, 1);
            owner.LiteralOperand(kind.Value);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            Load(1);
            owner.EmitCallLine(finish, CallLocation(render, colorLocation));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finish.Module, finish.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, 2);

            return true;
        }

        private bool TryEmitIndexedDetailPage(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable info], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable index, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Literal key, Expr.Variable source]
                            } read
                        },
                    Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Variable receiver, Expr.Literal page, Expr.Variable selected]
                        } finish]
                    }
                }
            ] || !DistinctBindings(parent, info, index)
                || info.Name != source.Name || parent.Name != receiver.Name || index.Name != selected.Name)
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
            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            owner.EmitConstant(page.Value, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            Load(0);
            owner.EmitCallLine(finish, CallLocation(read, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finish.Module, finish.Function, 3));
            owner.Operand(BeamWire.UnsignedTag, 1);

            return true;
        }
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleResultAppend(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable input],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Variable head, Pattern.Variable rest] },
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                            Expr.Variable argument, Expr.List { Items: [], Tail: null },
                            Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } defaults
                        ] } collect },
                    Expr.Tuple { Items: [
                        Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Variable prefix,
                            Right: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } suffix },
                        Expr.Variable tail
                    ] }
                ] }
            }] || !DistinctBindings(input, head, rest) || input.Name != argument.Name
                || head.Name != prefix.Name || rest.Name != tail.Name
                || !entries.TryGetValue((defaults.Function, 0), out int defaultEntry)
                || !entries.TryGetValue((collect.Function, 3), out int collectEntry)
                || !entries.TryGetValue((suffix.Function, 0), out int suffixEntry)
                || !ReturnsTuple(collect, 2, [], exact: true))
            {
                return false;
            }

            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.InitStackSlot(0);
            SaveWindowResult(1);
            var previous = location;
            EmitWindowLocal(defaults, defaultEntry, ref previous);
            owner.EmitConstant(Nil.Value, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            Load(1);
            owner.InitStackSlot(1);
            EmitWindowLocal(collect, collectEntry, ref previous);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 1);
            SaveWindowResult(0);
            EmitWindowLocal(suffix, suffixEntry, ref previous);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            TrimDetailFrame(1, 1);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}

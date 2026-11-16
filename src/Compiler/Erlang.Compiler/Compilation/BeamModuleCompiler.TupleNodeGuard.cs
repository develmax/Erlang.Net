using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitCallResultPair(Clause clause, BeamSourceLocation location)
    {
        if (clause is not
            {
                Guard: null, Patterns: [Pattern.Variable parameter],
                Body: Expr.Tuple
                {
                    Items: [
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } call,
                    Expr.Variable carried
                ]
                }
            } || !DistinctBindings(parameter) || parameter.Name != carried.Name)
        {
            return false;
        }

        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        EmitCallLine(call, location);
        Op(ExternalCall, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, Import(call.Module, call.Function, 0));
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
        EmitFrameReturn(1);

        return true;
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleNodeGuard(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Patterns: [
                        Pattern.Tuple
                    {
                        Items: [
                            Pattern.Variable key, Pattern.Variable endpoint, Pattern.Variable ignored
                        ]
                    },
                        Pattern.Variable repeated
                    ],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.NumericNotEqual or ErlangOperators.ExactNotEqual,
                        Left: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Node,
                            DynamicRemote: false, Arguments: [Expr.Variable tested]
                        } remoteNode,
                        Right: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Node,
                            DynamicRemote: false, Arguments: []
                        } localNode
                    },
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable returned] }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable state],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match
                        {
                            Pattern: Pattern.Variable { Name: VariableScopeNames.Wildcard }, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false, Arguments: [
                                var prefix, Expr.Variable argument,
                                Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable nested] } prepare
                            ]
                            } write
                        },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom fallbackTag }, Expr.Variable fallbackReturn] }
                    ]
                    }
                }
            ] || !DistinctBindings(key, endpoint) || ignored.Name == key.Name || ignored.Name == endpoint.Name
                || repeated.Name != key.Name || returned.Name != key.Name || tested.Name != endpoint.Name
                || !DistinctBindings(input, state) || argument.Name != state.Name || nested.Name != input.Name
                || fallbackReturn.Name != state.Name || !fallbackTag.Equals(tag)
                || remoteNode.Module is null && entries.ContainsKey((AutoImportNames.Node, 1))
                || localNode.Module is null && entries.ContainsKey((AutoImportNames.Node, 0))
                || !entries.TryGetValue((prepare.Function, 1), out int helper)
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term first))
            {
                return false;
            }

            int fallback = Label();
            owner.EmitTupleCheck(0, 3, fallback);
            owner.EmitTuplePart(0, 0, 2);
            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.EmitTuplePart(0, 1, 3);
            owner.Op(BifOne, LabelTag, fallback);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Node, 1));
            owner.Operand(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 3);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Node, 0));
            owner.Operand(XRegisterTag, 4);
            owner.Op(ExactNotEqualTest, LabelTag, fallback);
            owner.EmitTypedRegister(3, AtomTypeBits);
            owner.EmitTypedRegister(4, AtomTypeBits);
            EmitStateResult(
                tag,
                XRegisterTag,
                2,
                3
            );
            owner.instructions.WriteByte(Return);

            Mark(fallback);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(prepare, location);
            owner.EmitFieldCall(1, helper);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.EmitConstant(first, 0);
            owner.EmitCallLine(write, CallLocation(prepare, location));
            Call(write.Module, write.Function, 3);
            EmitStateResult(
                tag,
                YRegisterTag,
                0,
                0
            );
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}

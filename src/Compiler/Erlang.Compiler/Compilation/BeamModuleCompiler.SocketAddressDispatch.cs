using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketAddressDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [.., {
                Guard: null, Patterns: [Pattern.Variable module, Pattern.Variable socket],
                Body: Expr.Call { DynamicRemote: true, Arguments: [
                    Expr.Variable target, Expr.Literal { Value: Atom member },
                    Expr.List { Tail: null, Items: [Expr.Variable argument] }
                ] } dynamicCall
            }] || !DistinctBindings(module, socket) || target.Name != module.Name || argument.Name != socket.Name)
            {
                return false;
            }

            if (TryEmitPeerAddress(function.Clauses, dynamicCall, member))
            {
                return true;
            }

            if (function.Clauses is [var portClause, _]
                && ReadSocketAddressCall(portClause, 1) is { } port && portClause.Guard is null)
            {
                int fallback = Label();
                EmitConfigurationAtomTest(port.Transport, fallback);
                owner.EmitTupleCheck(1, 2, fallback);
                owner.EmitTuplePart(1, 1, 0);
                EmitSocketUnaryExternalTail(port.Call);
                Mark(fallback);
                EmitSocketUnaryDynamicTail(dynamicCall, member);

                return true;
            }

            if (function.Clauses is not [var nestedClause, var pairClause, var plainClause, _]
                || ReadSocketAddressCall(nestedClause, 2) is not { } nested
                || ReadSocketAddressCall(pairClause, 1) is not { } pair
                || ReadSocketAddressCall(plainClause, 0) is not { } plain
                || pairClause.Guard is not null || plainClause.Guard is not null
                || !nested.Transport.Equals(pair.Transport) || !nested.Transport.Equals(plain.Transport))
            {
                return false;
            }

            int pairTail = Label();
            int plainTail = Label();
            int dynamicTail = Label();
            EmitConfigurationAtomTest(nested.Transport, dynamicTail);
            owner.EmitTupleCheck(1, 2, plainTail);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitTupleCheck(0, 2, pairTail);
            owner.EmitTuplePart(1, 0, 1);
            owner.Op(IsPid, LabelTag, pairTail);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTuplePart(0, 1, 0);
            EmitSocketUnaryExternalTail(nested.Call);
            Mark(pairTail);
            EmitSocketUnaryExternalTail(pair.Call);
            Mark(plainTail);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            EmitSocketUnaryExternalTail(plain.Call);
            Mark(dynamicTail);
            EmitSocketUnaryDynamicTail(dynamicCall, member);

            return true;
        }

        private bool TryEmitPeerAddress(IReadOnlyList<Clause> clauses, Expr.Call dynamicCall, Atom member)
        {
            if (clauses is not [
                { Guard: null, Patterns: [
                    Pattern.Variable { }, Pattern.Literal { Value: Atom missing }
                ], Body: { } absent },
                { Guard: null, Patterns: [
                    Pattern.Literal { Value: Atom udp },
                    Pattern.Tuple { Items: [
                        Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Tuple { Items: [Pattern.Variable nested, Pattern.Variable ignoredNested] }
                    ] }
                ], Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom nestedTag }, Expr.Variable nestedValue] } },
                { Guard: null, Patterns: [
                    Pattern.Literal { Value: Atom otherUdp },
                    Pattern.Tuple { Items: [Pattern.Variable plain, Pattern.Variable ignoredPlain] }
                ], Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom plainTag }, Expr.Variable plainValue] } },
                _
            ] || !AttributeLiteral.TryNormalizeConstant(absent, out Term missingValue)
                || !udp.Equals(otherUdp) || nested.Name == VariableScopeNames.Wildcard || plain.Name == VariableScopeNames.Wildcard
                || nested.Name == ignoredNested.Name || plain.Name == ignoredPlain.Name
                || nested.Name != nestedValue.Name || plain.Name != plainValue.Name)
            {
                return false;
            }

            int present = Label();
            int pair = Label();
            int dynamic = Label();
            owner.Op(ExactEqualTest, LabelTag, present);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(missing);
            owner.EmitConstant(missingValue, 0);
            owner.instructions.WriteByte(Return);
            Mark(present);
            EmitConfigurationAtomTest(udp, dynamic);
            owner.EmitTupleCheck(1, 2, dynamic);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitTupleCheck(0, 2, pair);
            EmitAddressTuple(0, nestedTag);
            Mark(pair);
            EmitAddressTuple(1, plainTag);
            Mark(dynamic);
            EmitSocketUnaryDynamicTail(dynamicCall, member);

            return true;
        }

        private void EmitAddressTuple(int source, Atom tag)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, source + 1);
            owner.EmitTuplePart(source, 0, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
            owner.instructions.WriteByte(Return);
        }

        private (Atom Transport, Expr.Call Call)? ReadSocketAddressCall(Clause clause, int depth)
        {
            if (clause is not {
                Patterns: [Pattern.Literal { Value: Atom transport }, var input],
                Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable forwarded] } call
            } || owner.ResolvesErlangBif(call))
            {
                return null;
            }

            if (depth == 2)
            {
                if (input is not Pattern.Tuple { Items: [Pattern.Variable listener, var nested] }
                    || clause.Guard is not Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.IsPid, Arguments: [Expr.Variable checkedPid] } test
                    || !owner.ResolvesErlangBif(test) || listener.Name != checkedPid.Name || listener.Name == forwarded.Name
                    || listener.Name == VariableScopeNames.Wildcard)
                {
                    return null;
                }

                input = nested;
            }

            if (depth != 0)
            {
                if (input is not Pattern.Tuple { Items: [Pattern.Variable ignored, var value] }
                    || ignored.Name == forwarded.Name
                    || (depth == 2 && clause.Patterns[1] is Pattern.Tuple { Items: [Pattern.Variable listener, _] }
                        && ignored.Name == listener.Name))
                {
                    return null;
                }

                input = value;
            }

            return input is Pattern.Variable socket && socket.Name == forwarded.Name && socket.Name != VariableScopeNames.Wildcard
                ? (transport, call) : null;
        }

        private void EmitSocketUnaryExternalTail(Expr.Call call)
        {
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, 1));
        }

        private void EmitSocketUnaryDynamicTail(Expr.Call call, Atom member)
        {
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitConstant(member, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(call, location);
            owner.Op(TailDynamicApply, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
        }
    }
}

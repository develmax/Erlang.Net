using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketOptionRead(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [
                        Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Tuple { Items: [Pattern.Literal { Value: Atom socketTag }, ..] } record,
                        Pattern.Variable options
                    ], Body: Expr.Sequence { Items: [
                        Expr.Match { Pattern: Pattern.Variable split, Value: Expr.Call {
                            Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Literal { Value: Atom transport }, Expr.Variable toSplit]
                        } splitCall },
                        Expr.Call { Module: not null, DynamicRemote: false,
                            Arguments: [Expr.Variable listener, Expr.Variable splitArg] } read
                    ] }
                },
                {
                    Guard: null, Patterns: [
                        Pattern.Literal { Value: Atom nestedTransport },
                        Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Tuple { Items: [
                            Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard }] },
                            Pattern.Variable nestedSocket
                        ] }] }, Pattern.Variable nestedOptions
                    ], Body: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Variable nestedArg, Expr.Variable nestedValues] } nestedCall
                },
                {
                    Guard: null, Patterns: [
                        Pattern.Literal { Value: Atom plainTransport },
                        Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable plainSocket] },
                        Pattern.Variable plainOptions
                    ], Body: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Variable plainArg, Expr.Variable plainValues] } plainCall
                },
                {
                    Guard: null, Patterns: [Pattern.Variable module, Pattern.Variable socket, Pattern.Variable dynamicOptions],
                    Body: Expr.Call { DynamicRemote: true, Arguments: [
                        Expr.Variable target, Expr.Literal { Value: Atom member },
                        Expr.List { Tail: null, Items: [Expr.Variable socketArg, Expr.Variable optionArg] }
                    ] } dynamicCall
                }
            ] || !transport.Equals(nestedTransport) || !transport.Equals(plainTransport)
                || !DistinctBindings(options, split, new(listener.Name)) || !DistinctBindings(nestedSocket, nestedOptions)
                || !DistinctBindings(plainSocket, plainOptions) || !DistinctBindings(module, socket, dynamicOptions)
                || options.Name != toSplit.Name || split.Name != splitArg.Name
                || nestedSocket.Name != nestedArg.Name || nestedOptions.Name != nestedValues.Name
                || plainSocket.Name != plainArg.Name || plainOptions.Name != plainValues.Name
                || module.Name != target.Name || socket.Name != socketArg.Name || dynamicOptions.Name != optionArg.Name
                || owner.ResolvesErlangBif(splitCall) || owner.ResolvesErlangBif(read)
                || owner.ResolvesErlangBif(nestedCall) || owner.ResolvesErlangBif(plainCall)
                || ReadSocketListenerFields(record, listener.Name) is not { } fields)
            {
                return false;
            }

            int udp = Label();
            int plain = Label();
            int dynamic = Label();
            EmitSocketListenerTests(socketTag, record.Items.Count, fields, udp);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(XRegisterTag, 3, YRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
            owner.EmitConstant(transport, 0);
            var previous = location;
            EmitWindowCall(splitCall, ref previous);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            EmitConfigurationTail(read, 1, previous);

            Mark(udp);
            EmitConfigurationAtomTest(transport, dynamic);
            owner.EmitTupleCheck(1, 2, dynamic);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitTupleCheck(0, 2, plain);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTupleCheck(1, 2, plain);
            owner.EmitTuplePart(0, 1, 0);
            EmitSocketExternalTail(nestedCall);
            Mark(plain);
            EmitSocketExternalTail(plainCall);
            Mark(dynamic);
            EmitDynamicSocketTail(dynamicCall, member);

            return true;

        }

        private void EmitSocketExternalTail(Expr.Call call)
        {
            owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
            owner.EmitCallLine(call, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, 2));
        }

        private void EmitDynamicSocketTail(Expr.Call call, Atom member)
        {
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitConstant(member, 3);
            owner.Op(Swap, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 2);
            owner.EmitCallLine(call, location);
            owner.Op(TailDynamicApply, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
        }

        private void EmitSocketListenerTests(
            Atom socketTag,
            int arity,
            (int Handle, int Config, Atom Tag, int Arity) fields,
            int failure
        )
        {
            EmitDetailRecordTest(1, arity, socketTag, failure);
            owner.EmitTuplePart(1, fields.Handle, 3);
            owner.EmitTupleCheck(3, 2, failure);
            owner.EmitTuplePart(1, fields.Config, 4);
            EmitDetailRecordTest(4, fields.Arity, fields.Tag, failure);
        }

        private static (int Handle, int Config, Atom Tag, int Arity)? ReadSocketListenerFields(Pattern.Tuple record, string listenerName)
        {
            int handle = -1;
            int configuration = -1;
            Atom? configTag = null;
            int configArity = 0;
            for (int field = 1; field < record.Items.Count; field++)
            {
                switch (record.Items[field])
                {
                    case Pattern.Tuple { Items: [Pattern.Variable listener, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }
                        when handle < 0 && listener.Name == listenerName:
                        handle = field;
                        break;
                    case Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } config when configuration < 0
                        && config.Items.Skip(1).All(item => item is Pattern.Variable { Name: VariableScopeNames.Wildcard }):
                        configuration = field;
                        configTag = tag;
                        configArity = config.Items.Count;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    default:
                        return null;
                }
            }

            return handle >= 0 && configTag is not null ? (handle, configuration, configTag, configArity) : null;
        }
    }
}

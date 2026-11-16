using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketOptionDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, AliasPattern alias, Pattern.Variable options],
                    Body: Expr.Sequence { Items: [
                        var splitExpression,
                        Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable emulatedArg, Expr.Variable socketArg] } check,
                        Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable listenerArg, Expr.Variable splitArg] } update
                    ] }
                },
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom transport }, Pattern.Variable udpSocket, Pattern.Variable udpOptions],
                    Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable udpArg, Expr.Variable udpValues] } udp
                },
                {
                    Guard: null, Patterns: [Pattern.Variable module, Pattern.Variable dynamicSocket, Pattern.Variable dynamicOptions],
                    Body: Expr.Call { DynamicRemote: true, Arguments: [
                        Expr.Variable target, Expr.Literal { Value: Atom member },
                        Expr.List { Tail: null, Items: [Expr.Variable socketInput, Expr.Variable optionInput] }
                    ] } dynamicCall
                }
            ] || !TryReadPatternAlias(alias, out var socket, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom socketTag }, ..] } record
                || ReadOptionSplit(splitExpression) is not { } split
                || split.Call is not { Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Literal { Value: Atom splitTransport }, Expr.Variable splitOptions
                ] } splitCall || !transport.Equals(splitTransport)
                || !DistinctBindings(udpSocket, udpOptions) || !DistinctBindings(module, dynamicSocket, dynamicOptions)
                || options.Name != splitOptions.Name || emulatedArg.Name != split.Emulated.Name || socketArg.Name != socket.Name
                || splitArg.Name != split.Result.Name || udpArg.Name != udpSocket.Name || udpValues.Name != udpOptions.Name
                || target.Name != module.Name || socketInput.Name != dynamicSocket.Name || optionInput.Name != dynamicOptions.Name
                || !entries.TryGetValue((check.Function, 2), out int checkEntry)
                || owner.ResolvesErlangBif(splitCall) || owner.ResolvesErlangBif(update) || owner.ResolvesErlangBif(udp)
                || ReadSocketListenerFields(record, listenerArg.Name) is not { } fields
                || !DistinctBindings(socket, options, split.Result, split.Emulated, new(listenerArg.Name)))
            {
                return false;
            }

            int plain = Label();
            int fallback = Label();
            int badMatch = Label();
            EmitSocketListenerTests(socketTag, record.Items.Count, fields, plain);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.InitStackSlot(0);
            owner.MoveRegister(XRegisterTag, 3, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
            owner.EmitConstant(transport, 0);
            var previous = location;
            EmitWindowCall(splitCall, ref previous);
            var matchLocation = previous;
            SaveWindowResult(0);
            owner.EmitTupleCheck(0, 2, badMatch);
            owner.EmitTuplePart(0, 1, 0);
            Load(2, 1);
            owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 2);
            TrimDetailFrame(1, 2);
            EmitWindowLocal(check, checkEntry, ref previous);
            owner.Op(GetTupleElement, YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 0);
            Load(1, 1);
            EmitConfigurationTail(update, 2, previous);
            Mark(plain);
            EmitConfigurationAtomTest(transport, fallback);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.MoveRegister(XRegisterTag, 2, XRegisterTag, 1);
            owner.EmitCallLine(udp, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(udp.Module, udp.Function, 2));
            Mark(fallback);
            EmitDynamicSocketTail(dynamicCall, member);
            Mark(badMatch);
            owner.EmitLine(new(split.Match.PreprocessorFile ?? matchLocation.File, split.Match.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private static (Pattern.Variable Result, Pattern.Variable Emulated, Expr.Call Call, Expr.Match Match)? ReadOptionSplit(Expr expression)
        {
            if (expression is Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Match {
                Pattern: Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable emulated] },
                Value: Expr.Call call
            } } match)
            {
                return (result, emulated, call, match);
            }

            if (expression is Expr.Match { Pattern: AliasPattern alias, Value: Expr.Call aliased } aliasMatch
                && TryReadPatternAlias(alias, out var bound, out var shape)
                && shape is Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable payload] })
            {
                return (bound, payload, aliased, aliasMatch);
            }

            return null;
        }
    }
}

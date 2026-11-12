using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNewTransportListener(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable ip, Pattern.Variable requested, Pattern.Variable process, AliasPattern alias],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable options, Value: Expr.Binary {
                        Operator: ErlangOperators.Append, Left: Expr.Variable userOptions,
                        Right: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } defaults
                    } },
                    Expr.Case { Value: Expr.Call { DynamicRemote: true, Arguments: [
                        Expr.Variable transport, Expr.Literal { Value: Atom member },
                        Expr.List { Tail: null, Items: [Expr.Variable openPort, Expr.Variable openOptions] }
                    ] } open, Clauses: [
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom ok }, Pattern.Variable socket
                        ] }], Body: Expr.Sequence { Items: [
                            Expr.Match { Pattern: Pattern.Variable resolved, Value: Expr.Case { Value: Expr.Variable portValue, Clauses: [
                                { Guard: null, Patterns: [Pattern.Literal { Value: Integer ephemeral }], Body: Expr.Sequence { Items: [
                                    Expr.Match { Pattern: Pattern.Tuple { Items: [
                                        Pattern.Literal { Value: Atom portTag }, Pattern.Variable actual
                                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable socketArg] } lookup },
                                    Expr.Variable actualArg
                                ] } },
                                { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Variable fixedPort }
                            ] } },
                            Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                                Expr.Variable configArg, Expr.Variable ipArg, Expr.Variable resolvedArg,
                                Expr.Variable socketInput, Expr.Variable ownerArg
                            ] } start
                        ] } },
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom errorTag }, Pattern.Literal { Value: Atom occupied }
                        ] }], Body: var occupiedResult },
                        { Guard: null, Patterns: [Pattern.Variable error], Body: Expr.Variable returned }
                    ] }
                ] }
            }] || !TryReadPatternAlias(alias, out var config, out var shape)
                || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom configTag }, ..] } record
                || !DistinctBindings(ip, requested, process, config, options, socket, resolved, actual, error,
                    new(userOptions.Name), new(transport.Name))
                || openPort.Name != requested.Name || openOptions.Name != options.Name || portValue.Name != requested.Name
                || fixedPort.Name != requested.Name || socketArg.Name != socket.Name || actualArg.Name != actual.Name
                || configArg.Name != config.Name || ipArg.Name != ip.Name || resolvedArg.Name != resolved.Name
                || socketInput.Name != socket.Name || ownerArg.Name != process.Name || error.Name != returned.Name
                || !AttributeLiteral.TryNormalizeConstant(occupiedResult, out Term conflict) || ok.Equals(errorTag)
                || owner.ResolvesErlangBif(lookup) || !entries.TryGetValue((defaults.Function, 0), out int defaultsEntry)
                || !entries.TryGetValue((start.Function, 5), out int startEntry)
                || ReadListenerTransportFields(record, transport.Name, userOptions.Name) is not { } fields)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int opened = Label();
            int knownPort = Label();
            int startListener = Label();
            int openError = Label();
            int fallback = Label();
            int badMatch = Label();
            if (!AlwaysReceivesTaggedArgument(function, 3, configTag, record.Items.Count))
            {
                EmitDetailRecordTest(3, record.Items.Count, configTag, functionInfo);
            }

            owner.EmitTuplePart(3, fields.Transport, 4);
            owner.EmitTupleCheck(4, 5, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 5);
            for (int slot = 0; slot < 5; slot++)
            {
                owner.MoveRegister(XRegisterTag, 4 - slot, YRegisterTag, slot);
            }

            var previous = location;
            EmitWindowLocal(defaults, defaultsEntry, ref previous);
            LoadTupleField(1, fields.Options, XRegisterTag, 1);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            LoadTupleField(0, 0, XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(member, 3);
            owner.InitStackSlot(0);
            Load(3);
            owner.EmitCallLine(open, previous);
            previous = CallLocation(open, previous);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 2);
            owner.EmitTupleCheck(0, 2, fallback);
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            if (TermOrder.Compare(errorTag, ok, exact: true) < 0)
            {
                owner.EmitAtomSelection(1, fallback, errorTag.Name, openError, ok.Name, opened);
            }
            else
            {
                owner.EmitAtomSelection(1, fallback, ok.Name, opened, errorTag.Name, openError);
            }

            Mark(opened);
            owner.Op(ExactEqualTest, LabelTag, knownPort);
            owner.Operand(YRegisterTag, 3);
            owner.LiteralOperand(ephemeral);
            owner.InitStackSlot(3);
            Load(0);
            EmitWindowCall(lookup, ref previous);
            EmitDetailRecordTest(0, 2, portTag, badMatch);
            owner.EmitTuplePart(0, 1, 2);
            JumpTo(startListener);
            Mark(knownPort);
            Load(3, 2);
            Mark(startListener);
            Load(0, 3);
            Load(2, 4);
            Load(4, 1);
            Load(1);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 5);
            owner.Operand(LabelTag, startEntry);
            owner.Operand(BeamWire.UnsignedTag, 5);
            Mark(openError);
            owner.Op(ExactEqualTest, LabelTag, fallback);
            owner.Operand(YRegisterTag, 0);
            owner.LiteralOperand(occupied);
            owner.EmitConstant(conflict, 0);
            owner.EmitFrameReturn(5);
            Mark(fallback);
            owner.EmitFrameReturn(5);
            Mark(badMatch);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }

        private void LoadTupleField(int slot, int field, int destinationTag, int destination)
        {
            owner.Op(GetTupleElement, YRegisterTag, slot);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(destinationTag, destination);
        }

        private static (int Transport, int Options)? ReadListenerTransportFields(Pattern.Tuple record, string transport, string options)
        {
            int transportField = -1;
            int optionsField = -1;
            for (int field = 1; field < record.Items.Count; field++)
            {
                switch (record.Items[field])
                {
                    case Pattern.Tuple { Items: [Pattern.Variable module, ..] } tuple when transportField < 0
                        && module.Name == transport && tuple.Items.Count == 5
                        && tuple.Items.Skip(1).All(item => item is Pattern.Variable { Name: VariableScopeNames.Wildcard }):
                        transportField = field;
                        break;
                    case Pattern.Variable value when value.Name == options && optionsField < 0:
                        optionsField = field;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    default:
                        return null;
                }
            }

            return transportField >= 0 && optionsField >= 0 ? (transportField, optionsField) : null;
        }

        private bool AlwaysReceivesTaggedArgument(FunctionDefinition target, int argument, Atom tag, int arity)
        {
            if (owner.exportedFunctions.Contains((target.Name, target.Arity)))
            {
                return false;
            }

            bool found = false;
            foreach (var clause in localFunctions.Values.SelectMany(definition => definition.Clauses))
            {
                foreach (var expression in LocalFunctionEntries.ClauseExpressions([clause]).SelectMany(LocalFunctionEntries.Walk))
                {
                    if (expression is Expr.FunctionReference reference && reference.Name == target.Name && reference.Arity == target.Arity)
                    {
                        return false;
                    }

                    if (expression is Expr.Call { Module: null, DynamicRemote: false } call
                        && call.Function == target.Name && call.Arguments.Count == target.Arity)
                    {
                        if (call.Arguments[argument] is not Expr.Variable variable
                            || (!clause.Patterns.OfType<AliasPattern>()
                                .Any(pattern => TryReadPatternAlias(pattern, out var bound, out var shape)
                                    && bound.Name == variable.Name && shape is Pattern.Tuple { Items: [Pattern.Literal literal, ..] } tuple
                                    && tuple.Items.Count == arity && tag.Equals(literal.Value))
                                && !IsConstructedRecordArgument(clause.Body, call, variable.Name, tag, arity)))
                        {
                            return false;
                        }

                        found = true;
                    }
                }
            }

            return found;
        }

        private static bool IsConstructedRecordArgument(Expr body, Expr.Call call, string name, Atom tag, int arity)
        {
            if (body is not Expr.Sequence sequence || !sequence.Items.Any(item => ReferenceEquals(item, call)))
            {
                return false;
            }

            return sequence.Items.TakeWhile(item => !ReferenceEquals(item, call)).Any(item =>
                item is Expr.Match { Pattern: Pattern.Variable variable, Value: Expr.RecordConstruction record }
                && variable.Name == name && record.Definition.Name == tag.Name && record.Definition.Fields.Count + 1 == arity);
        }
    }
}

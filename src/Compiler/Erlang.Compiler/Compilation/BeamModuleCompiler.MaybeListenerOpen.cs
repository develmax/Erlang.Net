using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitMaybeListenerOpen(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom addressTag }, ..] } address, Pattern.Variable options
                ],
                Body: Expr.Maybe { Items: [
                    Expr.Match { Pattern: Pattern.Variable key, Value: Expr.Literal { Value: Atom option } },
                    Expr.Match { Pattern: Pattern.Variable defaultBinding, Value: Expr.Literal { Value: Integer defaultValue } },
                    Expr.Match { Pattern: Pattern.Variable backlog, Value: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Variable keyArg, Expr.Variable optionArg, Expr.Variable defaultArg] } lookup },
                    Expr.MaybeMatch { Pattern: Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom opened }, Pattern.Variable socket
                    ] }, Value: Expr.Call { Module: not null, DynamicRemote: false,
                        Arguments: [Expr.Variable family, Expr.Literal { Value: Atom kind }] } open },
                    Expr.MaybeMatch { Pattern: Pattern.Literal { Value: Atom configured },
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                            Expr.Variable socketArg, var optionExpression
                        ] } set },
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom result }, Expr.Tuple { Items: [
                        Expr.Variable resultSocket, Expr.Variable resultBacklog
                    ] }] }
                ], Clauses: [{
                    Guard: null, Patterns: [AliasPattern errorAlias], Body: Expr.Variable returned
                }] } conditional
            }] || !TryReadPatternAlias(errorAlias, out var error, out var errorShape)
                || errorShape is not Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom errorTag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ] }
                || !DistinctBindings(options, key, defaultBinding, backlog, socket, new(family.Name))
                || error.Name != returned.Name || error.Name == VariableScopeNames.Wildcard
                || error.Name == family.Name || error.Name == options.Name
                || keyArg.Name != key.Name || optionArg.Name != options.Name || defaultArg.Name != defaultBinding.Name
                || socketArg.Name != socket.Name || resultSocket.Name != socket.Name || resultBacklog.Name != backlog.Name
                || owner.ResolvesErlangBif(lookup) || owner.ResolvesErlangBif(open)
                || !entries.TryGetValue((set.Function, 2), out int setEntry))
            {
                return false;
            }

            var deleted = ReadListenerOptionDeletes(optionExpression, options.Name, key.Name);
            Expr.Call? merge = null;
            Term mergeValue = Nil.Value;
            if (deleted is null)
            {
                if (optionExpression is not Expr.Call { Module: not null, DynamicRemote: false,
                    Arguments: [Expr.Variable mergeOptions, var additions, Expr.List { Items: [], Tail: null }] } mergeCall
                    || mergeOptions.Name != options.Name || owner.ResolvesErlangBif(mergeCall)
                    || !AttributeLiteral.TryNormalizeConstant(additions, out mergeValue))
                {
                    return false;
                }
                merge = mergeCall;
            }

            int field = -1;
            for (int index = 1; index < address.Items.Count; index++)
            {
                if (address.Items[index] is Pattern.Variable variable && variable.Name == family.Name && field < 0)
                {
                    field = index;
                }
                else if (address.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }

            if (field < 0)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int openFailed = Label();
            int matchFailed = Label();
            int elseFailed = Label();
            EmitDetailRecordTest(0, address.Items.Count, addressTag, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.InitStackSlot(0);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
            SaveWindowResult(2);
            owner.EmitConstant(defaultValue, 2);
            owner.EmitConstant(option, 0);
            var previous = location;
            EmitWindowCall(lookup, ref previous);
            LoadTupleField(2, field, XRegisterTag, 1);
            SaveWindowResult(2);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.EmitConstant(kind, 1);
            EmitWindowCall(open, ref previous);
            SaveWindowResult(0);
            EmitDetailRecordTest(0, 2, opened, openFailed);
            if (deleted is { } flow)
            {
                EmitWindowCall(flow.Default, ref previous);
                Load(1, 1);
                SaveWindowResult(1);
                owner.EmitConstant(flow.Key, 0);
                EmitWindowCall(flow.Inner, ref previous);
                owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
                owner.EmitConstant(option, 0);
                EmitWindowCall(flow.Outer, ref previous);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(PutList, YRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 1);
                LoadTupleField(0, 1, YRegisterTag, 1);
            }
            else
            {
                owner.EmitConstant(mergeValue, 1);
                owner.EmitConstant(Nil.Value, 2);
                Load(1);
                owner.InitStackSlot(1);
                EmitWindowCall(merge!, ref previous);
                LoadTupleField(0, 1, YRegisterTag, 1);
                owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            }
            owner.InitStackSlot(0);
            Load(1);
            EmitWindowLocal(set, setEntry, ref previous);
            EmitConfigurationAtomTest(configured, matchFailed);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 1), (YRegisterTag, 2));
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(result.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(3);
            Mark(openFailed);
            Load(0);
            Mark(matchFailed);
            EmitMaybeListenerElse(conditional, errorTag, elseFailed);

            return true;
        }

        private (Expr.Call Default, Expr.Call Inner, Expr.Call Outer, Atom Key)? ReadListenerOptionDeletes(
            Expr expression, string options, string backlogKey
        ) => expression is Expr.List
        {
            Items: [Expr.Call { Module: not null, DynamicRemote: false, Arguments: [] } defaultOption],
            Tail: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                Expr.Variable backlog, Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Literal { Value: Atom key }, Expr.Variable input
                ] } inner
            ] } outer
        } && backlog.Name == backlogKey && input.Name == options
            && !owner.ResolvesErlangBif(defaultOption) && !owner.ResolvesErlangBif(inner) && !owner.ResolvesErlangBif(outer)
            ? (defaultOption, inner, outer, key) : null;

        private void EmitMaybeListenerElse(Expr.Maybe conditional, Atom errorTag, int elseFailed, int slots = 3)
        {
            EmitDetailRecordTest(0, 2, errorTag, elseFailed);
            owner.EmitFrameReturn(slots);
            Mark(elseFailed);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(ErlangErrorReasons.ElseClause)), (XRegisterTag, 0));
            owner.EmitLine(new(conditional.PreprocessorFile ?? location.File, conditional.SourceLine));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Error, 1));
            owner.Operand(BeamWire.UnsignedTag, slots);
        }
    }
}

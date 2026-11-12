using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool ResolvesErlangBif(Expr.Call call) =>
        call.Module == ErlangModuleNames.Module
        || sourceModule is not null && SourceCallResolution.IsGuardAutoImport(sourceModule, call.Function, call.Arguments.Count);

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListDetails(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [Pattern.Variable ignored, Pattern.Literal { Value: Atom missing }] } absent,
                {
                    Guard: null, Patterns: [Pattern.Variable id, Pattern.Variable data], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable props, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Literal keys, Expr.Variable input]
                            } convert
                        },
                    Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom result }, Expr.Tuple
                            {
                                Items: [
                        Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Literal prefix, Right: Expr.Variable suffix } title,
                        Expr.Variable value, Expr.Literal extra]
                            }]
                        }]
                    }
                }
            ] || ignored.Name != VariableScopeNames.Wildcard && !ignored.Name.StartsWith('_')
                || !DistinctBindings(id, data, props) || data.Name != input.Name
                || id.Name != suffix.Name || props.Name != value.Name || !TryFoldBindings(absent, out Term missingResult))
            {
                return false;
            }

            int found = Label();
            owner.EmitAtomTest(1, missing.Name, found);
            owner.EmitConstant(missingResult, 0);
            owner.instructions.WriteByte(Return);
            Mark(found);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitConstant(keys.Value, 0);
            owner.EmitCallLine(convert, location);
            owner.EmitStaticRemoteCall(convert);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitConstant(prefix.Value, 0);
            var previous = CallLocation(convert, location);
            owner.EmitArithmeticLine(title.SourceLine, ref previous);
            EmitAppend();
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, 0);
            owner.LiteralOperand(extra.Value);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(result.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(1);

            return true;
        }

        private bool TryEmitMapDetailsPage(FunctionDefinition function)
        {
            Expr.Call? firstLookup = null;
            if (function.Clauses is [
                {
                    Patterns: [_, Pattern.Variable infoInput], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match
                        {
                            Pattern: Pattern.Variable type, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Literal, Expr.Variable lookupInput]
                            } readType
                        },
                    Expr.Match { Value: Expr.Call { Module: null, Arguments: [Expr.Variable argument] } },
                    _, _, _]
                    } sequence
                } page
            ] && infoInput.Name == lookupInput.Name && type.Name == argument.Name && DistinctBindings(type))
            {
                if (LocalFunctionEntries.Walk(sequence).OfType<Expr.Variable>().Count(variable => variable.Name == type.Name) != 1
                    || page.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == type.Name)
                    || sequence.Items.Skip(1).OfType<Expr.Match>().Any(match => match.Pattern is Pattern.Variable bound && bound.Name == type.Name))
                {
                    return false;
                }
                firstLookup = readType;
                function = function with { Clauses = [page with { Body = sequence with { Items = sequence.Items.Skip(1).ToArray() } }] };
            }

            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Variable parent, Pattern.Variable info], Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Match { Pattern: Pattern.Variable fields, Value: Expr.Call { Module: null } prepare },
                    Expr.Match
                        {
                            Pattern: Pattern.Variable details, Value: Expr.Call
                            {
                                Module: not null, DynamicRemote: false,
                                Arguments: [Expr.Literal key, Expr.Variable input]
                            } lookup
                        },
                    Expr.Match
                        {
                            Pattern: Pattern.Variable expanded, Value: Expr.If
                            {
                                Clauses: [
                                {
                                    Guard: Expr.Call
                                    {
                                        Module: null or ErlangModuleNames.Module, Function: "is_map",
                                        Arguments: [Expr.Variable tested]
                                    } guard,
                                    Body: Expr.Binary
                                    {
                                        Operator: ErlangOperators.Append, Left: Expr.Variable original,
                                        Right: Expr.Call
                                        {
                                            Module: "maps", Function: "to_list", DynamicRemote: false,
                                            Arguments: [Expr.Variable mapped]
                                        } convert
                                    } append
                                },
                                { Guard: Expr.Literal { Value: Atom { Name: "true" } }, Body: Expr.Variable fallback }]
                            }
                        },
                    Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [Expr.Variable receiver,
                        Expr.Tuple { Items: [Expr.Variable labels, Expr.Variable payload, var extra] }]
                        } finish]
                    }
                }
            ] || !DistinctBindings(
                parent,
                info,
                fields,
                details,
                expanded
            )
                || parent.Name != receiver.Name || fields.Name != labels.Name || expanded.Name != payload.Name
                || info.Name != input.Name || info.Name != original.Name || info.Name != fallback.Name
                || details.Name != tested.Name || details.Name != mapped.Name
                || prepare.Arguments.Count != (firstLookup is null ? 0 : 1)
                || !entries.TryGetValue((prepare.Function, prepare.Arguments.Count), out int target)
                || !owner.ResolvesErlangBif(guard) || !AttributeLiteral.TryNormalizeConstant(extra, out Term extraValue))
            {
                return false;
            }

            int unmapped = Label();
            int join = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            InitPageSlot(0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            var beforePrepare = location;
            if (firstLookup is not null)
            {
                owner.EmitConstant(firstLookup.Arguments[0], 0);
                owner.EmitCallLine(firstLookup, location);
                owner.EmitStaticRemoteCall(firstLookup);
                beforePrepare = CallLocation(firstLookup, location);
            }
            owner.EmitCallLine(prepare, beforePrepare);
            owner.EmitFieldCall(prepare.Arguments.Count, target);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                1
            );
            owner.EmitConstant(key.Value, 0);
            var prepareLocation = CallLocation(prepare, beforePrepare);
            owner.EmitCallLine(lookup, prepareLocation);
            owner.EmitStaticRemoteCall(lookup);
            owner.Op(IsMap, LabelTag, unmapped);
            owner.Operand(XRegisterTag, 0);
            var lookupLocation = CallLocation(lookup, prepareLocation);
            owner.EmitCallLine(convert, lookupLocation);
            owner.EmitStaticRemoteCall(convert);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            InitPageSlot(1);
            var previous = CallLocation(convert, lookupLocation);
            owner.EmitArithmeticLine(append.SourceLine, ref previous);
            EmitAppend();
            owner.Op(Jump, LabelTag, join);
            Mark(unmapped);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            Mark(join);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(extraValue);
            owner.MoveRegister(
                YRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(finish, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(finish.Module, finish.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, 3);

            return true;
        }

        private void InitPageSlot(int slot)
        {
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, slot);
        }

        private void EmitAppend()
        {
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
        }
    }
}

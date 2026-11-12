using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConfiguredTableStart(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [], Body: Expr.Case
                    {
                        Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Literal { Value: Atom setting }] } read,
                        Clauses: [
                            {
                                Guard: null, Patterns: [Pattern.Tuple { Items: [
                                    Pattern.Literal { Value: Atom ok }, Pattern.List { Tail: null, Items: [
                                        Pattern.List { Tail: null, Items: [Pattern.Variable file] }
                                    ] }
                                ] }],
                                Body: Expr.Sequence { Items: [
                                    Expr.Match { Pattern: Pattern.Variable data, Value: Expr.Call
                                        { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input] } consult },
                                    Expr.Match { Pattern: Pattern.Variable options, Value: var optionValue },
                                    Expr.Match { Pattern: Pattern.Variable table, Value: Expr.Call { Module: not null,
                                        DynamicRemote: false, Arguments: [Expr.Literal { Value: Atom tableName }, Expr.Variable newOptions] } create },
                                    Expr.Match { Pattern: Pattern.Literal { Value: Atom required }, Value: Expr.Call { Module: not null,
                                        DynamicRemote: false, Arguments: [Expr.Variable insertTable, Expr.Variable insertData] } insert } checkedInsert,
                                    Expr.Call { Module: not null, DynamicRemote: false, Arguments.Count: 3 } start
                                ] }
                            },
                            {
                                Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom badTag }, Pattern.Variable badValue] }],
                                Body: Expr.Call { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.Error,
                                    DynamicRemote: false, Arguments: [Expr.Tuple { Items: [
                                        Expr.Literal { Value: Atom badArgument }, Expr.Variable rejected
                                    ] }]
                                } fail
                            },
                            {
                                Guard: null, Patterns: [Pattern.Literal { Value: Atom missing }],
                                Body: Expr.Call { Module: not null, DynamicRemote: false, Arguments.Count: 3 } fallback
                            }
                        ]
                    } dispatch
                }
            ] || !DistinctBindings(file, data, options, table)
                || input.Name != file.Name || newOptions.Name != options.Name
                || insertTable.Name != table.Name || insertData.Name != data.Name
                || !ok.Equals(badTag) || badValue.Name == VariableScopeNames.Wildcard || badValue.Name != rejected.Name
                || !owner.ResolvesErlangBif(fail) || !entries.TryGetValue((consult.Function, 1), out int consultEntry)
                || !AttributeLiteral.TryNormalizeConstant(optionValue, out Term tableOptions)
                || !TryReadStartConstants(start, out var startArgs) || !TryReadStartConstants(fallback, out var fallbackArgs))
            {
                return false;
            }

            int invalidArgument = Label();
            int noArgument = Label();
            int insertFailure = Label();
            int caseFailure = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.InitStackSlot(0);
            owner.EmitConstant(setting, 0);
            var previous = location;
            EmitWindowCall(read, ref previous);
            owner.Op(IsTuple, LabelTag, noArgument);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, caseFailure);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(ok);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitNonemptyList(0, invalidArgument);
            owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 2);
            owner.EmitNonemptyList(1, invalidArgument);
            owner.EmitListParts(1, XRegisterTag, 3, XRegisterTag, 1);
            owner.EmitNilTest(XRegisterTag, 1, invalidArgument);
            owner.EmitNilTest(XRegisterTag, 2, invalidArgument);
            owner.MoveRegister(XRegisterTag, 3, XRegisterTag, 0);
            EmitWindowLocal(consult, consultEntry, ref previous);
            SaveWindowResult(0);
            owner.EmitConstant(tableOptions, 1);
            owner.EmitConstant(tableName, 0);
            EmitWindowCall(create, ref previous);
            Load(0, 1);
            TrimDetailFrame(1, 0);
            EmitWindowCall(insert, ref previous);
            EmitConfigurationAtomTest(required, insertFailure);
            owner.EmitIndependentCallMoves(startArgs);
            EmitConfigurationTail(start, 0, previous);

            Mark(invalidArgument);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(badArgument.Name)), (XRegisterTag, 0));
            EmitConfigurationTail(fail with { Module = ErlangModuleNames.Module }, 1, CallLocation(read, location));
            Mark(noArgument);
            EmitConfigurationAtomTest(missing, caseFailure);
            owner.EmitIndependentCallMoves(fallbackArgs);
            EmitConfigurationTail(fallback, 1, CallLocation(read, location));
            Mark(insertFailure);
            owner.EmitLine(new(checkedInsert.PreprocessorFile ?? previous.File, checkedInsert.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(caseFailure);
            owner.EmitLine(new(dispatch.PreprocessorFile ?? location.File, dispatch.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private static bool TryReadStartConstants(Expr.Call call, out List<(int Destination, Term Value)> arguments)
        {
            arguments = [];
            for (int index = 0; index < call.Arguments.Count; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[index], out Term value))
                {
                    return false;
                }

                arguments.Add((index, value));
            }

            return true;
        }

        private void EmitConfigurationAtomTest(Atom atom, int failure)
        {
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(atom);
        }

        private void EmitConfigurationTail(Expr.Call call, int slots, BeamSourceLocation previous)
        {
            owner.EmitCallLine(call, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, call.Arguments.Count));
            owner.Operand(BeamWire.UnsignedTag, slots);
        }
    }
}

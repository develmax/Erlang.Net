using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRegisteredProcessCase(FunctionDefinition function)
        {
            if (function.Clauses is not
                [{
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                            {
                                Pattern: Pattern.Variable self,
                                Value: Expr.Call { Function: AutoImportNames.Self, Arguments: [] } current
                            },
                            Expr.Case
                            {
                                Value: Expr.Call
                                {
                                    Function: AutoImportNames.Whereis,
                                    Arguments: [Expr.Literal { Value: Atom name }]
                                } selector,
                                Clauses:
                                [
                                    { Guard: null, Patterns: [Pattern.Variable same], Body: var sameBody },
                                    {
                                        Guard: Expr.Call
                                        {
                                            Function: AutoImportNames.IsPid, Arguments: [Expr.Variable checkedPid]
                                        } guard,
                                        Patterns: [Pattern.Variable pid],
                                        Body: Expr.Call { Arguments: [Expr.Variable pidInput] } onPid
                                    },
                                    {
                                        Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                        Body: Expr.Call { Arguments: [Expr.Variable fallbackInput] } onMissing
                                    }
                                ]
                            }
                        ]
                    }
                }]
                || input.Name == VariableScopeNames.Wildcard || self.Name == VariableScopeNames.Wildcard
                || input.Name == self.Name || same.Name != self.Name
                || pid.Name == VariableScopeNames.Wildcard || pid.Name == input.Name || pid.Name == self.Name
                || checkedPid.Name != pid.Name || pidInput.Name != input.Name || fallbackInput.Name != input.Name
                || !IsRegisteredProcessBif(current) || !IsRegisteredProcessBif(selector) || !IsRegisteredProcessBif(guard)
                || ReadEntryCall(onPid, [input.Name]) is not { } pidTail
                || ReadEntryCall(onMissing, [input.Name]) is not { } missingTail)
            {
                return false;
            }

            if (sameBody is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                        {
                            Pattern: Pattern.Variable result,
                            Value: Expr.Binary
                            {
                                Operator: ErlangOperators.Append, Left: var prefix,
                                Right: Expr.Binary
                                {
                                    Operator: ErlangOperators.Append,
                                    Left: Expr.Call { Arguments: [Expr.Variable converted] } convert,
                                    Right: var suffix
                                } inner
                            } outer
                        },
                        Expr.Call { Arguments: [Expr.Variable returned] } onSelf
                    ]
                }
                || result.Name == VariableScopeNames.Wildcard || result.Name == input.Name || result.Name == self.Name
                || returned.Name != result.Name || converted.Name != input.Name
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term head)
                || !AttributeLiteral.TryNormalizeConstant(suffix, out Term tail)
                || ReadRegisterCaseUnaryCall(convert, input.Name, AutoImportNames.AtomToList) is not { } conversion
                || ReadRegisterCaseUnaryCall(onSelf, result.Name, AutoImportNames.Halt) is not { } selfTail)
            {
                return false;
            }

            const int frame = 2;
            int pidBranch = Label();
            int missingBranch = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(YRegisterTag, 0);
            owner.EmitConstant(name, 0);
            owner.EmitCallLine(selector, location);
            Call(ErlangModuleNames.Module, AutoImportNames.Whereis, 1);
            owner.Op(ExactEqualTest, LabelTag, pidBranch);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTypedRegister(0, PidTypeBits, registerTag: YRegisterTag);

            owner.MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 0);
            var selectorLocation = CallLocation(selector, location);
            EmitEntryCall(conversion, selectorLocation);
            var previous = CallLocation(convert, selectorLocation);
            owner.EmitConstant(tail, 1);
            owner.EmitArithmeticLine(inner.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
            owner.EmitConstant(head, 0);
            owner.EmitArithmeticLine(outer.SourceLine, ref previous);
            Call(ErlangModuleNames.Module, ErlangOperators.Append, 2);
            EmitPreparedFrameTail(selfTail, previous, 0);

            Mark(pidBranch);
            owner.Op(IsPid, LabelTag, missingBranch);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
            EmitPreparedFrameTail(pidTail, selectorLocation, frame);

            Mark(missingBranch);
            owner.MoveRegister(YRegisterTag, 1, XRegisterTag, 0);
            EmitPreparedFrameTail(missingTail, selectorLocation, frame);

            return true;
        }

        private EntryCall? ReadRegisterCaseUnaryCall(Expr.Call call, string input, string externalBif)
        {
            if (call.Function == externalBif && IsRegisteredProcessBif(call))
            {
                return new EntryCall(call, ErlangModuleNames.Module, [], []);
            }

            return ReadEntryCall(call, [input]);
        }

        private bool IsRegisteredProcessBif(Expr.Call call) =>
            call is { DynamicRemote: false, Module: null or ErlangModuleNames.Module }
            && owner.ResolvesErlangBif(call);
    }
}

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConvertedCallTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Patterns: [Pattern.Variable parameter], Body: Expr.Call tail }]
                || parameter.Name == VariableScopeNames.Wildcard || tail.DynamicRemote)
            {
                return false;
            }

            if (tail is { Module: not null, Arguments: [
                Expr.Call { Module: null, DynamicRemote: false, Arguments: [
                    Expr.Call { Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Literal key, Expr.Variable argument
                    ] } lookup
                ] } convert, var flags
            ] } && argument.Name == parameter.Name && entries.TryGetValue((convert.Function, 1), out int target)
                && AttributeLiteral.TryNormalizeConstant(flags, out Term flagValue)
                && !owner.ResolvesErlangBif(tail) && !owner.ResolvesErlangBif(lookup))
            {
                EmitConversionFrame();
                owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
                owner.EmitConstant(key.Value, 0);
                var previous = location;
                EmitWindowCall(lookup, ref previous);
                EmitWindowLocal(convert, target, ref previous);
                owner.EmitConstant(flagValue, 1);
                EmitConfigurationTail(tail, 0, previous);

                return true;
            }

            if (tail is not { Module: null or ErlangModuleNames.Module, Function: AutoImportNames.ListToAtom, Arguments: [
                Expr.Binary { Operator: ErlangOperators.Append, Left: Expr.Call {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: AutoImportNames.AtomToList,
                    Arguments: [Expr.Variable source]
                } atomToList, Right: var suffix }
            ] } || source.Name != parameter.Name || !owner.ResolvesErlangBif(atomToList) || !owner.ResolvesErlangBif(tail)
                || !AttributeLiteral.TryNormalizeConstant(suffix, out Term suffixValue))
            {
                return false;
            }

            EmitConversionFrame();
            var conversionLocation = location;
            EmitWindowCall(atomToList with { Module = ErlangModuleNames.Module }, ref conversionLocation);
            owner.EmitConstant(suffixValue, 1);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Append, 2));
            EmitConfigurationTail(tail with { Module = ErlangModuleNames.Module }, 0, conversionLocation);

            return true;
        }

        private void EmitConversionFrame()
        {
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
        }
    }
}

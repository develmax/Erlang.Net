using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool IsNonAllocatingBinaryGuardCall(Expr.Call call) =>
            call is { Function: AutoImportNames.IsMapKey or AutoImportNames.MapGet or AutoImportNames.Min or AutoImportNames.Max, DynamicRemote: false, Arguments.Count: 2 }
            && owner.ResolvesErlangBif(call);

        private void EmitNonAllocatingBinaryGuardValue(Expr.Call call, int failure)
        {
            Expr key = call.Arguments[0];
            Expr map = call.Arguments[1];
            int? keySlot = GuardSlot(key, failure);
            int? mapSlot = GuardSlot(map, failure);

            owner.Op(BifTwo, LabelTag, call.Function is AutoImportNames.Min or AutoImportNames.Max ? 0 : failure);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 2));
            GuardOperand(key, keySlot);
            GuardOperand(map, mapSlot);
            owner.Operand(XRegisterTag, 0);
        }
    }
}

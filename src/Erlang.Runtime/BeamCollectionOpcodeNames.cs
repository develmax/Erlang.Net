namespace Erlang;

internal static class BeamCollectionOpcodeNames
{
    internal const string UpdateRecord = "update_record";
    internal const string BadRecord = "badrecord";
    internal const string FloatMove = "fmove";
    internal const string FloatConvert = "fconv";
    internal const string FloatMultiply = "fmul";
    internal const string FloatAdd = "fadd";
    internal const string FloatSubtract = "fsub";
    internal const string FloatDivide = "fdiv";
    internal const string FloatNegate = "fnegate";

    internal static bool ClobbersFloats(string opcode) => opcode is
        BeamRegisterNames.Call or BeamLinkNames.LocalCall or BeamGeneralOpcodeNames.LocalLast
        or BeamLinkNames.ExternalCall or BeamLinkNames.ExternalTailCall or BeamOnLoadNames.ExternalLast
        or BeamGeneralOpcodeNames.DynamicCall or BeamLambdaNames.Call or BeamApplyNames.Opcode or BeamApplyNames.Last
        or BeamLinkNames.Send or BeamLinkNames.LoopReceive or BeamLinkNames.WaitTimeout or BeamLinkNames.Wait;
}

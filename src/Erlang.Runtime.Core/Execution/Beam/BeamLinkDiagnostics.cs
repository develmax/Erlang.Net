namespace Erlang;

internal static class BeamLinkDiagnostics
{
    internal const string TypeTable = "Invalid BEAM type table";
    internal const string TypeIndex = "BEAM register type index outside recognized table";
    internal const string FunctionBoundary = "Invalid BEAM function boundary";
    internal const string FunctionCount = "BEAM declared and decoded function counts differ";
    internal const string Label = "Invalid or duplicate BEAM label definition";
    internal const string LabelReference = "BEAM label reference has no declared target";
    internal const string Export = "BEAM export target does not identify a linked function";
    internal const string Import = "Invalid BEAM import instruction reference";
    internal const string LocalCall = "BEAM local call target or arity is invalid";
    internal const string ModuleIdentity = "BEAM function module differs from atom-table module";
    internal const string OpcodeRole = "Unsupported BEAM opcode role in bounded linker";
    internal const string OperandRole = "Invalid operand role in bounded BEAM linker";
}

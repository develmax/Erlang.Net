using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool TryEmitBif(Clause clause, Expr.Call call, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        int arity = call.Arguments.Count;
        if (clause.Body != call || call.DynamicRemote || call.Module is not (null or ErlangModuleNames.Module)
            || (call.Module is null && (entries.ContainsKey((call.Function, arity))
                || sourceModule?.CallBindings.ImportedModule(call.Function, arity) is not (null or ErlangModuleNames.Module))))
        {
            return false;
        }

        if (arity == 0 && call.Function is AutoImportNames.Self or AutoImportNames.Node)
        {
            Op(BifZero, BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, call.Function, 0));
        }
        else if (call.Arguments is [Expr.Variable argument]
            && ReadUnaryBifOpcode(call.Function) is byte opcode)
        {
            int register = ArgumentRegister(clause, argument.Name);
            Op(opcode, LabelTag, 0);
            if (opcode == GcBifOne)
            {
                Operand(BeamWire.UnsignedTag, register + 1);
            }
            Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, call.Function, 1));
            Operand(XRegisterTag, register);
        }
        else
        {
            return false;
        }

        Operand(XRegisterTag, 0);
        instructions.WriteByte(Return);

        return true;
    }

    private static byte? ReadUnaryBifOpcode(string function) => function switch
    {
        AutoImportNames.IsAtom or AutoImportNames.IsInteger or AutoImportNames.IsTuple
            or AutoImportNames.IsNumber or AutoImportNames.IsFloat or AutoImportNames.IsMap
            or AutoImportNames.IsPort or AutoImportNames.IsPid or AutoImportNames.IsFunction
            or AutoImportNames.TupleSize or AutoImportNames.Hd or AutoImportNames.Node => BifOne,
        AutoImportNames.ByteSize or AutoImportNames.Length or AutoImportNames.Abs => GcBifOne,
        _ => null
    };
}

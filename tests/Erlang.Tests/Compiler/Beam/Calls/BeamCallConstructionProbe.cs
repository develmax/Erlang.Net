using System.Collections;
using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal sealed class BeamCallConstructionProbe
{
    private readonly object[] instructions;
    private readonly string[] atoms;
    private readonly object[] imports;

    internal BeamCallConstructionProbe(byte[] image)
    {
        object code = Code(image);
        instructions = Items(code, "Instructions");
        object structure = Property(code, "Structure");
        atoms = Items(structure, "Atoms").Cast<string>().ToArray();
        imports = Items(structure, "Imports");
    }

    internal object[] Body(string name)
    {
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operand(instruction, 1))] == name
        );

        return entry < 0
            ? []
            : instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    internal int CallIndex(
        object[] body,
        string module,
        string function,
        int arity
    ) =>
        Array.FindIndex(body, operation => Opcode(operation) == ExternalCall
            && UsesImport(
                operation,
                1,
                module,
                function,
                arity
            ));

    internal bool UsesImport(
        object instruction,
        int index,
        string module,
        string function,
        int arity
    )
    {
        object import = imports[Value(Operand(instruction, index))];

        return atoms[(int)Property(import, "ModuleAtomIndex")] == module
            && atoms[(int)Property(import, "FunctionAtomIndex")] == function
            && (int)Property(import, "Arity") == arity;
    }

    internal static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    internal static object Operand(object instruction, int index) => Items(instruction, "Operands")[index];

    internal static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    internal static string Kind(object operand) => (string)Property(operand, "Kind");

    internal static bool IsX(object operand, int register) => Kind(operand) == "x" && Value(operand) == register;

    internal static bool SameRegister(object left, object right) => Kind(left) == Kind(right) && Value(left) == Value(right);

    internal static object[] WithoutLines(IEnumerable<object> instructions) =>
        instructions.Where(instruction => Opcode(instruction) != Line).ToArray();

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}

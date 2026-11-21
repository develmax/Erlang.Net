using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedRecordColdCasesChecks
{
    internal const string TestName = "compiler/beam/orders-closed-nested-record-case-failures";
    private const string WrongLayout = "The nested record case did not preserve the guarded failure order.";

    internal static Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(
            BeamOwnedRecordFrameSlotsChecks.Source,
            BeamOwnedRecordFrameSlotsChecks.Fixture
        );
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasTrailingOrder("preserve") || !HasTrailingOrder("reordered")
            || !Body("run").Where(IsFailure).Select(Opcode).SequenceEqual([CaseEnd, CaseEnd, CaseEnd, BadRecord]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        return Task.CompletedTask;

        bool HasTrailingOrder(string name)
        {
            object[] body = Body(name);
            int first = Array.FindIndex(body, IsFailure);
            if (first < 0)
            {
                return false;
            }

            byte[] cold = body.Skip(first).Select(Opcode).ToArray();

            return cold.Where(opcode => opcode is CaseEnd or BadRecord).SequenceEqual([CaseEnd, BadRecord, CaseEnd])
                && cold.All(opcode => opcode is Label or Line or CaseEnd or BadRecord);
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == name
            );

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool IsFailure(object instruction) => Opcode(instruction) is CaseEnd or BadRecord;

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}

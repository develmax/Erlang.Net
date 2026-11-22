using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFinalAssemblyTablesChecks
{
    internal const string TestName = "compiler/beam/orders-tables-from-final-physical-instructions";
    private const int MapType = 1 << 6;
    private const string WrongOrder = "Final BEAM table positions must follow the surviving physical instruction order.";

    internal static Task Run()
    {
        Check(0);
        Check(1);
        Check(2);
        CheckTypesWithoutOtherTableEdits();

        return Task.CompletedTask;
    }

    private static void Check(int closureCount)
    {
        var compiler = new BeamModuleCompiler("assembly_table_probe");
        var atoms = ReadField<Dictionary<string, int>>("atoms");
        foreach (string name in new[] { "late", "remote_late", "finish", "early", "remote_early", "start", "discarded" })
        {
            atoms.Add(name, atoms.Count + 1);
        }

        var imports = ReadField<Dictionary<(int Module, int Function, int Arity), int>>("imports");
        imports.Add((3, 4, 1), 0);
        imports.Add((6, 7, 1), 1);
        imports.Add((3, 8, 0), 2);

        var types = ReadField<List<(int Bits, long? Lower, long? Upper, int? Unit)>>("typeBits");
        var integer = (Bits: 1 << 5, Lower: (long?)null, Upper: (long?)null, Unit: (int?)null);
        var floatType = (Bits: 1 << 4, Lower: (long?)null, Upper: (long?)null, Unit: (int?)null);
        types.Add(integer);
        types.Add(floatType);
        types.Add((MapType, null, null, null));

        var locals = ReadField<List<(int Atom, int Arity, int Entry)>>("locals");
        locals.Add((2, 0, 4));
        locals.Add((5, 0, 2));
        var literals = ReadField<List<byte[]>>("literalBytes");
        byte[] lateValue = [31];
        byte[] earlyValue = [12];
        literals.Add(lateValue);
        literals.Add(earlyValue);
        literals.Add([4]);

        if (closureCount > 0)
        {
            var closures = ReadField<IList>("closures");
            AddClosure(closures, "late", 4);
            ExpectUnchanged([]);
            if (closureCount == 2)
            {
                AddClosure(closures, "early", 2);
                ExpectUnchanged([1, 0]);
                ExpectUnchanged([0, 0]);
            }
        }

        int[] firstUse = closureCount switch
        {
            0 => [],
            1 => [0],
            _ => [0, 0, 1]
        };
        ReadOnlyMemory<byte> before = Instructions(
            5,
            2,
            1,
            0,
            firstUse,
            firstType: 2
        );
        var after = (ReadOnlyMemory<byte>)Call(
            compiler,
            "OrderFinalAssemblyTables",
            before,
            5
        );
        if (!after.Span.SequenceEqual(Instructions(
            2,
            5,
            0,
            1,
            firstUse,
            firstType: 1
        ))
            || types.Count != 3 || types[1] != floatType || types[2] != integer
            || !literals.SequenceEqual(new[] { earlyValue, lateValue })
            || !locals.SequenceEqual(new[] { (5, 0, 2), (2, 0, 4) })
            || !imports.OrderBy(pair => pair.Value).Select(pair => pair.Key).SequenceEqual(new[] { (6, 7, 1), (3, 4, 1) })
            || !ReadField<int[]>("emittedAtoms").SequenceEqual(new[] { 0, 1, 5, 6, 7, 2, 3, 4, 0 }))
        {
            throw new InvalidOperationException(WrongOrder);
        }

        if (closureCount > 0)
        {
            using var table = new MemoryStream();
            Call(
                compiler,
                "EmitClosureTable",
                table,
                0
            );
            byte[] row = table.ToArray();
            if (BinaryPrimitives.ReadInt32BigEndian(row) != closureCount
                || row.Length != 4 + closureCount * 24
                || !EntryMatches(
                    row.AsSpan(4),
                    5,
                    4,
                    0
                )
                || closureCount == 2 && !EntryMatches(
                    row.AsSpan(28),
                    2,
                    2,
                    1
                ))
            {
                throw new InvalidOperationException(WrongOrder);
            }
        }

        void ExpectUnchanged(int[] roots)
        {
            ReadOnlyMemory<byte> source = Instructions(
                5,
                2,
                1,
                0,
                roots
            );
            var unchanged = (ReadOnlyMemory<byte>)Call(
                compiler,
                "OrderFinalAssemblyTables",
                source,
                5
            );
            if (!unchanged.Span.SequenceEqual(source.Span) || imports.Count != 3 || literals.Count != 3)
            {
                throw new InvalidOperationException(WrongOrder);
            }
        }

        T ReadField<T>(string name) => (T)typeof(BeamModuleCompiler)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiler)!;
    }

    private static void CheckTypesWithoutOtherTableEdits()
    {
        var compiler = new BeamModuleCompiler("type_only_table_probe");
        var atoms = (Dictionary<string, int>)typeof(BeamModuleCompiler)
            .GetField("atoms", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiler)!;
        atoms.Add("value", 2);
        var locals = (List<(int Atom, int Arity, int Entry)>)typeof(BeamModuleCompiler)
            .GetField("locals", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiler)!;
        locals.Add((2, 1, 2));
        var types = (List<(int Bits, long? Lower, long? Upper, int? Unit)>)typeof(BeamModuleCompiler)
            .GetField("typeBits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(compiler)!;
        types.Add((1 << 4, null, null, null));
        types.Add((1 << 5, null, null, null));
        types.Add((MapType, null, null, null));

        var code = (ReadOnlyMemory<byte>)Call(
            compiler,
            "OrderFinalAssemblyTables",
            new ReadOnlyMemory<byte>(TypedInstructions(2)),
            3
        );
        if (!code.Span.SequenceEqual(TypedInstructions(1)) || types.Count != 3 || types[1].Bits != 1 << 5 || types[2].Bits != 1 << 4)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        static byte[] TypedInstructions(int firstType) =>
        [
            Label, 16, FunctionInfo, CompactAtom(1), CompactAtom(2), 16, Label, 32,
            .. TypedMoves(firstType), Return, EndCode
        ];
    }

    private static bool EntryMatches(
        ReadOnlySpan<byte> row,
        int name,
        int label,
        int index
    ) => BinaryPrimitives.ReadInt32BigEndian(row) == name
        && BinaryPrimitives.ReadInt32BigEndian(row[8..]) == label
        && BinaryPrimitives.ReadInt32BigEndian(row[12..]) == index;

    private static void AddClosure(IList closures, string name, int label)
    {
        const BindingFlags members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type closureType = typeof(BeamModuleCompiler).GetNestedType("LiftedClosure", BindingFlags.NonPublic)!;
        ConstructorInfo constructor = closureType.GetConstructors(members)
            .Single(candidate => candidate.GetParameters()[0].ParameterType == typeof(string));
        object?[] arguments = constructor.GetParameters().Select(parameter =>
            parameter.HasDefaultValue ? parameter.DefaultValue : null).ToArray();
        arguments[0] = name;
        arguments[2] = new BeamSourceLocation(null, 0);
        arguments[3] = 0;
        object closure = constructor.Invoke(arguments);
        closureType.GetProperty("Entry", members)!.SetValue(closure, label);
        closures.Add(closure);
    }

    private static byte[] Instructions(
        int firstFunction,
        int secondFunction,
        int firstIndex,
        int secondIndex,
        int[] firstUse,
        int firstType = 2
    ) =>
        [
            Label, 16,
            FunctionInfo, CompactAtom(1), CompactAtom(firstFunction), 0,
            Label, 32,
            Move, ExtendedLiteralOperand, CompactUnsigned(firstIndex), XRegisterTag,
            ExternalCall, 16, CompactUnsigned(firstIndex),
            .. firstUse.SelectMany(Closure),
            .. TypedMoves(firstType),
            Return,
            Label, 48,
            FunctionInfo, CompactAtom(1), CompactAtom(secondFunction), 0,
            Label, 64,
            Move, ExtendedLiteralOperand, CompactUnsigned(secondIndex), XRegisterTag,
            ExternalCall, 16, CompactUnsigned(secondIndex),
            Return,
            EndCode
        ];

    private static byte[] TypedMoves(int firstType) =>
    [
        Move, ExtendedTypedRegisterOperand, XRegisterTag, CompactUnsigned(firstType), XRegisterTag,
        Move, ExtendedTypedRegisterOperand, XRegisterTag, CompactUnsigned(3 - firstType), XRegisterTag
    ];

    private static byte[] Closure(int index) =>
    [
        MakeFun, CompactUnsigned(index), XRegisterTag, ExtendedListOperand, 0
    ];

    private static byte CompactUnsigned(int value) => (byte)(value << 4);

    private static byte CompactAtom(int value) => (byte)(CompactUnsigned(value) | AtomTag);
}

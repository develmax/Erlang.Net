using System.Buffers.Binary;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamFinalLineOrderChecks
{
    internal const string TestName = "compiler/beam/orders-ordinary-lines-by-final-instruction-use";
    private const string WrongOrder = "The final line table must preserve source locations in first physical use order.";
    private const string FirstFile = "first_line_order_probe.erl";
    private const string SecondFile = "second_line_order_probe.erl";
    private const int SmallLocation = 15;
    private const int WideLocation = 16;
    private const int FirstSourceLine = 79;
    private const int SecondSourceLine = 81;
    private const int LabelCount = 3;

    internal static Task Run()
    {
        BeamModuleCompiler compiler = Prepared();
        byte[] input = Fixture(SmallLocation, WideLocation, firstUseInOrder: false);
        byte[] expected = Fixture(SmallLocation, WideLocation, firstUseInOrder: true);
        if (Rewrite(compiler, input) is not { } result || !result.SequenceEqual(expected)
            || Rewrite(compiler, result) is not null
            || Rewrite(Prepared(), Fixture(
                SmallLocation,
                WideLocation,
                firstUseInOrder: false,
                executableMarker: true
            )) is not null)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        var finalized = (ReadOnlyMemory<byte>)Call(
            Prepared(),
            "OrderFinalAssemblyTables",
            new ReadOnlyMemory<byte>(input),
            LabelCount
        );
        byte[] marked = Fixture(
            SmallLocation,
            WideLocation,
            firstUseInOrder: false,
            executableMarker: true
        );
        var finalizedMarked = (ReadOnlyMemory<byte>)Call(
            Prepared(),
            "OrderFinalAssemblyTables",
            new ReadOnlyMemory<byte>(marked),
            LabelCount
        );
        if (!finalized.Span.SequenceEqual(expected) || !finalizedMarked.Span.SequenceEqual(marked))
        {
            throw new InvalidOperationException(WrongOrder);
        }

        using var line = new MemoryStream();
        _ = Call(compiler, "EmitLines", line);
        byte[] payload = line.ToArray();
        if (BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(8)) != WideLocation + 2)
        {
            throw new InvalidOperationException(WrongOrder);
        }

        byte[] image = BeamMetadataModeledBytes.WithChunk(
            Image(result[..^1], labels: LabelCount),
            "Line",
            payload
        );
        object structure = Property(Code(image), "Structure");
        var positions = (IReadOnlyList<Term>)Call(Type("BeamLineTableReader"), "Read", structure);
        if (positions.Count != WideLocation + 1 || positions[0] is not Nil
            || !positions[SmallLocation].Equals(Position(SecondFile, SecondSourceLine))
            || !positions[WideLocation].Equals(Position(FirstFile, FirstSourceLine)))
        {
            throw new InvalidOperationException(WrongOrder);
        }

        return Task.CompletedTask;
    }

    private static BeamModuleCompiler Prepared()
    {
        var compiler = new BeamModuleCompiler(BeamOpcodeEdgeNames.Module);
        _ = Call(compiler, "Intern", BeamOpcodeEdgeNames.Run);
        for (int number = 1; number < SmallLocation; number++)
        {
            Add(null, number);
        }

        Add(FirstFile, FirstSourceLine);
        Add(SecondFile, SecondSourceLine);

        return compiler;

        void Add(string? file, int line) => Call(compiler, "IndexLineLocation", new BeamSourceLocation(file, line));
    }

    private static byte[]? Rewrite(BeamModuleCompiler compiler, ReadOnlyMemory<byte> code) => (byte[]?)Call(
        compiler,
        "ReorderFinalLineLocations",
        code,
        LabelCount
    );

    private static byte[] Fixture(
        int small,
        int wide,
        bool firstUseInOrder,
        bool executableMarker = false
    )
    {
        var locations = new List<byte>();
        for (int index = 1; index < small; index++)
        {
            locations.Add(Line);
            locations.Add(U(index));
        }

        int early = firstUseInOrder ? small : wide;
        int late = firstUseInOrder ? wide : small;
        byte[] marked = executableMarker ? [ExecutableLine, U(1), U(1)] : [];

        return
        [
            .. Start(
                1,
                2,
                2,
                1
            ),
            .. locations, .. marked, Line, .. WriteIndex(early), Line, .. WriteIndex(late),
            Line, .. WriteIndex(early), Line, U(0), Return, EndCode
        ];
    }

    private static byte[] WriteIndex(int index) => index == WideLocation ? [8, 16] : [U(index)];

    private static Term Position(string file, int line) => Term.List(
        Term.Tuple(Term.A("file"), Term.String(file)),
        Term.Tuple(Term.A("line"), Term.I(line))
    );
}

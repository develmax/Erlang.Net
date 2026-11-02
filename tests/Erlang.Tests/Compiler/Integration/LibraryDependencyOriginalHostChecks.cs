using System.Buffers.Binary;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang;
using static Erlang.Tests.LibraryDependencyOriginalNames;

namespace Erlang.Tests;

internal static class LibraryDependencyOriginalHostChecks
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Digests = new(ManifestDigests);

    internal static Task WarningMap398Owner() => WarningMap398HostChecks.Run(
        Read(WarningMap398HostNames.ModuleName + Beam),
        JsonSerializer.Deserialize<string[]>(Read(WarningMap398HostNames.ImportsResource))!
    );

    internal static Task FixedWhole() => Compare(Fixed);

    internal static Task UnicodeFieldsWhole() => Compare(UnicodeFields);

    internal static Task MetadataWhole() => Compare(Metadata);

    internal static Task Math394Whole() => Compare(Math394);

    internal static Task Math394Owner() => LibrarySelectionPolicyOriginalMathChecks.OriginalMath24(Read(MathModule + Beam), [], []);

    internal static Task Binary395Whole() => Compare(Binary395);

    internal static Task Bif395Whole() => Compare(Bif395);

    internal static Task Frame395Whole() => Compare(Frame395);

    internal static Task FileEncoding396Owner() => FileNameEncoding395HostChecks.Run(Read(FileNameEncoding396TestNames.FileModule + Beam), JsonSerializer.Deserialize<string[]>(Read(Dependencies))!);

    internal static Task Receive396Whole() => Compare(Receive396);

    internal static Task Encoding396Whole() => Compare(Encoding396);

    internal static Task Handler397Whole() => Compare(Handler397);

    internal static Task Map397Whole() => Compare(Map397);

    internal static Task Context397Whole() => Compare(Context397);

    internal static Task Position398Whole() => Compare(Position398);

    internal static Task Receive398Whole() => Compare(Receive398);

    internal static Task WarningDefault398Whole() => Compare(WarningDefault398);

    internal static Task WarningInfo398Whole() => Compare(WarningInfo398);

    internal static Task WarningError398Whole() => Compare(WarningError398);

    internal static Task WarningWarning398Whole() => Compare(WarningWarning398);

    internal static Task Sofs398Whole() => Compare(Sofs398);

    internal static Task NewConnection400Whole() => Compare(NewConnection400);

    internal static Task NetKernel400Whole() => Compare(NetKernel400);

    internal static Task Callback399Whole() => Compare(Callback399);

    internal static Task Nested399Whole() => Compare(Nested399);

    internal static Task Os399Whole() => OperatingSystem.IsWindows() ? Compare(Os399) : Task.CompletedTask;

    internal static Task Port399Whole() => OperatingSystem.IsWindows() ? Compare(Port399) : Task.CompletedTask;

    internal static Task Small393Whole() => Compare(Small393);

    internal static Task FloatString393Whole() => Compare(FloatString393);

    internal static Task CatchWhole() => Compare(Catch);

    internal static Task Bif3Whole() => Compare(Bif3);

    internal static Task WideWhole() => Compare(Wide);

    internal static Task PrettyWhole() => Compare(Pretty);

    internal static Task UnicodeWhole() => Compare(UnicodeSuite);

    internal static Task ReceiveWhole() => Compare(Receive);

    internal static Task<ProcessRuntime> StartRetainedSuite(LibraryDependencyOriginalSuite suite) => Start(suite);

    internal static Task CompareRetainedSuite(LibraryDependencyOriginalSuite suite) => Compare(suite);

    internal static byte[] ReadRetainedResource(string name) => Read(name);

    private static async Task Compare(LibraryDependencyOriginalSuite suite)
    {
        await using ProcessRuntime runtime = await Start(suite);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Func<Func<ProcessContext, Task<bool>>, Task<bool>> invoke500 = actor.InvokeAsync;
        if (ReferenceEquals(suite, Fixed))
            invoke500 = command => actor.InvokeWithDeadline500Async(command, Deadline);
        await invoke500(async context =>
        {
            if (ReferenceEquals(suite, Os399))
                Require((await runtime.Modules.Call(
                    context,
                    OsModule,
                    OsPutEnv,
                    Cons.From(OsSeedKey.Select(c => (Term)Term.I(c))),
                    Cons.From(OsSeedValue.Select(c => (Term)Term.I(c)))
                )).Equals(Term.A(ErlangBooleanAtoms.True)));
            Term original = runtime.ImportExternalTerm(Read(suite.Prefix + ValueSuffix)).Term;
            Term actual = await runtime.Modules.Call(context, suite.Observer, Run);
            CheckRows(original, actual, suite.Rows);
            Require(original.Equals(actual));
            Term originalStacks = runtime.ImportExternalTerm(Read(suite.Prefix + StackSuffix)).Term;
            Term actualStacks = await runtime.Modules.Call(context, suite.Observer, Stacks);
            Term[] first = Proper(originalStacks), second = Proper(actualStacks);
            Require(first.Length == 5 && second.Length == 5);
            Term[] requested = [Term.A(DefaultDepth), Term.I(0), Term.I(1), Term.I(2), Term.I(8)];
            for (int index = 0; index < first.Length; index++)
            {
                Require(first[index] is TupleTerm && second[index] is TupleTerm);
                var left = (TupleTerm)first[index];
                var right = (TupleTerm)second[index];
                Require(left.Items.Count == suite.ProfileArity && right.Items.Count == suite.ProfileArity);
                Require(left.Items[0].Equals(requested[index]) && right.Items[0].Equals(requested[index]));
                CheckRows(left.Items[^1], right.Items[^1], suite.Rows);
                Require(left.Equals(right));
            }
            Require(originalStacks.Equals(actualStacks));

            return true;
        }).WaitAsync(Deadline);
    }

    private static void CheckRows(Term first, Term second, int count)
    {
        Term[] expected = Proper(first), actual = Proper(second);
        Require(expected.Length == count && actual.Length == count);
        var names = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < count; index++)
        {
            Require(expected[index] is TupleTerm { Items.Count: 2 } && actual[index] is TupleTerm { Items.Count: 2 });
            var row = (TupleTerm)expected[index];
            var result = (TupleTerm)actual[index];
            Require(row.Items[0] is Atom name && names.Add(name.Name));
            Require(row.Equals(result));
        }
    }

    private static Term[] Proper(Term term)
    {
        var result = new List<Term>();
        while (term is Cons list)
        {
            result.Add(list.Head);
            term = list.Tail;
        }
        Require(term is Nil);

        return result.ToArray();
    }

    private static async Task<ProcessRuntime> Start(LibraryDependencyOriginalSuite suite)
    {
        bool requiresCodeServer = suite.AdditionalLibraries.Contains(IoPrintableOriginal404Names.CodeModule, StringComparer.Ordinal);
        var runtime = suite.PrintableRange is { } range
            ? new ProcessRuntime(
                ProcessNodeDefaults.Local,
                null,
                IoPrintableRange404Names.ProcessLimit,
                null,
                IoPrintableRange404Names.ActorConcurrency,
                new ProcessRuntimeOptions(suite.WarningMapping ?? RuntimeWarningMapping.Warning, range)
            )
            : suite.WarningMapping is { } mapping
            ? new ProcessRuntime(
                ProcessNodeDefaults.Local,
                null,
                WarningMap398HostNames.ProcessLimit,
                null,
                WarningMap398HostNames.ActorConcurrency,
                new ProcessRuntimeOptions(mapping)
            )
            : new ProcessRuntime();
        try
        {
            if (requiresCodeServer)
                await CodeServerRuntime410Checks.Prepare(runtime, suite.Fixtures);
            await runtime.InitializeModulesAsync(Erlang.Otp.OtpModules.Register);
            string[] declared = JsonSerializer.Deserialize<string[]>(Read(Dependencies)) ?? throw new InvalidDataException(Failure);
            Require(declared.Length >= 180 && declared.Distinct(StringComparer.Ordinal).Count() == declared.Length);
            var dependencies = new BeamDependencySelection(declared.Concat([ErlangModuleNames.Module]).Concat(suite.Fixtures));
            string[] modules = Libraries.Concat(suite.AdditionalLibraries).Concat(suite.Fixtures)
                .Where(module => !requiresCodeServer || !CodeServerRuntime410Names.Modules.Contains(module, StringComparer.Ordinal))
                .ToArray();
            await PreparedBeamFixtureImages825.PrepareAndPublishAsync(
                runtime,
                modules.Select(module => module == IoPrintableOriginal404Names.CodeModule
                    ? InterpreterCode411Resources.Read() : Read(module + Beam)),
                dependencies,
                (registry, images) =>
                {
                    Require(registry.PublishPreparedBeam(images).Status == BeamPublicationStatus.Published);
                    foreach (PreparedBeamImage image in images)
                    {
                        var exports = registry.Exports.Where(export => export.Module == image.Name).ToArray();
                        Require(exports.Length > 0 && exports.All(export => ReferenceEquals(registry.SourceOwner(image.Name, export.Function, export.Arity), image)));
                        int count = image.Name switch
                        {
                            "lists" => 5,
                            "string" => 1,
                            Unicode => 3,
                            MathModule => 24,
                            NetKernelModule => 1,
                            IoPrintableRange404Names.ModuleName => 1,
                            IoPrintableOriginal404Names.CodeModule => InterpreterCode411Names.NativeCount,
                            FileModule => 1,
                            PrimFileEncodingOriginal410Names.ModuleName => PrimFileEncodingOriginal410Names.NativeCount,
                            WarningMap398HostNames.ModuleName => 1,
                            OsModule when OperatingSystem.IsWindows() => 10,
                            _ => 0
                        };
                        Require(image.NativePrimitiveExports.Count == count);
                        if (image.Name == Unicode)
                            Require(image.NativePrimitiveExports.Select(export => export.Name + "/" + export.Arity).Order(StringComparer.Ordinal)
                                .SequenceEqual(UnicodeNative.Order(StringComparer.Ordinal)));
                    }
                }
            );

            if (requiresCodeServer)
            {
                string output = Path.GetFullPath(Path.Combine(
                    IoPrintableOriginal404Names.CodeServerBootRoot,
                    Guid.NewGuid().ToString(IoPrintableOriginal404Names.GuidFormat)
                ));
                Directory.CreateDirectory(output);
                using var startup = new CancellationTokenSource(LibraryDependencyOriginalNames.Deadline);
                RuntimeOtpBootSession session = await runtime.StartOtpBootAsync(CodeServerOriginal410Fixture.Create(output), startup.Token);
                Require(!session.Completion.IsCompleted);
            }

            return runtime;
        }
        catch
        {
            await runtime.DisposeAsync();
            throw;
        }
    }

    internal static async Task ForeignAndMalformedNativeHelper()
    {
        await using var owner = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        string[] declared = JsonSerializer.Deserialize<string[]>(Read(Dependencies))!;
        var dependencies = new BeamDependencySelection(declared);
        byte[] bytes = Read(Unicode + Beam);
        PreparedBeamImage image = owner.Modules.PrepareBeam(bytes, dependencies);
        Require(image.NativePrimitiveExports.Count == 3);
        bool refused = false;
        try
        {
            foreign.Modules.PublishPreparedBeam(image);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused && !image.IsConsumed && !owner.Modules.IsModuleLoaded(Unicode) && !foreign.Modules.IsModuleLoaded(Unicode));

        object code = typeof(PreparedBeamImage).GetProperty(Code, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(image)!;
        object structure = code.GetType().GetProperty(Structure)!.GetValue(code)!;
        var atoms = (IReadOnlyList<string?>)structure.GetType().GetProperty(Atoms)!.GetValue(structure)!;
        int trap = Enumerable.Range(1, atoms.Count - 1).Single(index => atoms[index] == UnicodeTrap);
        byte[] exportBytes = Chunk(bytes, ExportChunk);
        int count = BinaryPrimitives.ReadInt32BigEndian(exportBytes);
        var retained = new List<byte>();
        int removed = 0;
        for (int index = 0; index < count; index++)
        {
            ReadOnlySpan<byte> row = exportBytes.AsSpan(4 + index * 12, 12);
            if (BinaryPrimitives.ReadInt32BigEndian(row) == trap && BinaryPrimitives.ReadInt32BigEndian(row[4..]) == 2)
            {
                removed++;
                continue;
            }
            retained.AddRange(row.ToArray());
        }
        Require(removed == 1);
        byte[] replacement = new byte[4 + retained.Count];
        BinaryPrimitives.WriteInt32BigEndian(replacement, count - 1);
        retained.CopyTo(replacement, 4);
        byte[] malformed = BeamMetadataModeledBytes.WithChunk(bytes, ExportChunk, replacement);
        refused = false;
        try
        {
            _ = foreign.Modules.PrepareBeam(malformed, dependencies);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            refused = true;
        }
        Require(refused && !foreign.Modules.IsModuleLoaded(Unicode));
    }

    private static byte[] Chunk(byte[] image, string name)
    {
        for (int cursor = 12; cursor < image.Length;)
        {
            int size = BinaryPrimitives.ReadInt32BigEndian(image.AsSpan(cursor + 4, 4));
            if (Encoding.ASCII.GetString(image, cursor, 4) == name) return image.AsSpan(cursor + 8, size).ToArray();
            cursor += 8 + ((size + 3) & ~3);
        }
        throw new InvalidDataException(Failure);
    }

    private static IReadOnlyDictionary<string, string> ManifestDigests()
    {
        using JsonDocument document = JsonDocument.Parse(Raw(Manifest));
        Require(document.RootElement.ValueKind == JsonValueKind.Object);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty item in document.RootElement.EnumerateObject())
        {
            string value = item.Value.GetString() ?? throw new InvalidDataException(Failure);
            Require(item.Name != Manifest && item.Name.Length > 0 && value.Length == 64 && value.All(char.IsAsciiHexDigit));
            result.Add(item.Name, value);
        }
        foreach (var item in result)
            Require(Convert.ToHexString(SHA256.HashData(Raw(item.Key))).Equals(item.Value, StringComparison.OrdinalIgnoreCase));

        return result;
    }

    private static byte[] Read(string name)
    {
        byte[] bytes = Raw(name);
        Require(Digests.Value.TryGetValue(name, out string? expected) && Convert.ToHexString(SHA256.HashData(bytes)).Equals(expected, StringComparison.OrdinalIgnoreCase));

        return bytes;
    }

    private static byte[] Raw(string name)
    {
        using Stream stream = typeof(LibraryDependencyOriginalHostChecks).Assembly.GetManifestResourceStream(Prefix + name) ?? throw new InvalidDataException(Failure + ": " + name);
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}

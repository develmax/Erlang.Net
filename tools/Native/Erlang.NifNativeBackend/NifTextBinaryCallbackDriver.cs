using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class NifTextBinaryCallbackDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(arguments.Length == 4 && OperatingSystem.IsWindows());
        string admissionPath = Path.GetFullPath(arguments[0]);
        Verify(admissionPath, arguments[1]);
        NifTextBinaryCallbackAdmission admission = JsonSerializer.Deserialize<NifTextBinaryCallbackAdmission>(File.ReadAllBytes(admissionPath))
            ?? throw new InvalidOperationException(NifTextBinaryCallbackNames.Broken);
        Require(admission.Status == NifTextBinaryCallbackNames.Admission);
        Verify(admission.BuildReceiptPath, admission.BuildReceiptSha256);
        Verify(admission.NativeSourcePath, admission.NativeSourceSha256);
        Verify(admission.ErlangSourcePath, admission.ErlangSourceSha256);
        Verify(admission.LibraryPath, admission.LibrarySha256);
        NifTextBinaryCallbackBuildReceipt build = JsonSerializer.Deserialize<NifTextBinaryCallbackBuildReceipt>(File.ReadAllBytes(admission.BuildReceiptPath))
            ?? throw new InvalidOperationException(NifTextBinaryCallbackNames.Broken);
        Require(build.Status == NifTextBinaryCallbackNames.Build && build.Platform == NifTextBinaryCallbackNames.Platform);
        Require(build.BuilderSourceFreezeSha256 == admission.BuilderFreezeSha256);
        Require(build.SourceManifestSha256 == admission.FixtureManifestSha256 && build.Images.Length == 1);
        NifTextBinaryCallbackImage image = build.Images.Single();
        Require(Path.GetFullPath(image.Path) == Path.GetFullPath(admission.LibraryPath));
        Require(image.Sha256 == admission.LibrarySha256 && image.SourceSha256 == admission.NativeSourceSha256);
        string route = arguments[3];
        Require(route is NifTextBinaryCallbackNames.Reviewed or NifTextBinaryCallbackNames.Ordinary);
        string output = Path.GetFullPath(arguments[2]);
        Require(!Directory.Exists(output) && admission.LibraryPath.EndsWith(NifTextBinaryCallbackNames.LibrarySuffix, StringComparison.OrdinalIgnoreCase));
        Directory.CreateDirectory(output);
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(admission.ErlangSourcePath));
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        ModuleCodeImage ownedImage = runtime.Modules.CurrentCodeImage(NifTextBinaryCallbackNames.Module)
            ?? throw new InvalidOperationException(NifTextBinaryCallbackNames.Broken);
        if (route == NifTextBinaryCallbackNames.Reviewed)
        {
            runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
            {
                Require(path == admission.LibraryPath);

                return new NifNativeReviewedBasicAbi(
                    admission.LibraryPath,
                    admission.LibrarySha256,
                    admission.BuildReceiptSha256
                );
            });
        }
        Term? observed = null;
        try
        {
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                string stem = admission.LibraryPath[..^4];
                Term loaded = await Invoke(
                    compiled.Definition,
                    context,
                    NifTextBinaryCallbackNames.Load,
                    [Term.String(stem)]
                );
                File.WriteAllBytes(Path.Combine(output, NifTextBinaryCallbackNames.LoadRaw), ExternalTermFormat.Encode(loaded));
                Require(loaded.Equals(Term.A(NifTextBinaryCallbackNames.Ok)));
                observed = await Invoke(
                    compiled.Definition,
                    context,
                    NifTextBinaryCallbackNames.Run,
                    []
                );
                File.WriteAllBytes(Path.Combine(output, NifTextBinaryCallbackNames.Raw), ExternalTermFormat.Encode(observed));
                Require(observed is TupleTerm { Items.Count: 3 } result && result.Items[0].Equals(Term.A(NifTextBinaryCallbackNames.Result)));
                var actual = (TupleTerm)observed;
                IReadOnlyList<Term> rows = Cons.Items(actual.Items[1]).ToArray();
                Require(rows.Count == NifTextBinaryCallbackNames.Cases.Length);
                for (int index = 0; index < rows.Count; index++)
                {
                    Require(rows[index] is TupleTerm { Items.Count: 2 } row && row.Items[0].Equals(Term.A(NifTextBinaryCallbackNames.Cases[index])));
                    File.WriteAllBytes(
                        Path.Combine(output, NifTextBinaryCallbackNames.Cases[index] + NifTextBinaryCallbackNames.TermSuffix),
                        ExternalTermFormat.Encode(rows[index])
                    );
                }
                Require(actual.Items[2].Equals(Term.List(
                    Term.I(4),
                    Term.I(4),
                    Term.I(8),
                    Term.I(40)
                )));


            }
            );
        }
        finally
        {
            await ownedImage.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
        }
        Verify(admission.LibraryPath, admission.LibrarySha256);
        Require(observed is not null);
        File.WriteAllText(
            Path.Combine(output, NifTextBinaryCallbackNames.Completion),
            JsonSerializer.Serialize(new
            {
                Status = NifTextBinaryCallbackNames.Retired,
                Route = route,
                AdmissionSha256 = arguments[1],
                admission.BuildReceiptSha256,
                admission.FixtureManifestSha256,
                admission.NativeSourceSha256,
                admission.ErlangSourceSha256,
                admission.LibrarySha256,
                ReturnedSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, NifTextBinaryCallbackNames.Raw)))),
                ObservationCount = NifTextBinaryCallbackNames.Cases.Length,
                ObservationShapeOnly = true,
                NoExpectedResultSeededFromManaged = true,
                Widths = new[] { 4, 4, 8, 40 },
                OriginalComparisonExecuted = false
            })
        );
    }

    private static ValueTask<Term> Invoke(
        ModuleDefinition module,
        ProcessContext context,
        string name,
        IReadOnlyList<Term> arguments
    ) => Execution.InvokeAsync(
        module,
        name,
        context,
        arguments
    );

    private static void Verify(string path, string expected)
        => Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == expected);

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifTextBinaryCallbackNames.Broken);
    }
}

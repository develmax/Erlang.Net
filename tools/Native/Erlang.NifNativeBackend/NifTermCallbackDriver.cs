using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class NifTermCallbackDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(arguments.Length == 4 && OperatingSystem.IsWindows());
        string admissionPath = Path.GetFullPath(arguments[0]);
        Verify(admissionPath, arguments[1]);
        NifTermCallbackAdmission admission = JsonSerializer.Deserialize<NifTermCallbackAdmission>(File.ReadAllBytes(admissionPath))
            ?? throw new InvalidOperationException(NifTermCallbackNames.Broken);
        Require(admission.Status == NifTermCallbackNames.Admission);
        Verify(admission.BuildReceiptPath, admission.BuildReceiptSha256);
        Verify(admission.NativeSourcePath, admission.NativeSourceSha256);
        Verify(admission.ErlangSourcePath, admission.ErlangSourceSha256);
        Verify(admission.LibraryPath, admission.LibrarySha256);
        NifTermCallbackBuildReceipt build = JsonSerializer.Deserialize<NifTermCallbackBuildReceipt>(File.ReadAllBytes(admission.BuildReceiptPath))
            ?? throw new InvalidOperationException(NifTermCallbackNames.Broken);
        Require(build.Status == NifTermCallbackNames.Build && build.Platform == NifTermCallbackNames.Platform);
        Require(build.BuilderSourceFreezeSha256 == admission.BuilderFreezeSha256);
        Require(build.SourceManifestSha256 == admission.FixtureManifestSha256 && build.Images.Length == 1);
        NifTermCallbackImage image = build.Images.Single();
        Require(Path.GetFullPath(image.Path) == Path.GetFullPath(admission.LibraryPath));
        Require(image.Sha256 == admission.LibrarySha256 && image.SourceSha256 == admission.NativeSourceSha256);
        string route = arguments[3];
        Require(route is NifTermCallbackNames.Reviewed or NifTermCallbackNames.Ordinary);
        string output = Path.GetFullPath(arguments[2]);
        Require(!Directory.Exists(output) && admission.LibraryPath.EndsWith(NifTermCallbackNames.LibrarySuffix, StringComparison.OrdinalIgnoreCase));
        Directory.CreateDirectory(output);
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(admission.ErlangSourcePath));
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        ModuleCodeImage ownedImage = runtime.Modules.CurrentCodeImage(NifTermCallbackNames.Module)
            ?? throw new InvalidOperationException(NifTermCallbackNames.Broken);
        if (route == NifTermCallbackNames.Reviewed)
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
                    NifTermCallbackNames.Load,
                    [Term.String(stem)]
                );
                File.WriteAllBytes(Path.Combine(output, NifTermCallbackNames.LoadRaw), ExternalTermFormat.Encode(loaded));
                Require(loaded.Equals(Term.A(NifTermCallbackNames.Ok)));
                Term[] samples = NifTermCallbackSamples.Values(context);
                var encoding = new EtfEncodingOptions { FunctionOwner = context.Self };
                Require(samples.Length == NifTermCallbackNames.Cases.Length);
                var rows = new List<Term>();
                for (int index = 0; index < samples.Length; index++)
                {
                    Term expected = NifTermCallbackSamples.Expected(index, samples[index]);
                    Term actual = await Invoke(
                        compiled.Definition,
                        context,
                        NifTermCallbackNames.Inspect,
                        [samples[index]]
                    );
                    Term nullActual = await Invoke(
                        compiled.Definition,
                        context,
                        NifTermCallbackNames.InspectNull,
                        [samples[index]]
                    );
                    rows.Add(Term.Tuple(
                        Term.A(NifTermCallbackNames.Cases[index]),
                        samples[index],
                        actual,
                        nullActual
                    ));
                    File.WriteAllBytes(
                        Path.Combine(output, NifTermCallbackNames.Cases[index] + NifTermCallbackNames.TermSuffix),
                        ExternalTermFormat.Encode(rows[^1], encoding)
                    );
                    Require(actual.Equals(expected) && nullActual.Equals(expected));
                }
                var arrays = new List<Term>();
                foreach (int count in new[] { 0, 3, NifTermCallbackNames.LargeCount })
                {
                    Term[] items = Enumerable.Range(0, count).Select(value => (Term)Term.I(value)).ToArray();
                    Term expected = Term.Tuple(Term.Tuple(items), Term.List(items));
                    Term actual = await Invoke(
                        compiled.Definition,
                        context,
                        NifTermCallbackNames.Arrays,
                        [Term.I(count)]
                    );
                    arrays.Add(Term.Tuple(Term.I(count), actual));
                    string filename = NifTermCallbackNames.ArrayPrefix + count.ToString(CultureInfo.InvariantCulture) + NifTermCallbackNames.TermSuffix;
                    File.WriteAllBytes(Path.Combine(output, filename), ExternalTermFormat.Encode(arrays[^1], encoding));
                    Require(actual.Equals(expected));
                }
                observed = Term.Tuple(Term.A(NifTermCallbackNames.Result), Cons.From(rows), Cons.From(arrays));
                File.WriteAllBytes(Path.Combine(output, NifTermCallbackNames.Raw), ExternalTermFormat.Encode(observed, encoding));
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
            Path.Combine(output, NifTermCallbackNames.Completion),
            JsonSerializer.Serialize(new
            {
                Status = NifTermCallbackNames.Retired,
                Route = route,
                AdmissionSha256 = arguments[1],
                admission.BuildReceiptSha256,
                ObservationCount = NifTermCallbackNames.Cases.Length,
                Arrays = new[] { 0, 3, NifTermCallbackNames.LargeCount },
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
            throw new InvalidOperationException(NifTermCallbackNames.Broken);
    }
}

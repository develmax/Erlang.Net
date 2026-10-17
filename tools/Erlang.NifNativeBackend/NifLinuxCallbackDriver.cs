using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class NifLinuxCallbackDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(OperatingSystem.IsLinux() && IntPtr.Size == 8 && arguments.Length == 4);
        string admissionPath = Path.GetFullPath(arguments[0]);
        Verify(admissionPath, arguments[1]);
        var admission = JsonSerializer.Deserialize<NifLinuxCallbackAdmission>(File.ReadAllBytes(admissionPath))
            ?? throw new InvalidOperationException(NifLinuxCallbackNames.Failure);
        Require(admission.Status == NifLinuxCallbackNames.Admission && admission.Images.Length == 2);
        Verify(admission.BridgePath, admission.BridgeSha256);
        Verify(admission.BridgeReceiptPath, admission.BridgeReceiptSha256);
        using var bridgeProof = JsonDocument.Parse(File.ReadAllBytes(admission.BridgeReceiptPath));
        Require(bridgeProof.RootElement.GetProperty(NifLinuxCallbackNames.StatusProperty).GetString() == NifLinuxCallbackNames.BridgeBuildStatus);
        Require(bridgeProof.RootElement.GetProperty(NifLinuxCallbackNames.SubjectHeadProperty).GetString() == admission.SubjectHead);
        Require(bridgeProof.RootElement.GetProperty(NifLinuxCallbackNames.BridgeHashProperty).GetString() == admission.BridgeSha256);
        foreach (var image in admission.Images)
        {
            Verify(image.LibraryPath, image.LibrarySha256);
            Verify(image.NativeSourcePath, image.NativeSourceSha256);
            Verify(image.ErlangSourcePath, image.ErlangSourceSha256);
            Verify(image.ManifestPath, image.ManifestSha256);
            Verify(image.BuildReceiptPath, image.BuildReceiptSha256);
            using var buildProof = JsonDocument.Parse(File.ReadAllBytes(image.BuildReceiptPath));
            Require(buildProof.RootElement.GetProperty(NifLinuxCallbackNames.StatusProperty).GetString() == NifLinuxCallbackNames.NativeBuildStatus);
            Require(buildProof.RootElement.GetProperty(NifLinuxCallbackNames.SubjectHeadProperty).GetString() == admission.SubjectHead);
            Require(buildProof.RootElement.GetProperty(NifLinuxCallbackNames.SourceManifestHashProperty).GetString() == image.ManifestSha256);
            var nativeImage = buildProof.RootElement.GetProperty(NifLinuxCallbackNames.ImagesProperty).EnumerateArray().Single();
            Require(nativeImage.GetProperty(NifLinuxCallbackNames.ImageHashProperty).GetString() == image.LibrarySha256);
            Require(nativeImage.GetProperty(NifLinuxCallbackNames.ImageSourceHashProperty).GetString() == image.NativeSourceSha256);
        }
        Require(admission.Images.Select(image => image.Module).Distinct().Count() == 2);
        Require(admission.Images.Sum(image => image.ObservationNames.Length) == NifLinuxCallbackNames.ObservationCount);
        string output = Path.GetFullPath(arguments[2]);
        string route = arguments[3];
        Require(route is NifLinuxCallbackNames.Reviewed or NifLinuxCallbackNames.Ordinary);
        Require(!Directory.Exists(output));
        if (route == NifLinuxCallbackNames.Ordinary)
            Verify(Path.Combine(AppContext.BaseDirectory, NifLinuxCallbackNames.PackagedBridge), admission.BridgeSha256);
        Directory.CreateDirectory(output);
        int arrived = 0;
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task Domain(string node, string leaf)
        {
            string destination = Path.Combine(output, leaf);
            Directory.CreateDirectory(destination);
            await using var runtime = new ProcessRuntime(node);
            object identity = typeof(ProcessRuntime).GetProperty(NifLinuxCallbackNames.LocalIdentityProperty, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
            identity.GetType().GetMethod(NifLinuxCallbackNames.PublishIdentityMethod, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(identity, [node, (uint)1]);
            Require(runtime.Node == node);
            var compiled = admission.Images.Select(image => GeneratedModuleCompiler.Compile(File.ReadAllText(image.ErlangSourcePath))).ToArray();
            var owned = new List<ModuleCodeImage>();
            try
            {
                foreach (var module in compiled)
                    module.Definition.Register(runtime.Modules);
                if (route == NifLinuxCallbackNames.Reviewed)
                    runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
                    {
                        var image = admission.Images.Single(image => image.LibraryPath == path);

                        return new NifNativeReviewedBasicAbi(
                            path,
                            image.LibrarySha256,
                            image.BuildReceiptSha256,
                            admission.BridgePath,
                            admission.BridgeSha256
                        );
                    });
                await NifNativeHarness.Context(
                    runtime,
                    async context =>
                {
                    // Load the reference image first in each domain. No shared/static allocator
                    // can supply its foreign-thread independent environment from another runtime.
                    foreach (var image in admission.Images.OrderBy(image => image.Module == NifLinuxCallbackNames.Module ? 0 : 1))
                    {
                        var module = compiled.Single(module => module.Definition.Name == image.Module).Definition;
                        Term loaded = await Invoke(
                            destination,
                            module,
                            NifLinuxCallbackNames.Load,
                            context,
                            [Term.String(image.LibraryPath[..^3])]
                        );
                        File.WriteAllBytes(Path.Combine(destination, image.Module + NifLinuxCallbackNames.LoadTermSuffix), ExternalTermFormat.Encode(loaded));
                        Require(loaded.Equals(Term.A(NifLinuxCallbackNames.Ok)));
                        owned.Add(runtime.Modules.CurrentCodeImage(image.Module) ?? throw new InvalidOperationException(NifLinuxCallbackNames.Failure));
                    }
                    if (Interlocked.Increment(ref arrived) == NifLinuxCallbackNames.DomainCount)
                        ready.TrySetResult();
                    await ready.Task.WaitAsync(NifLinuxCallbackNames.Deadline);
                    var referenceModule = compiled.Single(module => module.Definition.Name == NifLinuxCallbackNames.Module).Definition;
                    Term foreign = await Invoke(
                        destination,
                        referenceModule,
                        NifLinuxCallbackNames.Independent,
                        context,
                        [Term.I(NifLinuxCallbackNames.ForeignAllocationMode)]
                    );
                    File.WriteAllBytes(Path.Combine(destination, NifLinuxCallbackNames.ForeignAllocationTerm), ExternalTermFormat.Encode(foreign));
                    Term[] references = Cons.Items(foreign).ToArray();
                    Require(references.Length == 3 && references.All(term => term is ReferenceTerm reference && reference.Node == runtime.Node));
                    Require(!references[0].Equals(references[1]) && references[0].Equals(references[2]));
                    foreach (var image in admission.Images)
                    {
                        var module = compiled.Single(module => module.Definition.Name == image.Module).Definition;
                        Term observed = await Invoke(
                            destination,
                            module,
                            NifLinuxCallbackNames.Run,
                            context,
                            []
                        );
                        File.WriteAllBytes(Path.Combine(destination, image.Module + NifLinuxCallbackNames.TermSuffix), ExternalTermFormat.Encode(observed));
                        Require(observed is TupleTerm { Items.Count: 3 });
                        var tuple = (TupleTerm)observed;
                        Require(tuple.Items[0].Equals(Term.A(image.Module)));
                        Term[] rows = Cons.Items(tuple.Items[1]).ToArray();
                        Require(rows.Length == image.ObservationNames.Length);
                        for (int index = 0; index < rows.Length; index++)
                            Require(rows[index] is TupleTerm { Items.Count: 2 } row && row.Items[0].Equals(Term.A(image.ObservationNames[index])));
                        Require(tuple.Items[2].Equals(Term.List(NifLinuxCallbackNames.Widths.Select(value => Term.I(value)).ToArray())));
                    }
                }
                );
            }
            finally
            {
                foreach (var image in owned.AsEnumerable().Reverse())
                    await image.NifBindings.RetireAsync().WaitAsync(NifLinuxCallbackNames.Deadline);
                foreach (var module in compiled)
                    module.Dispose();
            }
        }
        await Task.WhenAll(
            Domain(NifLinuxCallbackNames.FirstNode, NifLinuxCallbackNames.First),
            Domain(NifLinuxCallbackNames.SecondNode, NifLinuxCallbackNames.Second)
        );
        Verify(admissionPath, arguments[1]);
        Verify(admission.BridgePath, admission.BridgeSha256);
        foreach (var image in admission.Images)
            Verify(image.LibraryPath, image.LibrarySha256);
        File.WriteAllText(
            Path.Combine(output, NifLinuxCallbackNames.Completion),
            JsonSerializer.Serialize(new
            {
                Status = NifLinuxCallbackNames.AcquiredStatus,
                admission.SubjectHead,
                AdmissionSha256 = arguments[1],
                Route = route,
                PrimaryObservationCount = NifLinuxCallbackNames.ObservationCount,
                RuntimeDomains = NifLinuxCallbackNames.DomainCount,
                FirstForeignAllocationOwnershipChecked = true,
                Widths = NifLinuxCallbackNames.Widths,
                OriginalComparisonExecuted = false,
                AllImageRetirementsCompleted = true,
                RuntimeDisposalsCompleted = true,
                Files = Directory.GetFiles(output, NifLinuxCallbackNames.TermPattern, SearchOption.AllDirectories).Order().Select(path => new { Path = Path.GetRelativePath(output, path), Sha256 = Hash(path) }).ToArray()
            })
        );
    }

    private static async Task<Term> Invoke(
        string destination,
        ModuleDefinition module,
        string function,
        ProcessContext context,
        IReadOnlyList<Term> arguments
    )
    {
        try
        {
            return await Execution.InvokeAsync(
                module,
                function,
                context,
                arguments
            );
        }
        catch (ErlangException failure)
        {
            File.WriteAllBytes(
                Path.Combine(destination, module.Name + NifLinuxCallbackNames.InvocationSeparator + function + NifLinuxCallbackNames.RaisedTermSuffix),
                ExternalTermFormat.Encode(Term.Tuple(Term.A(failure.ExceptionClass), failure.Reason, failure.StackTraceTerm))
            );

            throw;
        }
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Verify(string path, string hash) => Require(Hash(path) == hash);

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifLinuxCallbackNames.Failure);
    }
}

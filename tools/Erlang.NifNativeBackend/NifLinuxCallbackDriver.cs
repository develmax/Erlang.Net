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
        Require(bridgeProof.RootElement.GetProperty(NifLinuxCallbackNames.SubjectHeadProperty).GetString() == (admission.BridgeSubjectHead ?? admission.SubjectHead));
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
        int foreignArrived = 0;
        var foreignReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Domain(string node, string leaf)
        {
            string destination = Path.Combine(output, leaf);
            Directory.CreateDirectory(destination);
            var runtime = new ProcessRuntime(node);
            await using var diagnosticRuntime = new NifLinuxCallbackDiagnosticRuntime(runtime, destination);
            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.RuntimeCreated);
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

                        return new NifNativeReviewedBasicAbi(path, image.LibrarySha256, image.BuildReceiptSha256, admission.BridgePath, admission.BridgeSha256);
                    });
                await NifNativeHarness.Context(runtime, async context =>
                {
                    // Load the reference image first in each domain. No shared/static allocator
                    // can supply its foreign-thread independent environment from another runtime.
                    foreach (var image in admission.Images.OrderBy(image => image.Module == NifLinuxCallbackNames.Module ? 0 : 1))
                    {
                        var module = compiled.Single(module => module.Definition.Name == image.Module).Definition;
                        Term loaded = await Invoke(destination, module, NifLinuxCallbackNames.Load, context, [Term.String(image.LibraryPath[..^3])]);
                        File.WriteAllBytes(Path.Combine(destination, image.Module + NifLinuxCallbackNames.LoadTermSuffix), ExternalTermFormat.Encode(loaded));
                        Require(loaded.Equals(Term.A(NifLinuxCallbackNames.Ok)));
                        owned.Add(runtime.Modules.CurrentCodeImage(image.Module) ?? throw new InvalidOperationException(NifLinuxCallbackNames.Failure));
                    }
                    if (Interlocked.Increment(ref arrived) == NifLinuxCallbackNames.DomainCount)
                        ready.TrySetResult();
                    await ready.Task.WaitAsync(NifLinuxCallbackNames.Deadline);
                    var referenceModule = compiled.Single(module => module.Definition.Name == NifLinuxCallbackNames.Module).Definition;
                    Term widths = await Invoke(destination, referenceModule, NifLinuxCallbackTraceNames.Widths, context, []);
                    File.WriteAllBytes(Path.Combine(destination, NifLinuxCallbackTraceNames.WidthsTerm), ExternalTermFormat.Encode(widths));
                    var mapModule = compiled.Single(module => module.Definition.Name == NifLinuxCallbackTraceNames.MapModule).Definition;
                    Term[] preflightValues = [new MapTerm([]), new MapTerm([new KeyValuePair<Term, Term>(Term.I(1), Term.I(2))]), Term.I(42)];
                    for (int mode = 0; mode < preflightValues.Length; mode++)
                    {
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.PreflightPrefix + mode, NifLinuxCallbackTraceNames.MapModule, NifLinuxCallbackTraceNames.Observe);
                        Term diagnostic = await Invoke(destination, mapModule, NifLinuxCallbackTraceNames.Observe, context, [Term.I(mode), preflightValues[mode]]);
                        File.WriteAllBytes(Path.Combine(destination, NifLinuxCallbackTraceNames.PreflightPrefix + mode + NifLinuxCallbackTraceNames.PreflightSuffix), ExternalTermFormat.Encode(diagnostic));
                    }
                    Term foreign = await Invoke(destination, referenceModule, NifLinuxCallbackNames.Independent, context, [Term.I(NifLinuxCallbackNames.ForeignAllocationMode)]);
                    File.WriteAllBytes(Path.Combine(destination, NifLinuxCallbackNames.ForeignAllocationTerm), ExternalTermFormat.Encode(foreign));
                    Term[] references = Cons.Items(foreign).ToArray();
                    Require(references.Length == 3 && references.All(term => term is ReferenceTerm reference && reference.Node == runtime.Node));
                    Require(!references[0].Equals(references[1]) && references[0].Equals(references[2]));
                    NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.ForeignValidated);
                    NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.ForeignBarrier);
                    if (Interlocked.Increment(ref foreignArrived) == NifLinuxCallbackNames.DomainCount)
                    {
                        try
                        {
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.CollectFirstBefore);
                            GC.Collect();
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.CollectFirstAfter);
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.FinalizersBefore);
                            GC.WaitForPendingFinalizers();
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.FinalizersAfter);
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.CollectSecondBefore);
                            GC.Collect();
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.CollectSecondAfter);
                            foreignReady.TrySetResult();
                        }
                        catch (Exception failure)
                        {
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.CollectionFailed, failure: failure);
                            foreignReady.TrySetException(failure);
                            throw;
                        }
                    }
                    await foreignReady.Task.WaitAsync(NifLinuxCallbackNames.Deadline);
                    foreach (int count in NifLinuxCallbackTraceNames.MapSizes)
                    {
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.MapConstructionBefore + count, mapModule.Name, NifLinuxCallbackTraceNames.MakeMap);
                        Term map = await Invoke(destination, mapModule, NifLinuxCallbackTraceNames.MakeMap, context, [Term.I(count)]);
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.MapConstructionAfter + count, mapModule.Name, NifLinuxCallbackTraceNames.MakeMap);
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.DirectSizeBefore + count, mapModule.Name, NifLinuxCallbackTraceNames.Observe);
                        Term sized = await Invoke(destination, mapModule, NifLinuxCallbackTraceNames.Observe, context, [Term.I(0), map]);
                        File.WriteAllBytes(Path.Combine(destination, NifLinuxCallbackTraceNames.DirectSizeFile + count + NifLinuxCallbackNames.TermSuffix), ExternalTermFormat.Encode(sized));
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.DirectSizeAfter + count, mapModule.Name, NifLinuxCallbackTraceNames.Observe);
                    }
                    Term independentMap = await Invoke(destination, mapModule, NifLinuxCallbackTraceNames.MakeMap, context, [Term.I(33)]);
                    bool retained = false;
                    try
                    {
                        for (int mode = 3; mode <= 4; mode++)
                        {
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.IndependentBefore + mode, mapModule.Name, NifLinuxCallbackTraceNames.Observe);
                            Term sized = await Invoke(destination, mapModule, NifLinuxCallbackTraceNames.Observe, context, [Term.I(mode), mode == 3 ? independentMap : Term.A(NifLinuxCallbackTraceNames.Unused)]);
                            if (mode == 3)
                                retained = true;
                            File.WriteAllBytes(Path.Combine(destination, NifLinuxCallbackTraceNames.IndependentFile + mode + NifLinuxCallbackNames.TermSuffix), ExternalTermFormat.Encode(sized));
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.IndependentAfter + mode, mapModule.Name, NifLinuxCallbackTraceNames.Observe);
                        }
                    }
                    catch (Exception failure)
                    {
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.BeforeCleanupFailure, mapModule.Name, NifLinuxCallbackTraceNames.Observe, failure);
                        throw;
                    }
                    finally
                    {
                        if (retained)
                        {
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.IndependentBefore + 5, mapModule.Name, NifLinuxCallbackTraceNames.Observe);
                            Term released = await Invoke(destination, mapModule, NifLinuxCallbackTraceNames.Observe, context, [Term.I(5), Term.A(NifLinuxCallbackTraceNames.Unused)]);
                            File.WriteAllBytes(Path.Combine(destination, NifLinuxCallbackTraceNames.IndependentFile + 5 + NifLinuxCallbackNames.TermSuffix), ExternalTermFormat.Encode(released));
                            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.IndependentAfter + 5, mapModule.Name, NifLinuxCallbackTraceNames.Observe);
                        }
                    }

                    foreach (var image in admission.Images)
                    {
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.FullRunBefore, image.Module, NifLinuxCallbackNames.Run);
                        var module = compiled.Single(module => module.Definition.Name == image.Module).Definition;
                        Term observed = await Invoke(destination, module, NifLinuxCallbackNames.Run, context, []);
                        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.FullRunAfter, image.Module, NifLinuxCallbackNames.Run);
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
                });
            }
            catch (Exception failure)
            {
                NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.BeforeCleanupFailure, failure: failure);
                throw;
            }
            finally
            {
                foreach (var image in owned.AsEnumerable().Reverse())
                {
                    NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.RetireBefore, image.Module);
                    await image.NifBindings.RetireAsync().WaitAsync(NifLinuxCallbackNames.Deadline);
                    NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.RetireAfter, image.Module);
                }
                foreach (var module in compiled)
                {
                    NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.ModuleDisposeBefore, module.Definition.Name);
                    module.Dispose();
                    NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.ModuleDisposeAfter);
                }
            }
        }
        await Task.WhenAll(Domain(NifLinuxCallbackNames.FirstNode, NifLinuxCallbackNames.First), Domain(NifLinuxCallbackNames.SecondNode, NifLinuxCallbackNames.Second));
        Verify(admissionPath, arguments[1]);
        Verify(admission.BridgePath, admission.BridgeSha256);
        foreach (var image in admission.Images)
            Verify(image.LibraryPath, image.LibrarySha256);
        File.WriteAllText(Path.Combine(output, NifLinuxCallbackNames.Completion), JsonSerializer.Serialize(new
        {
            Status = NifLinuxCallbackNames.AcquiredStatus,
            admission.SubjectHead,
            AdmissionSha256 = arguments[1],
            Route = route,
            PrimaryObservationCount = NifLinuxCallbackNames.ObservationCount,
            DiagnosticOnly = false,
            OriginalSeventeenNotExecuted = false,
            PreflightNumericCallsPerRuntime = 3,
            ForcedGcBarrierExecuted = true,
            RuntimeDomains = NifLinuxCallbackNames.DomainCount,
            FirstForeignAllocationOwnershipChecked = true,
            Widths = NifLinuxCallbackNames.Widths,
            OriginalComparisonExecuted = false,
            AllImageRetirementsCompleted = true,
            RuntimeDisposalsCompleted = true,
            Files = Directory.GetFiles(output, NifLinuxCallbackNames.TermPattern, SearchOption.AllDirectories).Order().Select(path => new { Path = Path.GetRelativePath(output, path), Sha256 = Hash(path) }).ToArray()
        }));
    }

    private static async Task<Term> Invoke(
        string destination,
        ModuleDefinition module,
        string function,
        ProcessContext context,
        IReadOnlyList<Term> arguments
    )
    {
        NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.InvokeBefore, module.Name, function);
        try
        {
            Term result = await Execution.InvokeAsync(module, function, context, arguments);
            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.InvokeAfter, module.Name, function);
            object? nativeDomain = typeof(ProcessRuntime).GetField(NifLinuxPthreadProbeNames.DomainField, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(context.Runtime);
            if (nativeDomain is not null)
            {
                string? witness = nativeDomain.GetType().GetProperty(NifLinuxPthreadProbeNames.WitnessProperty, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(nativeDomain) as string;
                File.WriteAllText(Path.Combine(destination, NifLinuxPthreadProbeNames.WitnessFile), witness ?? throw new InvalidOperationException(NifLinuxCallbackNames.Failure));
            }

            return result;
        }
        catch (ErlangException failure)
        {
            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.InvokeFailed, module.Name, function, failure);
            File.WriteAllBytes(
                Path.Combine(destination, module.Name + NifLinuxCallbackNames.InvocationSeparator + function + NifLinuxCallbackNames.RaisedTermSuffix),
                ExternalTermFormat.Encode(Term.Tuple(Term.A(failure.ExceptionClass), failure.Reason, failure.StackTraceTerm))
            );

            throw;
        }
        catch (Exception failure)
        {
            NifLinuxCallbackTrace.Mark(destination, NifLinuxCallbackTraceNames.InvokeFailed, module.Name, function, failure);
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

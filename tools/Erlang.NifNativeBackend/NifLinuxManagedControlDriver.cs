using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class NifLinuxManagedControlDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(OperatingSystem.IsLinux() && IntPtr.Size == 8 && arguments.Length == 3);
        string admissionPath = Path.GetFullPath(arguments[0]);
        Verify(admissionPath, arguments[1]);
        var admission = JsonSerializer.Deserialize<NifLinuxCallbackAdmission>(File.ReadAllBytes(admissionPath))
            ?? throw new InvalidOperationException(NifLinuxManagedControlNames.Failure);
        Require(admission.Status == NifLinuxCallbackNames.Admission);
        var image = admission.Images.Single(image => image.Module == NifLinuxManagedControlNames.Module);
        Verify(image.ManifestPath, image.ManifestSha256);
        Verify(image.ErlangSourcePath, image.ErlangSourceSha256);
        string source = File.ReadAllText(image.ErlangSourcePath);
        string output = Path.GetFullPath(arguments[2]);
        Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        int[] arrived = new int[NifLinuxManagedControlNames.Counts.Length];
        var ready = arrived.Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();

        async Task<object> Domain(string node, string leaf)
        {
            string destination = Path.Combine(output, leaf);
            Directory.CreateDirectory(destination);
            var runtime = new ProcessRuntime(node);
            GeneratedModuleArtifact? compiled = null;
            var rows = new List<object>();
            try
            {
                object identity = typeof(ProcessRuntime).GetProperty(NifLinuxManagedControlNames.LocalIdentity, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
                identity.GetType().GetMethod(NifLinuxManagedControlNames.Publish, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(identity, [node, (uint)1]);
                Require(runtime.Node == node);
                compiled = GeneratedModuleCompiler.Compile(source);
                Require(compiled.Definition.Name == NifLinuxManagedControlNames.Module);
                var module = compiled.Definition;
                module.Register(runtime.Modules);
                await NifNativeHarness.Context(runtime, async context =>
                {
                    for (int index = 0; index < NifLinuxManagedControlNames.Counts.Length; index++)
                    {
                        int requested = NifLinuxManagedControlNames.Counts[index];
                        if (Interlocked.Increment(ref arrived[index]) == NifLinuxManagedControlNames.Domains)
                            ready[index].TrySetResult();
                        await ready[index].Task.WaitAsync(NifLinuxManagedControlNames.Deadline);
                        Journal(destination, new { Event = NifLinuxManagedControlNames.Before, Node = node, Iteration = index, Requested = requested });
                        Term result = await Execution.InvokeAsync(module, NifLinuxManagedControlNames.Function, context, [Term.I(requested)]);
                        int count = result is MapTerm map ? map.Entries.Count : -1;
                        var row = new { Event = NifLinuxManagedControlNames.After, Node = node, Iteration = index, Requested = requested, Actual = count, Type = result.GetType().Name };
                        Journal(destination, row);
                        Require(result is MapTerm && count == requested);
                        rows.Add(row);
                    }
                });
                return new { Node = node, CSharpSha256 = compiled.CSharpSha256, Rows = rows.ToArray() };
            }
            catch (Exception failure)
            {
                Journal(destination, new { Event = NifLinuxManagedControlNames.Failed, Node = node, Error = failure.ToString() });
                throw;
            }
            finally
            {
                await runtime.DisposeAsync();
                compiled?.Dispose();
                Journal(destination, new { Event = NifLinuxManagedControlNames.Disposed, Node = node });
            }
        }

        object[] domains = await Task.WhenAll(
            Task.Run(() => Domain(NifLinuxManagedControlNames.FirstNode, NifLinuxManagedControlNames.First)),
            Task.Run(() => Domain(NifLinuxManagedControlNames.SecondNode, NifLinuxManagedControlNames.Second))
        );
        Verify(admissionPath, arguments[1]);
        Verify(image.ManifestPath, image.ManifestSha256);
        Verify(image.ErlangSourcePath, image.ErlangSourceSha256);
        File.WriteAllText(Path.Combine(output, NifLinuxManagedControlNames.Completion), JsonSerializer.Serialize(new
        {
            Status = NifLinuxManagedControlNames.Acquired,
            admission.SubjectHead,
            AdmissionSha256 = arguments[1],
            image.ManifestSha256,
            image.ErlangSourceSha256,
            RuntimeDomains = NifLinuxManagedControlNames.Domains,
            NativeImagesLoaded = 0,
            ForeignWorkersCreated = 0,
            NativeEnvironmentsAllocated = 0,
            RuntimeDisposalsCompleted = true,
            Domains = domains
        }));
    }

    private static void Journal(string destination, object row)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(row) + Environment.NewLine);
        using var stream = new FileStream(Path.Combine(destination, NifLinuxManagedControlNames.Journal), FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
        stream.Write(bytes);
        stream.Flush(true);
    }

    private static void Verify(string path, string hash) => Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == hash);

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(NifLinuxManagedControlNames.Failure);
    }
}

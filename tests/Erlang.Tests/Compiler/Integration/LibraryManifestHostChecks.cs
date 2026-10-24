using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Erlang;

namespace Erlang.Tests;

internal static class LibraryManifestHostChecks
{
    internal static async Task Beam()
    {
        string root = Create();
        try
        {
            string path = Write(root, true, true);
            RuntimeLibrarySelection selection = RuntimeLibraryManifest.Load(path);
            File.WriteAllBytes(Path.Combine(root, LibraryManifestHostNames.ListImage), []);
            await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(selection);
            foreach (RuntimeLibraryModuleSnapshot module in runtime.LibrarySelection.Modules)
            {
                Require(module.Backend == RuntimeModuleBackend.Beam && module.InputSha256 is not null);
                Require(ReferenceEquals(module.Image, runtime.Modules.CurrentCodeImage(module.Module)) && !runtime.Modules.HasOldCode(module.Module));
                Require(module.Exports.All(export => runtime.Modules.SourceOwner(module.Module, export.Name, export.Arity) is PreparedBeamImage));
            }
            await Observe(runtime);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    internal static async Task Mixed()
    {
        string root = Create();
        try
        {
            foreach ((bool lists, bool queue) in new[] { (false, false), (false, true), (true, false) })
            {
                await using ProcessRuntime runtime = await ProcessRuntime.CreateAsync(RuntimeLibraryManifest.Load(Write(root, lists, queue)));
                Require(runtime.LibrarySelection.Modules.Single(value => value.Module == ListBeamHostNames.Module).Backend
                    == (lists ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed));
                Require(runtime.LibrarySelection.Modules.Single(value => value.Module == QueueModuleNames.Module).Backend
                    == (queue ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed));
                await Observe(runtime);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    internal static Task Invalid()
    {
        string root = Create();
        try
        {
            string path = Write(root, true, true);
            JsonObject original = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            var variants = new List<JsonObject>();
            JsonObject next = original.DeepClone().AsObject();
            next[LibraryManifestHostNames.VersionName] = RuntimeLibraryManifest.CurrentVersion + 1;
            variants.Add(next);
            next = original.DeepClone().AsObject();
            next[LibraryManifestHostNames.UnexpectedName] = true;
            variants.Add(next);
            next = original.DeepClone().AsObject();
            next[LibraryManifestHostNames.ModulesName]![0]!.AsObject().Remove(LibraryManifestHostNames.BackendName);
            variants.Add(next);
            next = original.DeepClone().AsObject();
            next[LibraryManifestHostNames.ModulesName]![0]![LibraryManifestHostNames.BackendName] = 1;
            variants.Add(next);
            foreach (JsonObject variant in variants)
            {
                File.WriteAllText(path, variant.ToJsonString());
                Reject(() => RuntimeLibraryManifest.Load(path));
            }
            foreach (string text in new[] { LibraryManifestHostNames.EmptyManifest, LibraryManifestHostNames.DuplicateVersion })
            {
                File.WriteAllText(path, text);
                Reject(() => RuntimeLibraryManifest.Load(path));
            }
            RuntimeLibraryManifestModule[] rows = Document(true, true).Modules;
            foreach (RuntimeLibraryManifestModule invalid in new[]
            {
                rows[0] with { Image = LibraryManifestHostNames.RelativeEscape },
                rows[0] with { Image = Path.Combine(root, LibraryManifestHostNames.ListImage) },
                rows[0] with { Sha256 = new string('0', SHA256.HashSizeInBytes * 2) },
                rows[0] with { Backend = RuntimeModuleBackend.Managed }
            })
            {
                Save(path, new(RuntimeLibraryManifest.CurrentVersion, [invalid, rows[1]]));
                Reject(() => RuntimeLibraryManifest.Load(path));
            }
            Save(path, new(RuntimeLibraryManifest.CurrentVersion, [rows[1]]));
            Reject(() => RuntimeLibraryManifest.Load(path));
            Save(path, new(RuntimeLibraryManifest.CurrentVersion, [rows[0], rows[0]]));
            Reject(() => RuntimeLibraryManifest.Load(path));
            Write(root, true, true);
            File.Delete(Path.Combine(root, LibraryManifestHostNames.QueueImage));
            Reject(() => RuntimeLibraryManifest.Load(path));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        return Task.CompletedTask;
    }

    internal static async Task NoFallback()
    {
        string root = Create();
        try
        {
            string path = Write(root, true, true);
            RuntimeLibraryManifestModule[] rows = Document(true, true).Modules;
            rows[0] = rows[0] with { Image = LibraryManifestHostNames.QueueImage, Sha256 = QueueBeamAdmissionHostNames.Sha256 };
            Save(path, new(RuntimeLibraryManifest.CurrentVersion, rows));
            bool refused = false;
            try
            {
                await using ProcessRuntime unexpected = await ProcessRuntime.CreateAsync(RuntimeLibraryManifest.Load(path));
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused);
            await using var ordinary = new ProcessRuntime();
            await Observe(ordinary);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string Create()
    {
        string root = Path.Combine(Path.GetTempPath(), LibraryManifestHostNames.RootPrefix + Guid.NewGuid());
        Directory.CreateDirectory(root);

        return root;
    }

    private static RuntimeLibraryManifestDocument Document(bool lists, bool queue)
        => new(
            RuntimeLibraryManifest.CurrentVersion,
            [
            new(
                ListBeamHostNames.Module,
                lists ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed,
                lists ? LibraryManifestHostNames.ListImage : null,
                lists ? ListBeamHostNames.Sha256 : null,
                []
            ),
            new(
                QueueModuleNames.Module,
                queue ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed,
                queue ? LibraryManifestHostNames.QueueImage : null,
                queue ? QueueBeamAdmissionHostNames.Sha256 : null,
                queue ? [ListBeamHostNames.Module] : []
            )
        ]
        );

    private static string Write(string root, bool lists, bool queue)
    {
        File.WriteAllBytes(Path.Combine(root, LibraryManifestHostNames.ListImage), LibrarySelectionHostData.Lists());
        File.WriteAllBytes(Path.Combine(root, LibraryManifestHostNames.QueueImage), LibrarySelectionHostData.Queue());
        string path = Path.Combine(root, RuntimeLibraryManifest.FileName);
        Save(path, Document(lists, queue));

        return path;
    }

    private static void Save(string path, RuntimeLibraryManifestDocument document)
        => File.WriteAllBytes(path, JsonSerializer.SerializeToUtf8Bytes(document, RuntimeLibraryManifest.SerializationOptions));

    private static async Task Observe(ProcessRuntime runtime)
    {
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.List(Term.I(3), Term.I(1), Term.I(2));
            Term queue = await runtime.Modules.Call(
                context,
                QueueModuleNames.Module,
                QueueModuleNames.FromList,
                input
            );
            Term output = await runtime.Modules.Call(
                context,
                QueueModuleNames.Module,
                QueueModuleNames.ToList,
                queue
            );
            Require(output.Equals(input));
            Term sorted = await runtime.Modules.Call(
                context,
                ListBeamHostNames.Module,
                ListBeamHostNames.Sort,
                output
            );
            Require(sorted.Equals(Term.List(Term.I(1), Term.I(2), Term.I(3))));

            return true;
        });
    }

    private static void Reject(Action action)
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (Exception error) when (error is InvalidDataException or JsonException or ArgumentException or IOException)
        {
            rejected = true;
        }
        Require(rejected);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(LibraryManifestHostNames.Failure);
    }
}

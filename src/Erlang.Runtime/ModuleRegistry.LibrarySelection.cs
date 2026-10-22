using System.Security.Cryptography;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal RuntimeLibraryPreparedSelection PrepareStartupLibraries(RuntimeLibrarySelection request)
        => PrepareStartupLibrariesCore(request, null);

    internal RuntimeLibraryPreparedSelection PrepareStartupLibrariesWithOnLoad(
        RuntimeLibrarySelection request,
        IReadOnlyList<string> onLoadOrder
    ) => PrepareStartupLibrariesCore(request, onLoadOrder);

    private RuntimeLibraryPreparedSelection PrepareStartupLibrariesCore(
        RuntimeLibrarySelection request,
        IReadOnlyList<string>? onLoadOrder
    )
    {
        var images = new Dictionary<string, PreparedBeamImage>(StringComparer.Ordinal);
        var bootstrap = new Dictionary<string, ModuleCodeImage>(StringComparer.Ordinal);
        foreach (RuntimeModuleSelection choice in request.Modules)
        {
            ModuleCodeImage? image = RequireStartupOwner(choice);
            if (image is not null)
                bootstrap.Add(choice.Module, image);
            if (choice.Backend != RuntimeModuleBackend.Beam)
                continue;
            try
            {
                PreparedBeamImage prepared = PrepareBeam(choice.OwnedImage, new BeamDependencySelection(choice.Dependencies.Append(choice.Module)));
                if (prepared.Name != choice.Module || prepared.HasOnLoad && onLoadOrder is null)
                    throw new InvalidDataException(LibrarySelectionNames.Request);
                foreach (var signature in functions.Keys.Where(key => key.Module == choice.Module))
                    if (!prepared.Code.Exports.Any(export => export.Name == signature.Function && export.Arity == signature.Arity))
                        throw new InvalidDataException(LibrarySelectionNames.MissingExport + LibrarySelectionNames.ContextSeparator
                            + choice.Module + LibrarySelectionNames.FunctionSeparator + signature.Function
                            + LibrarySelectionNames.AritySeparator + signature.Arity);
                if (image is not null && !LibrarySelectionNames.ManagedModules.Contains(choice.Module, StringComparer.Ordinal))
                    RequireStartupNativeBootstrap(choice.Module, prepared);
                images.Add(choice.Module, prepared);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(LibrarySelectionNames.Phase + LibrarySelectionNames.ContextSeparator + choice.Module, error);
            }
        }
        string[] callbacks = images.Values.Where(image => image.HasOnLoad).Select(image => image.Name).ToArray();
        if (onLoadOrder is not null && (callbacks.Length != onLoadOrder.Count
            || !callbacks.ToHashSet(StringComparer.Ordinal).SetEquals(onLoadOrder)))
            throw new InvalidOperationException(LibrarySelectionNames.OnLoadOrder);
        var result = new RuntimeLibraryPreparedSelection(
            request,
            images,
            bootstrap,
            onLoadOrder ?? Array.Empty<string>()
        );
        foreach (RuntimeModuleSelection choice in request.Modules.Where(value => value.Backend == RuntimeModuleBackend.Beam))
            foreach (BeamImport import in images[choice.Module].Code.Structure.Imports)
                ValidateStartupImport(
                    result,
                    choice,
                    images[choice.Module].Code,
                    import
                );

        return result;
    }

    private void ValidateStartupImport(
        RuntimeLibraryPreparedSelection plan,
        RuntimeModuleSelection caller,
        BeamLinkedCode code,
        BeamImport import
    )
    {
        string module = code.Structure.Atoms[import.ModuleAtomIndex]!;
        string function = code.Structure.Atoms[import.FunctionAtomIndex]!;
        if (module == ErlangModuleNames.Module && function == BeamPreparationNames.GetModuleInfo && import.Arity is 1 or 2)
            return;
        bool selectedBeam = plan.Images.TryGetValue(module, out PreparedBeamImage? target);
        bool native = selectedBeam
            ? target!.ExecutionIdentity.NativePrimitives.ContainsKey((function, import.Arity))
            : IsNativeEntry(module, function, import.Arity);
        if (native || IsRuntimeIntrinsic(module, function, import.Arity))
            return;
        bool self = caller.Module == module;
        bool declared = self || caller.Dependencies.Contains(module, StringComparer.Ordinal);
        bool chosen = plan.Request.Modules.Any(choice => choice.Module == module);
        bool exists = selectedBeam
            ? target!.Code.Exports.Any(export => export.Name == function && export.Arity == import.Arity)
            : functions.ContainsKey((module, function, import.Arity));
        if (!declared || !chosen || !exists)
            throw new InvalidOperationException(LibrarySelectionNames.Dependency + LibrarySelectionNames.ContextSeparator + caller.Module
                + LibrarySelectionNames.DependencyArrow + module + LibrarySelectionNames.FunctionSeparator + function
                + LibrarySelectionNames.AritySeparator + import.Arity);
    }

    internal void PublishStartupLibraries(RuntimeLibraryPreparedSelection plan)
    {
        PreparedBeamImage[] ordinary = plan.Images.Values.Where(image => !image.HasOnLoad)
            .OrderBy(image => image.Name, StringComparer.Ordinal).ToArray();
        if (ordinary.Length == 0)
            return;
        BeamPublicationResult result = PublishPreparedBeam(ordinary);
        if (result.Status != BeamPublicationStatus.Published)
            throw new InvalidOperationException(LibrarySelectionNames.Publish + LibrarySelectionNames.ContextSeparator + result.Status);
    }

    internal RuntimeLibrarySelectionSnapshot CaptureStartupLibraries(RuntimeLibraryPreparedSelection plan)
    {
        lock (gate)
        {
            var modules = new List<RuntimeLibraryModuleSnapshot>();
            foreach (RuntimeModuleSelection choice in plan.Request.Modules)
            {
                bool beam = plan.Images.TryGetValue(choice.Module, out PreparedBeamImage? prepared);
                ModuleCodeImage expected = beam ? prepared!.ExecutionIdentity.Image : plan.BootstrapImages[choice.Module];
                var exports = functions.Where(pair => pair.Key.Module == choice.Module).ToArray();
                if (oldCode.ContainsKey(choice.Module) || !ReferenceEquals(codeImages.GetValueOrDefault(choice.Module), expected)
                    || exports.Length == 0 || exports.Any(pair => beam
                        ? !ReferenceEquals(sourceOwners.GetValueOrDefault(pair.Key), prepared)
                        : sourceOwners.ContainsKey(pair.Key)))
                    throw new InvalidOperationException(LibrarySelectionNames.Owner + LibrarySelectionNames.ContextSeparator + choice.Module);
                var primitiveBodies = beam ? prepared!.ExecutionIdentity.NativePrimitives.Values : Array.Empty<ErlangFunction>();
                var assemblies = (beam ? primitiveBodies : exports.Select(pair => pair.Value))
                    .Select(body => new RuntimeLibraryAssemblyIdentity(
                        body.Method.Module.Assembly.GetName().FullName ?? body.Method.Module.Assembly.GetName().Name!,
                        body.Method.Module.ModuleVersionId
                    ));
                modules.Add(new(
                    choice.Module,
                    choice.Backend,
                    expected,
                    beam ? Convert.ToHexString(SHA256.HashData(choice.OwnedImage.Span)) : null,
                    (BitString)ModuleInformation(choice.Module, ModuleInformationKeys.Md5),
                    exports.Select(pair => new BeamFunctionSignature(pair.Key.Function, pair.Key.Arity)),
                    exports.Where(pair => IsNativeEntry(pair.Key.Module, pair.Key.Function, pair.Key.Arity)).Select(pair => new BeamFunctionSignature(pair.Key.Function, pair.Key.Arity)),
                    assemblies,
                    choice.Dependencies.Select(module => new RuntimeLibraryDependencySnapshot(module, plan.Request.Modules.Single(value => value.Module == module).Backend))
                ));
            }

            return new(modules);
        }
    }
}

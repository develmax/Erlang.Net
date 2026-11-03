namespace Erlang;

public sealed partial class ModuleRegistry
{
    public PreparedBeamImage PrepareBeam(ReadOnlyMemory<byte> bytes)
        => PrepareBeam(bytes, new BeamDependencySelection(Array.Empty<string>()));

    public PreparedBeamImage PrepareBeam(ReadOnlyMemory<byte> bytes, BeamDependencySelection dependencies)
    {
        using var source = EnterSourceMutation();
        using var atomScope = owningRuntime is null ? null : new AtomNameScope(owningRuntime.AtomNames);
        ArgumentNullException.ThrowIfNull(dependencies);
        byte[] owned = ObservedPayloadStorage.Code(bytes.ToArray());
        EtfDecodeOptions decoding = PreparationDecodeOptions();
        BeamLinkedCode code = BeamLinkedCodeReader.Read(BeamStructureReader.Read(owned, decoding)) with
        {
            RegistryModuleDependencies = dependencies.RegistryModules
        };
        var validatedEntries = new List<BeamLinkedFunction>();
        var initialValidation476 = new BeamRegisterValidation.PreparationBatch476(code);
        if (owningRuntime is not null)
            foreach (string? atom in code.Structure.Atoms)
                if (atom is not null)
                    owningRuntime.AtomNames.Observe(atom);
        if (code.OnLoadEntryLabel is int callbackLabel)
        {
            BeamLinkedFunction callback = code.Functions.Single(function => function.EntryLabel == callbackLabel);
            initialValidation476.Validate(callback);
            validatedEntries.Add(callback);
        }
        BeamPreparedMetadata metadata = BeamPublicationMetadata.Read(code, decoding);
        if (code.OnLoadEntryLabel is not null)
            BeamOnLoadMetadata.RequireSupported(metadata);
        foreach (BeamLinkedFunction function in code.Exports)
            if (!BeamGeneratedMetadata.IsGenerated(code, function))
            {
                initialValidation476.Validate(function);
                validatedEntries.Add(function);
            }

        ModuleLiteralDomain800? literals800 = null;
        IReadOnlyList<Term>? recordDefaults313 = null;
        try
        {
            if (owningRuntime is not null)
            {
                (code, literals800, recordDefaults313) = PrepareModuleLiteralCode802(owningRuntime, code);
                // Native literal conversion creates the FINAL code identity.
                // Validation reads literals, so certify its actual values too.
                var finalValidation476 = new BeamRegisterValidation.PreparationBatch476(code);
                foreach (BeamLinkedFunction entry in validatedEntries)
                    finalValidation476.Validate(entry);
            }
            owningRuntime?.MarkPreparedLiterals425(literals800?.Literals800 ?? code.Structure.Literals);
            owningRuntime?.RegisterLoaderMapKeys565(code);
            PreparedBeamValidationCache.SealFreshPreparedCode(code, validatedEntries);

            return new PreparedBeamImage(
                this,
                code,
                metadata,
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(owned)),
                literals800,
                recordDefaults313
            );
        }
        catch
        {
            _ = literals800?.CloseCodeOwner800();
            throw;
        }
    }

    public BeamPublicationResult PublishPreparedBeam(params PreparedBeamImage[] images)
    {
        using var source = EnterSourceMutation();
        using var modification = CodeModification.Enter();
        ArgumentNullException.ThrowIfNull(images);
        PreparedBeamImage[] owned = images.ToArray();
        lock (gate)
        {
            ThrowIfSourceClosing();
            foreach (PreparedBeamImage image in owned)
            {
                ArgumentNullException.ThrowIfNull(image);
                if (!ReferenceEquals(image.Owner, this))
                    throw new ArgumentException(BeamPreparationNames.ForeignImage, nameof(images));
                if (image.Disposed800) throw new ObjectDisposedException(nameof(PreparedBeamImage));
                if (image.Consumed)
                    throw new InvalidOperationException(BeamPreparationNames.ConsumedImage);
                if (image.HasOnLoad)
                    throw new InvalidOperationException(BeamOnLoadDiagnostics.RequiresStaging);
                if (pendingBeamImages.ContainsKey(image.Name))
                    throw new InvalidOperationException(BeamOnLoadDiagnostics.BusyModule);
            }
            using PreparedLoaderImageReservations807 nativePreparations = ReservePreparedImagesUnderGate807(owned);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string[] duplicates = owned.Where(image => !seen.Add(image.Name)).Select(image => image.Name).Reverse().ToArray();
            if (duplicates.Length != 0)
                return new(BeamPublicationStatus.Duplicated, Array.AsReadOnly(duplicates));
            string[] blocked = owned.Where(image => loadedModules.ContainsKey(image.Name) && oldCode.ContainsKey(image.Name))
                .Select(image => image.Name).Reverse().ToArray();
            if (blocked.Length != 0)
                return new(BeamPublicationStatus.NotPurged, Array.AsReadOnly(blocked));
            Apply(registry =>
            {
                foreach (PreparedBeamImage image in owned)
                {
                    RecordPreparedFunEntries(image);
                    if (loadedModules.ContainsKey(image.Name))
                        registry.DeleteModule(image.Name);
                    for (int index = 0; index < image.Code.Exports.Count; index++)
                    {
                        BeamLinkedFunction function = image.Code.Exports[index];
                        registry.RegisterSource(
                            image.Name,
                            function.Name,
                            function.Arity,
                            image.Bodies[index],
                            image.ExecutionIdentity.Owner
                        );
                    }
                    registry.PublishModuleCode(image.Name, image.Descriptor);
                    codeImages[image.Name] = image.ExecutionIdentity.Image;
                    BindPreparedOwnedBodies507(image);
                }
            });
            foreach (PreparedBeamImage image in owned)
            {
                image.Consumed = true;
                PreparedLoaderImageConsumed807(image);
            }

            return new(BeamPublicationStatus.Published, Array.AsReadOnly(owned.Select(image => image.Name).ToArray()));
        }
    }

    internal bool IsPreparedBeamConsumed(PreparedBeamImage image)
    {
        lock (gate)
            return image.Consumed;
    }
}

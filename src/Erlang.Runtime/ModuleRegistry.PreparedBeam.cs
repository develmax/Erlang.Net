namespace Erlang;

public sealed partial class ModuleRegistry
{
    public PreparedBeamImage PrepareBeam(ReadOnlyMemory<byte> bytes)
    {
        byte[] owned = bytes.ToArray();
        EtfDecodeOptions decoding = PreparationDecodeOptions();
        BeamLinkedCode code = BeamLinkedCodeReader.Read(BeamStructureReader.Read(owned, decoding));
        if (code.OnLoadEntryLabel is int callbackLabel)
        {
            BeamLinkedFunction callback = code.Functions.Single(function => function.EntryLabel == callbackLabel);
            BeamRegisterValidation.Validate(code, callback);
        }
        BeamPreparedMetadata metadata = BeamPublicationMetadata.Read(code, decoding);
        if (code.OnLoadEntryLabel is not null)
            BeamOnLoadMetadata.RequireSupported(metadata);
        foreach (BeamLinkedFunction function in code.Exports)
            if (!BeamGeneratedMetadata.IsGenerated(code, function))
                BeamRegisterValidation.Validate(code, function);

        return new PreparedBeamImage(
            this,
            code,
            metadata
        );
    }

    public BeamPublicationResult PublishPreparedBeam(params PreparedBeamImage[] images)
    {
        using var modification = CodeModification.Enter();
        ArgumentNullException.ThrowIfNull(images);
        PreparedBeamImage[] owned = images.ToArray();
        lock (gate)
        {
            foreach (PreparedBeamImage image in owned)
            {
                ArgumentNullException.ThrowIfNull(image);
                if (!ReferenceEquals(image.Owner, this))
                    throw new ArgumentException(BeamPreparationNames.ForeignImage, nameof(images));
                if (image.Consumed)
                    throw new InvalidOperationException(BeamPreparationNames.ConsumedImage);
                if (image.HasOnLoad)
                    throw new InvalidOperationException(BeamOnLoadDiagnostics.RequiresStaging);
                if (pendingBeamImages.ContainsKey(image.Name))
                    throw new InvalidOperationException(BeamOnLoadDiagnostics.BusyModule);
            }
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
                }
            });
            foreach (PreparedBeamImage image in owned)
                image.Consumed = true;

            return new(BeamPublicationStatus.Published, Array.AsReadOnly(owned.Select(image => image.Name).ToArray()));
        }
    }

    internal bool IsPreparedBeamConsumed(PreparedBeamImage image)
    {
        lock (gate)
            return image.Consumed;
    }
}

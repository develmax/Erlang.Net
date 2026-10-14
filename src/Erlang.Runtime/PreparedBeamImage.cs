namespace Erlang;

public sealed class PreparedBeamImage
{
    internal PreparedBeamImage(
        ModuleRegistry owner,
        BeamLinkedCode code,
        BeamPreparedMetadata metadata,
        IReadOnlyList<ErlangFunction> bodies
    )
    {
        Owner = owner;
        Code = code;
        Descriptor = metadata.Descriptor;
        AttributeProvenance = metadata.Attributes;
        CompilationProvenance = metadata.Compilation;
        Bodies = bodies;
    }

    internal ModuleRegistry Owner { get; }
    internal BeamLinkedCode Code { get; }
    internal ModuleCodeDescriptor Descriptor { get; }
    internal IReadOnlyList<ErlangFunction> Bodies { get; }
    internal bool Consumed { get; set; }
    public bool HasOnLoad => Code.OnLoadEntryLabel is not null;
    public string Name => Code.Structure.Atoms[1]!;
    public BitString NativeMd5 => Descriptor.Fingerprint;
    public Term Attributes => Descriptor.Attributes!;
    public Term Compilation => Descriptor.Compilation;
    public BeamMetadataProvenance AttributeProvenance { get; }
    public BeamMetadataProvenance CompilationProvenance { get; }
    public bool IsConsumed => Owner.IsPreparedBeamConsumed(this);
}

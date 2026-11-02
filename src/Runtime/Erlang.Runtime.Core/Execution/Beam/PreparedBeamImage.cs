namespace Erlang;

public sealed partial class PreparedBeamImage : ISourceLocalFunctionResolver, IDisposable, IAsyncDisposable
{
    // Preserve the original metadata-only constructor signature.
    internal PreparedBeamImage(
        ModuleRegistry owner,
        BeamLinkedCode code,
        BeamPreparedMetadata metadata,
        string originalBeamSha256
    ) : this(
        owner,
        code,
        metadata,
        originalBeamSha256,
        null
    )
    {
    }

    internal PreparedBeamImage(
        ModuleRegistry owner,
        BeamLinkedCode code,
        BeamPreparedMetadata metadata,
        string originalBeamSha256,
        ModuleLiteralDomain800? literals800,
        IReadOnlyList<Term>? recordDefaults313 = null
    )
    {
        OriginalBeamSha256 = originalBeamSha256;
        LiteralDomain800 = literals800;
        Owner = owner;
        Code = code;
        Descriptor = metadata.Descriptor;
        AttributeProvenance = metadata.Attributes;
        CompilationProvenance = metadata.Compilation;
        var primitives507 = owner.CaptureBeamPrimitives(code);
        ExecutionIdentity = new(new ModuleCodeImage(Name, this), this)
        {
            NativePrimitives = primitives507,
            OwnedNativeCapabilities507 = owner.PreparedOwnedCapabilities507(primitives507)
        };
        Bodies = Array.AsReadOnly(ObservedPayloadStorage.Code(code.Exports.Select<BeamLinkedFunction, ErlangFunction>(function =>
            (context, arguments) => BeamRegisterInterpreter.Execute(
                context,
                code,
                function.Name,
                Array.AsReadOnly(arguments.ToArray()),
                ExecutionIdentity
            )
        ).ToArray()));
        literals800?.BindImage800(owner, ExecutionIdentity.Image);
        if (literals800 is not null && recordDefaults313 is not null)
            ExecutionIdentity.Image.BindNativeRecordDefaults313(new(
                ExecutionIdentity.Image,
                literals800,
                recordDefaults313,
                metadata.Descriptor.NativeRecords
            ));
    }

    internal ModuleRegistry Owner
    {
        get;
    }
    internal BeamLinkedCode Code
    {
        get;
    }
    internal ModuleCodeDescriptor Descriptor
    {
        get;
    }
    internal IReadOnlyList<ErlangFunction> Bodies
    {
        get;
    }
    internal BeamExecutionIdentity ExecutionIdentity
    {
        get;
    }

    internal bool Consumed
    {
        get; set;
    }
    public bool HasOnLoad => Code.OnLoadEntryLabel is not null;
    public IReadOnlySet<string> RegistryModuleDependencies => Code.RegistryModuleDependencies;
    public IReadOnlyList<BeamFunctionSignature> NativePrimitiveExports => Array.AsReadOnly(
        ExecutionIdentity.NativePrimitives.Keys.Select(key => new BeamFunctionSignature(key.Function, key.Arity)).ToArray()
    );
    public string OriginalBeamSha256 { get; }

    public string Name => Code.Structure.Atoms[1]!;
    public BitString NativeMd5 => Descriptor.Fingerprint;
    public Term Attributes => Descriptor.Attributes!;
    public Term Compilation => Descriptor.Compilation;
    public BeamMetadataProvenance AttributeProvenance
    {
        get;
    }
    public BeamMetadataProvenance CompilationProvenance
    {
        get;
    }
    public bool IsConsumed => Owner.IsPreparedBeamConsumed(this);
    internal ModuleLiteralDomain800? LiteralDomain800 { get; }
    internal bool Disposed800 { get; set; }

    public void Dispose() => _ = Owner.DisposePreparedModuleLiteral800(this);

    public ValueTask DisposeAsync() => new(Owner.DisposePreparedModuleLiteral800(this));
}

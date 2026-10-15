namespace Erlang;

public sealed class PreparedBeamImage
{
    internal PreparedBeamImage(
        ModuleRegistry owner,
        BeamLinkedCode code,
        BeamPreparedMetadata metadata
    )
    {
        Owner = owner;
        Code = code;
        Descriptor = metadata.Descriptor;
        AttributeProvenance = metadata.Attributes;
        CompilationProvenance = metadata.Compilation;
        ExecutionIdentity = new(new ModuleCodeImage(Name), this);
        Bodies = Array.AsReadOnly(code.Exports.Select<BeamLinkedFunction, ErlangFunction>(function =>
            BeamGeneratedMetadata.IsGenerated(code, function)
                ? (context, arguments) => BeamGeneratedMetadata.Invoke(context, function.Module, arguments)
                : (context, arguments) => BeamRegisterInterpreter.Execute(
                    context,
                    code,
                    function.Name,
                    Array.AsReadOnly(arguments.ToArray()),
                    ExecutionIdentity
                )
        ).ToArray());
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
}

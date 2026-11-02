using System.Security.Cryptography;

namespace Erlang;

/// <summary>Describes code and its caller-supplied metadata.</summary>
/// <remarks>
/// Collections are copied, but Compilation, Attributes and native-record Term
/// graphs remain caller-owned borrowed values. Registration does not transfer
/// native custody or extend a supplying process, frame, image or Host lifetime.
/// Keep the genuine supplying custody alive through every use of those values.
/// Fingerprint alone is independently snapshotted by value. Canonical source
/// and BEAM producers supply detached metadata constants.
/// </remarks>
public sealed class ModuleCodeDescriptor
{
    public IReadOnlyList<(string Name, int Arity)> Functions { get; }
    public IReadOnlyList<(string Name, int Arity)> Exports { get; }
    public BitString Fingerprint { get; }
    public Term Compilation { get; }
    public Term? Attributes { get; }
    public ModuleAttributePolicy AttributePolicy { get; }
    public object? SourceOwner { get; }
    public IReadOnlyList<NativeRecordDefinition> NativeRecords { get; }

    private IReadOnlyList<(string Name, int Arity)>? declaredNifs;
    public IReadOnlyList<(string Name, int Arity)>? DeclaredNifs
    {
        get => declaredNifs;
        init => declaredNifs = value is null ? null : Array.AsReadOnly(ObservedPayloadStorage.Code(value.Distinct().ToArray()));
    }


    public ModuleCodeDescriptor(
        IEnumerable<(string Name, int Arity)> functions,
        IEnumerable<(string Name, int Arity)> exports,
        BitString fingerprint,
        Term compilation,
        Term? attributes = null,
        object? sourceOwner = null,
        IEnumerable<NativeRecordDefinition>? nativeRecords = null,
        ModuleAttributePolicy attributePolicy = ModuleAttributePolicy.AddDefaultVersion
    )
    {
        BitString snapshot = fingerprint.CopyIdentityValue255();
        if (!snapshot.IsBinary || snapshot.ToArray().Length != MD5.HashSizeInBytes)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (!Enum.IsDefined(attributePolicy))
            throw new ArgumentOutOfRangeException(nameof(attributePolicy));
        AttributePolicy = attributePolicy;
        Functions = Array.AsReadOnly(ObservedPayloadStorage.Code(functions.ToArray()));
        Exports = Array.AsReadOnly(ObservedPayloadStorage.Code(exports.ToArray()));
        Fingerprint = snapshot;
        Compilation = compilation;
        Attributes = attributes;
        SourceOwner = sourceOwner;
        NativeRecords = Array.AsReadOnly(ObservedPayloadStorage.Code((nativeRecords ?? []).ToArray()));
    }
}

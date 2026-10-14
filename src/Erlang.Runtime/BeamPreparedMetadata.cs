namespace Erlang;

internal sealed record BeamPreparedMetadata(
    ModuleCodeDescriptor Descriptor,
    BeamMetadataProvenance Attributes,
    BeamMetadataProvenance Compilation
);

namespace Erlang;

internal sealed record NifImageAdmissionGeneration(
    ProcessContext Context,
    SourceCodeEntry Caller,
    NifImageAdmissionOrigin Origin,
    ModuleCodeDescriptor TargetDescriptor,
    ModuleCodeImage? CurrentImage,
    ModuleCodeImage? OldImage,
    ModuleCodeDescriptor? CurrentDescriptor,
    BeamOnLoadTicket? PendingTicket,
    IReadOnlyDictionary<(string Module, string Function, int Arity), ErlangFunction> Bodies,
    IReadOnlyDictionary<(string Module, string Function, int Arity), object> SourceOwners
);

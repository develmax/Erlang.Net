namespace Erlang;

internal static class NifImageBindingNames
{
    internal const string Busy = "The code image already has a NIF attachment operation.";
    internal const string Retired = "The code image NIF attachment is retired.";
    internal const string ForeignReservation = "The NIF attachment reservation no longer owns this image.";
    internal const string InvalidFunction = "A NIF binding must identify an eligible existing local function.";
    internal const string DuplicateFunction = "A NIF binding cannot be declared twice.";
    internal const string ActiveUnderflow = "A NIF invocation lease was released more than once.";
}

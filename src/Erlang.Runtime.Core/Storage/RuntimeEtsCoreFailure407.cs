namespace Erlang;

// Typed internal failure, not yet the source native BIF frame/error_info wrapper.
internal sealed class RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407 cause) : Exception
{
    internal RuntimeEtsCoreCause407 Cause { get; } = cause;
}

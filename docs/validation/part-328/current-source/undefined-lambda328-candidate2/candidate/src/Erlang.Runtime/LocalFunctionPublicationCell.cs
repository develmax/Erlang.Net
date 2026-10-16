namespace Erlang;

internal sealed class LocalFunctionPublicationCell(LocalFunctionPublication publication)
{
    private LocalFunctionPublication current = publication;

    internal LocalFunctionPublication Current => Volatile.Read(ref current);

    internal bool IsCurrent(LocalFunctionPublication publication) => ReferenceEquals(Current, publication);

    internal void Publish(LocalFunctionPublication publication) => Volatile.Write(ref current, publication);
}

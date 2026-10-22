namespace Erlang;

internal sealed class ErlDriverHostImageRow420
{
    internal ErlDriverHostImageRow420(ulong identity) => Identity = identity;

    internal ulong Identity { get; }
    internal ErlDriverImage420? Image { get; set; }
    internal bool Published { get; set; }
}

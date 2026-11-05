namespace Erlang;

internal sealed class EtsFileOptions
{
    internal bool ObjectCount { get; set; }
    internal bool Md5 { get; set; }
    internal bool Sync { get; set; }

    internal Term Extended => Term.List((ObjectCount ? new Term[] { Term.A(EtsFileFields.ObjectCount) } : [])
        .Concat(Md5 ? new Term[] { Term.A(EtsFileFields.Md5sum) } : []).ToArray());
}

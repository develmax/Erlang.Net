using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class BeamReceiveExtendedHostAssets
{
    internal const string LazyTest = "runtime/beam/protected-receive386/original-lazy-timeout-values-and-profiles";
    internal const string AsmTest = "runtime/beam/protected-receive386/original-assembler-retained-tag-trim";
    private const string Prefix = "Erlang.Tests.BeamReceiveExtended386.";
    private const string LazyGroup = "Lazy.";
    private const string AsmGroup = "Asm.";
    private const string LazyModule = "beam_lazy_timeout386";
    private const string AsmObserver = "beam_trim_observer386";
    private const string AsmSubject = "beam_trim_protocol386";
    private static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["Lazy.beam_lazy_timeout386.beam"] = "AB2CA72B2DD3FF3CBCD7670D8515F01C027379C8F6FDD78B5EFD3F35EB7C8736",
        ["Lazy.beam_lazy_timeout386.erl"] = "94F064064851DADECB5E7585F57A8E8E4049EAE517479A5DB5B42FEFBA96804A",
        ["Lazy.values.etf"] = "D250F1697AAC45F7847D3ADABB2120E07233053F5E601A49A2ADBED31DEDA3F6",
        ["Lazy.stacks.etf"] = "8FED0CE6284B613A8CCAB52BE1E250E96D19ED7AE048B3F29A044690CE810369",
        ["Asm.beam_trim_observer386.beam"] = "2BFEFD2FB36808BF50B9E0A9FA46A378FB7C5A0C1116A1E914397BE6F865AAC9",
        ["Asm.beam_trim_protocol386.beam"] = "0F61E26EB97494AE8F7D9ADFD0497CC96FF610144C11A7F8DDEBF4F3A359CF28",
        ["Asm.beam_trim_observer386.erl"] = "691A2CC20211494A547ECB706E6F250B5BC9F738A7E99543E438A75293FA4F4A",
        ["Asm.beam_trim_protocol386.S"] = "99D00C187BC9F06B4B88E55C41F3F00719EFBF0C2A9C2D334B8DE8BEE26C45F5",
        ["Asm.assembly-admission.etf"] = "C5E4AAB50C032DB8677707EC60753027BDC0D2F5BB9F50A2CD1CA76430DFBD82",
        ["Asm.values.etf"] = "31AF6512AD0498ED148FACDD78BBB00BF5077649F3863D22E2ABFAD90C67CBB5",
        ["Asm.stacks.etf"] = "0DDF129D4919BA9332681DAD7D499CA5BF7C79A070B83B4C568D5585417BC881",
    };

    internal static Task Lazy() => BeamProtectedReceiveHostChecks.RunSubject(
        LazyModule,
        name => Read(LazyGroup + name),
        12,
        60,
        []
    );

    internal static Task Asm() => BeamProtectedReceiveHostChecks.RunSubject(
        AsmObserver,
        name => Read(AsmGroup + name),
        5,
        25,
        [AsmSubject]
    );

    private static byte[] Read(string name)
    {
        using Stream input = typeof(BeamReceiveExtendedHostAssets).Assembly.GetManifestResourceStream(Prefix + name)
            ?? throw new InvalidDataException(BeamProtectedReceiveHostNames.Failure);
        using var output = new MemoryStream();
        input.CopyTo(output);
        byte[] bytes = output.ToArray();
        if (Convert.ToHexString(SHA256.HashData(bytes)) != Digests[name])
            throw new InvalidDataException(BeamProtectedReceiveHostNames.Failure);

        return bytes;
    }
}

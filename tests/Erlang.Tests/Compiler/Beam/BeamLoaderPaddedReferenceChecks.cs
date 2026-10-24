namespace Erlang.Tests;

public static class BeamLoaderPaddedReferenceChecks
{
    public static async Task Run(byte[] bytes)
    {
        await using var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        PreparedBeamImage image = runtime.Modules.PrepareBeam(bytes);
        ResourceReferenceTerm original = runtime.ReferenceResources.CreatePrepared(image);
        ExternalTermFormat.Encode(original);
        foreach (uint[] suffix in new uint[][] { [0], [BeamLoaderPaddedReferenceNames.NonzeroSuffix], [0, BeamLoaderPaddedReferenceNames.NonzeroSuffix] })
        {
            var padded = new ReferenceTerm(original.Node, original.Words.Concat(suffix), original.Creation);
            Require(runtime.ReferenceResources.AcquirePrepared(padded) is null);
            Require(runtime.ReferenceResources.Rehydrate(padded) is not ResourceReferenceTerm);
            byte[] encoded = ExternalTermFormat.Encode(padded);
            Require(ExternalTermFormat.Decode(encoded) is ReferenceTerm and not ResourceReferenceTerm);
            ReferenceTerm restored = (ReferenceTerm)ExternalTermFormat.DecodeOne(
                encoded,
                new EtfDecodeOptions { LocalNode = runtime.Node, LocalCreation = original.Creation, ReferenceFactory = runtime.ReferenceResources.Rehydrate }
            ).Term;
            Require(restored is ResourceReferenceTerm && restored.Equals(original));
            using var lease = runtime.ReferenceResources.AcquirePrepared(restored);
            Require(lease is not null && ReferenceEquals(lease.Value, image));
            Require(foreign.ReferenceResources.Rehydrate(padded) is not ResourceReferenceTerm);
            Require(runtime.ReferenceResources.Rehydrate(new ReferenceTerm(BeamLoaderPaddedReferenceNames.ForeignNode, padded.Words, padded.Creation)) is not ResourceReferenceTerm);
            Require(runtime.ReferenceResources.Rehydrate(new ReferenceTerm(padded.Node, padded.Words, BeamLoaderPaddedReferenceNames.DifferentCreation)) is not ResourceReferenceTerm);
        }
        ReferenceTerm ordinary = runtime.MakeReference();
        var ordinaryPadded = new ReferenceTerm(ordinary.Node, ordinary.Words.Concat([BeamLoaderPaddedReferenceNames.NonzeroSuffix]), ordinary.Creation);
        byte[] ordinaryEncoded = ExternalTermFormat.Encode(ordinaryPadded);
        Term ordinaryRestored = ExternalTermFormat.DecodeOne(
            ordinaryEncoded,
            new EtfDecodeOptions { LocalNode = runtime.Node, LocalCreation = ordinary.Creation, ReferenceFactory = runtime.ReferenceResources.Rehydrate }
        ).Term;
        Require(ordinaryRestored is ReferenceTerm and not ResourceReferenceTerm && ordinaryRestored.Equals(ordinary));
        var foreignIdentity = new ReferenceTerm(BeamLoaderPaddedReferenceNames.ForeignNode, ordinaryPadded.Words, ordinary.Creation);
        Require(ExternalTermFormat.DecodeOne(ExternalTermFormat.Encode(foreignIdentity), new EtfDecodeOptions { LocalNode = runtime.Node, LocalCreation = ordinary.Creation }).Term.Equals(foreignIdentity));
        var shortIdentity = new ReferenceTerm(ordinary.Node, [ordinary.Words[0]], ordinary.Creation);
        var shortExpected = new ReferenceTerm(ordinary.Node, [ordinary.Words[0], 0, 0], ordinary.Creation);
        Require(ExternalTermFormat.DecodeOne(ExternalTermFormat.Encode(shortIdentity), new EtfDecodeOptions { LocalNode = runtime.Node, LocalCreation = ordinary.Creation }).Term.Equals(shortExpected));
        try
        {
            ExternalTermFormat.DecodeOne(
                ordinaryEncoded,
                new EtfDecodeOptions { LocalNode = runtime.Node, LocalCreation = ordinary.Creation, ReferenceFactory = _ => new ReferenceTerm(ordinary.Node, [ordinary.Words[0] + 1, 0, 0], ordinary.Creation) }
            );
            throw new InvalidOperationException(BeamLoaderPaddedReferenceNames.Failure);
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
        }
        PidReferenceIdentity(runtime);
        await runtime.DisposeAsync();
        Require(runtime.ReferenceResources.Rehydrate(new ReferenceTerm(original.Node, original.Words.Concat([BeamLoaderPaddedReferenceNames.NonzeroSuffix]), original.Creation)) is not ResourceReferenceTerm);
    }

    private static void PidReferenceIdentity(ProcessRuntime runtime)
    {
        var options = new EtfDecodeOptions { LocalNode = runtime.Node, LocalCreation = 0, ReferenceFactory = runtime.ReferenceResources.Rehydrate };

        ReferenceTerm Decode(ReferenceTerm identity, EtfDecodeOptions? settings = null)
            => (ReferenceTerm)ExternalTermFormat.DecodeOne(ExternalTermFormat.Encode(identity), settings ?? options).Term;
        ReferenceTerm ordinary = runtime.MakeReference();
        uint[] words = [ordinary.Words[0], ordinary.Words[1] | ReferenceIdentityLayout.PidMarker, ordinary.Words[2], ReferenceIdentityLayout.PidTag, uint.MaxValue];
        var unknownOwner = new ReferenceTerm(runtime.Node, words);
        Require(Decode(unknownOwner).Equals(unknownOwner));
        words[1] |= BeamLoaderPaddedReferenceNames.MagicMarker;
        var bothFlags = new ReferenceTerm(runtime.Node, words);
        Require(Decode(bothFlags).Equals(bothFlags) && Decode(bothFlags) is not ResourceReferenceTerm);
        words[ReferenceIdentityLayout.PidOwnerLowWord] = BeamLoaderPaddedReferenceNames.InvalidPidTag;
        var invalid = new ReferenceTerm(runtime.Node, words);
        try
        {
            Decode(invalid);
            throw new InvalidOperationException(BeamLoaderPaddedReferenceNames.Failure);
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
        }
        var foreignInvalid = new ReferenceTerm(BeamLoaderPaddedReferenceNames.ForeignNode, words);
        Require(Decode(foreignInvalid).Equals(foreignInvalid));
        var fourWords = new ReferenceTerm(runtime.Node, words.Take(ReferenceIdentityLayout.PidWordCount - 1));
        var fourExpected = new ReferenceTerm(runtime.Node, words.Take(ReferenceIdentityLayout.LocalWords));
        Require(Decode(fourWords).Equals(fourExpected));
        try
        {
            Decode(
                unknownOwner,
                options with { ReferenceFactory = identity => new ReferenceTerm(identity.Node, identity.Words.Take(ReferenceIdentityLayout.LocalWords), identity.Creation) }
            );
            throw new InvalidOperationException(BeamLoaderPaddedReferenceNames.Failure);
        }
        catch (ErlangException exception)
        {
            Require(exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
        }
    }

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(BeamLoaderPaddedReferenceNames.Failure);
    }
}

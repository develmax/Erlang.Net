using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class BeamOrdsetsFamilyHostChecks
{
    internal static Task Run(string group)
    {
        var selected = BeamOrdsetsFamilyHostNames.Groups.Single(value => value.Group == group);

        return BeamOrdsetsCoreHostChecks.RunSubject(
            selected.Subject,
            Read,
            selected.Values,
            selected.Profiles
        );

        byte[] Read(string name)
        {
            if (name == BeamOrdsetsCoreHostNames.Support + BeamOrdsetsCoreHostNames.BeamExtension
                || name == BeamOrdsetsCoreHostNames.Worker + BeamOrdsetsCoreHostNames.BeamExtension)
                return BeamOrdsetsCoreHostChecks.Read(name);
            string key = group + BeamOrdsetsFamilyHostNames.Separator + name;
            using Stream input = typeof(BeamOrdsetsFamilyHostChecks).Assembly.GetManifestResourceStream(BeamOrdsetsFamilyHostNames.Prefix + key)
                ?? throw new InvalidDataException(BeamOrdsetsFamilyHostNames.Failure);
            using var memory = new MemoryStream();
            input.CopyTo(memory);
            byte[] bytes = memory.ToArray();
            if (Convert.ToHexString(SHA256.HashData(bytes)) != BeamOrdsetsFamilyHostNames.Digests[key])
                throw new InvalidDataException(BeamOrdsetsFamilyHostNames.Failure);

            return bytes;
        }
    }
}

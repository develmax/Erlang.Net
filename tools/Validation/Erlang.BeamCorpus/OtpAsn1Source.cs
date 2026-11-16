internal static class OtpAsn1Source
{
    internal static string Generate(string root, string report)
    {
        string input = Path.Combine(root, "lib", "eldap-1.3.1", "asn1", "ELDAPv3.asn1");
        var schema = new Asn1BerSchema(File.ReadAllText(input));
        if (schema.Name != "ELDAPv3")
        {
            throw new InvalidOperationException($"Expected ELDAPv3 ASN.1, found {schema.Name}.");
        }

        string output = Path.Combine(report + ".sources", Guid.NewGuid().ToString("N"));
        Asn1BerErlangWriter.Write(schema, output, OtpWindowsReleaseProfiles.LdapAsn1Options);

        return Path.Combine(output, "ELDAPv3.erl");
    }
}

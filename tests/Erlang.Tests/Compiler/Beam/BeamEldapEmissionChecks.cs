using System.Buffers.Binary;
using System.Text;
using Erlang;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamEldapEmissionChecks
{
    internal static void Check()
    {
        const string name = "ELDAPv3";
        var schema = new Asn1BerSchema(Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(name + ".asn1")));
        var directory = Directory.CreateTempSubdirectory("erlang-beam-eldap-");
        try
        {
            Asn1BerErlangWriter.Write(schema, directory.FullName, OtpWindowsReleaseProfiles.LdapAsn1Options);
            string header = name + ".hrl";
            if (!File.ReadAllBytes(Path.Combine(directory.FullName, header)).AsSpan()
                .SequenceEqual(BeamModuleEmissionChecks.Read(header)))
            {
                throw new InvalidOperationException("The generated ELDAPv3 header differs from the OTP release.");
            }
            string source = Path.Combine(directory.FullName, name + ".erl");
            var preprocessor = new BeamMacroPreprocessor(File.ReadAllText(source), source, new BeamIncludeResolver(null, []));
            var tokens = preprocessor.Run();
            var module = new Parser(preprocessor.CombinedSource, tokens).ParseModule();
            if (!module.DeclaredExports.Contains(("dialyzer-suppressions", 1)))
            {
                throw new InvalidOperationException("The generated ELDAPv3 source lacks the OTP Dialyzer export.");
            }
            if (module.DeclaredFunctions.Any(function => function.Name is "schema" or "to_tlv" or "from_tlv"))
            {
                throw new InvalidOperationException("The generated ELDAPv3 source still uses descriptor interpretation.");
            }
            (string Name, int Arity)[] ordered =
            [
                ("encode_disp", 2), ("decode_disp", 2), ("info", 0),
                ("enc_Filter", 1), ("enc_Filter", 2),
                ("enc_Filter_and", 2), ("enc_Filter_and_components", 3),
                ("enc_Filter_or", 2), ("enc_Filter_or_components", 3),
                ("dec_Filter", 1), ("dec_Filter", 2), ("dec_Filter_and", 2), ("dec_Filter_or", 2)
            ];
            if (!module.DeclaredFunctions.Select(function => (function.Name, function.Arity))
                .Where(ordered.Contains).SequenceEqual(ordered))
            {
                throw new InvalidOperationException("The generated LDAP helpers differ from the OTP source order.");
            }
            var profile = OtpWindowsReleaseProfiles.Find("eldap-1.3.1", name);
            byte[] actual = CodeGeneration.CompileBeamFile(source, buildProfile: profile);
            var chunks = BeamDocumentationEmissionChecks.Chunks(actual);
            byte[] debug = chunks.Single(chunk => chunk.Id == "Dbgi").Data;
            if (ExternalTermFormat.Decode(BeamDocumentationEmissionChecks.RawEtf(debug)) is not
                TupleTerm { Items: [Atom { Name: "debug_info_v1" }, Atom { Name: "erl_abstract_code" }, TupleTerm { Items: [var forms, _] }] }
                || !Cons.Items(forms)
                    .OfType<TupleTerm>()
                    .Where(form => form.Items is [Atom { Name: "function" }, _, Atom, Integer, _])
                    .Select(form => (((Atom)form.Items[2]).Name, (int)((Integer)form.Items[3]).Value))
                    .SequenceEqual(module.DeclaredFunctions.Select(function => (function.Name, function.Arity))))
            {
                throw new InvalidOperationException("The generated LDAP debug chunk lacks its declared function forms.");
            }
            if (Cons.Items(forms).Count(form => form is TupleTerm { Items: [Atom { Name: "attribute" }, _, Atom { Name: "record" }, _] }) != 23)
            {
                throw new InvalidOperationException("The generated LDAP debug chunk lacks its included record declarations.");
            }
            int exports = Count(chunks, "ExpT");
            int locals = Count(chunks, "LocT");
            if (exports != module.DeclaredExports.Count + 2
                || exports + locals != module.DeclaredFunctions.Count + 2 + 11) // module_info and collection helpers
            {
                throw new InvalidOperationException("The generated ELDAPv3 BEAM has incomplete function tables.");
            }
            if (!BeamReleaseImageChecks.Matches(BeamModuleEmissionChecks.Read(name + ".beam"), actual))
            {
                throw new InvalidOperationException("The generated ELDAPv3 BEAM differs from the OTP release.");
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static int Count(IEnumerable<(string Id, byte[] Data)> chunks, string id) =>
        BinaryPrimitives.ReadInt32BigEndian(chunks.Single(chunk => chunk.Id == id).Data);
}

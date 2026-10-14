using Erlang;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Security.Cryptography;
var documents = new List<object>();
using (var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(args[0])))
{
    var metadata = provider.GetMetadataReader();
    foreach (var handle in metadata.Documents)
    {
        var document = metadata.GetDocument(handle);
        string name = metadata.GetString(document.Name);
        if (Path.GetFileName(name) is "Atom.cs" or "AtomNameRegistry.cs" or "AtomCache.cs" or "ErlangPrimordialAtomNames.cs")
        {
            byte[] checksum = metadata.GetBlobBytes(document.Hash);
            byte[] source = File.ReadAllBytes(name);
            documents.Add(new { Name = name, PdbHash = Convert.ToHexString(checksum), ActualSourceHash = Convert.ToHexString(SHA256.HashData(source)), Exact = checksum.SequenceEqual(SHA256.HashData(source)) });
            if (!checksum.SequenceEqual(SHA256.HashData(source))) throw new Exception("Actual old DLL PDB/source mismatch");
        }
    }
}
int before = AtomNameRegistry.Count;
var first = new Atom("\uD800");
var second = new Atom("\ud801");
var map = new MapTerm(new[] { new KeyValuePair<Term, Term>(first, Term.I(1)), new KeyValuePair<Term, Term>(second, Term.I(2)) });
bool tooLongAccepted = false;
try { _ = new Atom(new string('x', 256)); tooLongAccepted = true; } catch (ErlangException) { }
Console.WriteLine(JsonSerializer.Serialize(new { Mvid = typeof(Atom).Assembly.ManifestModule.ModuleVersionId, Documents = documents, InvalidInputsUtf16Hex = new[] { "D800", "D801" }, InvalidNameAccepted = true, Equal = first.Equals(second), SameHash = first.GetHashCode() == second.GetHashCode(), MapEntries = map.Entries.Count, RegistryGrowth = AtomNameRegistry.Count - before, TooLongAccepted = tooLongAccepted }, new JsonSerializerOptions { WriteIndented = true }));
return first.Equals(second) && tooLongAccepted && AtomNameRegistry.Count > before ? 0 : 1;

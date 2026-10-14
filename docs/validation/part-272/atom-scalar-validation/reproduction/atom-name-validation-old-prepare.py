from pathlib import Path
import subprocess,hashlib,json
r=Path.cwd();e=r/'artifacts/atom-name-validation-evidence';old=e/'old-probe';old.mkdir(exist_ok=True)
(old/'OldAtomProbe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Reference Include="Erlang.Terms"><HintPath>../old-main-terms/Erlang.Terms.dll</HintPath></Reference></ItemGroup></Project>')
(old/'Program.cs').write_text(r'''using Erlang;
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
var first = new Atom("\uD800".Replace("\\uD800", "\ud800"));
var second = new Atom("\ud801");
var map = new MapTerm(new[] { new KeyValuePair<Term, Term>(first, Term.I(1)), new KeyValuePair<Term, Term>(second, Term.I(2)) });
bool tooLongAccepted = false;
try { _ = new Atom(new string('x', 256)); tooLongAccepted = true; } catch (ErlangException) { }
Console.WriteLine(JsonSerializer.Serialize(new { Mvid = typeof(Atom).Assembly.ManifestModule.ModuleVersionId, Documents = documents, InvalidInputsUtf16Hex = new[] { "D800", "D801" }, InvalidNameAccepted = true, Equal = first.Equals(second), SameHash = first.GetHashCode() == second.GetHashCode(), MapEntries = map.Entries.Count, RegistryGrowth = AtomNameRegistry.Count - before, TooLongAccepted = tooLongAccepted }, new JsonSerializerOptions { WriteIndented = true }));
return first.Equals(second) && tooLongAccepted && AtomNameRegistry.Count > before ? 0 : 1;
''',encoding='utf8')
# Rewrite deliberate scalar-invalid fixture to exact C# escape, with no runtime string transformation.
p=old/'Program.cs';s=p.read_text();start=s.index('var first = ');end=s.index('\n',start);s=s[:start]+r'var first = new Atom("\uD800");'+s[end:];p.write_text(s)
for source in ['erts/emulator/beam/atom.c','erts/emulator/beam/bif.c','erts/emulator/beam/erl_unicode.c','erts/emulator/beam/utils.c']:
 raw=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:'+source]);dest=e/'pinned-original'/Path(source).name;dest.parent.mkdir(exist_ok=True);dest.write_bytes(raw)
print('Actual oldDLL negative +portablePDB binding source prepared; candidate AtomNameRegistry unmodified')

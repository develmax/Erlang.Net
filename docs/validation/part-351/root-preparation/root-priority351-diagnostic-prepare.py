from pathlib import Path
import hashlib, json, subprocess, xml.etree.ElementTree as ET

r=Path.cwd();o=r/'artifacts/spawn-priority351-diagnostic';o.mkdir(exist_ok=False)
bin=r/'artifacts/spawn-priority351-full-candidate/tests/Erlang.Tests/bin/Debug/net10.0'
project=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(project,'PropertyGroup')
for k,v in {'OutputType':'Exe','TargetFramework':'net10.0','EnableDefaultCompileItems':'false'}.items(): ET.SubElement(props,k).text=v
items=ET.SubElement(project,'ItemGroup');ET.SubElement(items,'Compile',Include='Program.cs')
for p in bin.glob('*.dll'):
 ref=ET.SubElement(items,'Reference',Include=p.stem);ET.SubElement(ref,'HintPath').text=str(p)
ET.ElementTree(project).write(o/'Diagnostic.csproj',encoding='utf-8')
(o/'Program.cs').write_text('''using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Differential;

bool passed = false;
string? actual = null, expected = null, error = null, source = null, generated = null;
try
{
    var fixture = CompiledModuleCases.Modules.Single(value => value.Name == args[0]);
    source = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fixture.Source)));
    using var artifact = GeneratedModuleCompiler.Compile(fixture.Source, fixture.CompilerPolicy);
    generated = artifact.CSharpSha256;
    var result = await CompiledModuleExecution.Run(artifact, fixture.CallerContext).WaitAsync(TimeSpan.FromSeconds(30));
    actual = result.ToString();
    expected = fixture.Expected.ToString();
    passed = result.Equals(fixture.Expected);
}
catch (Exception exception)
{
    error = exception.ToString();
}
File.WriteAllText(args[1], JsonSerializer.Serialize(new
{
    Name = args[0], Passed = passed, Actual = actual, Expected = expected, Error = error,
    SourceSha256 = source, GeneratedSha256 = generated,
    Loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.GetName().Name!.StartsWith("Erlang.")).Select(a => new { a.Location, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))) })
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(passed ? "PASS" : "FAIL");
return passed ? 0 : 1;
''',encoding='utf-8')
with (o/'build.stdout.txt').open('xb') as stdout,(o/'build.stderr.txt').open('xb') as stderr:
 p=subprocess.run(['dotnet','build',str(o/'Diagnostic.csproj'),'--nologo','-v:q'],stdout=stdout,stderr=stderr,timeout=90)
(o/'build-exit.json').write_text(json.dumps({'ExitCode':p.returncode})+'\n');assert p.returncode==0
print('Diagnostic runner built against actual failed351 DLLs; no runtime rebuild')

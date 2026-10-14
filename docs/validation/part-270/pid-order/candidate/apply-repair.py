from pathlib import Path
import shutil,json,hashlib
r=Path.cwd();e=r.parent/'pid-order-evidence';b=r/'tools/IdentifierOrderProbe/bin/Release/net10.0';o=e/'old-dll';o.mkdir(exist_ok=True)
rows=[]
for p in b.glob('*'):
 if p.is_file():shutil.copyfile(p,o/p.name);rows.append({'Name':p.name,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
(e/'old-dll-inventory.json').write_text(json.dumps(rows,indent=2));data=json.loads((e/'old-probe.json').read_text(encoding='utf-8-sig'));print('Old',sum(x['Pass'] for x in data),'/',len(data));print('Fail:',[x['Name'] for x in data if not x['Pass']])
shutil.copyfile(e/'TermOrder.after.cs',r/'src/Erlang.Terms/TermOrder.cs')
p=r/'tools/IdentifierOrderProbe/Program.cs';s=p.read_text();s='using System.Reflection.Metadata;\nusing System.Reflection.PortableExecutable;\n'+s;s=s.replace('var rows = new List<object>();','''using (var pe = new PEReader(File.OpenRead(args[0])))
{
    var metadata = pe.GetMetadataReader();
    Console.Error.WriteLine(metadata.GetGuid(metadata.GetModuleDefinition().Mvid));
}
Console.Error.WriteLine(typeof(TermOrder).Assembly.ManifestModule.ModuleVersionId);
var rows = new List<object>();''');p.write_text(s)

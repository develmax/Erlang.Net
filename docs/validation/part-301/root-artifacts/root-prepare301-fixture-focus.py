from pathlib import Path
import json,hashlib,shutil
r=Path.cwd();a=r/'artifacts';p=a/'system-profile-dead-receiver301-fixture-revision1';iso=a/'root-process-info301-isolated';h=lambda b:hashlib.sha256(b).hexdigest().upper();f=json.loads((p/'source-freeze.json').read_bytes());assert h((p/'source-freeze.json').read_bytes())=='85B73C8C659B4151A872287469794FDBA75293504086279702B3F2A2955CE9DA'
for row in f['Files']:
 b=(p/row['Path']).read_bytes();assert len(b)==row['Bytes'] and h(b)==row['Sha256']
n='tools/Erlang.Differential/SystemProfileCases.cs';assert h((r/n).read_bytes())==f['ParentSha256'];assert (r/n).read_bytes()==(iso/'candidate'/n).read_bytes();shutil.copyfile(p/'overlay'/n,iso/'candidate'/n)
paths=json.loads((iso/'paths11.json').read_bytes())+[n];(iso/'paths12.json').write_text(json.dumps(paths,indent=2)+'\n',encoding='utf-8')
t=(a/'root-format301-integration11.ps1').read_text();t=t.replace('paths11','paths12').replace('Count -ne 11','Count -ne 12').replace('eleven API','twelve API').replace('tokens11','tokens12').replace('sdk11','sdk12').replace('owned11','owned12').replace('custom11','custom12').replace('format11','format12').replace('SourcePaths=11','SourcePaths=12');(a/'root-format301-fixture12.ps1').write_text(t,encoding='utf-8')
p=a/'root-system-profile301-focus';shutil.copyfile(p/'Program.cs',p/'diagnostic-program.cs.txt');proj=(p/'Focus.csproj').read_text();proj=proj.replace('../../tools/','../root-process-info301-isolated/candidate/tools/');(p/'Focus.csproj').write_text(proj,encoding='utf-8')

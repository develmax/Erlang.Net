from pathlib import Path
import json,hashlib
out=Path('artifacts/proc-lib338-permanent-tests3');data=out/'source/tools/Erlang.Differential/ProcLibLifecycleCaseData.cs';t=data.read_text();old='receive {captured_log,_}->exit(unexpected_suppressed_report)';new='receive {captured_log,#{meta:=#{pid:=Silent}}}->exit(unexpected_suppressed_report)';assert t.count(old)==7;t=t.replace(old,new);data.write_text(t,encoding='utf-8');manifest=json.loads((out/'source-fixtures-manifest.json').read_bytes())
for r in manifest['AddedOriginalComparableAssertions7']:
 p=out/r['Path'];text=p.read_text();assert old in text;p.write_text(text.replace(old,new),encoding='utf-8');b=p.read_bytes();r['Bytes']=len(b);r['Sha256']=hashlib.sha256(b).hexdigest().upper()
(out/'source-fixtures-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');f=out/'source-freeze.json';j=json.loads(f.read_bytes());j['FixtureManifestSha256']=hashlib.sha256((out/'source-fixtures-manifest.json').read_bytes()).hexdigest().upper()
for r in j['Files']:
 b=(out/'source'/r['Path']).read_bytes();r['Bytes']=len(b);r['Sha256']=hashlib.sha256(b).hexdigest().upper()
f.write_text(json.dumps(j,indent=2)+'\n');print(hashlib.sha256(f.read_bytes()).hexdigest().upper())

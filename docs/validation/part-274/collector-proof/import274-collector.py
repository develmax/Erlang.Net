from pathlib import Path
import json,hashlib,subprocess
root=Path.cwd(); art=root/'artifacts'
freeze=art/'native-admission-collector3-integration-source-freeze-final-revision2.json'
assert hashlib.sha256(freeze.read_bytes()).hexdigest().upper()=='D4FE101E55F9F8CC4CAA688B5661F67FDADC1C5411A4D8F3BE87A9204B1803F7'
d=json.loads(freeze.read_text(encoding='utf-8-sig')); receipt=json.loads((art/'native-admission-collector3-integration-actual-local-receipt.json').read_text(encoding='utf-8-sig'))
assert receipt['Status']=='PASS_ACTUAL_ADDITIVE_COLLECTOR_TARGET_STYLE_REFUSAL_RAW_CAPTURE'
assert receipt['ActualPreWriteRefusals']==12 and receipt['BuildErrors']==receipt['BuildWarnings']==receipt['OwnedStyleChanges']==0
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==d['BaseHead']
assert not subprocess.check_output(['git','diff','--name-only'],text=True).strip()
rows=d['Files']; assert len(rows)==12
for r in rows:
    dest=root/r['Path']; source=art/'native-admission-collector3-integration-prototype'/r['Path']; raw=source.read_bytes()
    assert hashlib.sha256(raw).hexdigest().upper()==r['Sha256'].upper(),r['Path']
    assert not dest.exists(),r['Path']
for r in d['LinkedSharedSources']:
    assert hashlib.sha256((root/r['Path']).read_bytes()).hexdigest().upper()==r['Sha256'].upper()
for r in rows:
    dest=root/r['Path']; dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes((art/'native-admission-collector3-integration-prototype'/r['Path']).read_bytes())
proof={'Status':'Imported exact twelve already validated source paths; actual Main location gate pending','SourceFreezeSha256':hashlib.sha256(freeze.read_bytes()).hexdigest().upper(),'Paths':[{'Path':r['Path'],'Sha256':r['Sha256']} for r in rows],'RuntimeProductionChanged':False,'PrivateLicenseImported':False}
(art/'part274-main-source-import.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'ImportedPaths':len(rows),'Status':proof['Status']}))

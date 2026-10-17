from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();c=r/'artifacts/spawn-priority351-full-candidate';p=r/'artifacts/priority351-prefull-metadata-proposal';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()=='c1f219f31726fc93e537080f400651d300c47d72'
plan=read(p/'copy-plan.json')['Files'];assert len(plan)==175
for row in plan:assert sha((c/row['TargetPath']).read_bytes())==row['Sha256']
ledger=read(c/'docs/mfa-compatibility.json');assert len(ledger['VerifiedSourceFiles'])==74
needed={}
for entry in ledger['Entries']:
    for evidence in entry['Evidence']:
        path='docs/'+evidence['Path'];assert '..' not in Path(path).parts
        assert path not in needed or needed[path]==evidence['Sha256'];needed[path]=evidence['Sha256']
needed['docs/validation/part-350/full/tests.json']='4D8F1EDACF32E26B882EF4E3D9F16F0F613C6C3DCDDADC4A27A0043C59A8F4F4'
copied=[]
for path,expected in needed.items():
    target=c/path
    if not target.exists():
        original=r/path;raw=original.read_bytes();assert sha(raw)==expected
        target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);copied.append(dict(Path=path,Bytes=len(raw),Sha256=expected,Origin='Current published350 Main raw historical/transitive input'))
    assert sha(target.read_bytes())==expected,path
for row in ledger['VerifiedSourceFiles']:assert sha((c/row['Path']).read_bytes())==row['Sha256']
receipt=dict(Status='ROOT_PRIVATE351_PREFULL_METADATA_COMPLETE_TRANSITIVE_EVIDENCE_NOT_PUBLISHED',Counts=[10,24,553],SourceGuards=74,OverlayRows=175,AllReferencedEvidenceRehashed=len(needed),HistoricalInputsAdded=copied,FirstApplyFailure='175 overlay copies completed; audit then refused missing inherited part350 spawn-options classification. No SDK/Main mutation. This completion adds only raw current-Main historical dependencies and the actual350 comparison report; it does not change evidence bytes or relax validation.',OriginalApplyScriptSha256=sha((r/'artifacts/root-apply351-metadata.py').read_bytes()),MainModified=False,FullExecuted=False)
out=r/'artifacts/spawn-priority351-full-base/metadata-applied.json';assert not out.exists();out.write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n');print(json.dumps(dict(HistoricalInputsAdded=len(copied),EvidenceFiles=len(needed),Sha256=sha(out.read_bytes()))))

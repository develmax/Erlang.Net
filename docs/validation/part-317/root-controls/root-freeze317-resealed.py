from pathlib import Path
import json,hashlib
r=Path.cwd();d=r/'artifacts/root-part317-composed2';c=d/'candidate';p=r/'artifacts/root-part317-reseal1';h=lambda v:hashlib.sha256(v).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
v=read(d/'formatted-source-freeze.json');receipt=read(p/'receipt.json');assert receipt['CandidateMetadataApplied'] and receipt['All33TokenStreamsEqual'] and receipt['AllNonFilesImportAbiPolicyFieldsUnchanged']
assert receipt['FinalFormattedSourceFreezeSha256']==h((d/'formatted-source-freeze.json').read_bytes())
cl=p/'source-resource-closure.json';assert h(cl.read_bytes())==receipt['FinalSourceClosureSha256'];assert len(read(cl)['Files'])==2260
for e in read(cl)['Files']:
 raw=(c/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
for n in receipt['ChangedMetadataPaths']:
 raw=(c/n).read_bytes();v['Files'].append({'Path':n,'Bytes':len(raw),'Sha256':h(raw),'Added':not(r/n).exists(),'ParentSha256':h((r/n).read_bytes()) if (r/n).exists() else None})
assert len(v['Files'])==38
v['Files'].sort(key=lambda e:e['Path'])
v.update(Status='ROOT_FINAL_RESEALED317_READY_FOR_PEER',SourcePaths=38,CSharpPaths=33,SourceClosurePaths=2260,SourceClosureSha256=h(cl.read_bytes()),FinalSourceClosureSha256=h(cl.read_bytes()),NativePlansIntentionallyStalePendingReseal=False,Plans=receipt['Plans'],ResealReceiptSha256=h((p/'receipt.json').read_bytes()),ExpectedFullPassed=6667,ExpectedRegistry=509,ExpectedErlangMfas=351)
(d/'final-source-resource-closure.json').write_bytes(cl.read_bytes());write(d/'final-source-freeze.json',v)
print(json.dumps({'FreezeSha256':h((d/'final-source-freeze.json').read_bytes()),'ClosureSha256':h(cl.read_bytes()),'Owned':38,'CSharp':33,'ExpectedFull':6667}))

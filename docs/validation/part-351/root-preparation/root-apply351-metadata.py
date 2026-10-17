from pathlib import Path
import json,hashlib,collections
r=Path.cwd();c=r/'artifacts/spawn-priority351-full-candidate';p=r/'artifacts/priority351-prefull-metadata-proposal';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert sha((p/'source-freeze.json').read_bytes())=='B565AA7DF9F19C236D9AB33623FE12098AE85A2ADFEE4FE0162AF8877F7AFDE4'
assert sha((p/'proposal.json').read_bytes())=='544E233A05EE37360E6B2B46A701E2B30661C77389510CCF72905C162B6A48AE'
proposal=read(p/'proposal.json');plan=read(p/'copy-plan.json')['Files'];assert len(plan)==175
for row in read(p/'source-freeze.json')['Files']:
    path=p/row['Path'];assert sha(path.read_bytes())==row['Sha256']
old=read(c/'docs/mfa-compatibility.json');new=read(p/'overlay/docs/mfa-compatibility.json');changed=[]
for a,b in zip(old['Entries'],new['Entries'],strict=True):
    key=(a['Module'],a['Function'],a['Arity']);assert key==(b['Module'],b['Function'],b['Arity'])
    if a!=b:
        changed.append(key);assert b['Evidence'][:len(a['Evidence'])]==a['Evidence']
        if a['Status']=='Verified compatible':assert b['Status']==a['Status'] and b['Scope']==a['Scope'] and b['Limits']==a['Limits']
        elif key==('erlang','statistics',1):assert a['Status']=='Needs verification' and b['Status']=='Known differences'
        else:assert key[0:2]==('erlang','spawn_opt') and key[2] in [2,3,4,5] and a['Status']==b['Status']=='Known differences'
assert len(changed)==15 and len(old['Entries'])==len(new['Entries'])==587
assert collections.Counter(x['Status'] for x in new['Entries'])=={'Verified compatible':10,'Known differences':24,'Needs verification':553}
assert len(new['VerifiedSourceFiles'])==74
for row in new['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and sha((c/row['Path']).read_bytes())==row['Sha256']
a=read(c/'docs/supported-mfas.json');b=read(p/'overlay/docs/supported-mfas.json');registry_changes=[]
for x,y in zip(a,b,strict=True):
    assert {k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'}
    if x!=y:registry_changes.append((x['Module'],x['Function'],x['Arity']))
assert set(registry_changes)=={('erlang','spawn_opt',n) for n in [2,3,4,5]}|{('erlang','process_flag',2),('erlang','process_info',1),('erlang','process_info',2)}
for row in plan:
    target=row['TargetPath'];assert target.startswith('docs/') and '..' not in Path(target).parts and 'LICENSE.local' not in target
    src=r/row['SourcePath'];dst=c/target;raw=src.read_bytes();assert len(raw)==row['Bytes'] and sha(raw)==row['Sha256']
    assert (sha(dst.read_bytes()) if dst.exists() else None)==row['ParentSha256'],target
for row in plan:
    dst=c/row['TargetPath'];dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes((r/row['SourcePath']).read_bytes())
for entry in new['Entries']:
    for evidence in entry['Evidence']:assert sha((c/'docs'/evidence['Path']).read_bytes())==evidence['Sha256']
receipt=dict(Status='ROOT_APPROVED_PRIVATE351_PREFULL_METADATA_APPLIED_NOT_PUBLISHED',Candidate=c.relative_to(r).as_posix(),Files=plan,Counts=[10,24,553],SourceGuards=74,ChangedLedgerEntries=changed,NewVerifiedPromotions=0,RegistryLimitsOnlyChanges=registry_changes,AllNonLimits587Preserved=True,MainModified=False,FullExecuted=False)
out=r/'artifacts/spawn-priority351-full-base/metadata-applied.json';assert not out.exists();out.write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n');print(json.dumps(dict(Rows=len(plan),Counts=[10,24,553],SourceGuards=74,Sha256=sha(out.read_bytes()))))

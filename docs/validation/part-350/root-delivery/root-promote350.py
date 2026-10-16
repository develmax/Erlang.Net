from pathlib import Path
import json,hashlib,subprocess,collections
r=Path.cwd(); read=lambda p:json.loads(p.read_bytes()); sha=lambda b:hashlib.sha256(b).hexdigest().upper()
parent='712933b97a654a3b6ed42250902017bde0740f17'
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==parent
assert not subprocess.check_output(['git','diff','--name-only']).strip()
assert not subprocess.check_output(['git','diff','--cached','--name-only']).strip()
o=r/'artifacts/root-part350-delivery';o.mkdir(exist_ok=False)
c=r/'artifacts/additional-type-predicate-candidate';s=r/'artifacts/additional-type-predicate350-full-source3'
assert sha((s/'source-freeze.json').read_bytes())=='B2CA082349C03497A4992D9148D122848EE59FFF517940B30BAF49C4EB0D1AC5'
assert sha((r/'artifacts/additional-type-predicate350-full-independent/report.json').read_bytes())=='A880ED1A33BE32A6807B0A92B8609F41890F61105683C79A7C0241693476F42C'
owned=read(s/'source-freeze.json')['Files'];assert len(owned)==11
for row in read(s/'source-resource-closure.json')['Files']:
    assert sha((c/row['Path']).read_bytes())==row['Sha256'],row['Path']
for row in read(s/'unowned-main-raw.json')['Files']:
    assert sha((r/row['Path']).read_bytes())==row['Sha256'],row['Path']
registry=(r/'docs/supported-mfas.json').read_bytes()
assert sha(registry)=='3B2591EFD44F896F4F12ACC1A73428C4AD90FFFE1723E76B38CFDB5722157F29'
raw=read(c/'docs/supported-mfas.json');old=json.loads(registry)
assert len(raw)==len(old)==587
assert [{k:v for k,v in x.items() if k!='Limits'} for x in raw]==[{k:v for k,v in x.items() if k!='Limits'} for x in old]
rows={}
def add(path,b,origin):
    assert path not in rows and not path.startswith(('artifacts/','/')) and '..' not in Path(path).parts and 'LICENSE.local' not in path
    dst=o/'overlay'/path;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
    rows[path]=dict(SourcePath=dst.relative_to(r).as_posix(),TargetPath=path,Bytes=len(b),Sha256=sha(b),Origin=origin)
for row in owned:
    b=(c/row['Path']).read_bytes();assert sha(b)==row['Sha256'];add(row['Path'],b,'exact tested350 source')
plan=read(r/'artifacts/part350-docs-delivery-plan/final-plan.json')
for row in plan['Files']:
    b=(r/row['SourcePath']).read_bytes();assert sha(b)==row['Sha256'];assert row['ParentSha256']==(sha((r/row['TargetPath']).read_bytes()) if (r/row['TargetPath']).exists() else None);add(row['TargetPath'],b,row['SourcePath'])
p=r/'artifacts/spawn-opt350-known-classification-proposal';proposal=read(p/'proposal.json')
assert sha((p/'source-freeze.json').read_bytes())=='369CAC4F7D03B270F23688C83928A2D33081B608AE9AB1E91A532F85664827E6'
for row in read(p/'source-freeze.json')['Files']:assert sha((p/row['Path']).read_bytes())==row['Sha256']
base=read(r/'docs/mfa-compatibility.json');new=read(p/'ledger-candidate.json')
assert sha((r/'docs/mfa-compatibility.json').read_bytes())==proposal['BaseLedgerSha256']
assert base['VerifiedSourceFiles']==new['VerifiedSourceFiles'] and len(new['VerifiedSourceFiles'])==59
changed=[]
for a,b in zip(base['Entries'],new['Entries'],strict=True):
    if a!=b:
        assert a['Module']==b['Module']=='erlang' and a['Function']==b['Function']=='spawn_opt' and a['Arity']==b['Arity'] and a['Status']=='Needs verification' and b['Status']=='Known differences';changed.append(a['Arity'])
assert sorted(changed)==[2,3,4,5]
assert collections.Counter(x['Status'] for x in new['Entries'])=={'Verified compatible':10,'Known differences':23,'Needs verification':554}
anchors=read(p/'proof/docs/validation/part-350/spawn-options-audit/pinned-source-anchors.json')
for row in anchors['Files']:
    native=subprocess.check_output(['git','-C',str(r.parent/'otp'),'show',anchors['PinnedOtpCommit']+':'+row['Path']]);assert sha(native)==row['Sha256']
for row in read(p/'proof/docs/validation/part-350/spawn-options-audit/source-known-review.json')['ManagedSourceBindings']:assert sha((r/row['Path']).read_bytes())==row['Sha256']
for src in (p/'proof').rglob('*'):
    if src.is_file():add(src.relative_to(p/'proof').as_posix(),src.read_bytes(),str(src))
for name in ['proposal.json','source-freeze.json','prepare.py']:add('docs/validation/part-350/spawn-options-proposal/'+name,(p/name).read_bytes(),str(p/name))
add('docs/mfa-compatibility.json',(p/'ledger-candidate.json').read_bytes(),'post-full source-proven known differences')
note=('Post-full reporting correction: erlang:spawn_opt/2,3,4,5 move from Needs verification to Known differences because valid OTP priority, message_queue_data, heap, GC and scheduler options are rejected by the managed parser. This source-proven correction changes the final ledger to 10 verified,23 known differences,554 pending (erlang10/6/335); all583 other entries and59 source guards remain unchanged. The immutable full350 input ledger remains10/19/558; this is a later reporting change, not a rewritten full result. No new native execution or runtime implementation is claimed. The first documentation finalizer invocation refused a relative guard path before creating its overlay; the absolute-path invocation completed without SDK or Main mutation.')
for path in ['AGENTS.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/semantic-differences.md','docs/NEXT_STEPS.md']:
    dst=o/'overlay'/path;text=dst.read_text(encoding='utf-8');first,rest=text.split('\n',1);dst.write_text(first+'\n\n'+note+'\n'+rest,encoding='utf-8',newline='\n')
log=o/'overlay/docs/changelog/0054.md';log.write_text(log.read_text(encoding='utf-8').rstrip()+'\n\n'+note+'\n',encoding='utf-8',newline='\n');assert log.stat().st_size<=32768 and len(log.read_bytes().splitlines())<=400
for path,row in rows.items():
    b=(o/'overlay'/path).read_bytes();row.update(Bytes=len(b),Sha256=sha(b))
    dst=r/path;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(b)
(c/'docs/supported-mfas.json').write_bytes(registry)
receipt=dict(Status='ROOT_EXACT350_SOURCE_PROMOTION_AND_POST_FULL_KNOWN_REPORTING',ParentHead=parent,Files=list(rows.values()),ActualFullCounts=[10,19,558],FinalCounts=[10,23,554],RegistryCustomLimitsRestored=True,ProductionCSharpChanges=0,FinalizerRelativePathRefusalPreserved=True)
(o/'promotion-plan.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(dict(Files=len(rows),Counts=[10,23,554],ChangelogBytes=log.stat().st_size)))

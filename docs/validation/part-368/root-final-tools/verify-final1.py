from pathlib import Path
import collections,gzip,hashlib,json,re,shutil,subprocess
R=Path.cwd();O=R/'artifacts/queue-integration368';D=R/'docs/validation/part-368';C=D/'local-completion';H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();J=lambda p:json.loads(p.read_bytes())
full=J(O/'full3/completion.json');assert full['Status']=='PASS_CANONICAL_PIPELINE' and full['SourceChanges']==[]
rows=J(O/'full3/source-before.json')['Files'];assert len(rows)==3299
for row in rows:assert H(R/row['Path'])==row['Sha256'],row['Path']
ledger=J(R/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==647 and len(ledger['VerifiedSourceFiles'])==382
assert collections.Counter(x['Status']for x in ledger['Entries'])=={'Verified compatible':76,'Known differences':30,'Needs verification':541}
for row in ledger['VerifiedSourceFiles']:assert H(R/row['Path'])==row['Sha256']
evidence=0
for row in ledger['Entries']:
    for item in row['Evidence']:
        evidence+=1;p=(R/'docs'/item['Path']).resolve();assert p.is_relative_to((R/'docs').resolve()) and p.is_file() and H(p)==item['Sha256'],item['Path']
assert len([x for x in ledger['Entries']if x['Module']=='queue' and x['Status']=='Verified compatible'])==38
tests=J(R/'artifacts/tests.json');assert tests['Passed']==7599 and tests['Failed']==0
old=J(O/'baseline/supported-mfas.json');reg=J(R/'docs/supported-mfas.json');key=lambda x:(x['Module'],x['Function'],x['Arity']);by={key(x):x for x in reg}
assert len(by)==647 and len(old)==617
for row in old:
    after=by[key(row)];assert after==row if row['Module']!='queue' else {k:v for k,v in after.items()if k!='Limits'}=={k:v for k,v in row.items()if k!='Limits'}
before=(O/'baseline/changelog-0059.md').read_bytes();journal=(R/'docs/changelog/0059.md').read_bytes();assert journal.startswith(before)
assert len(journal)<=32768 and len(journal.splitlines())<=400 and b'{{'not in journal
previous=D.parent/'part-367'
for row in J(previous/'final-seal.json')['Files']:assert H(previous/row['Path'])==row['Sha256'],'Historical367 changed: '+row['Path']
for name in ['MODULE_READINESS.md','API_COMPATIBILITY.md','module-readiness.json','native-infrastructure.json','native-infrastructure.md']:
    p=R/'artifacts/readiness-final368'/name;q=R/'artifacts/tool-readiness-final368'/name;assert p.read_bytes()==q.read_bytes()==(C/name).read_bytes()
    if name in ['MODULE_READINESS.md','API_COMPATIBILITY.md','module-readiness.json']:assert (R/'docs'/name).read_bytes()==p.read_bytes()
ready=J(R/'docs/module-readiness.json');assert (ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'],ready['VerifiedCompatibleMfas'],ready['LocalPassedTests'])==(647,617,30,76,7599)
assert J(R/'docs/module-readiness-scope.json')['ComparisonTestReport']=='validation/part-367/local-completion/canonical-tests-before-reverse-delta.json'
rawfiles=0
for index in ['lossless-raw-index.json','lossless-extra-index.json']:
    for row in J(D/index)['Files']:
        p=D/row.get('StoredPath',row.get('Stored'));assert H(p)==row['StoredSha256'];raw=gzip.decompress(p.read_bytes());assert len(raw)==row['RawBytes'] and hashlib.sha256(raw).hexdigest().upper()==row['RawSha256'];rawfiles+=1
for label,folder in [('classifier-review','artifacts/queue368-classifier-review-peer1'),('final-readiness','artifacts/queue-integration368/final-readiness1')]:
    base=R/folder
    for p in base.rglob('*'):
        if p.is_file() and p.suffix in {'.json','.py','.md','.log'}:
            q=D/label/p.relative_to(base);q.parent.mkdir(parents=True,exist_ok=True);assert not q.exists();shutil.copyfile(p,q)
for name in ['classification.json','registry-restoration.json','acceptance-before-classification.json']:
    assert (C/name).exists()
oldsource=J(R/'artifacts/queue-integration367/final-source-closure.json');oldby={x['Path']:x['Sha256']for x in oldsource['Files']}
changed=[x for x in rows if oldby.get(x['Path'])!=x['Sha256']]
for row in changed:
    p=R/row['Path'];q=D/'final-source-delta'/row['Path'];q.parent.mkdir(parents=True,exist_ok=True);assert not q.exists();shutil.copyfile(p,q)
(C/'final-source-closure.json').write_text(json.dumps(dict(SourceInputs=3299,SourceGuards=382,PreviousSourceInputs=3254,ChangedOrAdded=changed,Files=rows,NoSourceChangeAfterFinalFull=True),indent=2)+'\n')
for name in ['canonical-full.stdout.log','canonical-full.stderr.log']:
    shutil.copyfile(O/'full3'/name,C/name)
for name in ['classify-final1.py','finish-docs1.py','run-readiness1.py','verify-final1.py']:
    q=D/'root-final-tools'/name;q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(O/name,q)
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=R).decode().strip()=='da8c65f79c80cfbce5f4d3eca44479d0490690e9'
assert subprocess.run(['git','diff','--cached','--quiet'],cwd=R).returncode==0
assert not subprocess.check_output(['git','ls-files','LICENSE.local.txt'],cwd=R).strip()
linkfiles=[R/'docs/PROGRESS.md',R/'docs/COMPATIBILITY.md',R/'docs/NEXT_STEPS.md',R/'docs/WORK_PLAN.md',C/'README.md']
for p in linkfiles:
    for value in re.findall(r'\]\(([^)]+)\)',p.read_text()):
        if '://'in value or value.startswith('#'):continue
        path=value.split('#',1)[0]
        if path:assert (p.parent/path).exists(),(p,value)
verification=dict(Status='PASS_ALL_FINAL_QUEUE368_GATES',Apis=647,Complete=76,QueueRegistered=38,QueueComplete=38,Tests=7599,IntegrationOutcomes=11,LocalPackages=2,OriginalNewObservations=5358,OldCoreObservations=960,NegativeControls=72,SourceInputs=3299,SourceGuards=382,EvidenceReferencesChecked=evidence,LosslessFilesChecked=rawfiles,FinalSourceDeltaFiles=len(changed),PreviousPart367SealUnchanged=True,CanonicalReadinessByteEqual=True,ImplementationPublished=False,GitIndexEmpty=True,LicenseUncommitted=True)
(C/'final-verification.json').write_text(json.dumps(verification,indent=2)+'\n')
files=[dict(Path=p.relative_to(D).as_posix(),Bytes=p.stat().st_size,Sha256=H(p))for p in sorted(D.rglob('*'))if p.is_file() and p.name!='final-seal.json']
(D/'final-seal.json').write_text(json.dumps(dict(Status='QUEUE38_COMPLETE_UNPUBLISHED',Files=files,SourceInputs=3299,SourceGuards=382,ImplementationPublished=False),indent=2)+'\n')
print(json.dumps(verification))

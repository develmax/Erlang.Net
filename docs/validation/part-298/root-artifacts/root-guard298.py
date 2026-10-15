from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes())
assert j(a/'root-part298-full-exit.json')['ExitCode']==0
assert b'Validation passed' in (a/'root-part298-full-validation.log').read_bytes()
f=j(a/'root-part298-final-source-freeze.json')
assert f['SourcePaths']==len(f['Files'])==6 and f['TokensExact']
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256']
t=j(a/'tests.json');old=j(r/'docs/validation/part-297/tests.json')
assert (t['Passed'],t['Failed'])==(6610,0)
names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
assert len(names)==len(set(names))==6610 and len(oldnames)==6609
assert all(x['Status']=='Passed' for x in t['Tests'])
assert [n for n in names if n in set(oldnames)]==oldnames
assert set(names)-set(oldnames)=={'runtime/nif-installed-current-image-inventory-and-cache'}
assert j(a/'integration-results.json')==j(r/'docs/validation/part-297/integration-results.json')
assert len(j(r/'docs/supported-mfas.json'))==502
b=a/'root-nif-inventory298-isolated'
assert j(b/'tokens-before.json')==j(b/'tokens-after.json')
assert j(b/'focus51-guard.json')['GroupCount']==51
out={'Status':'PASS_ACTUAL_MAIN6610_FULL_SOURCE6','Passed':6610,'Failed':0,'Historical6609OrderedNamesExact':True,'Added':list(set(names)-set(oldnames)),'SourcePaths':6,'TokensExact':True,'FocusedGroupsPassed':51,'IntegrationAndPackagesPassed':True,'Registry':502,'NativeExecuted':False,'PublicLoadNifImplemented':False,'Scope':'Current installed image inventory and immutable cache identity; fake managed NIF bindings'}
(a/'root-part298-main-completion-guard.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(out))

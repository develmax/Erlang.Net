from pathlib import Path
import json,hashlib,re
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes())
assert j(a/'root-part297-full-exit.json')['ExitCode']==0
assert b'Validation passed' in (a/'root-part297-full-validation.log').read_bytes()
f=j(a/'root-part297-final-source-freeze.json')
assert f['SourcePaths']==len(f['Files'])==16 and f['TokensExact']
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256']
t=j(a/'tests.json');old=j(r/'docs/validation/part-296/tests.json')
assert (t['Passed'],t['Failed'])==(6609,0)
names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
assert len(names)==len(set(names))==6609 and len(oldnames)==6608
assert all(x['Status']=='Passed' for x in t['Tests'])
assert [n for n in names if n in set(oldnames)]==oldnames
assert set(names)-set(oldnames)=={'runtime/nif-beam-exact-image-entry-and-closure-routing'}
assert j(a/'integration-results.json')==j(r/'docs/validation/part-296/integration-results.json')
assert len(j(r/'docs/supported-mfas.json'))==502
b=a/'root-nif-beam297-isolated'
assert j(b/'tokens-before.json')==j(b/'tokens-after.json')
assert j(b/'focus50-guard.json')['GroupCount']==50
out={'Status':'PASS_ACTUAL_MAIN6609_FULL_SOURCE16','Passed':6609,'Failed':0,'Historical6608OrderedNamesExact':True,'Added':list(set(names)-set(oldnames)),'SourcePaths':16,'TokensExact':True,'FocusedGroupsPassed':50,'IntegrationAndPackagesPassed':True,'Registry':502,'NativeExecuted':False,'PublicLoadNifImplemented':False,'Scope':'Exact-image BEAM named and closure dispatch, validated lambda publication; fake managed NIF bindings'}
(a/'root-part297-main-completion-guard.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(out))

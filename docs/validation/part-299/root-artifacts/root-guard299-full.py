from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper()
assert j(a/'root-part299-full-exit.json')['ExitCode']==0
log=(a/'root-part299-full-validation.log').read_text(encoding='utf-8-sig')
assert 'Validation passed' in log and log.count('Managed backend checks passed; native ABI and original OTP NOT executed.')==1
f=j(a/'root-part299-final-source-freeze.json');assert f['SourcePaths']==55 and f['CSharpPaths']==31 and f['TokensExactForCSharp']
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256']
t=j(a/'tests.json');old=j(r/'docs/validation/part-298/tests.json')
assert (t['Passed'],t['Failed'])==(6610,0) and all(x['Status']=='Passed' for x in t['Tests'])
assert [x['Name'] for x in t['Tests']]==[x['Name'] for x in old['Tests']]
assert j(a/'integration-results.json')==j(r/'docs/validation/part-298/integration-results.json')
assert len(j(r/'docs/supported-mfas.json'))==502
assert j(a/'root-nif-native299-isolated/tokens31-before.json')==j(a/'root-nif-native299-isolated/tokens31-after.json')
(a/'root-part299-main-completion-guard.json').write_text(json.dumps({'Status':'PASS_ACTUAL_MAIN6610_FULL_SOURCE55_CSHARP31','Passed':6610,'Failed':0,'Historical6610OrderedNamesExact':True,'SourcePaths':55,'CSharpPaths':31,'TokensExactForCSharp':True,'IntegrationAndPackagesPassed':True,'ManagedPrerequisiteToolRan':True,'Registry':502,'PublicLoadNifImplemented':False,'MainNativeRetestRequired':True},indent=2)+'\n')
print('PASS actualMain6610/full source55/CSharp31; actualMainnative retest still required')

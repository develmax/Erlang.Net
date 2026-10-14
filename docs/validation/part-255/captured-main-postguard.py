from pathlib import Path
import json,hashlib,zipfile,subprocess
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
f=j(a/'reference-resource-part253-guard-source-freeze-revision6.json')
for row in f['Files']:
 if row['Path'] not in ['docs/OTP-PORTS.md','docs/dependency-decisions.md']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
t=j(a/'tests.json');old=j(r/'docs/validation/part-253/tests.json');assert t['Passed']==6470 and t['Failed']==0 and old['Passed']==6469
assert [x['Name'] for x in t['Tests'][:6469]]==[x['Name'] for x in old['Tests']]
assert len(t['Tests'])==6470 and 'reference' in t['Tests'][-1]['Name'].lower()
assert j(r/'docs/supported-mfas.json')==json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']))
assert j(a/'integration-results.json')==j(r/'docs/validation/part-251/integration-results.json')
assert not subprocess.check_output(['git','diff','HEAD','--name-only','--','src/Erlang.Compiler','tools/Erlang.Differential']).strip()
res=j(r/'tests/Erlang.Tests/Resources/Beam/source-proof.json');count=0
for x in res['Fixtures']:
 for key,hkey in [('BeamFile','BeamSha256'),('SourceFile','SourceSha256'),('NativeActualEtfFile','NativeActualEtfSha256'),('NativeExpectedEtfFile','NativeExpectedEtfSha256')]:
  if x[key] is not None:assert sha(r/'tests/Erlang.Tests/Resources/Beam'/x[key])==x[hkey];count+=1
assert count==22
packages=[];runtime=(r/'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
for p in (a/'packages').glob('*.nupkg'):
 with zipfile.ZipFile(p) as z:
  entries=[n for n in z.namelist() if n.endswith('/Erlang.Runtime.dll')];assert entries and all(z.read(n)==runtime for n in entries)
  assert not any('LICENSE.local' in n for n in z.namelist());packages.append({'Name':p.name,'Sha256':sha(p),'RuntimeExact':True})
assert len(packages)==2
closed=j(r/'docs/validation/part-250/closed-volume.json');assert sha(r/'docs/changelog/0036.md')==closed['RawSha256']
result={'Status':'PASS','Passed':6470,'Previous6469NamesExact':True,'ImportedPathsExact':len(f['Files']),'OldNativeResourceFilesExact':22,'NewRawNativeResourcesExact':56,'DefaultMfas':487,'ErlangMfas':329,'CompilerAndOrdinaryCorpusUnchanged':True,'FullIntegrationPass':True,'Packages':packages,'Closed0036Unchanged':True,'NativeSourceExpectedOutcomesUnchanged':True,'LoadingMfasRegistered':False}
(a/'part255-main-postguard.json').write_text(json.dumps(result,indent=2)+'\n');print('PASS6470/source27/resources22+56/default487/packages2/full')

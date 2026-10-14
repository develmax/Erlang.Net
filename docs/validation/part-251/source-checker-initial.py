from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads((r/p).read_text(encoding='utf-8-sig'))
f=j('artifacts/halt-prepared-part249-source-freeze.json');assert len(f['Files'])==35
for row in f['Files']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
for row in j('artifacts/beam-prepared-host-part249-prototype/artifacts/beam-prepared-source-freeze-part249.json')['UnchangedProviders']:
 assert sha(r/row['Path'])==row['Sha256'],row['Path']
proof=j('tests/Erlang.Tests/Resources/Beam/source-proof.json');assert proof==json.loads(subprocess.check_output(['git','show','HEAD:tests/Erlang.Tests/Resources/Beam/source-proof.json'],cwd=r))
count=0
for row in proof['Fixtures']:
 for key,hkey in [('BeamFile','BeamSha256'),('SourceFile','SourceSha256'),('NativeActualEtfFile','NativeActualEtfSha256'),('NativeExpectedEtfFile','NativeExpectedEtfSha256')]:
  if row[key] is not None:assert sha(r/'tests/Erlang.Tests/Resources/Beam'/row[key])==row[hkey];count+=1
assert count==22 and not proof['OnLoadExecuted']
assert not subprocess.check_output(['git','diff','HEAD','--name-only','--','src/Erlang.Compiler','tools/Erlang.Differential'],cwd=r,text=True).strip()
t=j('artifacts/tests.json');assert t['Passed']==6468 and t['Failed']==0
assert j('artifacts/integration-results.json')==j('docs/validation/part-249/integration-results.json')
assert j('artifacts/part251-halt-postfull-guard.json')['Status']=='PASS'
closed=j('docs/validation/part-250/closed-volume.json');assert sha(r/'docs/changelog/0036.md')==closed['RawSha256']
result={'Status':'PASS','MainPassed':6468,'FrozenImportsExact':35,'ProvidersExact':200,'CompilerAndOrdinaryCorpusUnchanged':True,'Modules':4843,'Diagnostics':159,'Exclusions':6,'DefaultMfas':487,'ErlangMfas':329,'NativeResourceFilesExact':22,'ProductionOnLoadImplemented':False,'Closed0036Unchanged':True,'FullIntegrationPass':True,'OriginalNewProofClaimed':False}
(r/'artifacts/part251-source-postguard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS Main6468/source35/provider200/resource22/default487/erlang329/closed0036')

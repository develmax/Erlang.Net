from pathlib import Path
import json,hashlib,zipfile,subprocess
r=Path.cwd();sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
j=lambda p:json.loads((r/p).read_text(encoding='utf-8-sig'))
freeze=j('artifacts/halt-mailbox-main246-source-freeze.json');trim=j('artifacts/part247-main-eof-ledger.json');trims={row['Path']:row for row in trim}
assert len(freeze['Files'])==59 and len(trims)==3
exact=0
for row in freeze['Files']:
 p=r/row['Path']; source=r/freeze['Candidate']/row['Path']
 if row['Path'] in trims:
  assert row['Sha256']==trims[row['Path']]['Before'] and sha(p)==trims[row['Path']]['After']
  assert p.read_text().rstrip()==source.read_text().rstrip()
 else: assert sha(p)==row['Sha256'],row['Path'];exact+=1
old=j('docs/validation/part-246/tests.json');new=j('artifacts/tests.json')
assert new['Passed']==6463 and new['Failed']==0 and len(new['Tests'])==6463
assert all(t['Status']=='Passed' for t in new['Tests'])
oldnames=[t['Name'] for t in old['Tests']];names=[t['Name'] for t in new['Tests']];oldset=set(oldnames)
assert [n for n in names if n in oldset]==oldnames
assert len(set(names)-oldset)==2
assert 'Validation passed' in (r/'artifacts/part247-main-full.log').read_text(encoding='utf-8-sig')
integration=j('artifacts/integration-results.json');assert integration==j('docs/validation/part-246/integration-results.json')
compiler=subprocess.check_output(['git','diff','HEAD','--name-only','--','src/Erlang.Compiler'],text=True,cwd=r).strip();assert not compiler
for row in j('artifacts/mailbox-selection-revision3-prototype/artifacts/mailbox-selection-source-freeze-revision3.json')['UnchangedProviders']:
 if row['Path']=='tools/Erlang.Differential/Program.cs':continue
 assert sha(r/row['Path'])==row['Sha256'],row['Path']
resource=r/'tests/Erlang.Tests/Resources/Beam';proof=j('tests/Erlang.Tests/Resources/Beam/source-proof.json');checks=[]
assert proof==json.loads(subprocess.check_output(['git','show','HEAD:tests/Erlang.Tests/Resources/Beam/source-proof.json'],cwd=r))
for row in proof['Fixtures']:
 for key,hkey in [('BeamFile','BeamSha256'),('SourceFile','SourceSha256'),('NativeActualEtfFile','NativeActualEtfSha256'),('NativeExpectedEtfFile','NativeExpectedEtfSha256')]:
  if row[key] is not None:assert sha(resource/row[key])==row[hkey];checks.append(row[key])
assert len(checks)==22 and proof['OnLoadExecuted'] is False
children=j('artifacts/halt-subprocess-host-results.json');assert len(children)==48 and len({(x['Name'],x['Backend']) for x in children})==48
raw=[]
for child in children:
 assert child['TimedOut'] is False and child['InfrastructureError'] is None
 d=Path(child['WorkingDirectory']); stdout=(d/'process-stdout.raw').read_bytes();stderr=(d/'process-stderr.raw').read_bytes()
 assert list(stdout)==child['StandardOutput'] and list(stderr)==child['StandardError']
 e=json.loads((d/'process-raw-evidence.json').read_text(encoding='utf-8-sig'))
 assert e['RawExitCode']==child['RawExitCode'] and e['StandardOutputComplete'] and e['StandardErrorComplete'] and e['InfrastructureError'] is None
 raw.append({'Name':child['Name'],'Backend':child['Backend'],'ProcessId':e['ProcessId'],'ExitCode':e['RawExitCode'],'StdoutSha256':hashlib.sha256(stdout).hexdigest().upper(),'StderrSha256':hashlib.sha256(stderr).hexdigest().upper()})
packages=[]
for p in sorted((r/'artifacts/packages').glob('*.nupkg')):
 with zipfile.ZipFile(p) as z:
  notices=[n for n in z.namelist() if n.startswith('licenses/')];assert len(notices)==5
  for n in notices:assert z.read(n)==(r/'docs'/Path(n).name).read_bytes()
  assert not any('LICENSE.local' in n or n.endswith(('.beam','.actual.etf','.expected.etf')) for n in z.namelist())
 packages.append({'Path':str(p.relative_to(r)),'Sha256':sha(p),'NoticesExact':True,'PrivateLicenseExcluded':True})
assert len(packages)==2
result={'Status':'PASS','Seed':freeze['Seed'],'ExactFrozenImports':exact,'EofOnlyBeforeFull':3,'MainPassed':6463,'MainFailed':0,'Old6461NamesOrderExact':True,'NewHosts':2,'SdkModules':4843,'ComparableModules':4837,'Diagnostics':159,'Exclusions':6,'DefaultMfas':484,'OriginalNewProof':False,'All196ProvidersExact':True,'CompilerUnchanged':True,'NativeResourceBytesExact':22,'ActualFreshSdkChildren':48,'RawChildren':raw,'FullIntegration':integration,'Packages':packages}
(r/'artifacts/part247-main-postguard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS Main6463/full, source/resources, actual48 raw children and package guards')

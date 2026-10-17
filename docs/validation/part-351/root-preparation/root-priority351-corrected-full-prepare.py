from pathlib import Path
import collections,hashlib,json,re,shutil,subprocess

r=Path.cwd();c=r/'artifacts/spawn-priority351-full-corrected-candidate';seed=r/'artifacts/spawn-priority351-full-source3'
parent='add05fc61d8c6f5f024ffcdad1ea19aaea4e3725'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==parent
assert not subprocess.check_output(['git','diff','--name-only']).strip()
oldseed=r/'artifacts/spawn-priority351-full-source2';corrected=r/'artifacts/spawn-priority351-corrected-candidate2'
oldrows=read(oldseed/'source-resource-closure.json')['Files']
focus=read(r/'artifacts/spawn-priority351-corrected-preparation/focus-source-freeze.json')
for row in focus['Files']:assert sha(corrected/row['Path'])==row['Sha256'],row['Path']
summary=read(r/'artifacts/spawn-priority351-corrections-focus-actual1/summary.json');assert (summary['Passed'],summary['Failed'])==(59,0)
assert summary['SourceFreezeSha256']==sha(r/'artifacts/spawn-priority351-corrected-preparation/focus-source-freeze.json')
metadata=r/'artifacts/priority351-corrected-metadata-proposal/copy-plan.json'
assert sha(metadata)=='459B6844636AC0FB8A560F8F2A54AC7A1630B8D598936D2F0768424485F18B74'
overlays=read(metadata)['Files']
for row in overlays:
 assert sha(r/row['SourcePath'])==row['Sha256']
 if row['CandidateParentSha256']:assert sha(corrected/row['TargetPath'])==row['CandidateParentSha256']
 else:assert not (corrected/row['TargetPath']).exists()
owned={row['Path'] for row in read(oldseed/'source-freeze.json')['Files']}
owned|={row['Path'] for row in read(r/'artifacts/spawn-priority351-corrected-preparation/corrections.json')['Changes'] if not row['Path'].startswith('tools/Erlang.Tool/')}
assert len(owned)==49
tracked=[p for p in subprocess.check_output(['git','ls-files','-z']).decode().split('\0') if p]
non_docs={p for p in tracked if not p.startswith('docs/')}
c.mkdir(exist_ok=False);seed.mkdir(exist_ok=False)
for row in oldrows:
 p=row['Path'];dst=c/p;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes((corrected/p).read_bytes())
for p in non_docs-owned:
 dst=c/p;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes((r/p).read_bytes())
(c/'docs/supported-mfas.json').write_bytes((oldseed/'registry-before.json').read_bytes())
for row in overlays:
 dst=c/row['TargetPath'];dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes((r/row['SourcePath']).read_bytes())
ledger=read(c/'docs/mfa-compatibility.json');counts=dict(collections.Counter(e['Status'] for e in ledger['Entries']))
assert counts=={'Verified compatible':10,'Known differences':24,'Needs verification':553}
assert len(ledger['VerifiedSourceFiles'])==74
for row in ledger['VerifiedSourceFiles']:assert sha(c/row['Path'])==row['Sha256']
inputs={row['Path'] for row in read(oldseed/'readiness-inputs.json')['Files']}
for entry in ledger['Entries']:
 for e in entry['Evidence']:
  p='docs/'+e['Path'];assert sha(c/p)==e['Sha256'];inputs.add(p)
plans=[]
for path,wf,var in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256'),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256')]:
 raw=(c/path).read_bytes();assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf')
 for row in json.loads(raw)['Files']:
  b=(c/row['Path']).read_bytes();b=b.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else b
  assert len(b)==row['Bytes'] and hashlib.sha256(b).hexdigest().upper()==row['Sha256'],row['Path']
 assert re.findall(var+r':\s*([0-9A-F]{64})',(c/wf).read_text())==[sha(c/path)]
 plans.append({'Path':path,'Sha256':sha(c/path),'Files':len(read(c/path)['Files'])})
def row(p):return {'Path':p,'Bytes':(c/p).stat().st_size,'Sha256':sha(c/p)}
def save(name,value):
 with (seed/name).open('x',encoding='utf-8',newline='\n') as f:json.dump(value,f,indent=2);f.write('\n')
for p in owned:
 dst=seed/'source'/p;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes((c/p).read_bytes())
save('source-freeze.json',{'Status':'ROOT_FROZEN_CORRECTED351_BEFORE_FULL','ParentHead':parent,'Files':[row(p) for p in sorted(owned)],'ProductionFiles':21,'FullExecuted':False,'MainModified':False})
stable={row['Path'] for row in oldrows}|non_docs|inputs|{row['TargetPath'] for row in overlays};stable.discard('docs/supported-mfas.json')
save('source-resource-closure.json',{'ParentHead':parent,'GeneratedRegistryExcluded':True,'Files':[row(p) for p in sorted(stable)]})
packages=[e['Path'] for e in read(oldseed/'package-resources.json')['Files']]
assert len(packages)==6
for p in packages:assert sha(c/p)==sha(r/p)
save('package-resources.json',{'Files':[row(p) for p in packages]})
save('readiness-inputs.json',{'Files':[row(p) for p in sorted(inputs)]})
save('unowned-main-raw.json',{'ParentHead':parent,'Files':[row(p) for p in sorted(non_docs-owned)],'RawVariances':0})
for name in ['previous-tests.json','registry-before.json']:shutil.copyfile(oldseed/name,seed/name)
added=read(oldseed/'added-tests.json');added['SourceFreezeSha256']=sha(seed/'source-freeze.json');save('added-tests.json',added)
save('preflight.json',{'Status':'ROOT_SOURCE_ONLY_CORRECTED351_PREFLIGHT_REQUIRES_INDEPENDENT_GATE','ParentHead':parent,'SourceFreezeSha256':sha(seed/'source-freeze.json'),'SourceClosureSha256':sha(seed/'source-resource-closure.json'),'OwnedPaths':len(owned),'SourceClosureRows':len(stable),'ReadinessInputs':len(inputs),'CompatibilityCounts':counts,'CurrentRuntimeBindings':74,'ExpectedFullTestsArithmetic':7297,'SourcePlans':plans,'FocusedManaged59':True,'Original351Executed':False})
runner=(oldseed/'run-full.py').read_text(encoding='utf-8').replace("r/'artifacts/spawn-priority351-full-candidate'","r/'artifacts/spawn-priority351-full-corrected-candidate'")
(seed/'run-full.py').write_text(runner,encoding='utf-8',newline='\n')
config={'ReadyForExecution':True,'CandidateRelativePath':c.relative_to(r).as_posix(),'SeedRelativePath':seed.relative_to(r).as_posix(),'OutputRelativePath':'artifacts/spawn-priority351-full-actual2','ParentHead':parent,'CompatibilityCounts':counts,'CurrentRuntimeSourceBindings':74}
for key,name in [('SourceFreezeSha256','source-freeze.json'),('SourceClosureSha256','source-resource-closure.json'),('PreviousTestsSha256','previous-tests.json'),('RegistryBeforeSha256','registry-before.json'),('PackageResourcesSha256','package-resources.json'),('ReadinessInputsSha256','readiness-inputs.json'),('AddedTestsSha256','added-tests.json')]:config[key]=sha(seed/name)
save('configuration.json',config)
print(json.dumps({'ConfigurationSha256':sha(seed/'configuration.json'),'SourceFreezeSha256':config['SourceFreezeSha256'],'SourceClosureSha256':config['SourceClosureSha256'],'Owned':len(owned),'Closure':len(stable),'RunnerSha256':sha(seed/'run-full.py')}))

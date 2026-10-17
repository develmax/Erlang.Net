"""Read-only final source/input admission; never starts SDK or full validation."""
from pathlib import Path
import ast,collections,hashlib,json,re,subprocess
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent;S=R/'artifacts/spawn-priority351-full-source2';C=R/'artifacts/spawn-priority351-full-candidate'
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
def verify(rows):
 for row in rows:
  p=C/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256'],row['Path']
source=rd(S/'source-freeze.json');closure=rd(S/'source-resource-closure.json');config=rd(S/'configuration.json')
assert h(S/'source-freeze.json')=='5DF99C8F91ECDFACC0EE79138A7A412F8BE211133F0B284D88A7303F3671E238'
assert h(S/'source-resource-closure.json')=='554A77C7298621BE17FB5DAD19148A166C25BFC7D07356C45A694873339A9C46'
assert h(S/'configuration.json')=='B550600ED0886C0840FE78F7EF343EC9F75D5E9BB37AC20B8F811CE3F489D438'
assert len(source['Files'])==43 and sum(x['Path'].endswith('.cs') for x in source['Files'])==36 and len(closure['Files'])==3002
verify(source['Files']);verify(closure['Files'])
assert 'docs/supported-mfas.json' not in {x['Path'] for x in source['Files']+closure['Files']}
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,text=True).strip()==source['ParentHead']==config['ParentHead']=='c1f219f31726fc93e537080f400651d300c47d72'
assert config['ReadyForExecution'] and config['CandidateRelativePath']=='artifacts/spawn-priority351-full-candidate' and config['SeedRelativePath']=='artifacts/spawn-priority351-full-source2'
assert not (R/config['OutputRelativePath']).exists()
for path,key in [('source-freeze.json','SourceFreezeSha256'),('source-resource-closure.json','SourceClosureSha256'),('previous-tests.json','PreviousTestsSha256'),('registry-before.json','RegistryBeforeSha256'),('package-resources.json','PackageResourcesSha256'),('readiness-inputs.json','ReadinessInputsSha256'),('added-tests.json','AddedTestsSha256')]:assert h(S/path)==config[key]
permanent=R/'artifacts/spawn-priority351-permanent-preparation2';owned_before=rd(permanent/'source-freeze.json')['Files'];assert len(owned_before)==40
allowed={'tests/Erlang.Tests/Program.cs','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml'}
for row in owned_before:
 if row['Path'] not in allowed:assert h(C/row['Path'])==row['Sha256']
program='tests/Erlang.Tests/Program.cs';a=(permanent/'source'/program).read_text();b=(C/program).read_text();assert re.sub(r'\s+','',a)==re.sub(r'\s+','',b)
style=rd(R/'artifacts/spawn-priority351-full-style/changes.json');assert len(style['Files'])==1 and style['Files'][0]['Path']==program and style['Files'][0]['BeforeSha256']==h(permanent/'source'/program) and style['Files'][0]['Sha256']==h(C/program)
attrs=(R/'.gitattributes').read_bytes();assert (C/'.gitattributes').read_bytes()==attrs+b'\ndocs/validation/part-351/** binary\n'
scope=rd(C/'docs/module-readiness-scope.json');oldscope=rd(R/'docs/module-readiness-scope.json');assert scope['ComparisonTestReport']=='validation/part-350/full/tests.json'
assert {k:v for k,v in scope.items() if k not in ['Components','ComparisonTestReport']}=={k:v for k,v in oldscope.items() if k not in ['Components','ComparisonTestReport']}
for a,b in zip(oldscope['Components'],scope['Components'],strict=True):
 if a['Project']=='src/Erlang.Runtime':
  assert {k:v for k,v in a.items() if k!='Implemented'}=={k:v for k,v in b.items() if k!='Implemented'} and b['Implemented'][:-1]==a['Implemented']
 else:assert a==b
metadata=rd(R/'artifacts/priority351-prefull-metadata-proposal/copy-plan.json')['Files'];assert len(metadata)==175
for row in metadata:
 p=C/row['TargetPath'];assert h(p)==h(R/row['SourcePath'])==row['Sha256'] and p.stat().st_size==row['Bytes']
applied=rd(R/'artifacts/spawn-priority351-full-base/metadata-applied.json');assert len(applied['HistoricalInputsAdded'])==5
for row in applied['HistoricalInputsAdded']:assert h(C/row['Path'])==h(R/row['Path'])==row['Sha256']
verify(rd(S/'package-resources.json')['Files']);assert len(rd(S/'package-resources.json')['Files'])==6
for row in rd(S/'package-resources.json')['Files']:assert h(C/row['Path'])==h(R/row['Path'])
verify(rd(S/'readiness-inputs.json')['Files']);assert len(rd(S/'readiness-inputs.json')['Files'])==36
verify(rd(S/'unowned-main-raw.json')['Files'])
for row in rd(S/'unowned-main-raw.json')['Files']:assert h(C/row['Path'])==h(R/row['Path'])
ledger=rd(C/'docs/mfa-compatibility.json');old=rd(R/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==len(old['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==74
assert dict(collections.Counter(x['Status'] for x in ledger['Entries']))==config['CompatibilityCounts']=={'Verified compatible':10,'Known differences':24,'Needs verification':553}
bindings={x['Path']:x['Sha256'] for x in ledger['VerifiedSourceFiles']};assert len(bindings)==74
for path,value in bindings.items():assert path.startswith('src/') and h(C/path)==value
assert {x['Path'] for x in old['VerifiedSourceFiles']}<=set(bindings)
changed=[]
for a,b in zip(old['Entries'],ledger['Entries'],strict=True):
 assert (a['Module'],a['Function'],a['Arity'])==(b['Module'],b['Function'],b['Arity'])
 if a!=b:
  changed.append((a['Module'],a['Function'],a['Arity']))
  assert b['Evidence'][:len(a['Evidence'])]==a['Evidence']
  if a['Status']=='Verified compatible':assert {k:v for k,v in a.items() if k!='Evidence'}=={k:v for k,v in b.items() if k!='Evidence'}
  elif a['Module']=='erlang' and a['Function']=='spawn_opt':assert a['Status']==b['Status']=='Known differences'
  else:assert (a['Module'],a['Function'],a['Arity'])==('erlang','statistics',1) and a['Status']=='Needs verification' and b['Status']=='Known differences'
assert len(changed)==15
evidence={}
for entry in ledger['Entries']:
 for row in entry['Evidence']:
  p=C/'docs'/row['Path'];assert h(p)==row['Sha256'];evidence[row['Path']]=row['Sha256']
assert len(evidence)==31
before=rd(S/'registry-before.json');original=rd(R/'docs/supported-mfas.json');assert len(before)==len(original)==587 and h(C/'docs/supported-mfas.json')==h(S/'registry-before.json')
registry_changes=[]
for a,b in zip(original,before,strict=True):
 assert {k:v for k,v in a.items() if k!='Limits'}=={k:v for k,v in b.items() if k!='Limits'}
 if a['Limits']!=b['Limits']:registry_changes.append((a['Module'],a['Function'],a['Arity']))
assert set(registry_changes)=={('erlang','spawn_opt',n) for n in [2,3,4,5]}|{('erlang','process_flag',2),('erlang','process_info',1),('erlang','process_info',2)}
previous=rd(S/'previous-tests.json');assert previous['Passed']==len(previous['Tests'])==7286 and previous['Failed']==0 and all(x['Status']=='Passed' for x in previous['Tests'])
assert h(S/'previous-tests.json')==h(C/'docs'/scope['ComparisonTestReport'])=='4D8F1EDACF32E26B882EF4E3D9F16F0F613C6C3DCDDADC4A27A0043C59A8F4F4'
def constants(path):return {k:json.loads(v) for k,v in re.findall(r'(?:public|internal) const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;',path.read_text())}
hostnames=constants(C/'tests/Erlang.Tests/SpawnPriorityTestNames.cs');casenames=constants(C/'tools/Erlang.Differential/SpawnPriorityCaseNames.cs');programtext=(C/program).read_text();cat=(C/'tools/Erlang.Differential/SpawnPriorityCases.cs').read_text()
hosts=[hostnames[k] for k in re.findall(r'Test\(\s*Erlang.Tests.SpawnPriorityTestNames\.(\w+)\s*,',programtext)];compiled=['oracle/compiled-module/'+casenames[k] for k in re.findall(r'SpawnPriorityCaseNames\.(\w+)\s*,',cat)]
assert len(hosts)==8 and len(compiled)==3 and programtext.index('foreach (var fixture in Erlang.Differential.CompiledModuleCases.All)')<programtext.index('Test(\n    Erlang.Tests.SpawnPriorityTestNames.')
added=rd(S/'added-tests.json');assert added['SourceFreezeSha256']==h(S/'source-freeze.json') and added['Names']==compiled+hosts and len(set(added['Names']))==11
assert set(added['Names']).isdisjoint(x['Name'] for x in previous['Tests']) and h(S/'added-tests.json')=='95EC07A819B645E011722924428C7188D2B98F9FC0888898E984D776930A9C7D'
plans=[]
for path,wf,var,count in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1262),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1287)]:
 raw=(C/path).read_bytes();assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf');plan=json.loads(raw);oldplan=rd(R/path)
 assert len(plan['Files'])==count and {k:v for k,v in plan.items() if k!='Files'}=={k:v for k,v in oldplan.items() if k!='Files'}
 oldrows={x['Path']:x for x in oldplan['Files']};newrows={x['Path']:x for x in plan['Files']};assert len(newrows)==count and set(oldrows)<=set(newrows) and len(set(newrows)-set(oldrows))==12
 for row in plan['Files']:
  raw=(C/row['Path']).read_bytes();assert row['HashMode'] in ['TextLF','BinaryExact'];raw=raw.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else raw
  assert len(raw)==row['Bytes'] and hashlib.sha256(raw).hexdigest().upper()==row['Sha256']
  if row['Path'] in oldrows:assert row['HashMode']==oldrows[row['Path']]['HashMode']
 found=re.findall(var.encode()+rb': ([0-9A-F]{64})',(C/wf).read_bytes());assert found==[h(C/path).encode()]
 plans.append(dict(Path=path,Rows=count,Sha256=h(C/path),AllRowsVerified=True))
runner=S/'run-full.py';assert h(runner)=='049B537133EA4AE7D55084FCF25B7D198D9AB76974B8826C96A7B9DD183ADE61';body=runner.read_text();ast.parse(body)
assert "assert sha(seed/'added-tests.json')==config['AddedTestsSha256']" in body and body.index("assert sha(seed/'added-tests.json')")<body.index('o.mkdir(exist_ok=False)')
assert "peer['SourceFreezeSha256']==source and peer['SourceClosureSha256']==closure" in body
assert "assert [n for n in names if n in oldset]==previous" in body and "[n for n in names if n not in oldset]==added['Names']" in body
result=dict(Status='PASS_INDEPENDENT_FINAL351_SOURCE2_PREFLIGHT_FOR_ROOT_CANONICAL_FULL',SourceFreezeSha256=h(S/'source-freeze.json'),SourceClosureSha256=h(S/'source-resource-closure.json'),ConfigurationSha256=h(S/'configuration.json'),RunnerSha256=h(runner),ParentHead=source['ParentHead'],OwnedPaths=43,OwnedCSharp=36,StableClosureRows=3002,RegistryExcludedFromStableClosure=True,MetadataOverlayRows175RawExact=True,HistoricalInputs5RawExactMain=True,ReadinessInputs=36,PackageResources=6,CurrentRuntimeSourceBindings=74,ReferencedEvidenceRows=31,AllGuardsAndEvidenceRehashed=True,CompatibilityCounts=config['CompatibilityCounts'],TenPriorVerifiedScopesAndEvidencePrefixPreserved=True,OtherStatusChangeOnlyStatistics1PendingToKnown=True,FourSpawnOptLimitsRefinedKnownUnchanged=True,RegistryRows=587,RegistryNonLimitsAndOrderExactMain=True,IntentionalRegistryLimitsChanges=registry_changes,Other580CustomLimitsUnchanged=True,PreviousTests=7286,ExpectedNewTestNamesInActualSourceOrder=added['Names'],ExpectedFullCountArithmeticOnly=7297,AddedTestsSha256=h(S/'added-tests.json'),Production20Host10Portable3Lineage='Exact AFE permanent/focused inputs; only Program7call whitespace reflow plus declared attrs/scope/ledger/reallocation changes.',StyleOnlyProgramChanged=True,Plans=plans,InitialOrderCorrectionSha256=h(S/'order-correction.json'),Source1BadOrderNeverExecuted=True,CanonicalFullAdmissible=True,RequiredAfterFull='Root must independently inspect settled full0/all7297/11integrations/2packages/12fresh boundedchildren, stable source closure and raw registry; restore all587 intended prefull customLimits from registry-before, retaining generatedraw separately. No completion claim from this preflight.',SdkExecuted=False,NativeExecuted=False,MainModified=False,NetworkUsed=False)
out=O/'report.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',newline='\n');print(h(out))

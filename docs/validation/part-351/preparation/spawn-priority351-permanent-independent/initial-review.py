from pathlib import Path
import hashlib,json,re
R=Path(__file__).resolve().parents[2];P=R/'artifacts/spawn-priority351-permanent-preparation';C=R/'artifacts/spawn-priority351-permanent-candidate';O=Path(__file__).resolve().parent
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
f=P/'source-freeze.json';assert h(f)=='D74CCF45D969027058201E4EC987E34EA0A5F045B467F14068510B0522647ED4'
source=rd(f);assert len(source['Files'])==40;owned={x['Path'] for x in source['Files']}
for x in source['Files']:
 a=C/x['Path'];assert a.stat().st_size==x['Bytes'] and h(a)==h(P/'source'/x['Path'])==x['Sha256']
closure=rd(P/'source-resource-closure.json')['Files'];assert len(closure)==2826
for x in closure:
 a=C/x['Path'];assert a.stat().st_size==x['Bytes'] and h(a)==x['Sha256']
assert not any('Erlang.Priority351.Focus' in x['Path'] or '/bin/' in x['Path'] or '/obj/' in x['Path'] or 'LICENSE.local' in x['Path'] for x in closure)
unowned=[x for x in rd(P/'baseline-main-raw.json')['Files'] if x['Path'] not in owned]
for x in unowned:assert h(C/x['Path'])==h(R/x['Path'])==x['Sha256']
previous=P.parent/'spawn-priority351-preparation'
for x in rd(previous/'runtime-styled-source-freeze2.json')['Files']:assert h(C/x['Path'])==x['Sha256']
oldfocus=R/'artifacts/spawn-priority351-candidate/tests/Erlang.Priority351.Focus'
for x in oldfocus.glob('SpawnPriority*.cs'):assert x.read_bytes()==(C/'tests/Erlang.Tests'/x.name).read_bytes()
program='tests/Erlang.Tests/Program.cs';a=(R/program).read_bytes();b=(C/program).read_bytes()
lines=b.splitlines(keepends=True);added=[x for x in lines if x.startswith(b'Test(Erlang.Tests.SpawnPriorityTestNames.')];assert len(added)==8
assert b''.join(x for x in lines if x not in added).replace(b'\r\n',b'\n').replace(b'\n\n\n',b'\n\n')==a.replace(b'\r\n',b'\n').replace(b'\n\n\n',b'\n\n')
names=dict(re.findall(r'internal const string (\w+) = "([^"]+)";',(C/'tests/Erlang.Tests/SpawnPriorityTestNames.cs').read_text()));assert len(names)==len(set(names.values()))==8
for line in added:assert re.search(rb'SpawnPriorityTestNames\.(\w+), Erlang.Tests.SpawnPriority\w+HostChecks\.\w+\);',line)
catalog='tools/Erlang.Differential/CompiledModuleCases.cs';before=(R/catalog).read_text();after=(C/catalog).read_text();assert after.replace('.. NumericBoundaryCases.Modules,\n        .. SpawnPriorityCases.Modules','.. NumericBoundaryCases.Modules')==before
sources=(C/'tools/Erlang.Differential/SpawnPriorityCaseSources.cs').read_text();decoded={}
for key,body,indent in re.findall(r'public const string (\w+) = """\n(.*?)\n([ \t]*)""";',sources,re.S):
 decoded[key]='\n'.join(line[len(indent):] if line.startswith(indent) else '' for line in body.split('\n'))
assert len(decoded)==3
for key,name in [('FirstObservation','priority_first_observation'),('FlagOldAndDefault','priority_flag_old_and_default'),('DuplicateInvalid','priority_duplicate_invalid')]:assert decoded[key]==(oldfocus/'Portable'/(name+'.erl')).read_text()
case_names=dict(re.findall(r'public const string (\w+) = "([^"]+)";',(C/'tools/Erlang.Differential/SpawnPriorityCaseNames.cs').read_text()))
cat=(C/'tools/Erlang.Differential/SpawnPriorityCases.cs').read_text();pairs=re.findall(r'new\(\s*SpawnPriorityCaseNames\.(\w+),\s*SpawnPriorityCaseSources\.(\w+),',cat);assert len(pairs)==3
assert all(n==s for n,s in pairs) and case_names['Done']=='done' and cat.count('Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(SpawnPriorityCaseNames.Done))')==3
plan_results=[]
for path,wf,var,count in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1262),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1287)]:
 old=rd(R/path);new=rd(C/path);assert len(new['Files'])==count
 assert {k:v for k,v in old.items() if k!='Files'}=={k:v for k,v in new.items() if k!='Files'}
 oldrows={x['Path']:x for x in old['Files']};newrows={x['Path']:x for x in new['Files']};assert len(oldrows)==len(old['Files']) and len(newrows)==len(new['Files']) and set(oldrows)<=set(newrows)
 addedpaths=sorted(set(newrows)-set(oldrows));assert len(addedpaths)==12 and all(x.startswith('src/') for x in addedpaths)
 changed=[]
 for pathrow,row in newrows.items():
  raw=(C/pathrow).read_bytes();assert row['HashMode'] in ['TextLF','BinaryExact'];data=raw.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else raw
  assert len(data)==row['Bytes'] and hashlib.sha256(data).hexdigest().upper()==row['Sha256']
  if pathrow in oldrows:
   assert oldrows[pathrow]['HashMode']==row['HashMode']
   if row!=oldrows[pathrow]:changed.append(pathrow)
 assert len([x for x in changed if x.startswith('src/')])==8
 raw=(C/path).read_bytes();assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf')
 oldwf=(R/wf).read_bytes();newwf=(C/wf).read_bytes();pattern=rb'('+var.encode()+rb': )([0-9A-F]{64})';assert len(re.findall(pattern,oldwf))==len(re.findall(pattern,newwf))==1
 assert re.search(pattern,oldwf).group(2).decode()==h(R/path) and re.search(pattern,newwf).group(2).decode()==h(C/path)
 assert re.sub(pattern,lambda m:m.group(1)+b'0'*64,oldwf)==re.sub(pattern,lambda m:m.group(1)+b'0'*64,newwf)
 plan_results.append(dict(Path=path,Rows=count,AddedProductionRows=addedpaths,ChangedRows=changed,AllRowsRehashed=True,HashPoliciesAndNonFilesUnchanged=True,RawLfAndWorkflowSealVerified=True,Sha256=h(C/path)))
base=rd(R/'docs/validation/part-350/full/tests.json');assert base['Passed']==7286 and base['Failed']==0
newnames=list(names.values())+['oracle/compiled-module/'+case_names[k] for k,_ in pairs];assert len(newnames)==len(set(newnames))==11 and set(newnames).isdisjoint(x['Name'] for x in base['Tests'])
assert (C/'docs/mfa-compatibility.json').read_bytes()==(R/'docs/mfa-compatibility.json').read_bytes()
v=dict(Status='PASS_SOURCE_ONLY_PERMANENT_PRIORITY351_INTEGRATION_AND_PLANS_REQUIRES_STYLE_AND_BINDING_GATE',SourceFreezeSha256=h(f),SourceClosureSha256=h(P/'source-resource-closure.json'),SourceRows40Rehashed=True,ClosureRows=2826,UnownedRowsRawExactMain=len(unowned),Production20ExactActuallyFocusedRename2=True,Host10ExactActuallyFocused=True,FocusedActualIndependentGuardSha256=h(R/'artifacts/spawn-priority351-focused-independent/report.json'),ProgramOnlyAdditive8RegistrationsDecodedTextAndUnrelatedStatementsPreserved=True,CatalogOnlyAppend3=True,Portable3ExactActuallyFocusedSources=True,DeclaredNewNames=newnames,PreviousActualTests=7286,ExpectedTestCountArithmeticOnly=7297,Plans=plan_results,CurrentLedger59ByteExactButIntentionallyStaleForModifiedProduction=True,NoNewExports=True,BlockingSemanticFindings=[],NarrowPermanentFocusAdmissibleAfterRequiredStyleCheck=True,FullValidationAdmissible=False,RemainingFullGates=['Run appropriate SourceLayout check/write and bind any trivia-only changes before fresh sourcefreeze.','Root/Pub current59-to-reviewed-new-source binding update must admit modified shared paths; stale copied ledger cannot authorize full/readiness.','Future proof attributes, package/readiness closure, current350 published parent and actual7286 baseline must be frozen for full; this is not a whole-feature completion claim.'],InitialPreparationRefusalSha256=h(P/'initial-preparation-refusal.json'),SdkExecuted=False,NativeExecuted=False,MainModified=False,NetworkUsed=False)
out=O/'report.json';assert not out.exists();out.write_text(json.dumps(v,indent=2)+'\n',newline='\n');print(h(out))

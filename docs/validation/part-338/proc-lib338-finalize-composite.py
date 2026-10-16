from pathlib import Path
import json,hashlib,shutil
r=Path(__file__).resolve().parent.parent;c=r/'artifacts/proc-lib338-candidate';o=r/'artifacts/proc-lib338-full-source'
h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'))
def save(name,value):
 with (o/name).open('xb') as f:f.write((json.dumps(value,indent=2)+'\n').encode())
owned=[row['Path'] for row in j(o/'source-freeze.json')['Files']];closure=j(o/'source-resource-closure.json')['Files'];package=j(o/'package-resources.json')['Files'];before=j(o/'registry-before.json')
def exact():
 for row in closure+package:
  raw=(c/row['Path']).read_bytes();assert len(raw)==row['Bytes'] and h(raw)==row['Sha256'],row['Path']
assert j(o/'full-exit.json')['ExitCode']==1 and j(o/'full-exit.json')['Settled'] and not j(o/'full-exit.json')['TimedOut']
assert j(o/'integration-continuation-exit.json')['ExitCode']==0 and j(o/'integration-continuation-exit.json')['Settled']
original=(c/'tools/validate.ps1').read_text();start=original.index("    Invoke-DotNet @('run', '--project', 'tools/Erlang.Tool'");end=original.index('} finally { Pop-Location }',start)
continuation=(r/'artifacts/proc-lib338-readiness-integration-continuation.ps1').read_text();assert original[start:end] in continuation
repair=j(o/'readiness-missing-input-repair.json');assert h((c/repair['Path']).read_bytes())==repair['Sha256']
exact();save('validation-suffix-binding.json',{'CanonicalValidateSha256':h((c/'tools/validate.ps1').read_bytes()),'SuffixFirstLine':original[:start].count('\n')+1,'SuffixSha256':h(original[start:end].encode()),'SuffixTextExact':True,'TestsRepeated':False})
report=j(c/'artifacts/tests.json');names=[row['Name'] for row in report['Tests']];old=j(r/'docs/validation/part-337/final-candidate/tests.json');oldnames=[row['Name'] for row in old['Tests']];oldset=set(oldnames)
assert report['Failed']==0 and report['Passed']==len(names)==len(set(names))==7105 and all(row['Status']=='Passed' for row in report['Tests'])
assert len(oldnames)==7017 and [name for name in names if name in oldset]==oldnames
added=[name for name in names if name not in oldset];assert len(added)==88
integrations=j(c/'artifacts/integration-results.json');expected={'Readiness':'Passed','PackagedReadiness':'Passed','Build':'Passed','Tests':'Passed','OracleUnavailableReport':'Expected failure recorded','HybridOutput':'Hello World','Incremental':'Passed','DisabledPreprocessing':'Expected failure','CleanRebuild':'Passed','PackageConsumer':'Passed','LocalTool':'Passed'};assert integrations==expected
raw=(c/'docs/supported-mfas.json').read_bytes();registry=json.loads(raw);assert j(o/'raw-supported-mfas.json')==registry;key=lambda row:(row['Module'],row['Function'],row['Arity']);oldrows={key(row):row for row in before}
assert len(registry)==len({key(row) for row in registry})==576
newrows=[row for row in registry if key(row) not in oldrows];assert len(newrows)==36 and {row['Module'] for row in newrows}=={'proc_lib','logger','application','error_logger'}
for row in registry:
 if key(row) in oldrows:assert {k:v for k,v in row.items() if k!='Limits'}=={k:v for k,v in oldrows[key(row)].items() if k!='Limits'}
restored=[oldrows.get(key(row),row) for row in registry];(c/'docs/supported-mfas.json').write_bytes((json.dumps(restored,indent=2)+'\n').encode());assert all(next(row for row in restored if key(row)==k)==v for k,v in oldrows.items())
for name,source in [('tests.json',c/'artifacts/tests.json'),('integration-results.json',c/'artifacts/integration-results.json'),('supported-mfas.json',c/'docs/supported-mfas.json')]:(None if (o/name).exists() and (o/name).read_bytes()==source.read_bytes() else shutil.copyfile(source,o/name))
exact();guard={'Status':'PASS_ACTUAL_ISOLATED338_CUMULATIVE_TESTS_AND_CONTINUATION','Passed':report['Passed'],'Failed':report['Failed'],'PreviousOrderedNames':len(oldnames),'PreviousOrderedNamesUnchanged':True,'AddedTestNames':added,'Registry':len(registry),'PreviousRegistry':len(before),'All540PreviousRegistryRowsRestoredExact':True,'ErlangMfas':sum(row['Module']=='erlang' for row in registry),'ProcLibMfas':sum(row['Module']=='proc_lib' for row in registry),'IntegrationResults':integrations,'SourceFreezeSha256':h((o/'source-freeze.json').read_bytes()),'SourceClosureSha256':h((o/'source-resource-closure.json').read_bytes()),'SourceClosurePaths':len(closure),'OwnedPaths':len(owned),'TestReportSha256':h((o/'tests.json').read_bytes()),'FullExitSha256':h((o/'full-exit.json').read_bytes()),'OriginalExecutedByFull':False,'SingleUninterruptedFullPass':False,'InitialFullExitCode':1,'InitialFailureClass':'MISSING_PUBLISHED_IMMUTABLE_READINESS_COMPARISON_INPUT','TestsFromInitialSettledRun':7105,'IntegrationContinuationExitCode':0,'IntegrationContinuationSha256':h((r/'artifacts/proc-lib338-readiness-integration-continuation.ps1').read_bytes()),'MissingInputRepairSha256':h((o/'readiness-missing-input-repair.json').read_bytes()),'PackageResourceRows':6,'PackageResourceManifestSha256':h((o/'package-resources.json').read_bytes())};guard['InitialExit']=1;guard['ContinuationExit']=0;save('completion-guard.json',guard);print(json.dumps(guard),flush=True)

save('source-after.json',{'Files':j(o/'source-freeze.json')['Files'],'SourceClosureSha256':h((o/'source-resource-closure.json').read_bytes()),'SourceClosureExactAfter':True,'Package6ExactAfter':True})

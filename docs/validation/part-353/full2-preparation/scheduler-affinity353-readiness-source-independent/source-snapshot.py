"""Root-owned readiness after actual full353 independent acceptance; no build/test rerun."""
from pathlib import Path
import hashlib, importlib.util, json, os, shutil, sys, zipfile
r=Path(__file__).resolve().parent.parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
load=lambda p:json.loads(p.read_bytes())
p=r/'artifacts/scheduler-affinity353-composition-plan/composition-plan.json'
assert sha(p)=='B13C31B4A1AC65E8BC8DD2349AD051687E1DEBB438CC4C63BE4FBC5D895CD38A'
plan=load(p);c=Path(plan['Destination']);a=r/'artifacts/scheduler-affinity353-full-actual1'
g=r/'artifacts/scheduler-affinity353-full-independent/report.json'
assert sha(g)==sys.argv[1].upper()
guard=load(g)
assert guard['Status']=='PASS_INDEPENDENT_ACTUAL_CANONICAL353_FULL_RAW_REGISTRY_RESTORATION_PENDING'
assert guard['Passed']==7313 and guard['Failed']==0 and guard['CompositionPlanSha256']==sha(p)
assert sha(a/'raw-supported-mfas.json')==guard['RawRegistrySha256']
assert sha(c/'docs/supported-mfas.json')==guard['RawRegistrySha256']
registry=r/plan['RegistryBefore']['SourcePath']
assert sha(registry)==guard['RegistryBeforeSha256']==plan['RegistryBefore']['Sha256']
def stable():
    for row in plan['StableClosure']:
        f=c/row['Path']
        assert f.stat().st_size==row['Bytes'] and sha(f)==row['Sha256'],row['Path']
stable()
helper=r/'artifacts/spawn-priority351-preparation/execute-root.py'
assert sha(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec=importlib.util.spec_from_file_location('owned_readiness353',helper)
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
o=r/'artifacts/scheduler-affinity353-restored-readiness-actual1';o.mkdir(exist_ok=False)
shutil.copyfile(a/'raw-supported-mfas.json',o/'raw-registry-before-restoration.json')
shutil.copyfile(registry,o/'reviewed-registry-restored.json')
shutil.copyfile(registry,c/'docs/supported-mfas.json')
tool=c/'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'
pkg=c/'artifacts/packages/Erlang.Net.Tool.0.1.0.nupkg'
assert any(row['Sha256']==sha(pkg) for row in guard['Packages'])
with zipfile.ZipFile(pkg) as z:
    assert z.read('tools/net10.0/any/Erlang.Tool.dll')==tool.read_bytes()
m.write(o/'inputs.json',{'IndependentGuardSha256':sha(g),'PlanSha256':sha(p),'RunnerSha256':sha(Path(__file__)),
    'RawRegistrySha256':sha(a/'raw-supported-mfas.json'),'RestoredRegistrySha256':sha(registry),'ToolSha256':sha(tool),
    'ToolPackageSha256':sha(pkg),'ActualTestReportSha256':sha(a/'artifacts/tests.json'),
    'RegistryExcludedFrom3095StableRows':True,'SdkBuildOrGuestRerun':False})
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
m.owned(['dotnet',str(tool),'readiness',str(c/'docs'),str(a/'artifacts/tests.json'),str(o/'generated')],o,'readiness',90,env,c)
v=load(o/'generated/module-readiness.json')
for k,n in {'RegisteredMfas':587,'VerifiedCompatibleMfas':16,'KnownDifferenceMfas':24,'NeedsVerificationMfas':547,'LocalPassedTests':7313,'ImplementedApiModules':14}.items():
    assert v[k]==n,(k,v[k])
ledger=load(c/'docs/mfa-compatibility.json')
assert v['VerifiedSourceFiles']==ledger['VerifiedSourceFiles'] and len(v['VerifiedSourceFiles'])==89
assert sha(c/'docs/supported-mfas.json')==sha(registry)
stable()
files=[]
for f in sorted((o/'generated').iterdir()):
    assert f.name in ['API_COMPATIBILITY.md','MODULE_READINESS.md','module-readiness.json']
    files.append({'SourcePath':f.relative_to(r).as_posix(),'TargetPath':'docs/'+f.name,'Bytes':f.stat().st_size,'Sha256':sha(f)})
assert len(files)==3
m.write(o/'completion.json',{'Status':'PASS_ACTUAL_RESTORED_REGISTRY_READINESS_ONLY_NO_BUILD_OR_GUEST_RERUN',
    'IndependentFullGuardSha256':sha(g),'SourceRowsUnchanged':3095,'RegistrySha256':sha(registry),'Files':files,
    'Verified':16,'KnownDifferences':24,'NeedsVerification':547,'PassedTestsFromActualReport':7313,'SourceGuards':89,'MainChanged':False})
print('PASS actual restored-registry readiness16/24/547,7313tests,89guards; no build/guest rerun.')

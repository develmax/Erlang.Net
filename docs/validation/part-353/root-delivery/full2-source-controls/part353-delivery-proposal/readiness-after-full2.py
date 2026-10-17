"""Root-only future readiness2; never a full or native execution substitute."""
from pathlib import Path, PurePosixPath
import argparse, hashlib, importlib.util, json, os, shutil, zipfile

ROOT=Path(__file__).resolve().parents[2]
PLAN=ROOT/'artifacts/scheduler-affinity353-composition-plan2/composition-plan.json'
PLAN_SHA='3F8C5F95912959D7BE22BDDA3E6F39B696AC644EA98E5461C6B67C64771F06BC'
FULL=ROOT/'artifacts/scheduler-affinity353-full-actual2'
EXPECTED_STATUS='PASS_INDEPENDENT_ACTUAL_CORRECTED_CANONICAL353_FULL2_RAW_REGISTRY_RESTORATION_PENDING'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
load=lambda p:json.loads(p.read_bytes())
def safe(path):
    assert '\\' not in path and not PurePosixPath(path).is_absolute() and '..' not in PurePosixPath(path).parts and ':' not in path
    return path
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--root-completion-notice',choices=['FULL353_2_TERMINAL_AND_INDEPENDENT_PASS_ACCEPTED'],required=True)
p.add_argument('--independent-guard',type=Path,required=True)
p.add_argument('--independent-guard-sha256',required=True)
p.add_argument('--output',type=Path,required=True)
a=p.parse_args()
assert sha(PLAN)==PLAN_SHA
plan=load(PLAN);candidate=Path(plan['Destination'])
assert candidate.resolve()==(ROOT/'artifacts/scheduler-affinity353-full-candidate2').resolve()
guard_path=a.independent_guard.resolve(strict=True)
assert guard_path.is_relative_to(ROOT/'artifacts') and sha(guard_path)==a.independent_guard_sha256.upper()
g=load(guard_path)
assert g['Status']==EXPECTED_STATUS and (g['Passed'],g['Failed'])==(7313,0)
assert g['CompositionPlanSha256']==PLAN_SHA and g['StableClosureRows']==3106 and g['OwnedPaths']==169
assert sha(FULL/'artifacts/tests.json')==g['TestReportSha256']
assert sha(FULL/'raw-supported-mfas.json')==g['RawRegistrySha256']==sha(candidate/'docs/supported-mfas.json')
registry_row=plan['RegistryBefore'];registry=ROOT/safe(registry_row['SourcePath'])
assert sha(registry)==registry_row['Sha256']==g['RegistryBeforeSha256']
assert registry.stat().st_size==registry_row['Bytes']
reviewed=load(registry);assert len(reviewed)==587
module_count=len({r['Module'] for r in reviewed});assert module_count==14
def stable():
    for row in plan['StableClosure']:
        path=candidate/safe(row['Path'])
        assert path.stat().st_size==row['Bytes'] and sha(path)==row['Sha256'],row['Path']
stable()
ledger=load(candidate/'docs/mfa-compatibility.json');assert len(ledger['VerifiedSourceFiles'])==89
for row in ledger['VerifiedSourceFiles']:assert sha(candidate/safe(row['Path']))==row['Sha256']
for entry in ledger['Entries']:
    for proof in entry['Evidence']:assert sha(candidate/'docs'/safe(proof['Path']))==proof['Sha256']

# Complete actual report/package/Tool dependency preflight BEFORE restoring registry.
tool_dir=candidate/'tools/Erlang.Tool/bin/Debug/net10.0'
tool=tool_dir/'Erlang.Tool.dll';pkg=candidate/'artifacts/packages/Erlang.Net.Tool.0.1.0.nupkg'
assert any(row['Sha256']==sha(pkg) for row in g['Packages'])
dependencies=[]
with zipfile.ZipFile(pkg) as z:
    names=z.namelist();assert len(names)==len(set(names)) and z.testzip() is None
    for name in names:
        assert not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and '\\' not in name
        prefix='tools/net10.0/any/'
        if name.startswith(prefix) and name.endswith(('.dll','.deps.json','.runtimeconfig.json')):
            relative=name.removeprefix(prefix);safe(relative)
            path=tool_dir/relative
            assert path.is_file() and z.read(name)==path.read_bytes(),name
            dependencies.append({'Member':name,'ActualPath':path.relative_to(ROOT).as_posix(),'Sha256':sha(path)})
assert any(r['Member']=='tools/net10.0/any/Erlang.Tool.dll' for r in dependencies)
helper=ROOT/'artifacts/spawn-priority351-preparation/execute-root.py'
assert sha(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec=importlib.util.spec_from_file_location('owned_future_readiness353_2',helper)
owned=importlib.util.module_from_spec(spec);spec.loader.exec_module(owned)
out=a.output.resolve();assert out.is_relative_to(ROOT/'artifacts') and not out.exists()
out.mkdir()
shutil.copyfile(FULL/'raw-supported-mfas.json',out/'raw-registry-before-restoration.json')
shutil.copyfile(registry,out/'reviewed-registry-restored.json')
owned.write(out/'restoration-intent.json',{'Status':'ALL_PREFLIGHT_GATES_PASSED_BEFORE_PRIVATE_REGISTRY_RESTORE',
 'IndependentGuardSha256':sha(guard_path),'PlanSha256':sha(PLAN),'RunnerSha256':sha(Path(__file__)),
 'RawRegistrySha256':g['RawRegistrySha256'],'ReviewedRegistrySha256':sha(registry),
 'ActualTestReportSha256':g['TestReportSha256'],'ToolPackageSha256':sha(pkg),'ToolDependencies':dependencies,
 'StableRowsVerified':3106,'RootOnlyRegistryMutationNext':True,'MainChanged':False})
shutil.copyfile(registry,candidate/'docs/supported-mfas.json')
assert sha(candidate/'docs/supported-mfas.json')==registry_row['Sha256']
owned.write(out/'registry-restored.json',{'Status':'PRIVATE_CANDIDATE_REGISTRY_RESTORED_AFTER_INDEPENDENT_PASS',
 'RawRegistryRetainedSha256':sha(out/'raw-registry-before-restoration.json'),
 'RestoredSha256':sha(candidate/'docs/supported-mfas.json'),'MainChanged':False})
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
command=['dotnet',str(tool),'readiness',str(candidate/'docs'),str(FULL/'artifacts/tests.json'),str(out/'generated')]
owned.write(out/'inputs.json',{'Command':command,'ToolSha256':sha(tool),'ActualTestReportSha256':g['TestReportSha256'],
 'IndependentGuardSha256':sha(guard_path),'RegistrySha256':sha(registry),'LedgerSha256':sha(candidate/'docs/mfa-compatibility.json'),
 'NoSdkBuildOrGuestRerunOrOriginalExecution':True})
owned.owned(command,out,'readiness',90,env,candidate)
generated=load(out/'generated/module-readiness.json')
for key,value in {'RegisteredMfas':587,'VerifiedCompatibleMfas':16,'KnownDifferenceMfas':24,'NeedsVerificationMfas':547,'LocalPassedTests':7313,'ImplementedApiModules':module_count}.items():
    assert generated[key]==value,(key,generated[key])
assert generated['VerifiedSourceFiles']==ledger['VerifiedSourceFiles']
assert sha(candidate/'docs/supported-mfas.json')==registry_row['Sha256']
stable()
files=[]
for path in sorted((out/'generated').iterdir()):
    assert path.name in ['API_COMPATIBILITY.md','MODULE_READINESS.md','module-readiness.json']
    files.append({'SourcePath':path.relative_to(ROOT).as_posix(),'TargetPath':'docs/'+path.name,'Bytes':path.stat().st_size,'Sha256':sha(path)})
assert len(files)==3
owned.write(out/'copy-plan.json',{'Status':'ACTUAL_ROOT_READINESS2_GENERATED_OUTPUT_PROPOSAL_NOT_MAIN_APPLIED',
 'Files':files,'IndependentFullGuardSha256':sha(guard_path),'RawCliBytesPreserved':True,
 'Counts':[16,24,547],'LocalPassedTests':7313,'RegistryRows':587,'RuntimeSourceGuards':89,
 'SdkBuildOrGuestRerun':False,'NativeAbiOrOriginalExecuted':False,'MainChanged':False})
owned.write(out/'completion.json',{'Status':'PASS_ACTUAL_RESTORED_REGISTRY_READINESS2_ONLY_NO_BUILD_OR_GUEST_RERUN',
 'SourceRowsUnchanged':3106,'RegistrySha256':sha(registry),'CopyPlanSha256':sha(out/'copy-plan.json'),'MainChanged':False})
print(sha(out/'copy-plan.json'))

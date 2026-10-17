"""Run unchanged Root-admitted native11 using the successful final356 canonical driver; bounded owned jobs only."""
from pathlib import Path
import hashlib,importlib.util,json,os,sys

root=Path(__file__).resolve().parents[2];proposal=root/'artifacts/native-entry356-windows-builder-proposal2'
h=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
read=lambda p:json.loads(Path(p).read_bytes())
admission=root/'artifacts/native-entry356-windows-admission/admission.json'
assert h(admission)=='8F675B5AD39432EAFE238D926AEE07FFE80669984504330051BC5ED4457AE6EA'
driver=root/'artifacts/native-load356-full-candidate4/tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
planpath=root/'artifacts/native-load356-canonical-preparation4/composition-plan.json'
assert h(planpath)=='CFD59AD745468D1F25E6B47FB5DF791C0413868C8346B50CE7090E1369720C06'
plan=read(planpath);candidate=Path(plan['Destination']);actual=root/'artifacts/native-load356-full-actual4'
full=read(actual/'full.exit.json');assert full['ExitCode']==0 and full['Settled'] and not full['TimedOut'] and not full['Failures']
assert h(actual/'full.stdout.log')==full['StdoutSha256'] and h(actual/'full.stderr.log')==full['StderrSha256']
assert read(actual/'inputs.json')['PlanSha256']==h(planpath)
assert read(actual/'producer-completion.json')['Status']=='ACTUAL_CANONICAL356_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW'
assert read(actual/'artifacts/tests.json')['Passed']==7345 and read(actual/'artifacts/tests.json')['Failed']==0
assert read(actual/'stable-closure-after.json')==dict(VerifiedRows=2952,PlanSha256=h(planpath),Unchanged=True)
for row in plan['StableClosure']:assert h(candidate/row['Path'])==row['Sha256'] and (candidate/row['Path']).stat().st_size==row['Bytes']
# Match the final tool's assemblies to hashes captured by canonical validation, not earlier private builds.
recorded={Path(x['Path']).name:x['Sha256'] for x in read(actual/'captured-outputs.json')['TestDlls']}
for leaf in ['Erlang.NifNativeBackend.dll','Erlang.Runtime.dll','Erlang.Terms.dll','Erlang.Compiler.dll','Erlang.Serialization.dll']:
 assert leaf in recorded and h(driver.parent/leaf)==recorded[leaf]

for relative,expected in [('artifacts/native-entry356-windows-builder-proposal2/jobs.py', 'C8C5A562A6FCE2B0D9C9F82BE675AC5BC5A56B226A8C48CCAE7C6F59163D17F3'), ('artifacts/native-entry356-windows-builder-proposal2/gated-tool.py', '789520523F528CB68F189DA3EA43170A89CEEA06CB5413F34B40ED820FD07E00')]:assert h(root/relative)==expected
spec=importlib.util.spec_from_file_location('owned_fixture_jobs',proposal/'jobs.py');jobs=importlib.util.module_from_spec(spec);spec.loader.exec_module(jobs)
output=root/'artifacts/native-final356-actual4';output.mkdir(exist_ok=False)
completed=[]
try:
    for case in read(admission)['Cases']:
        folder=output/case['Name'];folder.mkdir();command=folder/'command.json'
        arguments=['C:/Program Files/dotnet/dotnet.exe',str(driver),'--reviewed-entry-case',str(admission),h(admission),case['Name'],str(folder/'observations')]
        command.write_text(json.dumps(dict(Arguments=arguments,WorkingDirectory=str(root)),indent=2)+'\n',encoding='utf-8')
        receipt=jobs.invoke([sys.executable,'-B',str(proposal/'gated-tool.py'),str(command)],folder,60,os.environ.copy(),root)
        assert receipt['ExitCode']==0 and receipt['Settled'] and receipt['KillOnCloseJobAssigned'] and not receipt['TimedOut'] and not receipt['Failures'] and not receipt['JobTerminationRequested'] and receipt['RemainingJobProcesses']==0,case['Name']
        observation=folder/'observations/completion.json';assert read(observation)['Name']==case['Name']
        completed.append(dict(Name=case['Name'],ReceiptSha256=h(folder/'process.exit.json'),ObservationSha256=h(observation),OutcomeSha256=h(folder/'observations/observed.etf')))
        with (output/('completed-'+str(len(completed)).zfill(2)+'.json')).open('x',encoding='utf-8') as f:json.dump(completed,f,indent=2);f.write('\n')
        print('PASS '+case['Name'],flush=True)
    assert len(completed)==11
    result=dict(Status='PASS_WINDOWS_ELEVEN_OWN_NATIVE_FIXTURES_VIA_PRODUCTION_CAPTURED_ROUTE',Cases=completed,DriverSha256=h(driver),AdmissionSha256=h(admission),NativeBuildReceiptSha256=read(admission)['NativeBuildReceiptSha256'],OriginalOtpCompared=False,DefaultNoPolicyCompatibilityClaimed=False,CanonicalPlanSha256=h(planpath),CanonicalFullExitSha256=h(actual/'full.exit.json'),CanonicalCapturedOutputsSha256=h(actual/'captured-outputs.json'),BaselineNativeElevenUnchanged=True)
    (output/'completion.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
except Exception as error:
    (output/'failure.json').write_text(json.dumps(dict(Status='FAILED_NATIVE_CASE_RAW_RETAINED',Error=repr(error),Completed=completed),indent=2)+'\n',encoding='utf-8')
    raise

"""Root-owned canonical356 command. Requires independent source admission; never restores registry."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil
R=Path(__file__).resolve().parents[2];P=R/'artifacts/native-load356-canonical-preparation4';H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();J=lambda p:json.loads(p.read_bytes());W=lambda p,v:p.write_text(json.dumps(v,indent=2)+'\n')
a=argparse.ArgumentParser();a.add_argument('--approved-plan-sha256',required=True);a.add_argument('--source-peer',required=True);a.add_argument('--source-peer-sha256',required=True);a.add_argument('--tests-build',required=True);a.add_argument('--tests-build-sha256',required=True);v=a.parse_args()
p=P/'composition-plan.json';assert H(p)==v.approved_plan_sha256.upper();plan=J(p);C=Path(plan['Destination']);O=R/'artifacts/native-load356-full-actual4'
assert C==R/'artifacts/native-load356-full-candidate4' and not O.exists()
assert len(plan['Files'])==2953 and len(plan['StableClosure'])==2952 and len(plan['OwnedPaths'])==40
assert plan['ExpectedFullTestsIfPasses']==7345 and plan['ExpectedOldOrderedPrefix']==7343 and plan['RuntimeSourceGuards']==127
peer=(R/v.source_peer).resolve();build=(R/v.tests_build).resolve()
for q,h in [(peer,v.source_peer_sha256),(build,v.tests_build_sha256)]:assert q.is_relative_to((R/'artifacts').resolve()) and H(q)==h.upper()
assert J(peer)['Status'].startswith('PASS_') and J(build)['ExitCode']==0
for x in plan['InputPins']:
 q=R/x['Path'];assert H(q)==x['Sha256'] and q.stat().st_size==x['Bytes']
def verify(rows):
 for x in rows:
  q=C/x['Path'];assert H(q)==x['Sha256'] and q.stat().st_size==x['Bytes'],x['Path']
verify(plan['Files']);assert not (C/'artifacts/tests.json').exists() and not (C/'artifacts/integration-results.json').exists()
helper=R/'artifacts/spawn-priority351-preparation/execute-root.py';assert H(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec=importlib.util.spec_from_file_location('owned356',helper);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
O.mkdir();shutil.copyfile(p,O/'composition-plan.json');shutil.copyfile(peer,O/'runner-source-review.json');shutil.copyfile(build,O/'accepted-tests-build.json');shutil.copyfile(C/'docs/supported-mfas.json',O/'registry-before.json');shutil.copyfile(C/plan['PreviousTestReport'],O/'previous-tests.json')
W(O/'inputs.json',dict(PlanSha256=H(p),RunnerSha256=H(Path(__file__)),OwnershipHelperSha256=H(helper),SourcePeerSha256=H(peer),AcceptedTestsBuildSha256=H(build),Candidate=str(C),InputRows=2953,StableRows=2952,OwnedPaths=40,ExpectedTests=7345,OldOrderedPrefix=7343,ExpectedIntegrationOutcomes=11,ExpectedPackages=2,RegistryWillNotBeRestored=True,OriginalOtpExecuted=False,RuntimeSourceGuards=127))
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
command=['C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe','-NoProfile','-File',str(C/'tools/validate.ps1')]
try:m.owned(command,O,'full',900,env,C)
finally:
 shutil.copyfile(C/'docs/supported-mfas.json',O/'raw-supported-mfas.json');outputs=[]
 for name in ['artifacts/tests.json','artifacts/integration-results.json','artifacts/readiness/module-readiness.json','artifacts/readiness/MODULE_READINESS.md','artifacts/readiness/API_COMPATIBILITY.md','artifacts/tool-readiness/module-readiness.json']:
  q=C/name
  if q.is_file():
   d=O/name;d.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(q,d);outputs.append(dict(Path=name,Bytes=q.stat().st_size,Sha256=H(q)))
 children=[dict(Path=q.relative_to(C).as_posix(),Sha256=H(q)) for q in sorted((C/'artifacts/term-order-children').glob('*/receipt.json'))]
 packages=[dict(Path=q.relative_to(C).as_posix(),Bytes=q.stat().st_size,Sha256=H(q)) for q in sorted((C/'artifacts/packages').glob('*.nupkg'))]
 dlls=[dict(Path=q.relative_to(C).as_posix(),Bytes=q.stat().st_size,Sha256=H(q)) for q in sorted((C/'tests/Erlang.Tests/bin/Debug/net10.0').glob('*.dll'))]
 W(O/'captured-outputs.json',dict(Outputs=outputs,Children=children,Packages=packages,TestDlls=dlls,RawRegistrySha256=H(O/'raw-supported-mfas.json'),RegistryRestored=False))
 verify(plan['StableClosure']);W(O/'stable-closure-after.json',dict(VerifiedRows=2952,PlanSha256=H(p),Unchanged=True))
tests=J(O/'artifacts/tests.json');old=J(O/'previous-tests.json');assert tests['Passed']==7345 and tests['Failed']==0
assert [x['Name'] for x in tests['Tests'][:7343]]==[x['Name'] for x in old['Tests']]
assert [x['Name'] for x in tests['Tests'][7343:]]==plan['AddedTestNames'];assert len(J(O/'artifacts/integration-results.json'))==11
W(O/'producer-completion.json',dict(Status='ACTUAL_CANONICAL356_COMMAND_EXITED_ZERO_REQUIRES_INDEPENDENT_REVIEW',IndependentAcceptance=False,OriginalOtpExecuted=False,NativeAbiExecutedByCanonicalRun=False,MainChanged=False))

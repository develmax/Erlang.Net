"""Current source-bound Linux acceptance composition; reuses legacy fixtures, never historical admissions."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,subprocess,sys,time
R=Path(__file__).resolve().parents[3]
T=Path(__file__).resolve().parent
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
ORIGINAL_PLAN='AEBBA0D2DCF97E1248CA38407E8EBE628EB08F9EA4D13F48EC0C83A7A0364E61'
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def save(path,value):
 with path.open('x',encoding='utf-8') as f:json.dump(value,f,indent=2);f.write('\n')


def project_headers552(root, hash_file):
    paths=[root/'tools/Native/Erlang.NifNativeBackend/native'/name for name in ('nif_bridge_api.h','nif_snprintf791.h')]
    return [dict(Path=str(path.resolve()),Bytes=path.stat().st_size,Sha256=hash_file(path)) for path in paths]

def require_project_headers552(proof, root, hash_file):
    expected=project_headers552(root,hash_file);actual=proof['ProjectHeaderInputs']
    assert len(actual)==len(expected) and {row['Path']:(row['Bytes'],row['Sha256']) for row in actual}=={row['Path']:(row['Bytes'],row['Sha256']) for row in expected},'Project bridge headers changed or unbound'
    return expected

def main():
 p=argparse.ArgumentParser()
 for key in ['expected-head','plan-sha256','approved-n1-build-plan-sha256','source-freeze','approved-source-freeze-sha256','otp-source','original-build-receipt','n1-native-build','framework-version','output']:p.add_argument('--'+key,required=True)
 a=p.parse_args();assert sys.platform=='linux' and os.uname().machine=='x86_64'
 assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD'],cwd=R,text=True).strip()==a.expected_head
 workflow_head=os.environ.get('EXPECTED_HEAD',os.environ.get('GITHUB_SHA'))
 assert workflow_head is None or workflow_head==a.expected_head
 planpath=T/'acceptance-inputs.json';assert H(planpath)==a.plan_sha256.upper();plan=J(planpath)
 assert plan['Pin']==PIN and plan['OriginalPlanSha256']==ORIGINAL_PLAN and plan['N1BuildPlanSha256']==a.approved_n1_build_plan_sha256.upper()
 for row in plan['Files']:
  q=(R/row['Path']).resolve();assert q.is_relative_to(R) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'],row['Path']
 assert H(R/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json')==ORIGINAL_PLAN
 assert H(R/'tools/Native/Erlang.NifVersion651Observations/linux-build-inputs.json')==plan['N1BuildPlanSha256']
 n1planpath=R/'tools/Native/Erlang.NifVersion651Observations/linux-build-inputs.json';n1plan=J(n1planpath)
 assert n1plan['Pin']==PIN
 for row in n1plan['Inputs']:
  base=n1planpath.parent if row['Scope']=='Tool' else R;q=(base/row['Path']).resolve();assert q.is_relative_to(base) and H(q)==row['Sha256']
 freeze=Path(a.source_freeze).resolve();assert freeze.is_relative_to(R/'artifacts/nif-version651') and H(freeze)==a.approved_source_freeze_sha256.upper()
 frozen=J(freeze);assert frozen['SubjectHead']==a.expected_head and frozen['Rows']
 def verify_frozen():
  for row in frozen['Rows']:
   q=(R/row['Path']).resolve();assert q.is_relative_to(R) and H(q)==row['Sha256']
 verify_frozen()
 out=Path(a.output).resolve();assert out.is_relative_to(R/'artifacts/nif-version651') and not out.exists();out.mkdir(parents=True)
 legacy=out/'legacy';legacy.mkdir();rows=[];isolated=True
 common=load('acceptance_common',R/'tools/Native/Erlang.NifEntryOriginalObservations/common.py')
 identity=load('acceptance_identity',R/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py')
 owned=load('acceptance_owned',R/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py')
 env=identity.sanitized_environment(os.environ)
 for key in ['GITHUB_TOKEN','GH_TOKEN','LD_PRELOAD','LD_AUDIT','LD_LIBRARY_PATH']:env.pop(key,None)
 env.update(PYTHONDONTWRITEBYTECODE='1',DOTNET_PROCESSOR_COUNT='2')
 def skip(name,why):rows.append(dict(Name=name,Passed=False,Skipped=True,Reason=why));return False
 def run(name,argv,seconds=180):
  nonlocal isolated
  if not isolated:return skip(name,'Previous owned process did not establish cleanup isolation')
  start=time.monotonic();error=None;stage=None
  try:stage=common.stage(owned,identity,argv,R,env,out/name,seconds)
  except Exception as e:error=repr(e)
  try:
   receipt=J(out/(name+'.process.json'))
   assert receipt['ParentReaped'] and receipt['StreamsClosed'] and receipt['CleanupFailure'] is None and not receipt['RemainingGroupKillRequested']
  except Exception as e:isolated=False;error=(error or '')+'; cleanup isolation: '+repr(e)
  rows.append(dict(Name=name,Passed=error is None,Skipped=False,Seconds=time.monotonic()-start,Error=error,OwnedStage=stage))
  return error is None
 def check(name,body):
  try:body();rows.append(dict(Name=name,Passed=True,Skipped=False));return True
  except Exception as e:rows.append(dict(Name=name,Passed=False,Skipped=False,Error=repr(e)));return False
 original=Path(a.original_build_receipt).resolve();source=Path(a.otp_source).resolve()
 def provider():
  b=J(original);assert b['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and b['Commit']==PIN and b['SubjectHead']==a.expected_head and b['SourceManifestSha256']==ORIGINAL_PLAN
  assert Path(b['SourceRoot']).resolve()==source
  class Args:pass
  args=Args();args.otp_source=source;args.runtime_root=b['RuntimeRoot'];args.build_receipt=original;args.approved_build_receipt_sha256=H(original);args.expected_head=a.expected_head
  common.approve_runtime(args,{'RuntimeBuildManifestSha256':ORIGINAL_PLAN},identity)
 provider_ok=check('provider-before',provider)
 # SDK focus builds are independent of original-provider acquisition.
 dotnet=shutil.which('dotnet');focusok={};dllrows=[]
 for kind,project in [('callback','Erlang.NifLinuxCallbackFocus'),('ordinary','Erlang.NifOrdinaryLoaderFocus')]:
  focusok[kind]=run('build-'+kind,[dotnet,'build',R/'tools/Native'/project/(project+'.csproj'),'-c','Release','--disable-build-servers','-m:1','-nr:false'],300) if dotnet else skip('build-'+kind,'dotnet unavailable')
  if focusok[kind]:
   directory=R/'tools/Native'/project/'bin/Release/net10.0'
   dllrows.extend(dict(Path=str(q),Sha256=H(q)) for q in sorted(directory.glob('*.dll')))
 familyok={};nativeok={}
 for family,toolname in [('size','Erlang.NifMapSizeOriginalObservations'),('reference','Erlang.NifMakeReferenceOriginalObservations')]:
  tool=R/'tools/Native'/toolname;manifest=H(tool/'manifest.json');assert manifest==plan['FixtureManifests'][toolname]
  if not provider_ok:
   nativeok[family]=skip('native-'+family,'Original provider preparation failed');familyok[family]=skip('original-'+family,'Original provider preparation failed');continue
  runtime=Path(J(original)['RuntimeRoot'])
  base=[sys.executable,'-B'];args=['--repository',R,'--expected-head',a.expected_head,'--manifest-sha256',manifest,'--otp-source',source,'--runtime-root',runtime,'--build-receipt',original,'--approved-build-receipt-sha256',H(original)]
  nativeok[family]=run('native-'+family,base+[tool/'build-native.py',*args,'--output',legacy/(family+'-native')])
  if nativeok[family]:
   receipt=legacy/(family+'-native')/'native-build-receipt.json'
   familyok[family]=run('original-'+family,base+[tool/'collect.py',*args,'--native-build',receipt.parent,'--approved-native-build-receipt-sha256',H(receipt),'--output',legacy/(family+'-original')])
  else:familyok[family]=skip('original-'+family,'Its native fixture preparation failed')
 n1=Path(a.n1_native_build).resolve();bridgeparent=n1/'bridge/build-receipt.json'
 def bridge():
  n=J(n1/'build-receipt.json');assert n['Status']=='VERSION651_LINUX_BUILD' and n['Pin']==PIN and n['SubjectHead']==a.expected_head and n['PlanSha256']==plan['N1BuildPlanSha256']
  b=n['Bridge'];assert b is not None and H(b['BuildReceipt']['Path'])==b['BuildReceipt']['Sha256'] and Path(b['BuildReceipt']['Path']).resolve()==bridgeparent
  proof=J(bridgeparent);assert proof['Status']=='VERSION651_LINUX_BRIDGE_BUILD' and proof['Pin']==PIN and proof['SubjectHead']==a.expected_head and proof['NativeLoaded'] is False
  assert H(proof['BridgePath'])==proof['BridgeSha256']==b['Library']['Sha256']
  require_project_headers552(proof,R,H)
 bridge_ok=check('n1-bridge-parent',bridge)
 admission_ok=False
 if provider_ok and bridge_ok and all(nativeok.values()):
  args=[sys.executable,'-B',T/'prepare.py','--expected-head',a.expected_head,'--source-plan',planpath,'--approved-source-plan-sha256',H(planpath),'--otp-source',source,'--runtime-root',J(original)['RuntimeRoot'],'--build-receipt',original,'--approved-build-receipt-sha256',H(original),'--reuse-n1-bridge-receipt',bridgeparent,'--approved-n1-bridge-receipt-sha256',H(bridgeparent),'--output',legacy/'bridge']
  for family in ['size','reference']:
   receipt=legacy/(family+'-native')/'native-build-receipt.json';args+=['--'+family+'-native-build',receipt.parent,'--approved-'+family+'-native-receipt-sha256',H(receipt)]
  admission_ok=run('callback-admission',args)
 else:skip('callback-admission','Requires verified original provider, both native fixture producers and N1 bridge')
 callback_ok=False
 if admission_ok and focusok['callback']:
  admission=legacy/'bridge/admission.json';ad=J(admission);focus=R/'tools/Native/Erlang.NifLinuxCallbackFocus/bin/Release/net10.0/Erlang.NifLinuxCallbackFocus.dll'
  shutil.copyfile(ad['BridgePath'],focus.parent/'Erlang.NifBridge.so')
  callback_ok=run('callback-two-routes',[sys.executable,'-B',T/'run.py','--admission',admission,'--approved-admission-sha256',H(admission),'--focus',focus,'--framework-version',a.framework_version,'--output',legacy/'sdk'],420)
 else:skip('callback-two-routes','Requires callback host and current admission')
 if callback_ok and all(familyok.values()):
  args=[sys.executable,'-B',T/'compare.py','--capture-root',legacy,'--approved-sdk-completion-sha256',H(legacy/'sdk/completion.json'),'--approved-admission-sha256',H(legacy/'bridge/admission.json'),'--output',legacy/'comparison.json']
  for family in ['size','reference']:args+=['--approved-'+family+'-completion-sha256',H(legacy/(family+'-original')/'completion.json')]
  run('compare68',args)
  check('verify68',lambda: require68(J(legacy/'comparison.json')))
 else:skip('compare68','Requires both original captures and both managed routes')
 if callback_ok:run('pthread-bindings-create-join',[sys.executable,'-B',T/'verify-pthread.py','--capture-root',legacy])
 else:skip('pthread-bindings-create-join','Requires both runtime domains on both callback routes')
 if bridge_ok and focusok['ordinary'] and any(nativeok.values()):
  family=next(x for x in ['size','reference'] if nativeok[x]);headers=legacy/(family+'-native')/'headers';receipt=legacy/(family+'-native')/'native-build-receipt.json';proof=J(bridgeparent)
  focus=R/'tools/Native/Erlang.NifOrdinaryLoaderFocus/bin/Release/net10.0/Erlang.NifOrdinaryLoaderFocus.dll';shutil.copyfile(proof['BridgePath'],focus.parent/'Erlang.NifBridge.so')
  passed=run('ordinary3',[sys.executable,'-B',R/'tools/Native/Erlang.NifOrdinaryLoaderObservations/run.py','--expected-head',a.expected_head,'--header-receipt',receipt,'--approved-header-receipt-sha256',H(receipt),'--headers',headers,'--focus',focus,'--bridge-sha256',proof['BridgeSha256'],'--output',out/'ordinary3'],420)
  if passed:check('verify-ordinary3',lambda: require3(J(out/'ordinary3/completion.json')))
 else:skip('ordinary3','Requires ordinary host, bridge and at least one successful native/header producer')
 if provider_ok:check('provider-after',provider)
 def final_sources():
  for row in plan['Files']:assert H(R/row['Path'])==row['Sha256']
  for row in dllrows:assert H(row['Path'])==row['Sha256']
  if bridge_ok:require_project_headers552(J(bridgeparent),R,H)
  verify_frozen()
  assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD'],cwd=R,text=True).strip()==a.expected_head
 check('source-dll-head-after',final_sources)
 passed=all(row['Passed'] for row in rows)
 save(out/'completion.json',dict(Passed=passed,SubjectHead=a.expected_head,PlanSha256=H(planpath),Stages=rows,AdjacentDlls=dllrows,OriginalBuildReceiptSha256=H(original) if original.exists() else None,LegacyExpectedComparisons=68,OrdinaryExpectedCases=3,AllTlsFunctionsExecuted=False,CppTlsAndHiddenCallsVerified=False,WholeLoadNifCompatible=False,ApiDelta=0,FullContractDelta=0))
 return 0 if passed else 1

def require68(value):
 assert value['Status']=='ACTUAL_LINUX_CALLBACK17_ROWS_EQUAL' and value['Equal'] and value['RowComparisons']==68 and value['UniqueObservationGroups']==17 and value['Routes']==2 and value['RuntimeDomainsPerRoute']==2 and value['WidthsComparedExactly'] and value['OriginalCleanupAndSdkRetirementIndependentlyRequired']

def require3(value):
 assert value['Status']=='PASS_THREE_ORDINARY_OS_LOADER_REGRESSIONS' and value['AttemptedCases']==3 and len(value['Cases'])==3 and not value['Failures'] and value['OriginalOtpComparisonExecuted'] is False and value['WholeOrdinaryLoaderCompatibilityProved'] is False
if __name__=='__main__':raise SystemExit(main())

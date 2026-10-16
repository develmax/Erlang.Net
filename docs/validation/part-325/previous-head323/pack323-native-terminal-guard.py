import sys
sys.dont_write_bytecode=True
"""Detached evidence verification only; never executes native binaries or alters Main."""
from pathlib import Path
import hashlib,json,re,subprocess,sys,zipfile,importlib.util,runpy
spec=importlib.util.spec_from_file_location('bulk_audit',Path(__file__).with_name('pack323-audit-common.py'));bulk=importlib.util.module_from_spec(spec);spec.loader.exec_module(bulk)
r=Path.cwd();a=r/'artifacts';mapping=json.loads((a/'pack323-native-run-map.json').read_bytes());head=mapping['Head'];rid=mapping['Run'];d=a/f'pack323-watch-{rid}-terminal'
j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();cache={}
def blob(name):
 if name not in cache:cache.update(bulk.blobs(head,[name]))
 return cache[name]
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts']
assert run['id']==rid==mapping['Run'] and run['head_sha']==head and run['run_attempt']==1 and run['status']=='completed' and run['name']=='Pinned OTP pending and bounded native backend observations'
assert len(jobs)==1 and jobs[0]['head_sha']==head and jobs[0]['status']=='completed' and len(items)==1
job,item=jobs[0],items[0];assert job['id']==mapping['Job'] and item['workflow_run']['head_sha']==head and not item['expired']
z=d/f"artifact-{item['id']}.zip";ex=d/f"artifact-{item['id']}";assert z.stat().st_size==item['size_in_bytes'] and h(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as archive:
 for name in archive.namelist():
  assert not Path(name).is_absolute() and '..' not in Path(name).parts
  if not name.endswith('/'):assert archive.read(name)==(ex/name).read_bytes()
members=j(d/'download-manifest.json');assert {p.relative_to(d).as_posix() for p in d.rglob('*') if p.is_file() and p.name!='download-manifest.json'}=={row['Path'].replace('\\','/') for row in members}
for row in members:
 p=d/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p.read_bytes())==row['Sha256']
raw=(d/f"job-{job['id']}.log").read_bytes();log=raw.decode('utf-8-sig');assert head in log
def only(name):
 paths=list(ex.rglob(name));assert len(paths)==1,(name,len(paths));return paths[0]
result={'Head':head,'Run':rid,'Job':job['id'],'Artifact':item['id'],'Attempt':1,'RawZipSha256':h(z.read_bytes()),'RawJobSha256':h(raw),'WorkflowConclusion':run['conclusion'],'JobConclusion':job['conclusion'],'RawDownloadFilesVerified':len(members),'MainEdited':False,'NativeExecutedByVerifier':False,'DispatchOrRerun':False,'Original13SdkCompared':False,'GeneralNifAbiComplete':False}
build_proofs=list(ex.rglob('nif-pending-otp-build-provenance.json'));assert len(build_proofs)<=1
if build_proofs:assert j(build_proofs[0])['SourceHead']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
else:assert run['conclusion']!='success'
result['PinnedOriginalBuildProvenancePresent']=bool(build_proofs)
source_paths=list(ex.rglob('source-binding.json'));assert len(source_paths)<=1
if source_paths:
 binding=j(source_paths[0]);plan=json.loads(blob('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'))
 assert binding['Head']==head and binding['PlanSha256']==h(blob('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'))=='D0E9EF6F2103042BDB62694402E76C5A592514AE843E43701BFEB991BAB421E6' and binding['Files']==plan['Files'] and len(plan['Files'])==1108
 cache.update(bulk.blobs(head,[row['Path'] for row in plan['Files']]))
 for row in plan['Files']:
  data=blob(row['Path'])
  if row['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  else:assert row['HashMode']=='BinaryExact'
  assert len(data)==row['Bytes'] and h(data)==row['Sha256']
 result['Reviewed1108SourceBindingsExact']=True
sdk_paths=list(ex.rglob('sdk-receipt.json'));assert len(sdk_paths)<=1
result['SdkSixArgumentPassed']=False
if sdk_paths:
 sdk_path=sdk_paths[0];base=sdk_path.parent;sdk=j(sdk_path);build=j(base/'build-receipt.json')
 assert sdk['Status']=='BOUNDED_TRUSTED_SDK_NATIVE_AND_COMPILED_SOURCE_CHECKS_PASS' and not sdk['Original13Compared'] and not sdk['GeneralNifAbiImplemented'] and not sdk['GeneratedCSharpHashIndependentlyCompared'] and sdk['BuildReceiptSha256']==h((base/'build-receipt.json').read_bytes()) and build['PlanSha256']==binding['PlanSha256']
 for row in build['Files']+sdk['Capture']:
  p=(base/row['Path']).resolve();assert p.is_relative_to(base.resolve()) and row['HashMode']=='BinaryExact' and p.stat().st_size==row['Bytes'] and h(p.read_bytes())==row['Sha256']
 process=j(base/'sdk-six-argument.process.json');assert process['ExitCode']==0 and not process['TimedOut'] and process['PipesClosed'] and process['ParentReaped'] and process['OwnedPid']>0 and len(process['Arguments'])==8 and process['Arguments'][0]=='dotnet' and process['Arguments'][1].endswith('/managed/Erlang.NifNativeBackend.dll')
 for i,name in enumerate(['fixture.so','nif_bridge.so','nif_bridge_abi_probe.so']):assert process['Arguments'][2+i*2].endswith('/'+name) and process['Arguments'][3+i*2]==h((base/name).read_bytes())
 generated=sdk['GeneratedCSharpSha256'];assert re.fullmatch('[0-9A-F]{64}',generated)
 reader_source=blob('tools/Erlang.NifPendingVarargsObservations/reviewed_sdk_stdout.py');contracts=a/'pack323-sdk-source-contracts';contracts.mkdir(exist_ok=True);reader_path=contracts/'reviewed_sdk_stdout.py'
 if reader_path.exists():assert reader_path.read_bytes()==reader_source
 else:reader_path.write_bytes(reader_source)
 stdout=(base/'sdk-six-argument.stdout').read_bytes();stderr=(base/'sdk-six-argument.stderr').read_bytes();parsed=runpy.run_path(str(reader_path))['parse_sdk_stdout'](stdout,stderr,require_abi=True)
 assert len(stdout.decode('utf-8-sig').splitlines())==8 and not stderr and parsed['GeneratedCSharpSha256']==generated and len(parsed['LegacyOrderedSuccessLines'])==5 and len(parsed['NativePayloadDomainObservations'])==3 and parsed['NativePayloadDomainRolesValidated'] and parsed['LoadedOwnerSnapshotMatchesRuntime']
 assert sdk['NativePayloadDomainObservations']==parsed['NativePayloadDomainObservations'] and sdk['NativePayloadDomainRolesValidated'] and sdk['LoadedOwnerSnapshotMatchesRuntime']
 result.update(StrictEightLineSdkContractPassed=True,NativePayloadDomainRolesValidated=True,NativePayloadDomainObservations=parsed['NativePayloadDomainObservations'],LoadedOwnerSnapshotMatchesRuntime=True,PublishedStdoutReaderSha256=h(reader_source))
 expected={'fixture.so':{'nif_init','memory_allocator_fixture_unload_count'},'nif_bridge.so':{'enif_priv_data','enif_alloc','enif_free','enif_get_int','enif_make_badarg','enif_make_int','enif_make_string','enif_is_exception','enif_has_pending_exception','enif_raise_exception','enif_make_tuple','enif_make_list','nif_bridge_register','nif_bridge_read_term'},'nif_bridge_abi_probe.so':{'nif_bridge_probe_tuple','nif_bridge_probe_list','nif_bridge_probe_empty_tuple','nif_bridge_probe_empty_list','nif_bridge_probe_pending'}}
 for name,exports in expected.items():
  assert {line.split()[-1] for line in (base/(name.replace('.','-')+'-defined.stdout')).read_text().splitlines() if line.split()}==exports
  header=(base/(name.replace('.','-')+'-readelf.stdout')).read_text();assert 'ELF64' in header and 'Advanced Micro Devices X86-64' in header
 original_receipt=only('receipt.json');original=j(original_receipt)
 sdk_headers={x['Path']:x['Sha256'] for x in binding['ConfiguredHeaders']}
 for name in ['erl_nif.h','erl_drv_nif.h','erl_nif_api_funcs.h','erl_int_sizes_config.h']:assert sdk_headers[name]==h((original_receipt.parent/'headers'/name).read_bytes())
 for name in ['compile-veneer','compile-probe','compile-trusted-fixture','build-sdk','gcc-version','dotnet-version']:
  process=j(base/(name+'.process.json'));assert process['ExitCode']==0 and not process['TimedOut'] and process['PipesClosed'] and process['ParentReaped']
 result.update(SdkSixArgumentPassed=True,GeneratedCSharpSha256=generated,SameActualConfiguredHeadersAsOriginal=True,NativeLibraryExportsExact=True)
remote_original=list(ex.rglob('nif-pending-original-typed-review.json'));assert len(remote_original)<=1
result['RemoteOriginal13TypedGatePassed']=bool(remote_original and j(remote_original[0]).get('Status')=='ORIGINAL_ONLY_THIRTEEN_TYPED_SEMANTIC_PASS')
if result['SdkSixArgumentPassed'] and result['RemoteOriginal13TypedGatePassed']:
 assert run['conclusion']==job['conclusion']=='success' and all(step['conclusion'] in ['success','skipped'] for step in job['steps'])
 result['Status']='EXACT_HEAD323_NATIVE_SDK_PASS_REMOTE_ORIGINAL13_REPORT_PENDING_INDEPENDENT_TYPED_REVIEW'
else:
 result['Status']='VERIFIED_EXACT_HEAD323_TERMINAL_NATIVE_WORKFLOW_FAILURE_RAW_PRESERVED'
 assert run['conclusion']!='success' or job['conclusion']!='success'
 result['FailedSteps']=[step['name'] for step in job['steps'] if step['conclusion']=='failure']
out=a/f'pack323-native-{rid}-independent-envelope-and-sdk-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))

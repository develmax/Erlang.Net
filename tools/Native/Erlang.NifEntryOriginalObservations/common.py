"""Hash-bound original native-entry stages; no managed runtime execution."""
import argparse,hashlib,importlib.util,json,os,subprocess,sys,threading,time
from pathlib import Path
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL='tools/Native/Erlang.NifEntryOriginalObservations'
def digest(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def read(path):return json.loads(Path(path).read_bytes())
def write(path,value):
 with Path(path).open('xb') as stream:stream.write((json.dumps(value,ensure_ascii=False,indent=2)+'\n').encode())
def module(path,name):
 spec=importlib.util.spec_from_file_location(name,path);value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value
def verify(repository,expected,manifest_sha):
 assert sys.platform.startswith('linux') and os.uname().machine=='x86_64'
 root=Path(repository).resolve();tool=root/TOOL;assert tool==Path(__file__).resolve().parent
 assert subprocess.check_output(['git','--no-optional-locks','-C',str(root),'rev-parse','HEAD'],text=True,timeout=20).strip()==expected
 assert not os.environ.get('GITHUB_SHA') or os.environ['GITHUB_SHA']==expected
 path=tool/'manifest.json';assert digest(path)==manifest_sha.upper();plan=read(path);assert plan['PinnedOtpCommit']==PIN
 assert len(plan['Files'])==len({x['Path'] for x in plan['Files']})
 for x in plan['Files']:
  q=tool/x['Path'];assert q.resolve().is_relative_to(tool) and q.stat().st_size==x['Bytes'] and digest(q)==x['Sha256']
 for x in plan['Dependencies']:
  q=root/x['Path'];assert q.stat().st_size==x['Bytes'] and digest(q)==x['Sha256']
 assert digest(root/plan['Workflow']['Path'])==plan['Workflow']['Sha256']
 identity=module(root/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','entry_identity')
 owned=module(root/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py','entry_owned')
 decoder=module(root/'tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py','entry_decoder')
 return root,tool,plan,identity,owned,decoder

def approve_runtime(args,plan,identity):
 source=Path(args.otp_source).resolve();runtime=Path(args.runtime_root).resolve();path=Path(args.build_receipt).resolve()
 assert digest(path)==args.approved_build_receipt_sha256.upper();build=read(path)
 assert build['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['Commit']==PIN and build['SubjectHead']==args.expected_head
 assert build['SourceManifestSha256']==plan['RuntimeBuildManifestSha256']
 assert Path(build['SourceRoot']).resolve()==source and Path(build['RuntimeRoot']).resolve()==runtime
 assert (Path(build['Prefix']).resolve()/'lib/erlang')==runtime
 assert identity.source_identity(source)==read(path.parent/'source-before.json')==read(path.parent/'source-after.json')
 assert digest(path.parent/'source-before.json')==build['SourceBeforeSha256'] and digest(path.parent/'source-after.json')==build['SourceAfterSha256']
 assert [x['Name'] for x in build['Stages']]==['configure','make','install']
 for stage in build['Stages']:
  p=Path(stage['ReceiptPath']);assert p.resolve().is_relative_to(path.parent)
  assert digest(p)==stage['ReceiptSha256'];identity.require_normal_receipt(read(p))
  for stream in ['stdout','stderr']:assert digest(p.parent/(stage['Name']+'.'+stream))==stage[stream.capitalize()+'Sha256']
 assert len(build['InstalledFiles'])==len({x['Path'] for x in build['InstalledFiles']}) and len(build['InstalledFiles'])>=11
 for x in build['InstalledFiles']:
  p=runtime/x['Path'];assert p.resolve().is_relative_to(runtime) and digest(p)==x['Sha256']
 assert {'bin/erl','bin/erlc'}<={x['Path'] for x in build['InstalledFiles']}
 assert any(Path(x['Path']).name=='beam.smp' for x in build['InstalledFiles'])
 return runtime,build

def stage(owned,identity,argv,cwd,environment,prefix,timeout):
 process=None;failure=None
 try:
  process=owned.OwnedProcess([str(x) for x in argv],cwd,environment,prefix)
  process.wait(time.monotonic()+timeout,threading.Event().is_set)
 except BaseException as error:failure=repr(error)
 finally:
  if process is not None:
   try:process.close()
   except BaseException as error:failure=(failure or '')+'; cleanup: '+repr(error)
  write(prefix.with_suffix('.stage.json'),{'Failure':failure,'Started':process is not None,'Phase':prefix.name,'Arguments':[str(x) for x in argv]})
 if failure:raise RuntimeError(prefix.name+': '+failure)
 receipt=prefix.with_suffix('.process.json');identity.require_normal_receipt(read(receipt))
 return {'Name':prefix.name,'ReceiptPath':str(receipt),'ReceiptSha256':digest(receipt),'StdoutSha256':digest(prefix.with_suffix('.stdout')),'StderrSha256':digest(prefix.with_suffix('.stderr'))}

def arguments(kind):
 parser=argparse.ArgumentParser()
 for key in ['repository','expected-head','manifest-sha256','otp-source','runtime-root','build-receipt','approved-build-receipt-sha256','output']:parser.add_argument('--'+key,required=True)
 if kind=='capture':
  parser.add_argument('--native-build',required=True);parser.add_argument('--approved-native-build-receipt-sha256',required=True)
 return parser.parse_args()
A=lambda x:('atom',x)
T=lambda *x:('tuple',tuple(x))
def text(value):
 assert value[0]=='list' and all(x[0]=='integer' for x in value[1]);return ''.join(chr(x[1]) for x in value[1])
def verify_images(value,case,directory):
 assert value[0]=='list' and len(value[1])==2
 for item,module_name,filename in zip(value[1],['entry_observer',case['Module']],['entry_observer.beam',case['Module']+'.beam']):
  assert item[0]=='tuple' and len(item[1])==5;m,loaded,obj,code_md5,file_md5=item[1];assert m==A(module_name)
  path=(directory/'ebin'/filename).resolve();assert Path(text(loaded)).resolve()==path==Path(text(obj)).resolve()
  assert code_md5[0]=='binary' and len(code_md5[1])==16 and file_md5==('binary',hashlib.md5(path.read_bytes()).digest())
def verify_return(value,case):
 assert value[0]=='tuple' and len(value[1])==2;observed,cleanup=value[1];assert cleanup==T(A('cleanup'),A('deleted_and_purged'))
 assert observed[0]=='tuple' and len(observed[1])==5;tag,name,module_name,result,body=observed[1]
 assert (tag,name,module_name)==(A('entry_case'),A(case['Name']),A(case['Module']))
 if case['SourceExpected']=='success':assert result==A('ok')
 else:
  assert result[0]=='tuple' and result[1][0]==A('error');error=result[1][1];assert error[0]=='tuple' and error[1][0]==A(case['SourceExpected']);text(error[1][1]);assert body==T(A('stub_after_failure'),A('not_loaded'))

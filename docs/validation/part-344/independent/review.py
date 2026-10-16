"""Passive source binding and pure synthetic AST component controls; no process/network calls."""
from pathlib import Path
import ast,json,hashlib,subprocess,difflib,types
R=Path.cwd();O=R/'artifacts/proc-lib-real-peer-procfs-independent';C=R/'artifacts/proc-lib-real-peer-procfs-correction';O.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
freeze_raw=(C/'source-freeze.json').read_bytes();assert sha(freeze_raw)=='40DFA630C0D484B41928531964DE1B6BAC53213A8D1FAB773AF899908F51B096'
freeze=json.loads(freeze_raw);bindings=[]
for row in freeze['Files']:
 new=(C/'source'/row['Path']).read_bytes();old=(R/row['Path']).read_bytes()
 assert sha(new)==row['Sha256'] and len(new)==row['Bytes'] and sha(old)==row['ParentSha256'],row['Path']
 bindings.append(row)
source=C/'source/tools/Erlang.ProcLibRealPeerObservations/collect-original.py'
old=(R/'tools/Erlang.ProcLibRealPeerObservations/collect-original.py').read_text(encoding='utf-8');new=source.read_text(encoding='utf-8')
expected=old.replace('subprocess, sys, time\n','subprocess, sys, time, traceback\n').replace('except FileNotFoundError:', 'except (FileNotFoundError, ProcessLookupError):').replace("Error=repr(error))\n        finally:","Error=repr(error),ErrorTraceback=traceback.format_exc())\n        finally:")
assert new==expected
assert old.count('except FileNotFoundError:')==4 and new.count('except (FileNotFoundError, ProcessLookupError):')==4
before=ast.parse(old);after=ast.parse(new)
for name in ['retire_peer','run_owned','verify','verify_module_identities']:
 a=next(n for n in before.body if isinstance(n,ast.FunctionDef) and n.name==name);b=next(n for n in after.body if isinstance(n,ast.FunctionDef) and n.name==name)
 assert ast.dump(a,include_attributes=False)==ast.dump(b,include_attributes=False),name
manifest=json.loads((C/'source/tools/Erlang.ProcLibRealPeerObservations/source-manifest.json').read_bytes())
assert freeze['NewManifestSha256']=='C86FA344D5FB2CB1FCAAFC85B8A0370D03D2B252E693C0004DC654429F41E311'
for row in manifest['Files']:
 p=C/'source/tools/Erlang.ProcLibRealPeerObservations'/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256']
for row in manifest['ExternalDependencies']+[manifest['Workflow']]:assert sha((R/row['Path']).read_bytes())==row['Sha256']
# Execute only reviewed local control harness, with exclusive output writes removed.
text=(C/'controls2.py').read_text(encoding='utf-8')
prefix=text[:text.index("output=Path(__file__)")]
base_scope={'__file__':str(C/'controls2.py')}
exec(compile(prefix,'independent-base-12-controls','exec'),base_scope)
assert len(base_scope['results'])==12 and all(r['Passed'] for r in base_scope['results'])
# Extend the harness's synthetic stage matrix to require fatal behavior for access/unexpected errors.
extended=prefix.replace("except (PermissionError,ValueError) as exc:","except (PermissionError,OSError,RuntimeError,ValueError) as exc:").replace("assert isinstance(raised,PermissionError)","assert isinstance(raised,error)")
extended=extended.replace("3 if error is ProcessLookupError else 2", "5 if error is OSError else 3 if error is ProcessLookupError else 2")
matrix=ast.parse(extended)
loop=next(n for n in matrix.body if isinstance(n,ast.For) and isinstance(n.target,ast.Tuple))
assert isinstance(loop.iter,ast.List)
for error in ['PermissionError','OSError','RuntimeError']:
 for stage in ['cmdline','stat','exe','uid','children']:
  loop.iter.elts.append(ast.Tuple(elts=[ast.Constant('independent-'+stage+'-'+error),ast.Constant(stage),ast.Name(id=error,ctx=ast.Load()),ast.Constant('raise')],ctx=ast.Load()))
ast.fix_missing_locations(matrix);extended_scope={'__file__':str(C/'controls2.py')};exec(compile(matrix,'independent-27-stage-controls','exec'),extended_scope)
assert len(extended_scope['results'])==27
# The fourth catch is the EPMD fd-readlink discovery path, outside observe().
function=next(n for n in after.body if isinstance(n,ast.FunctionDef) and n.name=='epmd_owner')
class FakePath:
 def __init__(self,*parts):self.parts=tuple(map(str,parts))
 def iterdir(self):return ['disappearing-fd','listener-fd']
 def read_text(self):return 'header\n0: 0100007F:1234 00000000:0000 0A 0 0 0 0 0 55\n'
def require(ok,why):
 if not ok:raise ValueError(why)
epmd_controls=[]
for error in [FileNotFoundError,ProcessLookupError,PermissionError,OSError,RuntimeError]:
 def readlink(fd):
  if fd=='disappearing-fd':raise error('controlled fd race')
  return 'socket:[55]'
 context={'Path':FakePath,'os':types.SimpleNamespace(readlink=readlink),'re':__import__('re'),'require':require}
 exec(compile(ast.Module(body=[function],type_ignores=[]),'independent-epmd-fd','exec'),context)
 process=types.SimpleNamespace(process=types.SimpleNamespace(pid=123),poll=lambda:None)
 try:actual=context['epmd_owner'](process,0x1234)
 except (PermissionError,OSError,RuntimeError) as e:assert error in [PermissionError,OSError,RuntimeError] and isinstance(e,error)
 else:assert error in [FileNotFoundError,ProcessLookupError] and actual['OwnedSocketInodes']==['55']
 epmd_controls.append({'Error':error.__name__,'Passed':True})
diagnosis=(C/'diagnosis.json').read_bytes();assert sha(diagnosis)=='195A0368C8BAD1EACCB41D14D57248F96D9588D28EAF563055196717EE15CA58'
d=json.loads(diagnosis);assert not d['AllEightSemanticPassClaimed'] and len(d['Cases'])==8 and d['Cases'][0]['AcquisitionStatus']=='FAILED_RAW_RETAINED'
for row in d['Evidence']:assert sha((R/row['Path']).read_bytes())==row['Sha256']
report={'Status':'PASS_INDEPENDENT_NARROW_PROCFS_SOURCE_AND_SYNTHETIC_CONTROLS','SourceFreezeSha256':sha(freeze_raw),'DiagnosisSha256':sha(diagnosis),'SourceFiles':bindings,'ExactDeltaOnlyFourDisappearanceCatchesAndTraceback':True,'OwnedCleanupGenerationAndNormalExitGatesAstUnchanged':True,'OriginalControlCount':12,'IndependentExtendedStageControls':27,'IndependentEpmdFdControls':epmd_controls,'PermissionAndUnexpectedErrorsRemainFatal':True,'FixtureAndWorkflowRawUnchanged':True,'FailureHistoryRehashed':True,'SevenPartialObservationsNotEightPass':True,'OriginalReexecuted':False,'SdkExecuted':False,'NetworkUsed':False,'MainModified':False,'SourceControlsOnlyNotLinuxRuntimeProof':True}
(O/'report.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Status':report['Status'],'Sha256':sha((O/'report.json').read_bytes())}))

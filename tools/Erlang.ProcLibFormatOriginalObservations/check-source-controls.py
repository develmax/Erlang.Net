"""Standard-library source/CLI controls only; no original VM or SDK is executed."""
import contextlib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import shutil
import sys
import uuid

sys.dont_write_bytecode=True
HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('collector',HERE/'collect-original.py')
m=importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
rows=[]


def check(name, action):
    action()
    rows.append(dict(Name=name,Passed=True,OriginalExecuted=False,SdkExecuted=False))


def require(value, message):
    if not value:
        raise RuntimeError(message)


def refused(action):
    try:
        action()
    except ValueError:
        return
    raise RuntimeError('Expected explicit refusal')


root=HERE.parents[1]
manifest=HERE/'source-manifest.json'
approved=m.sha(manifest)
check('python-syntax',lambda:[compile(path.read_bytes(),str(path),'exec') for path in HERE.glob('*.py')])
check('exact-reviewed-source-closure',lambda:m.verify(root,approved))
check('manifest-hash-drift-refused',lambda:refused(lambda:m.verify(root,'0'*64)))
check('unsafe-relative-paths-refused',lambda:[refused(lambda value=value:m.safe_file(HERE,value)) for value in ['../outside','/absolute','C:/outside','nested\\outside']])
check('module-declaration-missing-refused',lambda:refused(lambda:m.module_name(b'-export([run/0]).')))
check('module-declaration-duplicate-refused',lambda:refused(lambda:m.module_name(b'-module(first).\n-module(second).')))
check('declared-module-identity',lambda:require(m.module_name(b'-module(oracle_proc_format).\n')=='oracle_proc_format','Module identity drift'))


def cli_refusals():
    with contextlib.redirect_stdout(io.StringIO()),contextlib.redirect_stderr(io.StringIO()):
        require(m.main(['verify','--repository',str(root),'--manifest-sha256','0'*64])==2,'Wrong hash accepted')
        require(m.main(['collect','--repository',str(root),'--manifest-sha256',approved])==2,'Missing original inputs accepted')


check('cli-preconditions-refuse-before-original-launch',cli_refusals)
scratch=root/'artifacts/source-control-scratch'
scratch.mkdir(parents=True,exist_ok=True)
with contextlib.nullcontext(scratch/('format-source-controls-'+uuid.uuid4().hex)) as temporary:
    target=Path(temporary).resolve()
    require(target.is_relative_to(scratch.resolve()),'Synthetic source target escaped scratch root')
    target.mkdir()
    target_tool=target/m.TOOL
    shutil.copytree(HERE,target_tool)
    workflow=target/json.loads(manifest.read_bytes())['Workflow']['Path']
    workflow.parent.mkdir(parents=True)
    shutil.copyfile(root/json.loads(manifest.read_bytes())['Workflow']['Path'],workflow)
    catalog_path=target_tool/'cases.json'
    original_catalog=catalog_path.read_bytes()

    def mutate_catalog(name, mutation):
        catalog=json.loads(original_catalog)
        mutation(catalog)
        catalog_path.write_bytes((json.dumps(catalog,indent=2)+'\n').encode())
        frozen=json.loads(manifest.read_bytes())
        frozen['Files']=[m.binding(target_tool/row['Path'],target_tool) for row in frozen['Files']]
        manifest_path=target_tool/'source-manifest.json'
        manifest_path.write_bytes((json.dumps(frozen,indent=2)+'\n').encode())
        refused(lambda:m.verify(target,m.sha(manifest_path)))

    check('rehashed-catalog-case-count-refused',lambda:mutate_catalog('count',lambda catalog:catalog['Cases'].pop()))
    check('rehashed-catalog-duplicate-name-refused',lambda:mutate_catalog('duplicate',lambda catalog:catalog['Cases'].__setitem__(1,catalog['Cases'][0])))
    check('rehashed-sdk-derived-expected-scope-refused',lambda:mutate_catalog('expected',lambda catalog:catalog.__setitem__('ExpectedValuesPresent',True)))
    catalog_path.write_bytes(original_catalog)
    shutil.copyfile(manifest,target_tool/'source-manifest.json')
    workflow.write_bytes(workflow.read_bytes()+b'\n')
    check('workflow-bytes-drift-refused',lambda:refused(lambda:m.verify(target,approved)))
    etf=target/'raw.etf'
    etf.write_bytes(bytes([131,97,0]))
    check('etf-container-header-and-size-only',lambda:m.validate_etf(etf))
    for name,data in [('missing-version',bytes([1,97,0])),('empty',b''),('version-without-term',bytes([131]))]:
        etf.write_bytes(data)
        check('etf-'+name+'-refused',lambda:refused(lambda:m.validate_etf(etf)))

if sys.platform=='linux':
    normal=target/'owned-normal'
    normal.mkdir()
    receipt=m.run_owned(normal,'python',[sys.executable,'-c','import sys;sys.stdout.buffer.write(bytes([0,255,65]));sys.stderr.buffer.write(bytes([10,66]))'],5,m.clean_environment(normal),normal)
    check('actual-linux-owned-python-normal-exit-raw-bytes',lambda:require(receipt['ExitCode']==0 and receipt['Settled'] and not receipt['TimedOut'] and (normal/'python.stdout').read_bytes()==bytes([0,255,65]) and (normal/'python.stderr').read_bytes()==bytes([10,66]),'Actual Python raw capture failed'))
    timeout=target/'owned-timeout'
    timeout.mkdir()
    code="import subprocess,sys,time;p=subprocess.Popen([sys.executable,'-c','import time;time.sleep(60)']);print(p.pid,flush=True);time.sleep(60)"
    receipt=m.run_owned(timeout,'python',[sys.executable,'-c',code],0.5,m.clean_environment(timeout),timeout)
    check('actual-linux-owned-python-group-timeout-joined-pipes',lambda:require(receipt['TimedOut'] and receipt['Settled'] and receipt['ExitCode']==-9 and int((timeout/'python.stdout').read_bytes())>0,'Actual owned-group timeout did not settle'))

print(json.dumps(dict(Status='SOURCE_AND_CLI_CONTROLS_PASS',Controls=len(rows),Rows=rows,OriginalExecuted=False,SdkExecuted=False,SourceAssertionsAreNotNativeComparisons=True,ActualLinuxOwnedPythonProcessesExercised=sys.platform=='linux',SyntheticScratchRetained=str(target))))

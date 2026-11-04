import argparse,hashlib,json,os,pathlib,subprocess
TOOL=pathlib.Path(__file__).resolve().parent
ROOT=TOOL.parents[2]
SCOPES=['src/Runtime','src/Compiler','src/Otp/Managed','src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam','tools/Native/Erlang.FileSystem.Native','tools/Native/Erlang.PortProbe','tools/Validation/Erlang.Differential','tools/Validation/Erlang.DescriptorPollVmValidation','tools/Validation/Erlang.DescriptorPollIntegratedValidation','tools/Validation/Erlang.DescriptorPollFdValidation']
TOOL_SCOPES=SCOPES[-3:]
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def digest(data):return hashlib.sha256(data).hexdigest().upper()
def verify(expected_head, expected_src_tree, expected_plan_sha256):
    assert len(expected_plan_sha256)==64 and all(c in '0123456789abcdefABCDEF' for c in expected_plan_sha256),'Explicit reviewed raw source-plan SHA256 required'
    raw_plan=(TOOL/'source-plan.json').read_bytes()
    assert digest(raw_plan)==expected_plan_sha256.upper(),'Raw source-plan pin differs; no automatic resealing'
    plan=json.loads((TOOL/'source-plan.json').read_bytes())
    head=git('rev-parse','HEAD').decode().strip()
    subject=os.environ['GITHUB_SHA']
    if os.environ.get('GITHUB_EVENT_NAME')=='pull_request':
        event=json.loads(pathlib.Path(os.environ['GITHUB_EVENT_PATH']).read_bytes())
        subject=event['pull_request']['head']['sha']
    assert head==expected_head==subject,'Actual checkout differs from reviewed exact workflow subject'
    tree=git('rev-parse','HEAD:src').decode().strip()
    assert tree==expected_src_tree==plan['ExpectedProductionSourceTree'],'Wrong reviewed production source tree'
    assert not git('diff','--name-only','HEAD').strip(),'Tracked source mutation'
    for row in plan['Inputs']:
        p=ROOT/row['Path'];data=p.read_bytes()
        assert not p.is_symlink() and len(data)==row['Bytes'] and digest(data)==row['Sha256'],row['Path']
    entries={}
    for item in git('ls-tree','-r','-z','HEAD','--',*SCOPES).split(b'\0'):
        if not item:continue
        metadata,path=item.split(b'\t',1);mode,kind,oid=metadata.split()
        relative=path.decode();assert kind==b'blob' and mode!=b'120000',relative
        if any(part in ('bin','obj') for part in pathlib.PurePosixPath(relative).parts):continue
        entries[relative]=oid.decode()
    actual={}
    for scope in SCOPES:
        folder=ROOT/scope;assert folder.is_dir(),scope
        for p in folder.rglob('*'):
            relative=p.relative_to(ROOT).as_posix()
            if any(part in ('bin','obj') for part in pathlib.PurePosixPath(relative).parts):continue
            assert not p.is_symlink(),relative
            if p.is_file():actual[relative]=p
    assert set(actual)==set(entries),('Unexpected/missing compile inputs',sorted(set(actual)^set(entries)))
    census=[]
    for relative in sorted(entries):
        data=actual[relative].read_bytes()
        blob=hashlib.sha1(b'blob '+str(len(data)).encode()+b'\0'+data).hexdigest()
        assert blob==entries[relative],relative
        census.append({'Path':relative,'GitBlob':blob,'Bytes':len(data),'Sha256':digest(data)})
    expected_tools={row['Path'] for row in plan['Inputs'] if any(row['Path'].startswith(s+'/') for s in TOOL_SCOPES)}|{'tools/Validation/Erlang.DescriptorPollVmValidation/source-plan.json'}
    actual_tools={p for p in entries if any(p.startswith(s+'/') for s in TOOL_SCOPES)}
    assert actual_tools==expected_tools,('Tool manifest membership differs',sorted(actual_tools^expected_tools))
    return {'Head':head,'ProductionSourceTree':tree,'SourcePlanSha256':digest((TOOL/'source-plan.json').read_bytes()),'InputCount':len(plan['Inputs']),'CompileCensusFiles':len(census),'CompileCensusSha256':digest(json.dumps(census,sort_keys=True,separators=(',',':')).encode()),'Inputs':plan['Inputs'],'ActualSourceStable':True,'Scope':'Final modified shutdown custody; all checked-out compile roots and tools match committed raw blobs; no API/full-contract promotion'}
if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('--expected-head',required=True)
    parser.add_argument('--expected-src-tree',required=True)
    parser.add_argument('--plan-sha256',required=True)
    args=parser.parse_args()
    result=verify(args.expected_head,args.expected_src_tree,args.plan_sha256);print(json.dumps({k:result[k] for k in ('Head','ProductionSourceTree','InputCount','CompileCensusFiles','CompileCensusSha256')}))

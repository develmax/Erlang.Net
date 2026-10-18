"""Independent passive strict closure comparison; never executes downloaded code."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,zipfile

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
POLICY='compiled-monitored-worker-values-and-stacks-v1'
MANIFEST='11E72CB41047BA9442B442A747D281138697D58E10AFA08CF8555160280893EC'
HEAD='53823b168e672fc946e65f6dd53c0ea2f9c94611'
def read(p):return json.loads(p.read_bytes())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()

def named(node):
    assert node[0]=='list' and node[1]
    result={}
    for row in node[1]:
        assert row[0]=='tuple' and len(row[1])==2 and row[1][0] not in result
        result[row[1][0]]=row[1][1]
    return result

def shape(node,stack):
    assert node[0]=='tuple'
    parts=node[1]
    if parts[0]==('atom','ok'):
        assert len(parts)==3
    else:
        assert parts[0]==('atom','error') and parts[1] in [('atom','error'),('atom','throw'),('atom','exit')]
        assert len(parts)==(5 if stack else 4)
        if stack:
            assert parts[3][0]=='list'
            assert all(frame[0]=='tuple' and len(frame[1])==4 for frame in parts[3][1])
    effects=parts[-1]
    assert effects[0]=='tuple' and len(effects[1])==2 and all(x[0]=='list' for x in effects[1])

def normal(folder,name):
    path=folder/(name+'.exit.json');row=read(path)
    assert row['Settled'] is True and row['TimedOut'] is False and row['ExitCode']==0 and not row['Failures']
    for stream in ['Stdout','Stderr']:assert sha(folder/(name+'.'+stream.lower()+'.log'))==row[stream+'Sha256']
    return dict(Path=str(path),Sha256=sha(path),ElapsedSeconds=row['ElapsedSeconds'],Command=row['Command'])

def main():
    p=argparse.ArgumentParser()
    for key in ['native','native-admission','sdk','tool','output']:p.add_argument('--'+key,type=Path,required=True)
    a=p.parse_args()
    for key in ['native','native_admission','sdk','tool']:setattr(a,key,getattr(a,key).resolve())
    out=a.output.resolve();assert not out.exists();out.mkdir(parents=True)
    inputs=[];rows=[];failures=[]
    try:
        admission=read(a.native_admission)
        assert admission['Status']=='PASS_PASSIVE_BINARY369_CLOSURE_ORIGINAL_ADMISSION' and admission['Head']==HEAD and admission['Run']==39084998283
        provenance_path=a.native_admission.parent/'provenance/acceptance.json'
        assert sha(provenance_path)==admission['ProvenanceSha256']
        provenance=read(provenance_path)
        remote=next(parent for parent in a.native.resolve().parents if (parent/'download-manifest.json').is_file())
        archives=[x for x in remote.glob('artifact-*.zip') if sha(x)==provenance['ArtifactZipSha256']];assert len(archives)==1
        artifact=remote/archives[0].stem
        assert sha(a.tool/'source-inputs.corrected.json')==MANIFEST
        for row in read(a.tool/'source-inputs.corrected.json')['ToolInputs']:
            path=a.tool/row['Path'];assert path.stat().st_size==row['Bytes'] and sha(path)==row['Sha256']
        profile=read(a.tool/'profile.json')
        assert profile['ValueAcquisition']==POLICY and len(profile['Fixtures'])==1 and len(profile['FixtureSources'])==4
        fixture=profile['Fixtures'][0]
        assert fixture['ValueAcquisition']==POLICY and fixture['Id']=='closure' and len(fixture['Names'])==14 and len(set(fixture['Names']))==14
        assert not profile['OpaquePolicy']['EligibleCases']
        decoder_path=a.tool/'passive_etf_family.py'
        spec=importlib.util.spec_from_file_location('trusted_closure_passive',decoder_path);decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
        capture=a.sdk.parent
        processes=[normal(capture,'build'),normal(capture,'binary31-capture')]
        harness=capture.parent/'main-capture'
        expected_harness=ROOT/'artifacts/binary369-closure-execution-proposal1/harness'
        for path in expected_harness.iterdir():
            if path.is_file():assert sha(harness/path.name)==sha(path)
        assert {x.name for x in harness.glob('*.cs')}=={x.name for x in expected_harness.glob('*.cs')}
        assert str(harness/'MainCapture.csproj') in processes[0]['Command']
        assert str(harness/'bin/Debug/net10.0/MainCapture.dll') in processes[1]['Command']
        assert str(a.sdk.resolve()) in processes[1]['Command']
        closure=read(capture/'main-dll-closure.json')
        assert closure['MainSource'] is True and closure['PrivateCandidateDllsUsed'] is False and len(closure['Files'])==5
        for row in closure['Files']:assert sha(ROOT/row['CopiedTo'])==row['Sha256']
        for row in read(a.sdk/'capture-seal.json')['Files']:
            path=a.sdk/row['Path'];assert path.resolve().is_relative_to(a.sdk.resolve()) and sha(path)==row['Sha256'] and path.stat().st_size==row['Bytes']
        sdk_result=read(a.sdk/'completion.json')
        assert sdk_result['FailedGroups']==0 and len(sdk_result['Groups'])==1
        group=sdk_result['Groups'][0]
        assert group['Id']=='closure' and group['Status']=='CAPTURED_PENDING_PASSIVE_CHECKS' and group['ExecutionSkipped'] is False
        sources=[fixture['Path'],*fixture['Companions']]
        assert len(group['Compile'])==len(sources)==4
        for path,row in zip(sources,group['Compile']):
            assert row['Passed'] is True and 'Error' not in row and row['SourceSha256']==sha(a.tool/path) and row['DeclaredSourceFile']==Path(path).name
        native_group=a.native/'closure'
        assert any(Path(x['Directory']).resolve()==native_group.resolve() for x in provenance['OriginalNormalProcesses'])
        names=[('atom',n) for n in fixture['Names']]
        with zipfile.ZipFile(archives[0]) as archive:
            for filename,stack in [('returned.etf',False),('stack-observations.etf',True)]:
                native=native_group/filename;sdk=a.sdk/'closure'/filename
                assert archive.read(native.resolve().relative_to(artifact.resolve()).as_posix())==native.read_bytes()
                inputs += [dict(Path=str(x),Bytes=x.stat().st_size,Sha256=sha(x)) for x in [native,sdk]]
                left=named(decoder.decode_exact(native.read_bytes()));right=named(decoder.decode_exact(sdk.read_bytes()))
                if not stack:
                    assert list(left)==list(right)==names
                    pairs=[('values',name,left[name],right[name]) for name in names]
                else:
                    depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
                    assert list(left)==list(right)==depths;pairs=[]
                    for depth in depths:
                        l,r=named(left[depth]),named(right[depth]);assert list(l)==list(r)==names
                        pairs += [(str(depth[1]),name,l[name],r[name]) for name in names]
                for depth,name,l,r in pairs:
                    shape(l,stack);shape(r,stack)
                    rows.append(dict(Case=name[1],Depth=depth,StrictEqual=l==r,OriginalWhole=None if l==r else repr(l),SdkWhole=None if l==r else repr(r)))
    except Exception as error:
        failures.append(repr(error))
    result=dict(Status='PASS_INDEPENDENT_STRICT_MATCHED_WORKER_CLOSURE' if not failures and len(rows)==84 and all(x['StrictEqual'] for x in rows) else 'FAIL_OR_INSUFFICIENT_CLOSURE_EVIDENCE',ExpectedValues=14,ExpectedProfiles=70,ExpectedObservations=84,ActualObservations=len(rows),StrictEqual=sum(x['StrictEqual'] for x in rows),Rows=rows,Failures=failures,Inputs=inputs,ValueAcquisition=POLICY,NativeAdmissionSha256=sha(a.native_admission),Projection=False,Normalization=False,OpaqueRelationsAccepted=0,OldTwoLiveValuesRetrospectivelyAccepted=False,MainClassificationChanged=False)
    if not failures:
        result['OwnedProcesses']=processes;result['ActualDlls']=closure['Files']
    (out/'acceptance.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({k:result[k] for k in ['Status','ActualObservations','StrictEqual','Failures']}))
    return 0 if result['Status'].startswith('PASS_') else 1

if __name__=='__main__':raise SystemExit(main())

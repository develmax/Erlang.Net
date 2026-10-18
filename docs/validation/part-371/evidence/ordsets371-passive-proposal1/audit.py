"""Root-only passive source/receipt admission of the incoming source-built OTP capture."""
from pathlib import Path
import argparse,hashlib,json,subprocess,sys,zipfile
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def main():
    p=argparse.ArgumentParser()
    for key in ['download-run','artifact','tool','bindings','output']:p.add_argument('--'+key,type=Path,required=True)
    for key in ['expected-head','manifest-sha256','zip-sha256']:p.add_argument('--'+key,required=True)
    for key in ['run-id','job-id','artifact-id']:p.add_argument('--'+key,type=int,required=True)
    a=p.parse_args();out=a.output.resolve();assert not out.exists() and out.is_relative_to(HERE)
    frozen=J(HERE/'source-freeze.json')
    for row in frozen['Files']:
        q=HERE/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    tool=a.tool.resolve();manifest=tool/'source-inputs.corrected.json';assert H(manifest)==a.manifest_sha256.upper()
    profile=J(tool/'profile.json');assert len(profile['Fixtures'])==1 and len(profile['Mfas'])==8 and len(profile['Fixtures'][0]['Names'])==112
    plan=J(manifest);bindings=J(a.bindings);assert bindings['Head']==a.expected_head
    expected={x['Path']:x for x in plan['RepositoryInputs']}
    for row in plan['ToolInputs']:expected['tools/Erlang.OrdsetsCoreOriginalObservations/'+row['Path']]=row
    expected['tools/Erlang.OrdsetsCoreOriginalObservations/source-inputs.corrected.json']=dict(Bytes=manifest.stat().st_size,Sha256=H(manifest))
    shared_path='tools/Erlang.SchedulerAffinityOriginalObservations/manifest.json'
    shared_bytes=subprocess.check_output(['git','-C',str(ROOT),'show',a.expected_head+':'+shared_path],timeout=30)
    assert hashlib.sha256(shared_bytes).hexdigest().upper()==plan['ExistingBuildManifestSha256']
    shared= json.loads(shared_bytes)
    for row in shared['Files']:expected['tools/Erlang.SchedulerAffinityOriginalObservations/'+row['Path']]=row
    for row in shared['Dependencies']+[shared['Workflow']]:expected[row['Path']]=row
    for row in bindings['Bindings']:expected[row['Path']]=row
    remote=a.download_run.resolve();artifact=a.artifact.resolve();archive=remote/('artifact-'+str(a.artifact_id)+'.zip')
    assert H(archive)==a.zip_sha256.upper() and artifact.is_relative_to(remote)
    with zipfile.ZipFile(archive) as zipped:
        files=[x for x in zipped.infolist() if not x.is_dir()];assert len(files)==len({x.filename for x in files})
        assert {q.relative_to(artifact).as_posix() for q in artifact.rglob('*') if q.is_file()}=={x.filename for x in files}
        for item in files:
            q=(artifact/item.filename).resolve();assert q.is_relative_to(artifact) and q.stat().st_size==item.file_size and H(q)==hashlib.sha256(zipped.read(item)).hexdigest().upper()
    out.mkdir();snapshot=out/'producer-snapshot';snapshot.mkdir()
    for path,row in expected.items():
        raw=subprocess.check_output(['git','-C',str(ROOT),'show',a.expected_head+':'+path],timeout=30)
        assert len(raw)==row['Bytes'] and hashlib.sha256(raw).hexdigest().upper()==row['Sha256']
        q=(snapshot/path).resolve();assert q.is_relative_to(snapshot);q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes(raw)
    preflight=artifact/'ordsets-preflight'
    argv=[sys.executable,'-B',str(HERE/'audit-provenance.py'),'--artifact',str(artifact),'--run-json',str(remote/'run.json'),'--source-snapshot',str(snapshot),'--output',str(out/'provenance'),'--artifact-id',str(a.artifact_id),'--job-id',str(a.job_id),'--zip-sha256',a.zip_sha256,'--expected-head',a.expected_head,'--run-id',str(a.run_id),'--manifest-sha256',a.manifest_sha256,'--tool',str(tool)]
    for name in ['parse-build','parse-sources','collector-admit']:argv+=['--preflight-receipt',str(preflight/(name+'.process.json'))]
    subprocess.run(argv,check=True)
    subprocess.run([sys.executable,'-B',str(HERE/'audit-source.py'),'--artifact',str(artifact),'--output',str(out/'source'),'--expected-head',a.expected_head,'--tool',str(tool),'--bindings',str(a.bindings)],check=True)
    captures=[q.parent for q in artifact.rglob('inputs.json') if J(q).get('SourceInputsSha256')==H(manifest)];assert len(captures)==1;native=captures[0]
    provenance=J(out/'provenance/acceptance.json');assert provenance['Values']==112 and provenance['FullStackProfiles']==560 and len(provenance['MetadataCorruptionControls'])==3
    native_files=[]
    for group in profile['Fixtures']:
        for name in ['returned.etf','stack-observations.etf','metadata.etf']:
            q=native/group['Id']/name;native_files.append(dict(Path=q.relative_to(native).as_posix(),Bytes=q.stat().st_size,Sha256=H(q)))
    result=dict(Status='PASS_PASSIVE_ORDSETS371_ORIGINAL_ADMISSION',Head=a.expected_head,Run=a.run_id,ManifestSha256=H(manifest),ProfileSha256=H(tool/'profile.json'),Tool=str(tool),NativeCapture=str(native),NativeFiles=native_files,Values=112,FullStackProfiles=560,OriginalNormalProcesses=5,PreflightNormalProcesses=3,MetadataCorruptionControls=3,PinnedSourceBlobs=12008,ProducerSnapshotFiles=len(expected),ProvenanceSha256=H(out/'provenance/acceptance.json'),PinnedBlobsSha256=H(out/'source/pinned-blobs.json'),ProducerAuthoritySha256=H(out/'source/producer-authority.json'),ReviewerFreezeSha256=H(HERE/'source-freeze.json'),DownloadedCodeExecuted=False,CompatibilityPromoted=False)
    (out/'acceptance.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(dict(Acceptance=str(out/'acceptance.json'),Sha256=H(out/'acceptance.json'))))
if __name__=='__main__':main()

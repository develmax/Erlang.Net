"""Root-only passive orchestrator. Executes only these sealed trusted local audit scripts."""
from pathlib import Path
import argparse,hashlib,json,subprocess,sys,zipfile
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
HEAD='53823b168e672fc946e65f6dd53c0ea2f9c94611'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def main():
    p=argparse.ArgumentParser();p.add_argument('--download-run',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    remote=a.download_run.resolve();out=a.output.resolve()
    assert out.is_relative_to(HERE) and not out.exists();out.mkdir()
    freeze=J(HERE/'source-freeze.json')
    for row in freeze['Files']:
        q=HERE/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    summary=J(remote/'capture-summary.json');assert summary['Run']==39084998283 and summary['Head']==HEAD and summary['Conclusion']=='success' and summary['ContentExecuted'] is False
    assert summary['HelperSha256']==H(HERE/'capture.py') and summary['SourceBindingsSha256']==H(HERE/'source-bindings.json')
    artifact=remote/('artifact-'+str(summary['Artifact']));assert artifact.is_dir()
    archive=remote/('artifact-'+str(summary['Artifact'])+'.zip')
    assert H(archive)==summary['ZipSha256']
    with zipfile.ZipFile(archive) as zipped:
        files=[x for x in zipped.infolist() if not x.is_dir()]
        assert len(files)==len({x.filename for x in files})
        assert {q.relative_to(artifact).as_posix() for q in artifact.rglob('*') if q.is_file()}=={x.filename for x in files}
        for item in files:
            path=(artifact/item.filename).resolve();assert path.is_relative_to(artifact.resolve())
            assert path.stat().st_size==item.file_size and H(path)==hashlib.sha256(zipped.read(item)).hexdigest().upper()

    plan=J(ROOT/'artifacts/binary-original369-closure1/tool/source-inputs.corrected.json')
    expected={x['Path']:x for x in plan['RepositoryInputs']}
    for row in plan['ToolInputs']:expected['tools/Erlang.ProplistFamilyObservations/'+row['Path']]=row
    for row in J(HERE/'source-bindings.json')['Workflows']:expected[row['Path']]=row
    snapshot=out/'producer-snapshot';snapshot.mkdir()
    for path,row in expected.items():
        data=subprocess.check_output(['git','-C',str(ROOT),'show',HEAD+':'+path],timeout=30)
        assert len(data)==row['Bytes'] and hashlib.sha256(data).hexdigest().upper()==row['Sha256']
        target=snapshot/path;assert target.resolve().is_relative_to(snapshot.resolve());target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
    preflight=artifact/'preflight'
    args=[sys.executable,'-B',str(HERE/'audit-provenance.py'),'--artifact',str(artifact),'--run-json',str(remote/'run.json'),'--source-snapshot',str(snapshot),'--output',str(out/'provenance'),'--artifact-id',str(summary['Artifact']),'--job-id',str(summary['Job']),'--zip-sha256',summary['ZipSha256']]
    for name in ['parse-build','parse-sources','collector-admit']:args+=['--preflight-receipt',str(preflight/(name+'.process.json'))]
    subprocess.run(args,check=True)
    subprocess.run([sys.executable,'-B',str(HERE/'audit-source.py'),'--artifact',str(artifact),'--output',str(out/'source')],check=True)
    result={'Status':'PASS_PASSIVE_BINARY369_CLOSURE_ORIGINAL_ADMISSION','Run':summary['Run'],'Head':HEAD,'Values':14,'FullStackProfiles':70,'OriginalNormalProcesses':5,'PreflightNormalProcesses':3,'ProducerSnapshotFiles':len(expected),'PinnedSourceBlobs':12008,'ProvenanceSha256':H(out/'provenance/acceptance.json'),'PinnedBlobsSha256':H(out/'source/pinned-blobs.json'),'ProducerAuthoritySha256':H(out/'source/producer-authority.json'),'SdkCompatibilityPromoted':False,'DownloadedCodeExecuted':False,'ReviewerSourceFreezeSha256':H(HERE/'source-freeze.json')}
    (out/'acceptance.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'Acceptance':str(out/'acceptance.json'),'Sha256':H(out/'acceptance.json')}))
if __name__=='__main__':main()

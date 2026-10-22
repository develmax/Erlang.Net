"""Passive provenance only. Never imports/executes downloaded artifact code."""
from pathlib import Path, PurePosixPath
import argparse,hashlib,importlib.util,json
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
HEAD='230eb081348a0c2b5056707b71c8b0086fe42b0c'
RUN=39072770120
ROOT=Path(__file__).resolve().parents[2]
TOOL=ROOT/'artifacts/queue-original367-original2/tool'
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())

def module_md5(data):
    assert len(data)<=32*1024*1024 and data[:4]==b'FOR1' and data[8:12]==b'BEAM'
    assert int.from_bytes(data[4:8],'big')+8==len(data)
    chunks={};pos=12
    while pos<len(data):
        assert pos+8<=len(data)
        name=data[pos:pos+4].decode('ascii');size=int.from_bytes(data[pos+4:pos+8],'big')
        assert name not in chunks and pos+8+size<=len(data)
        chunks[name]=data[pos+8:pos+8+size];pos+=8+((size+3)//4)*4
    assert pos==len(data) and all(x in chunks for x in ['Code','StrT','ImpT','ExpT'])
    selected=[]
    for name in ['Atom','AtU8','Code','StrT','ImpT','ExpT','FunT','LitT','Meta','Recs','DbgB']:
        if name not in chunks:continue
        chunk=chunks[name]
        if name=='FunT':
            chunk=bytearray(chunk)
            for offset in range(4,len(chunk)-23,24):chunk[offset+20:offset+24]=b'\0'*4
            chunk=bytes(chunk)
        selected.append(chunk)
    return hashlib.md5(b''.join(selected)).digest(),list(chunks)

def text(node):
    assert node[0]=='list' and all(x[0]=='integer' and 0<=x[1]<=0x10ffff for x in node[1])
    return ''.join(chr(x[1]) for x in node[1])

def normal_original(directory,name,expected):
    p=directory/(name+'.process.json');x=J(p)
    assert H(p)==expected['ReceiptSha256']
    assert x['ExitCode']==0 and x['ParentReaped'] and x['StreamsClosed']
    assert not x['TerminationRequested'] and not x['RemainingGroupKillRequested'] and x['CleanupFailure'] is None
    for stream in ['Stdout','Stderr']:
        q=directory/(name+'.'+stream.lower());assert H(q)==expected[stream+'Sha256']
        assert q.stat().st_size==x[stream+'Bytes']
    status=directory/(name+('.status.json' if name in ['configure','make','install'] else '.stage.json'))
    assert J(status)['Started'] and J(status)['Failure'] is None
    return dict(Name=name,Directory=str(directory),ReceiptSha256=H(p),Pid=x['OwnedPid'],ElapsedSeconds=x['ElapsedSeconds'],Normal=True)

def normal_preflight(p):
    x=J(p)
    if p.name.endswith('.exit.json'):
        assert x['Settled'] and not x['TimedOut'] and x['ExitCode']==0 and not x['Failures']
        stem=p.name[:-len('.exit.json')]
        for stream in ['Stdout','Stderr']:assert H(p.parent/(stem+'.'+stream.lower()+'.log'))==x[stream+'Sha256']
        return dict(Path=str(p),Sha256=H(p),Pid=x['Pid'],ElapsedSeconds=x['ElapsedSeconds'],Normal=True)
    assert p.name.endswith('.process.json')
    assert x['ParentReaped'] and x['StreamsClosed'] and x['ExitCode']==0
    assert not x['TerminationRequested'] and not x['RemainingGroupKillRequested'] and x['CleanupFailure'] is None
    stem=p.name[:-len('.process.json')]
    for stream in ['Stdout','Stderr']:assert (p.parent/(stem+'.'+stream.lower())).stat().st_size==x[stream+'Bytes']
    return dict(Path=str(p),Sha256=H(p),Pid=x['OwnedPid'],ElapsedSeconds=x['ElapsedSeconds'],Normal=True)

def main():
    p=argparse.ArgumentParser();p.add_argument('--artifact',required=True);p.add_argument('--run-json',required=True)
    p.add_argument('--source-snapshot',required=True);p.add_argument('--preflight-receipt',action='append',required=True)
    p.add_argument('--output',required=True);p.add_argument('--artifact-id',type=int,required=True);p.add_argument('--job-id',type=int,required=True);p.add_argument('--zip-sha256',required=True);a=p.parse_args()
    artifact=Path(a.artifact).resolve();snapshot=Path(a.source_snapshot).resolve();output=Path(a.output).resolve()
    assert not output.exists() and output.is_relative_to(ROOT/'artifacts/queue-original367-peer2')
    run=J(a.run_json);assert run['id']==RUN and run['head_sha']==HEAD and run['conclusion']=='success' and run['run_attempt']==1
    remote=Path(a.run_json).parent;archive=remote/('artifact-'+str(a.artifact_id)+'.zip')
    assert H(archive)==a.zip_sha256.upper()
    jobs=J(remote/'jobs.json')['jobs'];assert len(jobs)==1 and jobs[0]['id']==a.job_id and jobs[0]['conclusion']=='success'
    ars=J(remote/'artifacts.json')['artifacts'];assert len(ars)==1 and ars[0]['id']==a.artifact_id and ars[0]['workflow_run']['head_sha']==HEAD
    assert ars[0]['digest'].split(':')[1].upper()==H(archive)
    downloads=J(remote/'download-manifest.json')
    for row in downloads:
        q=(remote/row['Path']).resolve();assert q.is_relative_to(remote.resolve()) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    bindings=J(ROOT/'artifacts/queue-original367-original2/ci/dispatch-intent.json');assert bindings['Head']==HEAD
    for row in bindings['Bindings']:
        q=snapshot/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    manifest=snapshot/'tools/Erlang.ProplistFamilyObservations/source-inputs.corrected.json';assert H(manifest)=='DC8399C2E14E49B949365F5CCA441FDD1B047646AE27DA83C3102B565C10E148'
    plan=J(manifest);assert plan['PinnedOtpCommit']==PIN
    for row in plan['RepositoryInputs']:
        q=snapshot/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'],row['Path']
    for row in plan['ToolInputs']:
        q=snapshot/'tools/Erlang.ProplistFamilyObservations'/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256'],row['Path']
    assert H(TOOL/'profile.json')==H(snapshot/'tools/Erlang.ProplistFamilyObservations/profile.json')
    decoderpath=ROOT/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py'
    assert H(decoderpath)=='A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
    spec=importlib.util.spec_from_file_location('trusted_passive_meta',decoderpath);decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
    buildpaths=list(artifact.rglob('build-receipt.json'));assert len(buildpaths)==1
    buildpath=buildpaths[0];builddir=buildpath.parent;build=J(buildpath)
    assert build['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['Commit']==PIN and build['SubjectHead']==HEAD
    assert build['SourceManifestSha256']==plan['ExistingBuildManifestSha256']
    assert H(builddir/'source-before.json')==H(builddir/'source-after.json')==build['SourceBeforeSha256']==build['SourceAfterSha256']
    source=J(builddir/'source-before.json');assert source['Commit']==PIN
    rows=source['TrackedFiles'];assert len({x['Path'] for x in rows})==len(rows)
    previous=ROOT/'artifacts/part362-original-remote1/run-39041200099/artifact-12104680321/build/source-before.json'
    assert source==J(previous), 'New tracked source identity differs from previously independently accepted pinned source'
    proplist=next(x for x in rows if x['Path']=='lib/stdlib/src/queue.erl')
    assert proplist['Sha256']=='61B322302ECF74578D72B442D2317EA200A5DEE4E0994B58C5E1E961F252B101'
    stages=[];assert [x['Name'] for x in build['Stages']]==['configure','make','install']
    for x in build['Stages']:stages.append(normal_original(builddir,x['Name'],x))
    captures=[q for q in artifact.rglob('completion.json') if J(q).get('SourceInputsSha256')==H(manifest)]
    assert len(captures)==1;capture=captures[0].parent;completion=J(captures[0]);inputs=J(capture/'inputs.json')
    assert completion['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and completion['SubjectHead']==inputs['SubjectHead']==HEAD
    assert completion['BuildReceiptSha256']==inputs['BuildReceiptSha256']==H(buildpath)
    assert inputs['PinnedOtpCommit']==completion['PinnedOtpCommit']==PIN and inputs['PinnedQueueSource']==proplist
    profile=J(TOOL/'profile.json');assert len(profile['Mfas'])==len(set(profile['Mfas']))==5
    assert len(profile['Fixtures'])==4 and profile['ExpectedUniqueSourceCount']==2
    assert completion['ProfileSha256']==inputs['ProfileSha256']==H(TOOL/'profile.json')
    assert inputs['SourceInputsSha256']==H(manifest) and completion['AllGroupsAttempted'] and completion['FailedGroups']==0
    assert completion['OriginalBuildsByCollector']==0
    installed=capture/'queue-installed.beam';data=installed.read_bytes();installedsha=H(installed)
    assert inputs['InstalledQueueSha256']==installedsha
    installedrows={x['Path']:x for x in build['InstalledFiles']};rt=PurePosixPath(build['RuntimeRoot'])
    relative=PurePosixPath(inputs['InstalledQueuePath']).relative_to(rt).as_posix()
    # Shared build manifest deliberately does not include queue.beam.
    assert relative not in installedrows
    assert PurePosixPath(inputs['ErlPath'])==rt/'bin/erl' and PurePosixPath(inputs['ErlcPath'])==rt/'bin/erlc'
    assert inputs['ErlSha256']==installedrows['bin/erl']['Sha256'] and inputs['ErlcSha256']==installedrows['bin/erlc']['Sha256']
    md5,chunks=module_md5(data);groups=[]
    assert [x['Id'] for x in completion['Groups']]==[x['Id'] for x in profile['Fixtures']]
    for fixture,group in zip(profile['Fixtures'],completion['Groups']):
        directory=capture/fixture['Id'];assert group['Acquired'] and not group['VmSkipped']
        assert (directory/'queue-loaded.beam').read_bytes()==data
        metadata=decoder.decode_exact((directory/'metadata.etf').read_bytes());assert metadata[0]=='tuple' and len(metadata[1])==11
        fields=metadata[1];assert fields[0]==('atom','family_provenance')
        assert text(fields[1])==build['RuntimeRoot'] and text(fields[2])==text(fields[3])==inputs['InstalledQueuePath']
        assert fields[4]==('binary',hashlib.md5(data).digest()) and fields[5]==('binary',md5)
        assert fields[7]==('list',tuple(('atom',n) for n in fixture['Names']))
        assert text(fields[8])=='29' and fields[9:11]==(('integer',2),('integer',2))
        receipts=[x for x in completion['OwnedStages'] if x['Group']==fixture['Id']]
        assert [x['Name'] for x in receipts]==['compile','observe']
        for stage in receipts:stages.append(normal_original(directory,stage['Name'],stage))
        compileargs=J(directory/'compile.process.json')['Arguments'];observeargs=J(directory/'observe.process.json')['Arguments']
        assert '-Werror' in compileargs and '+deterministic' in compileargs
        assert '+S' in observeargs and observeargs[observeargs.index('+S')+1]=='2:2'
        assert text(fields[6])==compileargs[compileargs.index('-o')+1]+'/'+fixture['Module']+'.beam'
        for row in group['CompiledFiles']:assert H(directory/'ebin'/(row['Module']+'.beam'))==row['Sha256']
        for name,key in [('returned.etf','ReturnedSha256'),('metadata.etf','MetadataSha256'),('stack-observations.etf','RawStacksSha256')]:assert H(directory/name)==group[key]
        groups.append(dict(Id=fixture['Id'],Names=len(fixture['Names']),LoadedBytesExact=True,FullFileMd5=hashlib.md5(data).hexdigest().upper(),ModuleInfoMd5=md5.hex().upper(),MetadataSha256=H(directory/'metadata.etf')))
    assert len(stages)==11 and len({x['Pid'] for x in stages})==11
    preflights=[normal_preflight(Path(x).resolve()) for x in a.preflight_receipt];assert len(preflights)==3 and len({x['Path'] for x in preflights})==3
    preflight=artifact/'preflight';gate=J(preflight/'completion.json');assert gate['Status']=='PASS_LINUX_NAMED_FIXTURE_GATE'
    assert [x['Name'] for x in gate['Stages']]==['parse-build','parse-sources','collector-admit']
    for row in gate['Stages']:
        q=preflight/(row['Name']+'.process.json');assert H(q)==row['ReceiptSha256'] and row['Failure'] is None
        assert any(x['Path']==str(q.resolve()) for x in preflights)
    assert H(preflight/'parse-receipt.json')==gate['ParseGateSha256']==inputs['ParseGateSha256']
    parse=J(preflight/'parse-receipt.json');assert parse['Status']=='PASS_FIXTURE_PARSE_GATE' and len(parse['Fixtures'])==2
    expected_sources=[]
    for f in profile['Fixtures']:
        expected_sources.append((snapshot/'tools/Erlang.ProplistFamilyObservations'/f['Path']) if f['Location']=='tool' else snapshot/f['Path'])
        expected_sources.extend(snapshot/'tools/Erlang.ProplistFamilyObservations'/x for x in f['Companions'])
    expected_sources=list(dict.fromkeys(expected_sources))
    assert len(expected_sources)==profile['ExpectedUniqueSourceCount']==2
    assert len({x['Path'] for x in parse['Fixtures']})==2
    assert all(x.get('Passed') is True and 'Error' not in x for x in parse['Fixtures'])
    assert sorted(x['Sha256'] for x in parse['Fixtures'])==sorted(H(x) for x in expected_sources)
    for row in parse['Fixtures']:
        matches=[q for q in expected_sources if row['Path'].endswith(q.relative_to(snapshot).as_posix())]
        assert len(matches)==1 and H(matches[0])==row['Sha256']
    result=dict(Status='PASS_PASSIVE_QUEUE_LONG11_ORIGINAL_PROVENANCE_ONLY',Head=HEAD,Run=RUN,PinnedOtp=PIN,TrackedSourceRows=len(rows),SourceBeforeAfterExact=True,ManifestSha256=H(manifest),InstalledSha256=installedsha,SignificantChunks=chunks,Groups=groups,OriginalNormalProcesses=stages,PreflightNormalProcesses=preflights,ValueOrStackEqualityClaimed=False,DownloadedCodeExecuted=False,SnapshotHeadReadByGit=False,ArtifactZipSha256=H(archive),ArtifactFilesVerified=len(downloads),SourceMatchesPriorAcceptedPinnedManifest=True,InstalledRuntimeInventoryRows=len(installedrows),QueueBeamInBuildInventory=False,Limit='Source snapshot files are hash-bound; workflow head comes from supplied run metadata. Shared build inventory has11 runtime files and omits queue.beam. Its retained installed copy is bound by collector inputs and independently identical to the loaded object files, full-file and module-info MD5. Only retained BEAM files are independently reread.')
    output.mkdir();(output/'acceptance.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(Status=result['Status'],AcceptanceSha256=H(output/'acceptance.json')),indent=2))
if __name__=='__main__':main()

"""Passive erlang-preloaded provenance. Imports only hash-admitted local review code."""
from pathlib import Path
import argparse,hashlib,importlib.util,json
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).resolve().parent
HEAD='20001454356011e318f2dea6b3d7ed8b8d569f90';RUN=39108122243;PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL=ROOT/'artifacts/erlang370-closure-original-proposal2/tool'
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def load(p,name):
    s=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def main():
    p=argparse.ArgumentParser();p.add_argument('--artifact',type=Path,required=True);p.add_argument('--run-json',type=Path,required=True);p.add_argument('--source-snapshot',type=Path,required=True);p.add_argument('--preflight-receipt',action='append',required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--artifact-id',type=int,required=True);p.add_argument('--job-id',type=int,required=True);p.add_argument('--zip-sha256',required=True);a=p.parse_args()
    out=a.output.resolve();assert not out.exists() and out.is_relative_to(HERE)
    remote=a.run_json.parent;run=J(a.run_json);assert run['id']==RUN and run['head_sha']==HEAD and run['run_attempt']==1 and run['conclusion']=='success'
    assert H(remote/('artifact-'+str(a.artifact_id)+'.zip'))==a.zip_sha256.upper()
    jobs=J(remote/'jobs.json')['jobs'];assert len(jobs)==1 and jobs[0]['id']==a.job_id and jobs[0]['conclusion']=='success'
    ars=J(remote/'artifacts.json')['artifacts'];assert len(ars)==1 and ars[0]['id']==a.artifact_id and ars[0]['workflow_run']['head_sha']==HEAD and ars[0]['digest'].split(':')[1].upper()==a.zip_sha256.upper()
    snapshot=a.source_snapshot;manifest=snapshot/'tools/Erlang.ProplistFamilyObservations/source-inputs.corrected.json';assert H(manifest)=='0258DDF97CE3940C7C07AD129747726CFB8263F6F196D9369A1EDCD4419A1182'
    plan=J(manifest)
    for row in plan['RepositoryInputs']:
        q=snapshot/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    for row in plan['ToolInputs']:
        q=snapshot/'tools/Erlang.ProplistFamilyObservations'/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    # Source-only local copies must agree with exact committed producer inputs before import.
    for name in ['preloaded_provenance.py','passive_etf_family.py','profile.json']:
        assert H(TOOL/name)==H(snapshot/'tools/Erlang.ProplistFamilyObservations'/name)
    helper=load(HERE/'receipt_helpers.py','trusted_receipts');preloaded=load(TOOL/'preloaded_provenance.py','trusted_preloaded');decoder=load(TOOL/'passive_etf_family.py','trusted_raw')
    paths=list(a.artifact.rglob('build-receipt.json'));assert len(paths)==1
    buildpath=paths[0];builddir=buildpath.parent;build=J(buildpath)
    assert build['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['SubjectHead']==HEAD and build['Commit']==PIN and build['SourceManifestSha256']==plan['ExistingBuildManifestSha256']
    assert H(builddir/'source-before.json')==H(builddir/'source-after.json')==build['SourceBeforeSha256']==build['SourceAfterSha256']
    source=J(builddir/'source-before.json');assert source['Commit']==PIN and len(source['TrackedFiles'])==12008
    stages=[];assert [s['Name'] for s in build['Stages']]==['configure','make','install']
    for s in build['Stages']:stages.append(helper.normal_original(builddir,s['Name'],s))
    cp=[q for q in a.artifact.rglob('completion.json') if J(q).get('SourceInputsSha256')==H(manifest)];assert len(cp)==1
    capture=cp[0].parent;completion=J(cp[0]);inputs=J(capture/'inputs.json');profile=J(TOOL/'profile.json')
    assert completion['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and completion['SubjectHead']==inputs['SubjectHead']==HEAD
    assert completion['BuildReceiptSha256']==inputs['BuildReceiptSha256']==H(buildpath) and inputs['PinnedOtpCommit']==PIN
    assert completion['AllGroupsAttempted'] and completion['FailedGroups']==0 and len(completion['Groups'])==4 and completion['OriginalBuildsByCollector']==0
    assert completion['ProfileSha256']==inputs['ProfileSha256']==H(TOOL/'profile.json') and inputs['SourceInputsSha256']==H(manifest)
    inventory={r['Path']:r for r in build['InstalledFiles']};runtime=Path(build['RuntimeRoot']).resolve()
    for key,tail in [('InstalledEmulator','/bin/beam.smp'),('InstalledCompanion','/erlang.beam')]:
        match=[r for r in inventory.values() if r['Path'].endswith(tail)];assert len(match)==1
        assert inputs[key+'Sha256']==match[0]['Sha256']
        assert str(Path(inputs[key+'Path']).resolve())==str((runtime/match[0]['Path']).resolve())
    companion=capture/'erlang-preloaded-companion.beam';assert H(companion)==inputs['InstalledCompanionSha256']
    assert preloaded.module_md5(companion.read_bytes()).hex().upper()==inputs['InstalledCompanionModuleMd5'] and inputs['CompanionIsLoadedObjectClaimed'] is False
    for key,path in [('PinnedErlangSource','erts/preloaded/src/erlang.erl'),('PinnedBifTable','erts/emulator/beam/bif.tab')]:assert inputs[key]==next(r for r in source['TrackedFiles'] if r['Path']==path)
    authority=capture/'erlang-pinned-preload-authority.beam'
    tracked=next(r for r in source['TrackedFiles'] if r['Path']=='erts/preloaded/ebin/erlang.beam')
    assert H(authority)==tracked['Sha256'] and authority.stat().st_size==tracked['Bytes'] and inputs['PinnedPreloadBeam']==tracked
    assert preloaded.preloaded_module_md5(authority.read_bytes()).hex().upper()==inputs['PredictedPreloadModuleMd5']
    assert inputs['PinnedMakePreload']==next(r for r in source['TrackedFiles'] if r['Path']=='erts/emulator/utils/make_preload')
    count=profiles=0;groups=[]
    for fixture,group in zip(profile['Fixtures'],completion['Groups']):
        assert group['Id']==fixture['Id'] and group['Acquired'] and not group['VmSkipped']
        directory=capture/group['Id'];observestage=J(directory/'observe.stage.json');compilestage=J(directory/'compile.stage.json')
        for name in ['compile','observe']:
            proof=next(s for s in completion['OwnedStages'] if s['Group']==group['Id'] and s['Name']==name)
            stages.append(helper.normal_original(directory,name,proof))
        assert 'code:get_object_code' not in observestage['Arguments'][-1]
        assert observestage['Arguments'][observestage['Arguments'].index('+S')+1]=='2:2'
        ebin=Path(compilestage['Arguments'][compilestage['Arguments'].index('-o')+1]).resolve()
        md=decoder.decode_exact((directory/'metadata.etf').read_bytes());pid=J(directory/'observe.process.json')['OwnedPid']
        admission=preloaded.validate(md,inputs,runtime,fixture,ebin,pid,profile['Mfas']);assert admission==group['MetadataAdmission']
        for row in group['CompiledFiles']:assert H(directory/'ebin'/(row['Module']+'.beam'))==row['Sha256']
        for name,key in [('returned.etf','ReturnedSha256'),('metadata.etf','MetadataSha256'),('stack-observations.etf','RawStacksSha256')]:assert H(directory/name)==group[key]
        values=decoder.decode_exact((directory/'returned.etf').read_bytes());assert values[0]=='list' and len(values[1])==len(fixture['Names'])
        for name,row in zip(fixture['Names'],values[1]):
            assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name);preloaded.validate_outcome(row[1][1],False);count+=1
        depths=decoder.decode_exact((directory/'stack-observations.etf').read_bytes());assert depths[0]=='list' and len(depths[1])==5
        for depth,rows in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],depths[1]):
            assert rows[0]=='tuple' and len(rows[1])==2 and rows[1][0]==depth
            rows=rows[1][1];assert rows[0]=='list' and len(rows[1])==len(fixture['Names'])
            for name,row in zip(fixture['Names'],rows[1]):
                assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name);preloaded.validate_outcome(row[1][1],True);profiles+=1
        groups.append({'Id':group['Id'],'MetadataAdmission':admission,'Values':len(fixture['Names'])})
    preflights=[helper.normal_preflight(Path(p)) for p in a.preflight_receipt];assert len(preflights)==3
    gate=J(a.artifact/'preflight/completion.json');assert gate['Status']=='PASS_LINUX_NAMED_FIXTURE_GATE'
    parse=J(a.artifact/'preflight/parse-receipt.json');assert parse['Status']=='PASS_FIXTURE_PARSE_GATE' and len(parse['Fixtures'])==6
    assert all(row.get('Passed') is True and 'Error' not in row for row in parse['Fixtures']) and len({r['Path'] for r in parse['Fixtures']})==6
    expected={str((snapshot/'tools/Erlang.ProplistFamilyObservations'/r['Path']).relative_to(snapshot)).replace('\\','/'):r['Sha256'] for r in profile['FixtureSources']}
    for row in parse['Fixtures']:
        matches=[sha for path,sha in expected.items() if row['Path'].endswith(path)];assert matches==[row['Sha256']]
    assert H(a.artifact/'preflight/parse-receipt.json')==gate['ParseGateSha256']==inputs['ParseGateSha256']
    assert len(stages)==11 and len({r['Pid'] for r in stages})==11 and count==44 and profiles==220
    out.mkdir();(out/'acceptance.json').write_text(json.dumps(dict(Status='PASS_PASSIVE_ERLANG370_PRELOADED_ORIGINAL_PROVENANCE',Head=HEAD,Run=RUN,Values=count,FullStackProfiles=profiles,OriginalNormalProcesses=stages,PreflightNormalProcesses=preflights,Groups=groups,CompanionIsLoadedObjectClaimed=False,DownloadedCodeExecuted=False,CompatibilityPromoted=False),indent=2)+'\n')
if __name__=='__main__':main()

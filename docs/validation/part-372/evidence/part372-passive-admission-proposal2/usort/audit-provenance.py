"""Ordinary installed-module provenance, independently admitted before typed comparison."""
from pathlib import Path, PurePosixPath
import argparse,hashlib,importlib.util,json
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5';TOOLPATH='tools/Erlang.OrdsetsUsort372OriginalObservations'
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def load(path,name):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def metadata(node,inputs,runtime,fixture,ebin,installed,module_md5):
    assert node[0]=='tuple' and len(node[1])==11
    x=node[1]
    def text(n):
        assert n[0]=='list' and all(t[0]=='integer' and 0<=t[1]<=0x10ffff for t in n[1]);return ''.join(chr(t[1]) for t in n[1])
    assert x[0]==('atom','family_provenance') and PurePosixPath(text(x[1]))==PurePosixPath(runtime)
    assert PurePosixPath(text(x[2]))==PurePosixPath(text(x[3]))==PurePosixPath(inputs['InstalledOrdsetsPath'])
    assert PurePosixPath(inputs['InstalledOrdsetsPath']).is_relative_to(PurePosixPath(runtime))
    assert x[4]==('binary',hashlib.md5(installed).digest()) and x[5]==('binary',module_md5)
    assert PurePosixPath(text(x[6]))==PurePosixPath(ebin)/(fixture['Module']+'.beam')
    assert x[7]==('list',tuple(('atom',n) for n in fixture['Names'])) and text(x[8])=='29'
    assert x[9:11]==(('integer',2),('integer',2))
    return dict(Subject='ordsets',InstalledImage=True,ModuleMd5=module_md5.hex().upper(),Schedulers=2,Names=len(fixture['Names']))

def main():
    p=argparse.ArgumentParser()
    for key in ['artifact','run-json','source-snapshot','output','tool']:p.add_argument('--'+key,type=Path,required=True)
    p.add_argument('--preflight-receipt',action='append',type=Path,required=True)
    for key in ['artifact-id','job-id','run-id']:p.add_argument('--'+key,type=int,required=True)
    for key in ['zip-sha256','expected-head','manifest-sha256']:p.add_argument('--'+key,required=True)
    a=p.parse_args();out=a.output.resolve();assert not out.exists() and out.is_relative_to(HERE)
    run=J(a.run_json);assert run['id']==a.run_id and run['head_sha']==a.expected_head and run['run_attempt']==1 and run['conclusion'] in ['success','failure']
    remote=a.run_json.parent;assert H(remote/('artifact-'+str(a.artifact_id)+'.zip'))==a.zip_sha256.upper()
    jobs=J(remote/'jobs.json')['jobs'];assert len(jobs)==1 and jobs[0]['id']==a.job_id and jobs[0]['conclusion']==run['conclusion']
    artifacts=J(remote/'artifacts.json')['artifacts'];assert len(artifacts)==1 and artifacts[0]['id']==a.artifact_id and artifacts[0]['workflow_run']['head_sha']==a.expected_head and artifacts[0]['digest'].split(':')[1].upper()==a.zip_sha256.upper()
    steps=jobs[0]['steps']
    for stepname in ['Admit and parse independent usort cohort', 'Clone pinned source once after successful cohort preparation', 'Build one pinned original installation shared by independent collectors', 'Collect independent usort full outcomes and stacks', 'Run actions/upload-artifact@v7']:
        match=[step for step in steps if step['name']==stepname]
        assert len(match)==1 and match[0]['status']=='completed' and match[0]['conclusion']=='success',stepname
    outcomes=J(a.artifact/'outcomes.json')
    assert all(outcomes[key]=='success' for key in ['USORT_PREPARATION', 'SOURCE', 'BUILD', 'USORT_COLLECTION'])
    manifest=a.source_snapshot/TOOLPATH/'source-inputs.corrected.json';assert H(manifest)==a.manifest_sha256.upper()
    plan=J(manifest);assert plan['PinnedOtpCommit']==PIN
    for row in plan['RepositoryInputs']:
        q=a.source_snapshot/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    for row in plan['ToolInputs']:
        q=a.source_snapshot/TOOLPATH/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
        local=a.tool/row['Path'];assert H(local)==H(q)
    helper=load(HERE/'receipt_helpers.py','ordsets_receipts');decoder=load(a.tool/'passive_etf_family.py','admitted_ordsets_decoder')
    builds=list(a.artifact.rglob('build-receipt.json'));assert len(builds)==1;buildpath=builds[0];builddir=buildpath.parent;build=J(buildpath)
    assert build['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['Commit']==PIN and build['SubjectHead']==a.expected_head and build['SourceManifestSha256']==plan['ExistingBuildManifestSha256']
    assert H(builddir/'source-before.json')==H(builddir/'source-after.json')==build['SourceBeforeSha256']==build['SourceAfterSha256']
    source=J(builddir/'source-before.json');assert source['Commit']==PIN and len(source['TrackedFiles'])==12008
    assert [x['Name'] for x in build['Stages']]==['configure','make','install']
    stages=[helper.normal_original(builddir,x['Name'],x) for x in build['Stages']]
    captures=[q for q in a.artifact.rglob('completion.json') if J(q).get('SourceInputsSha256')==H(manifest)];assert len(captures)==1
    capture=captures[0].parent;completion=J(captures[0]);inputs=J(capture/'inputs.json');profile=J(a.tool/'profile.json')
    assert completion['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and completion['SubjectHead']==inputs['SubjectHead']==a.expected_head
    assert completion['BuildReceiptSha256']==inputs['BuildReceiptSha256']==H(buildpath) and completion['PinnedOtpCommit']==inputs['PinnedOtpCommit']==PIN
    assert completion['ProfileSha256']==inputs['ProfileSha256']==H(a.tool/'profile.json') and completion['SourceInputsSha256']==inputs['SourceInputsSha256']==H(manifest)
    assert completion['AllGroupsAttempted'] and completion['FailedGroups']==0 and len(completion['Groups'])==len(profile['Fixtures'])==1 and completion['OriginalBuildsByCollector']==0
    assert inputs['PinnedOrdsetsSource']==next(x for x in source['TrackedFiles'] if x['Path']=='lib/stdlib/src/ordsets.erl')
    runtime=PurePosixPath(build['RuntimeRoot']);inventory={x['Path']:x for x in build['InstalledFiles']}
    for key,path in [('Erl','bin/erl'),('Erlc','bin/erlc')]:assert inputs[key+'Sha256']==inventory[path]['Sha256'] and PurePosixPath(inputs[key+'Path'])==runtime/path
    installed=capture/'ordsets-installed.beam';raw=installed.read_bytes();assert H(installed)==inputs['InstalledOrdsetsSha256']
    md5,chunks=helper.module_md5(raw)
    count=profiles=0;controls=[];groups=[]
    for f,g in zip(profile['Fixtures'],completion['Groups']):
        assert g['Id']==f['Id'] and g['Acquired'] and not g['VmSkipped'];directory=capture/g['Id']
        for name in ['compile','observe']:
            proof=next(x for x in completion['OwnedStages'] if x['Group']==g['Id'] and x['Name']==name);stages.append(helper.normal_original(directory,name,proof))
        args=J(directory/'compile.stage.json')['Arguments'];ebin=args[args.index('-o')+1]
        expression=J(directory/'observe.stage.json')['Arguments'][-1]
        assert '{ok,{ordsets,LoadedMd5}}=beam_lib:md5(B),LoadedMd5=ordsets:module_info(md5)' in expression
        assert f['WorkerModule']+':'+f['WorkerEntry']+'('+f['Module']+','+f['Run']+')' in expression
        assert f['WorkerModule']+':'+f['WorkerEntry']+'('+f['StackModule']+','+f['StackEntry']+')' in expression
        assert H(directory/'ordsets-loaded.beam')==H(installed)
        md=decoder.decode_exact((directory/'metadata.etf').read_bytes());admitted=metadata(md,inputs,str(runtime),f,ebin,raw,md5)
        for index,key,bad in [(5,'module_md5',('binary',bytes(16))),(9,'schedulers',('integer',1)),(7,'names',('list',()))]:
            fields=list(md[1]);fields[index]=bad;refused=False
            try:metadata(('tuple',tuple(fields)),inputs,str(runtime),f,ebin,raw,md5)
            except AssertionError:refused=True
            assert refused;controls.append(dict(Group=g['Id'],Corruption=key,Rejected=True))
        for row in g['CompiledFiles']:assert H(directory/'ebin'/(row['Module']+'.beam'))==row['Sha256']
        for file,key in [('returned.etf','ReturnedSha256'),('stack-observations.etf','RawStacksSha256'),('metadata.etf','MetadataSha256')]:assert H(directory/file)==g[key]
        values=decoder.decode_exact((directory/'returned.etf').read_bytes());assert values[0]=='list' and len(values[1])==len(f['Names'])
        for name,row in zip(f['Names'],values[1]):assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name);count+=1
        depths=decoder.decode_exact((directory/'stack-observations.etf').read_bytes());assert depths[0]=='list' and len(depths[1])==5
        for depth,rows in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],depths[1]):
            assert rows[0]=='tuple' and len(rows[1])==2 and rows[1][0]==depth;rows=rows[1][1];assert rows[0]=='list' and len(rows[1])==len(f['Names'])
            for name,row in zip(f['Names'],rows[1]):assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name);profiles+=1
        groups.append(dict(Id=g['Id'],Metadata=admitted,Values=len(f['Names']),StackProfiles=5*len(f['Names'])))
    preflight=a.artifact/'usort-preflight';gate=J(preflight/'completion.json');assert gate['Status']=='PASS_LINUX_NAMED_FIXTURE_GATE' and [x['Name'] for x in gate['Stages']]==['parse-build','parse-sources','collector-admit']
    preflights=[]
    for receipt,stage in zip(a.preflight_receipt,gate['Stages']):
        assert receipt.parent.resolve()==preflight.resolve() and H(receipt)==stage['ReceiptSha256'] and stage['Failure'] is None
        preflights.append(helper.normal_preflight(receipt))
    parse=J(preflight/'parse-receipt.json');assert parse['Status']=='PASS_FIXTURE_PARSE_GATE' and len(parse['Fixtures'])==3
    assert len({x['Path'] for x in parse['Fixtures']})==3 and all(x.get('Passed') is True and 'Error' not in x for x in parse['Fixtures'])
    for row in parse['Fixtures']:
        matches=[x['Sha256'] for x in profile['FixtureSources'] if row['Path'].endswith(TOOLPATH+'/'+x['Path'])];assert matches==[row['Sha256']]
    assert H(preflight/'parse-receipt.json')==gate['ParseGateSha256']==inputs['ParseGateSha256']
    assert len(stages)==5 and len({x['Pid'] for x in stages})==5 and len(preflights)==3 and count==45 and profiles==225
    out.mkdir();(out/'acceptance.json').write_text(json.dumps(dict(Status='PASS_PASSIVE_USORT372_ORDINARY_PROVENANCE',ParentRunConclusion=run['conclusion'],FamilyOnlyAdmission=True,ParentRunRelabelled=False,Head=a.expected_head,Run=a.run_id,Values=count,FullStackProfiles=profiles,MetadataCorruptionControls=controls,OriginalNormalProcesses=stages,PreflightNormalProcesses=preflights,Groups=groups,DownloadedCodeExecuted=False,CompatibilityPromoted=False),indent=2)+'\n')
if __name__=='__main__':main()

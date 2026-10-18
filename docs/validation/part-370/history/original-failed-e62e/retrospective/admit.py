"""Independent retrospective admission; failed producer status remains failed."""
from pathlib import Path
import hashlib,json,zipfile,importlib.util,subprocess
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent;REMOTE=ROOT/'artifacts/erlang370-original-failure1';RAW=REMOTE/'raw';TOOL=ROOT/'artifacts/erlang370-original-proposal1/tool';REPAIR=ROOT/'artifacts/erlang370-preloaded-repair-proposal1'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def load(p,n):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
head='e62e0f383929a2f5acf83adc012ee26f11342bce';pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
meta=J(REMOTE/'artifacts.json');assert meta['total_count']==1;artifact=meta['artifacts'][0];archive=REMOTE/'original-queue8.zip'
assert artifact['workflow_run']['id']==39106192411 and artifact['workflow_run']['head_sha']==head and artifact['size_in_bytes']==archive.stat().st_size and artifact['digest'].split(':')[1].upper()==H(archive)
with zipfile.ZipFile(archive) as z:
 files=[i for i in z.infolist() if not i.is_dir()];assert len({i.filename for i in files})==len(files)
 assert {p.relative_to(RAW).as_posix() for p in RAW.rglob('*') if p.is_file()}=={i.filename for i in files}
 for i in files:
  q=(RAW/i.filename).resolve();assert q.is_relative_to(RAW.resolve()) and q.stat().st_size==i.file_size and H(q)==hashlib.sha256(z.read(i)).hexdigest().upper()
helpers=load(ROOT/'artifacts/erlang370-admission-proposal1/receipt_helpers.py','trusted_receipts');m=load(REPAIR/'tool/preloaded_provenance.py','trusted_repair');d=load(TOOL/'passive_etf_family.py','trusted_decoder')
originalseal=J(TOOL.parent/'source-seal.json')
for row in originalseal['Files']:assert H(TOOL.parent/row['Path'])==row['Sha256']
for row in J(REPAIR/'source-seal.json')['Files']:assert H(REPAIR/row['Path'])==row['Sha256']
p=J(TOOL/'profile.json');plan=J(TOOL/'source-inputs.corrected.json');b=J(RAW/'build/build-receipt.json');c=J(RAW/'capture/completion.json');inputs=J(RAW/'capture/inputs.json')
assert b['Commit']==pin and b['SubjectHead']==c['SubjectHead']==inputs['SubjectHead']==head
assert b['SourceManifestSha256']==plan['ExistingBuildManifestSha256'] and c['SourceInputsSha256']==inputs['SourceInputsSha256']==H(TOOL/'source-inputs.corrected.json')
assert c['Status']=='FAIL_ORIGINAL_CAPTURE_GROUPS_RETAINED' and c['FailedGroups']==8 and c['AllGroupsAttempted'] and c['OriginalBuildsByCollector']==0 and c['MetadataPolicy']=='erlang-preloaded-v1'
assert c['ProfileSha256']==inputs['ProfileSha256']==H(TOOL/'profile.json') and c['BuildReceiptSha256']==inputs['BuildReceiptSha256']==H(RAW/'build/build-receipt.json')
assert H(RAW/'build/source-before.json')==H(RAW/'build/source-after.json')==b['SourceBeforeSha256']==b['SourceAfterSha256']
source=J(RAW/'build/source-before.json');assert source['Commit']==pin and len(source['TrackedFiles'])==12008
assert J(OUT/'source/pinned-blobs.json')['Status']=='PASS_ALL12008_PINNED_GIT_BLOBS' and J(OUT/'source/producer-authority.json')['Head']==head
stages=[]
for s in b['Stages']:stages.append(helpers.normal_original(RAW/'build',s['Name'],s))
# Derive the actual loaded identity from pinned tracked preloaded bytes, not captured native output.
preload=REPAIR/'authority/erlang.beam';authority=next(r for r in source['TrackedFiles'] if r['Path']=='erts/preloaded/ebin/erlang.beam');assert H(preload)==authority['Sha256'] and preload.stat().st_size==authority['Bytes']
expected=m.preloaded_module_md5(preload.read_bytes()).hex().upper();corrected=dict(inputs,PredictedPreloadModuleMd5=expected)
assert H(RAW/'capture/erlang-preloaded-companion.beam')==inputs['InstalledCompanionSha256']
assert m.module_md5((RAW/'capture/erlang-preloaded-companion.beam').read_bytes()).hex().upper()==inputs['InstalledCompanionModuleMd5'] and expected!=inputs['InstalledCompanionModuleMd5']
installed={r['Path']:r for r in b['InstalledFiles']}
for key,suffix in [('InstalledEmulator','/bin/beam.smp'),('InstalledCompanion','/erlang.beam')]:
 row=[r for r in installed.values() if r['Path'].endswith(suffix)];assert len(row)==1 and inputs[key+'Sha256']==row[0]['Sha256']
assert inputs['PinnedErlangSource']==next(r for r in source['TrackedFiles'] if r['Path']=='erts/preloaded/src/erlang.erl') and inputs['PinnedBifTable']==next(r for r in source['TrackedFiles'] if r['Path']=='erts/emulator/beam/bif.tab')
groups=[];rawhashes=[];values=profiles=0
for fixture,group in zip(p['Fixtures'],c['Groups']):
 assert group['Id']==fixture['Id'] and group['Failure']=='AssertionError()' and group['Acquired'] is False and not group['VmSkipped']
 q=RAW/'capture'/fixture['Id'];compile=J(q/'compile.stage.json')['Arguments'];observe=J(q/'observe.stage.json')['Arguments'];ebin=Path(compile[compile.index('-o')+1]).resolve()
 expected_sources=[fixture['Path']]+fixture['Companions'];assert len([a for a in compile if a.endswith('.erl')])==len(expected_sources)
 for name in expected_sources:assert any(a.endswith('/tools/Erlang.ProplistFamilyObservations/'+name) for a in compile)
 assert '+deterministic' in compile and '-Werror' in compile and observe[observe.index('+S')+1]=='2:2'
 for name in ['compile','observe']:
  receipt=next(s for s in c['OwnedStages'] if s['Group']==fixture['Id'] and s['Name']==name);stages.append(helpers.normal_original(q,name,receipt))
 md=d.decode_exact((q/'metadata.etf').read_bytes());fields=m.fields(md);runtime=Path(m.text(fields['root'])).resolve();pid=J(q/'observe.process.json')['OwnedPid']
 assert runtime==Path(b['RuntimeRoot']).resolve()
 proof=m.validate(md,corrected,runtime,fixture,ebin,pid,p['Mfas'])
 original=load(TOOL/'preloaded_provenance.py','original_policy_'+fixture['Id'])
 try:original.validate(md,inputs,runtime,fixture,ebin,pid,p['Mfas'])
 except AssertionError as error:
  tb=error.__traceback__
  while tb.tb_next:tb=tb.tb_next
  assert tb.tb_lineno==73
 else:raise AssertionError('Original defect did not reproduce')
 compiled=[]
 for name in [fixture['Module']]+[Path(n).stem for n in fixture['Companions']]:
  f=q/'ebin'/(name+'.beam');compiled.append(dict(Module=name,Bytes=f.stat().st_size,Sha256=H(f)))
 rows=d.decode_exact((q/'returned.etf').read_bytes());assert rows[0]=='list' and len(rows[1])==len(fixture['Names'])
 for name,row in zip(fixture['Names'],rows[1]):
  assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name);m.validate_outcome(row[1][1],False);values+=1
 stacks=d.decode_exact((q/'stack-observations.etf').read_bytes());assert stacks[0]=='list' and len(stacks[1])==5
 for depth,row in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],stacks[1]):
  assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==depth
  nested=row[1][1];assert nested[0]=='list' and len(nested[1])==len(fixture['Names'])
  for name,row in zip(fixture['Names'],nested[1]):
   assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name);m.validate_outcome(row[1][1],True);profiles+=1
 groups.append(dict(Id=fixture['Id'],Metadata=proof,CompiledFiles=compiled,ProducerAcquired=False,RetrospectiveRawAdmission=True))
 for n in ['returned.etf','stack-observations.etf','metadata.etf']:rawhashes.append(dict(Group=fixture['Id'],Path=n,Sha256=H(q/n)))
gate=J(RAW/'preflight/completion.json');assert gate['Status']=='PASS_LINUX_NAMED_FIXTURE_GATE';preflight=[]
for s in gate['Stages']:
 assert s['Failure'] is None and H(RAW/'preflight'/(s['Name']+'.process.json'))==s['ReceiptSha256'];preflight.append(helpers.normal_preflight(RAW/'preflight'/(s['Name']+'.process.json')))
assert len(preflight)==3 and [r['Name'] for r in gate['Stages']]==['parse-build','parse-sources','collector-admit']
parse=J(RAW/'preflight/parse-receipt.json');assert parse['Status']=='PASS_FIXTURE_PARSE_GATE' and len(parse['Fixtures'])==len({r['Path'] for r in parse['Fixtures']})==10 and all(r.get('Passed') is True and 'Error' not in r for r in parse['Fixtures'])
for row in parse['Fixtures']:assert [s['Sha256'] for s in p['FixtureSources'] if row['Path'].endswith('/tools/Erlang.ProplistFamilyObservations/'+s['Path'])]==[row['Sha256']]
assert H(RAW/'preflight/parse-receipt.json')==gate['ParseGateSha256']==inputs['ParseGateSha256']
assert len(stages)==19 and len({s['Pid'] for s in stages})==19 and values==1597 and profiles==7985
result=dict(Status='PASS_RETROSPECTIVE_SOURCE_DERIVED_RAW_PROVENANCE',ProducerStatusPreserved=c['Status'],ProducerFailedGroupsPreserved=8,Head=head,Run=39106192411,OriginalManifestSha256=H(TOOL/'source-inputs.corrected.json'),ArtifactZipSha256=H(archive),RawFilesVerified=len(rawhashes),ZipFilesVerified=len(files),PinnedBlobs=12008,ProducerInputs=28,OriginalProcesses=stages,PreflightProcesses=preflight,Values=values,Profiles=profiles,Groups=groups,RawHashes=rawhashes,NativeExecuted=False,DownloadedCodeExecuted=False,CompatibilityAccepted=False,OldFun20DesignGap='Original input module/codekey reused across OldUnique groups; no whole-coverage claim.')
(OUT/'acceptance.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'Status':result['Status'],'Sha256':H(OUT/'acceptance.json'),'Values':values,'Profiles':profiles}))

"""Normalize independently authored fixture descriptors into the existing collector profile."""
from pathlib import Path
import hashlib,json,shutil,subprocess

ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).resolve().parent;TOOL=OUT/'tool';ORIGINAL=TOOL/'original';ORIGINAL.mkdir(exist_ok=True)
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5';BASE_HEAD='d5bc60c53a966e1eaf1c64f7e40bb15f9244fb14'
POLICY='compiled-monitored-worker-values-and-stacks-v1'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(p):return json.loads(p.read_bytes())
def write(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
numeric=read(ROOT/'artifacts/erlang370-numeric-proposal1/fixture-manifest.json')
support=ROOT/'artifacts/erlang370-numeric-proposal1/original/erlang370_pure_support.erl'
assert sha(support)=='D73BE9C502B86DF44C37C2E4F20F333BD47CABD1A5D66479E9BDC0AAD412CF29'
shutil.copyfile(support,ORIGINAL/support.name)
worker=(ROOT/'artifacts/binary369-closure-original-proposal1/tool/original/proplists_observation_worker.erl').read_text()
worker=worker.replace('proplists_observation_worker','erlang370_observation_worker').replace('worker_timeout() -> 30000.','worker_timeout() -> 540000.')
(ORIGINAL/'erlang370_observation_worker.erl').write_text(worker,encoding='utf-8')
packets=[dict(Id=f['Id'],Source=ROOT/f['Source'],Module=f['Module'],Names=f['Names'],Cases=f['Cases'],Mfas=f['Mfas'],ExpectedSha256=f['SourceSha256']) for f in numeric['Fixtures']]
for folder,filename,gid in [('erlang370-inventory-audit1','cases.json','size_type'),('erlang-tuple370-original-proposal1','cases.json','tuple')]:
    packet=read(ROOT/'artifacts'/folder/filename);source=ROOT/'artifacts'/folder/'original'/(packet['Module']+'.erl')
    packets.append(dict(Id=gid,Source=source,Module=packet['Module'],Names=[r['Name'] for r in packet['Cases']],Cases=packet['Cases'],Mfas=packet['Mfas'],ExpectedSha256=packet.get('FixtureSha')))
addendum=read(ROOT/'artifacts/erlang370-termorder-addendum1/fixture-manifest.json')
packets.append(dict(Id='local_fun_order',Source=ROOT/addendum['Source'],Module=addendum['Module'],Names=addendum['Names'],Cases=[dict(c,Mfa='erlang:'+c['Function']+'/2') for c in addendum['Cases']],Mfas=[],ExpectedSha256=addendum['SourceSha256']))

def mfa(value):
    return value if isinstance(value,str) else value['Module']+':'+value['Function']+'/'+str(value['Arity'])
fixtures=[];owners=[]
for packet in packets:
    source=packet['Source'];assert source.is_file()
    if packet['ExpectedSha256']:assert sha(source)==packet['ExpectedSha256']
    assert '?MODULE' not in source.read_text(), 'Use parseable explicit module name'
    target=ORIGINAL/source.name;shutil.copyfile(source,target)
    rows=[]
    for case in packet['Cases']:
        mf=case.get('Mfas') or ([case['Mfa']] if 'Mfa' in case else ['erlang:'+case['Function']+'/'+str(len(case['Arguments']))])
        rows.append(dict(Name=case['Name'],Mfas=[mfa(x) for x in mf],Scope=case.get('Scope',case.get('Policy','Owner-defined source observation; all returned data and full errors retained.')),ExpectedSeededOutcomes=False))
    assert packet['Names']==[r['Name'] for r in rows]
    fixtures.append(dict(Id=packet['Id'],Location='tool',Path='original/'+target.name,Module=packet['Module'],Run='run',NamesEntry='cases',Names=packet['Names'],StackModule=packet['Module'],StackEntry='run_stacks',WorkerModule='erlang370_observation_worker',WorkerEntry='run',ValueAcquisition=POLICY,ObserveTimeoutSeconds=600,WorkerTimeoutMilliseconds=540000,StackShape='depth_cases',OutcomeSchema='pure-full-values-and-stacks-v1',Companions=['original/erlang370_pure_support.erl','original/erlang370_observation_worker.erl'],Cases=rows))
    owners.append(dict(Source=source.relative_to(ROOT).as_posix(),Sha256=sha(source),CaseCount=len(rows)))
catalog=ROOT/'docs/supported-mfas.json';target=sorted(mfa(x) for x in read(catalog) if x['Module']=='erlang');assert len(target)==len(set(target))==351
selected=sorted({m for f in fixtures for c in f['Cases'] for m in c['Mfas']});assert len(selected)==57 and set(selected)<=set(target)
profile=dict(Family='erlang-registered351-first-pure57',PinnedOtpCommit=PIN,SubjectModule='erlang',MetadataPolicy='erlang-preloaded-v1',ValueAcquisition=POLICY,TargetMfaCount=351,TargetMfas=target,Mfas=selected,Fixtures=fixtures,FixtureSources=[dict(Location='tool',Path=p.relative_to(TOOL).as_posix(),Sha256=sha(p)) for p in sorted(ORIGINAL.glob('*.erl'))],ExpectedUniqueSourceCount=10,ExpectedSeededOutcomes=False,Scope='First pure57 batch, not completion of351. Remaining294 exports stay outside this batch.',ValueShape='{ok,Value}|{error,Class,Reason}',StackShape='{ok,Value}|{error,Class,Reason,WholeStack}',ExpectedValues=sum(len(f['Names']) for f in fixtures),ExpectedProfiles=sum(5*len(f['Names']) for f in fixtures))
assert profile['ExpectedValues']==1597 and len(fixtures)==8 and len(profile['FixtureSources'])==10
write(TOOL/'profile.json',profile)
write(OUT/'inventory.json',dict(RegisteredSnapshotPath='docs/supported-mfas.json',RegisteredSnapshotSha256=sha(catalog),RegisteredErlangMfas=351,SelectedMfas=selected,SelectedCount=57,NotInBatch=sorted(set(target)-set(selected)),RemainingCount=294,Values=1597,Profiles=7985,WholeObservations=9582,UniqueSources=10,OwnerFixtures=owners,ExpectedSeededOutcomes=False,OriginalExecuted=False,CompilerParseStatus='PENDING_ROOT'))
authority=[]
for name in ['erts/preloaded/src/erlang.erl','erts/emulator/beam/bif.tab','erts/emulator/beam/erl_bif_info.c','lib/kernel/src/code.erl','erts/emulator/beam/erl_arith.c','erts/emulator/beam/erl_bif_guard.c']:
    raw=subprocess.check_output(['git','-C',str(ROOT.parent/'otp'),'show',PIN+':'+name]);path=OUT/'authority'/Path(name).name;path.parent.mkdir(exist_ok=True);path.write_bytes(raw)
    authority.append(dict(Path=name,Bytes=len(raw),Sha256=sha(path)))
write(TOOL/'source-inputs.json',dict(Status='ORIGINAL_ONLY_ERLANG_PRELOADED_SOURCE',PinnedOtpCommit=PIN,MetadataPolicy=profile['MetadataPolicy'],Authority=authority,RegisteredErlangMfas=351,SelectedMfas=57,OriginalExecuted=False,ManagedImplementationExpectedOutcomesUsed=False))
base=read(ROOT/'artifacts/binary369-closure-original-proposal1/tool/source-inputs.corrected.json')
write(TOOL/'source-inputs.corrected.json',dict(Status='PRIVATE_ERLANG370_ORIGINAL_FIRST_PURE_BATCH',PinnedOtpCommit=PIN,ExistingBuildManifestSha256=base['ExistingBuildManifestSha256'],RepositoryInputs=base['RepositoryInputs'],ToolInputs=[dict(Path=p.relative_to(TOOL).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(TOOL.rglob('*')) if p.is_file() and p.name!='source-inputs.corrected.json'],ExpectedSubjectModule='erlang',MetadataPolicy=profile['MetadataPolicy'],WorkerObservationSchema='compiled-monitored-worker-values-and-stacks-v2-540s',OriginalExecutionPending=True,ImplementationPublicationAuthorized=False))
payload=[]
for path in sorted(TOOL.rglob('*')):
    if not path.is_file():continue
    destination='tools/Erlang.ProplistFamilyObservations/'+path.relative_to(TOOL).as_posix()
    prior=subprocess.run(['git','-C',str(ROOT),'show',BASE_HEAD+':'+destination],stdout=subprocess.PIPE,stderr=subprocess.PIPE)
    payload.append(dict(Source=path.relative_to(ROOT).as_posix(),Destination=destination,BaseSha256=hashlib.sha256(prior.stdout).hexdigest().upper() if prior.returncode==0 else None,Bytes=path.stat().st_size,Sha256=sha(path)))
write(OUT/'copy-plan.json',dict(Status='SOURCE_ONLY_ORIGINAL_TEST_INPUTS_NO_IMPLEMENTATION',BaseHead=BASE_HEAD,Files=payload))
print(json.dumps(dict(SelectedMfas=57,Values=1597,Profiles=7985,Sources=10,SourceInputsSha256=sha(TOOL/'source-inputs.corrected.json'),CopyPlanSha256=sha(OUT/'copy-plan.json'))))

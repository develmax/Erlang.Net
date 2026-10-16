"""Root-only promotion/full guard; source author does not invoke Main-writing phases."""
from pathlib import Path
import argparse,hashlib,json,re,subprocess
R=Path.cwd().resolve();H=Path(__file__).resolve().parent;D=R/'artifacts/root-part328-main2';PARENT='4bc4c00b713c1cc0bbf32a1c74add45174bbd10a'
read=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();write=lambda p,v:p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
p=argparse.ArgumentParser();p.add_argument('phase',choices=['promote','full-guard'])
for name in ['freeze','source-peer']:p.add_argument('--'+name,required=True,type=Path);p.add_argument('--'+name+'-sha256',required=True)
p.add_argument('--closure-sha256',required=True);p.add_argument('--case-ledger',required=True,type=Path);p.add_argument('--case-ledger-sha256',required=True);a=p.parse_args()
def exact(path,digest):assert sha(path)==digest.upper(),str(path)
def rows_exact(base,rows):
    for e in rows:
        q=base/e['Path'];assert q.stat().st_size==e['Bytes'] and sha(q)==e['Sha256'],e['Path']
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()==PARENT
fp=a.freeze.resolve();assert fp.is_relative_to(R/'artifacts');exact(fp,a.freeze_sha256);cl=fp.parent/'source-resource-closure.json';exact(cl,a.closure_sha256)
f=read(fp);rows=read(cl)['Files'];owned={x['Path']:x for x in f['Files']}
assert f['ParentHead']==PARENT and f['FinalSourceClosureSha256']==a.closure_sha256.upper()
assert len(rows)==f['SourceClosurePaths'] and len(owned)==f['SourcePaths'] and sum(n.endswith('.cs') for n in owned)==f['CSharpPaths']
peer=a.source_peer.resolve();assert peer.is_relative_to(R/'artifacts');exact(peer,a.source_peer_sha256);pv=read(peer)
assert pv.get('Passed') is True or str(pv.get('Status','')).startswith('PASS')
reviewed_author=a.freeze_sha256.upper() if a.phase=='promote' else f['AuthorFinalFreeze']['Sha256']
assert reviewed_author in json.dumps(pv) and a.closure_sha256.upper() in json.dumps(pv)
if a.phase=='full-guard':assert f['IndependentPeer']['Sha256']==a.source_peer_sha256.upper()
ledgerpath=a.case_ledger.resolve();assert ledgerpath.is_relative_to(R/'artifacts');exact(ledgerpath,a.case_ledger_sha256);ledger=read(ledgerpath);assert ledger['Passed'] is True
base=fp.parent/'candidate' if a.phase=='promote' else R
refs={}
for role in ['ManagedActual','FormattedSourceFreeze']:
    item=ledger[role];q=R/item['Path'];exact(q,item['Sha256']);refs[role]=read(q)
managed=refs['ManagedActual'];assert managed['RecipePassed'] is True and managed['Executed']==managed['FixturePassed']==managed['ExpectedCases'] and managed['FixtureFailed']==managed['Unexecuted']==0
assert managed['Cases'] and all(e['FixturePassed'] is True and e['Exception'] is None for e in managed['Cases'])
for item in ledger['HostActuals']:
    q=R/item['Path'];exact(q,item['Sha256']);actual=read(q);assert actual['Status']=='PASS' and actual['Cases'] and all(e['Status']=='PASS' for e in actual['Cases'])
assert ledger['HostActuals'] and ledger['NewHostRows']
for e in refs['FormattedSourceFreeze']['Files']:
    if e['Path'].endswith('.cs'):assert owned[e['Path']]['Sha256']==e['Sha256'] and owned[e['Path']]['Bytes']==e['Bytes']
new={'oracle/compiled-module/'+e['Case'] for e in managed['Cases']}
assert len(new)==len(managed['Cases'])
program=(base/'tests/Erlang.Tests/Program.cs').read_text(encoding='utf-8-sig')
for item in ledger['NewHostRows']:
    assert program.count(item['ProgramLine'])==1
    text=(base/item['NamesPath']).read_text(encoding='utf-8-sig')
    assert re.findall(r'const string '+re.escape(item['Constant'])+r' = "([^"]+)";',text)==[item['Name']]
    assert item['Name'] not in new;new.add(item['Name'])
expectedfull=6729+len(new)
if a.phase=='full-guard':assert f['ExpectedFullPassed']==expectedfull and f['ActualCaseLedger']=={'Path':ledgerpath.relative_to(R).as_posix(),'Sha256':sha(ledgerpath)}
if a.phase=='promote':
    assert not D.exists()
    failedmain=R/'artifacts/root-part328-main1';mf=failedmain/'source-freeze.json';capturepath=R/'artifacts/part328-main1-failure/capture.json'
    exact(mf,'1C6D9F182962B2AA04831C4187255E43433EFFA9A336A20CCCB11AADA98C7E7F')
    exact(capturepath,'C1252335323D98761C5B548C07F4ED498EECEF60E51087A0438C68D3AA8A0065')
    failedclosure=failedmain/'source-resource-closure.json';exact(failedclosure,'A7859120DBD894D6AAD45FC17A133D83DC88FA3E2993B1C8ED07926CFB912424')
    failed=read(capturepath);assert (failed['Passed'],failed['Failed'],failed['ExitCode'])==(6733,5,1) and failed['CurrentIntegrationAndPackageSuffixExecuted'] is False
    assert failed['SourceFreezeSha256']==sha(mf) and failed['RawSourceStillMain1Exact'] is True
    rows_exact(capturepath.parent,failed['Files']);failedrows=read(failedclosure)['Files'];assert len(failedrows)==2393;rows_exact(R,failedrows)
    currentregistry=R/'docs/supported-mfas.json';assert currentregistry.read_bytes()==(capturepath.parent/'raw-supported-mfas.json').read_bytes()
    savedregistry=failedmain/'registry-before-full.json';assert savedregistry.read_bytes()==(capturepath.parent/'registry-before-full.json').read_bytes()
    assert not subprocess.check_output(['git','--no-optional-locks','diff','--cached','--name-only']).strip()
    expected_dirty={e['Path'] for e in read(mf)['Files'] if not e['Added']};assert len(expected_dirty)==15
    # Preserve strict raw capture equality above; derive only Git's canonical dirty membership.
    registry_object=subprocess.check_output(['git','--no-optional-locks','hash-object','--path=docs/supported-mfas.json','--stdin'],input=currentregistry.read_bytes()).decode().strip()
    published_registry_object=subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD:docs/supported-mfas.json']).decode().strip()
    if registry_object!=published_registry_object:expected_dirty.add('docs/supported-mfas.json')
    dirty=set(subprocess.check_output(['git','--no-optional-locks','diff','--name-only'],text=True).splitlines());assert dirty==expected_dirty,(dirty,expected_dirty)
    tracked=set(subprocess.check_output(['git','--no-optional-locks','ls-files'],text=True).splitlines());assert expected_dirty<=tracked
    parentcl=R/'docs/validation/part-327/final-main/source-resource-closure.json';exact(parentcl,'57275A459C17E8A6F991CE0FAD8C3FDE16F3DF0D6B132CF8A9C53D0EC9491C3F');parent={e['Path']:e for e in read(parentcl)['Files']};assert len(parent)==2379;
    rows_exact(fp.parent/'candidate',rows)
    for e in rows:
        if e['Path'] not in owned:assert e==parent[e['Path']]
    for n,e in owned.items():assert e['Added']==(n not in parent) and e['ParentSha256']==(parent[n]['Sha256'] if n in parent else None)
    registry=R/'docs/supported-mfas.json';v=read(savedregistry);assert len(v)==509 and sum(e['Module']=='erlang' for e in v)==351
    D.mkdir();(D/'registry-before-full.json').write_bytes(savedregistry.read_bytes());(D/'failed-main1-raw-supported-mfas.json').write_bytes(registry.read_bytes());(D/'failed-main1-capture.json').write_bytes(capturepath.read_bytes())
    for n in owned:q=R/n;q.parent.mkdir(parents=True,exist_ok=True);q.write_bytes((fp.parent/'candidate'/n).read_bytes())
    rows_exact(R,rows);(D/'source-resource-closure.json').write_bytes(cl.read_bytes())
    registry.write_bytes(savedregistry.read_bytes())
    f.update(ReplacedFailedMain1={'Path':mf.relative_to(R).as_posix(),'Sha256':sha(mf)},FailedMain1Capture={'Path':capturepath.relative_to(R).as_posix(),'Sha256':sha(capturepath)},FailedMain1FullResult={'Passed':6733,'Failed':5,'IntegrationSuffixExecuted':False},FailedMain1UpgradeGitRegistry={'WorkingCanonicalObject':registry_object,'PublishedObject':published_registry_object,'RegistryTrackedDirty':registry_object!=published_registry_object,'RawRegistryExactCapture':True,'ExpectedTrackedDirtyPaths':sorted(expected_dirty)},Status='ROOT_PROMOTED328_PENDING_FRESH_FULL_NATIVE_READINESS',CandidateDirectory=str(R),ExpectedFullPassed=expectedfull,AuthorFinalFreeze={'Path':fp.relative_to(R).as_posix(),'Sha256':sha(fp)},IndependentPeer={'Path':peer.relative_to(R).as_posix(),'Sha256':sha(peer)},IndependentPeerRole='FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW',RootPromotionScriptSha256=sha(Path(__file__)),ActualCaseLedger={'Path':ledgerpath.relative_to(R).as_posix(),'Sha256':sha(ledgerpath)})
    write(D/'source-freeze.json',f);print(sha(D/'source-freeze.json'));raise SystemExit(0)
assert fp==D/'source-freeze.json' and not(D/'full-guard.json').exists();rows_exact(R,rows)
exitproof=read(D/'full-exit.json');assert exitproof['ExitCode']==0 and exitproof['Context']=='Outside sandbox'
current=read(R/'artifacts/tests.json');old=read(R/'docs/validation/part-327/final-main/tests.json');assert old['Passed']==6729 and current['Passed']==len(current['Tests'])==expectedfull and current['Failed']==0 and all(e['Status']=='Passed' for e in current['Tests'])
assert {e['Name'] for e in current['Tests']}-{e['Name'] for e in old['Tests']}==new
assert [e['Name'] for e in current['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
assert read(R/'artifacts/integration-results.json')==read(R/'docs/validation/part-327/final-main/integration-results.json') and len(read(R/'artifacts/integration-results.json'))==11
assert 'Source layout: 0 files need formatting' in (D/'full-validation.log').read_text(encoding='utf-8-sig')
for plan in f['Plans']:
    q=R/plan['Path'];exact(q,plan['Sha256']);assert b'\r' not in q.read_bytes() and len(read(q)['Files'])==plan['Inputs']
    for e in read(q)['Files']:
        raw=(R/e['Path']).read_bytes();assert e['HashMode'] in ['TextLF','BinaryExact'];raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and hashlib.sha256(raw).hexdigest().upper()==e['Sha256']
    wf='.github/workflows/nif-pending-native.yml' if 'PendingVarargs' in plan['Path'] else '.github/workflows/nif-reallocation-native.yml';key='REVIEWED_SOURCE_PLAN_SHA256' if 'PendingVarargs' in plan['Path'] else 'REVIEWED_REALLOCATION_PLAN_SHA256';assert re.findall(r'^\s*'+key+r': ([A-F0-9]{64})$',(R/wf).read_text(),re.M)==[plan['Sha256']]
raw=(R/'docs/supported-mfas.json').read_bytes();entries=json.loads(raw);prior=read(D/'registry-before-full.json');key=lambda e:(e['Module'],e['Function'],e['Arity']);before={key(e):e for e in prior}
custom={('erlang',n,z) for n,z in [('process_info',1),('process_info',2),('load_nif',2),('alloc_info',1),('alloc_sizes',1),('memory',0),('memory',1),('group_leader',2),('send',2),('send',3),('send_nosuspend',2),('send_nosuspend',3),('!',2),('process_flag',2),('dist_get_stat',1),('link',1),('link',2),('unlink',1),('exit',2),('exit',3),('exit_signal',2),('exit_signal',3)]}|{('erlang',n,z) for n in ['spawn_opt','spawn_request'] for z in [2,3,4,5]};assert len(custom)==30
assert len(entries)==len(prior)==509 and [key(e) for e in entries]==[key(e) for e in prior]
for e in entries:
    k=key(e);assert {n:v for n,v in e.items() if n!='Limits'}=={n:v for n,v in before[k].items() if n!='Limits'}
    if e.get('Limits')!=before[k].get('Limits'):assert k in custom;e['Limits']=before[k]['Limits']
    assert e==before[k]
suffix=' Restored local source fun invocation can select the executing process error_handler undefined_lambda/3 with the original wrapper and arguments after source retirement and no CURRENT code; active old bodies, pending purge control pumping, source callback leases and relevant factory publication validation are within the reviewed supported slice. Compatible source relink requires a pre-registered factory. Fresh CURRENT without a factory resolver, direct foreign captured-image transfer, default handler autoload, arbitrary raw BEAM and whole erlang parity remain partial. Current328 original comparison is a separate gate.'
target=next(e for e in entries if key(e)==('erlang','process_flag',2));target['Limits']=target['Limits'].replace('undefined_lambda/3, ', '')+suffix

for e in entries:
    if key(e)!=('erlang','process_flag',2):assert e==before[key(e)]
(D/'raw-supported-mfas.json').write_bytes(raw);write(R/'docs/supported-mfas.json',entries)
write(D/'full-guard.json',{'Status':'PASS_ACTUAL_MAIN328_'+str(expectedfull)+'_FULL_AND_ELEVEN_INTEGRATIONS','Passed':expectedfull,'Failed':0,'IntegrationResults':11,'All6729PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'SourceFreezeSha256':sha(fp),'SourceClosureSha256':sha(cl),'SourcePaths':len(owned),'CSharpPaths':f['CSharpPaths'],'SourceResourceClosureFiles':len(rows),'Registry':509,'PreviousRegistry':509,'PublicMfaDelta':0,'ErlangMfas':351,'ThirtyPriorCustomLimitsRestoredBeforeAppend':True,'OnlyProcessFlag2ScopeAppended':True,'TwentyNineOtherCustomLimitsExact':True,'UnrelatedMfaRowsExactParent':True,'RawRegistrySha256':hashlib.sha256(raw).hexdigest().upper(),'Plans':f['Plans'],'FullExecutionContext':'Outside sandbox','OriginalExecutedByFullPipeline':False,'NativeExecutedByFullPipeline':False,'ReadinessStillRequiredSeparately':True,'FailedMain1PreservedNotRelabeled':True,'GuardSourceSha256':sha(Path(__file__))})
print(sha(D/'full-guard.json'))

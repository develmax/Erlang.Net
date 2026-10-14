from pathlib import Path
import hashlib,json,struct,subprocess,zipfile
art=Path('artifacts'); root=art/'part-274-pinned-local-node-admission/local-node-admission'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
head='b87e7fe1539dc9850a0c24d1b426bc6851f19e5c'; pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
def decode(data):
    pos=0
    def take(n):
        nonlocal pos
        assert n>=0 and pos+n<=len(data)
        b=data[pos:pos+n]; pos+=n; return b
    def number(n):return int.from_bytes(take(n),'big')
    assert number(1)==131
    def term():
        tag=number(1)
        if tag in (97,98):return {'Tag':tag,'Type':'Integer','Value':number(1) if tag==97 else int.from_bytes(take(4),'big',signed=True)}
        if tag in (100,115,118,119):
            n=number(2 if tag in (100,118) else 1)
            return {'Tag':tag,'Type':'Atom','Value':take(n).decode('utf-8' if tag in (118,119) else 'latin1')}
        if tag in (104,105):return {'Tag':tag,'Type':'Tuple','Values':[term() for _ in range(number(1 if tag==104 else 4))]}
        if tag==106:return {'Tag':tag,'Type':'Nil'}
        if tag==107:
            b=take(number(2)); return {'Tag':tag,'Type':'String','BytesHex':b.hex(),'Values':list(b)}
        if tag==108:
            values=[term() for _ in range(number(4))]; return {'Tag':tag,'Type':'List','Values':values,'Tail':term()}
        if tag==109:return {'Tag':tag,'Type':'Binary','BytesHex':take(number(4)).hex()}
        if tag==116:return {'Tag':tag,'Type':'Map','Entries':[[term(),term()] for _ in range(number(4))]}
        if tag in (110,111):
            n=number(1 if tag==110 else 4); sign=number(1); assert sign in (0,1)
            return {'Tag':tag,'Type':'Integer','Value':int.from_bytes(take(n),'little')*(-1 if sign else 1)}
        if tag in (88,103):return {'Tag':tag,'Type':'Pid','Node':term(),'Id':number(4),'Serial':number(4),'Creation':number(4 if tag==88 else 1)}
        if tag in (90,114):
            n=number(2); node=term(); creation=number(4 if tag==90 else 1)
            return {'Tag':tag,'Type':'Reference','Node':node,'Creation':creation,'Ids':[number(4) for _ in range(n)]}
        raise ValueError(('Unexpected ETF tag',tag,pos))
    value=term(); assert pos==len(data); return value
# Projections are only for explicit term comparisons; wire tags/full typed values and raw bytes remain saved.
def project(v):
    if v['Type'] in ('Atom','Integer'):return v['Value']
    if v['Type']=='Tuple':return tuple(project(x) for x in v['Values'])
    if v['Type']=='Map':return [('MapEntry',project(k),project(val)) for k,val in v['Entries']]
    if v['Type']=='Nil':return []
    if v['Type']=='String':return v['Values']
    if v['Type']=='List':assert v['Tail']['Type']=='Nil'; return [project(x) for x in v['Values']]
    return v
metadata=j(art/'part-274-pinned-local-node-admission-metadata.json'); provenance=j(art/'part-274-admission-native-latest-provenance.json')
assert metadata['Run']['head_sha']==provenance['Run']['head_sha']==head
assert metadata['Run']['id']==provenance['Run']['id']==38636859124 and provenance['Job']['id']==116102113805
assert provenance['Job']['labels']==['ubuntu-latest'] and provenance['Job']['conclusion']=='success'
assert all(x['status']=='completed' and x['conclusion']=='success' for x in provenance['Job']['steps'])
archive=art/'part-274-pinned-local-node-admission.zip'
assert sha(archive.read_bytes())==metadata['ZipSHA256']==metadata['Artifact']['digest'][7:].upper()
with zipfile.ZipFile(archive) as z:
    names=[n for n in z.namelist() if not n.endswith('/')]
    for name in names:assert z.read(name)==(art/'part-274-pinned-local-node-admission'/name).read_bytes()
report=j(root/'local-node-admission.json'); plan=j(root/'source-plan.json'); evidence=j(root/'source-build-evidence.json')
assert report['Pin']==plan['Pin']==evidence['SourceCommit']==pin and plan['SourceTag']==evidence['SourceTag']=='OTP-29.1.1'
assert evidence['HarnessCommit']==head and evidence['SourceBuildCompleted'] and evidence['RunId']=='38636859124'
assert sha((root/'source-build.log').read_bytes())==evidence['SourceBuildLogSha256']
assert project(decode((root/'runtime.etf').read_bytes()))==(list(b'29'),list(b'17.1'),'jit',list(evidence['InstalledOtpRoot'].encode('latin1')))
planbytes=subprocess.check_output(['git','show',head+':tools/Erlang.LocalNodeAdmissionContrasts/source-plan.json']); assert planbytes==(root/'source-plan.json').read_bytes()
assert sha(planbytes)=='8E67B33365842F0D29156AD2B44F227481CC68A800D38F46E131E9F8B5F5198E'
for source,value in [(item['File'],item['Sha256']) for item in plan['Sources']]:
    b=subprocess.check_output(['git','show',head+':tools/Erlang.LocalNodeAdmissionContrasts/'+source]); assert b==(root/source).read_bytes() and sha(b)==value==report['Sources'][source]
for item in report['Files']:
    b=(root/item['Path']).read_bytes(); assert sha(b)==item['Sha256'] and len(b)==item['Bytes']
assert report['Planned']==report['Captured']==2 and not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim'] and not report['NativeNetworkingUsed']
facts={}
for row in report['Rows']:
    scenario=row['Scenario']; assert row['ExitCode']==0 and row['RawComplete'] and not row['TimedOut'] and row['Stage']=='complete' and row['Captured']
    folder=root/scenario; assert (folder/'stage').read_bytes()==b'complete'
    for name in plan['RequiredArtifacts'][scenario]:assert (folder/name).is_file()
    assert not (folder/'outer-exception.etf').exists()
    facts[scenario]={p.name:decode(p.read_bytes()) for p in sorted(folder.glob('*.etf'))}
get=lambda case,name:project(facts[case][name+'.etf'])
assert get('admission','before')==('nonode@nohost',0,'false')
assert get('admission','overflow-creation')==('result','true')
assert get('admission','active')==('admission_probe@localhost',5,'true','admission_probe@localhost')
assert get('admission','inactive')==('nonode@nohost',0,'false')
assert get('admission','retained-external')==get('admission','retained-after') and get('admission','retained-external')['Creation']==4
for name in ['missing-kernel','invalid-name','invalid-type','forbidden-nonode','negative-creation','admitted','already-active']:
    v=get('admission',name); assert v[:3]==('exception','error','badarg'),name
assert get('admission','nodeup-first')==get('admission','nodeup-second')=='timeout'
assert get('admission','nodedown-first')==get('admission','nodedown-second')==('nodedown','admission_probe@localhost')
assert get('admission','kernel-down')=='killed'
assert get('compiled','missing')=='badarg' and get('compiled','inactive')==('false','nonode@nohost',0)
assert get('compiled','watch')==('result',0) and get('compiled','started')==('result','true')
assert get('compiled','active')==('true','epoch@host',4) and get('compiled','already-active')=='badarg'
assert get('compiled','nodeup')==('nodeup','epoch@host',[]) and get('compiled','nodedown')==('nodedown','epoch@host',[])
assert get('compiled','kernel-down')=='killed' and get('compiled','stopped')==('false','nonode@nohost',0)
assert get('compiled','cleanup')==('false','undefined','nonode@nohost','false')
log=art/'part-274-admission-native-job.log'; assert sha(log.read_bytes())==j(Path(str(log)+'.digest.json'))['SHA256']
(art/'part274-admission-native-typed-facts.json').write_text(json.dumps({'Status':'Faithful raw ETF typed decode','WireTagsPreserved':True,'Facts':facts},indent=2)+'\n')
result={'Status':'PASS_NATIVE_CAPTURE_PROVENANCE_AND_ACTUAL_FACTS','Head':head,'Run':38636859124,'Job':116102113805,'Artifact':metadata['Artifact']['id'],'ZipFiles':len(names),'ReportedFiles':len(report['Files']),'PlanSha256':sha(planbytes),'Runtime':['29','17.1','jit'],'OriginalTwoScenariosComplete':True,'OverflowActualResult':['result','true'],'OverflowSelectedCreation':5,'LegacyNodeUpActual':'timeout twice','LegacyNodeDownActual':'two events','CompiledSelectedCreation':4,'CompiledMapWatchOldCount':0,'CompiledMapEventsActual':True,'SdkOverflowMismatchIdentified':True,'SdkFullCompatibilityClaim':False,'KnownCauseScope':'Pinned LP64 term_to_Uint32 SMALL cast plus exact native active5; source-order monitors installed after overflow activation. No invented SDK or VM result.','ThirtyOneRequiredArtifactsPresent':sum(len(v) for v in plan['RequiredArtifacts'].values()),'RawJobSha256':sha(log.read_bytes()),'OutcomeNormalization':False}
(art/'part274-admission-native-independent-guard.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result))

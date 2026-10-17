"""Passive actual original356 eleven-case native proof and selected managed comparison. No downloaded code execution."""
from pathlib import Path,PurePosixPath
import base64,hashlib,importlib.util,json,re,struct,subprocess
import common356 as m

c,e,run,job,artifact,log=m.transport('original')
tool='tools/Erlang.NifEntryOriginalObservations/'
pub=m.blobs([tool+'manifest.json',tool+'cases.json','docs/validation/part-356/native-completion.json','docs/validation/part-356/native-raw.json','docs/validation/part-356/full-exit.json','docs/validation/part-356/captured-outputs.json'])
manifest=json.loads(pub[tool+'manifest.json']);assert m.D(pub[tool+'manifest.json'])==m.MANIFEST
localmanifest=(m.R/tool/'manifest.json').read_bytes();assert m.D(localmanifest)=='FBB2A50336A968F46CA622349CE6A75A0836226BB34DB66E026A7C9DB812827C' and localmanifest.replace(b'\r\n',b'\n')==pub[tool+'manifest.json']
source=m.blobs([tool+x['Path'] for x in manifest['Files']]+[x['Path'] for x in manifest['Dependencies']]+[manifest['Workflow']['Path']])
for row in manifest['Files']:
    b=source[tool+row['Path']];assert len(b)==row['Bytes'] and m.D(b)==row['Sha256']
for row in manifest['Dependencies']+[manifest['Workflow']]:assert m.D(source[row['Path']])==row['Sha256']
decoderpath=m.R/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py'
assert m.H(decoderpath)==m.D(source['tools/Erlang.GuestConnectedSendObservations/passive_etf.py'])=='A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
spec=importlib.util.spec_from_file_location('trusted_local_passive356',decoderpath);decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
def etf(p):
    raw=p if isinstance(p,bytes) else Path(p).read_bytes();v,n=decoder.decode_one(raw);assert n==len(raw);return v
A=lambda x:('atom',x)
I=lambda x:('integer',x)
T=lambda *x:('tuple',tuple(x))
L=lambda *x:('list',tuple(x))
def text(v):
    assert v[0]=='list' and all(x[0]=='integer' for x in v[1]);return ''.join(chr(x[1]) for x in v[1])

build=e/'runtime-build/build-receipt.json';b=m.J(build)
assert b['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and b['Commit']==m.PIN and b['SubjectHead']==m.HEAD and b['SourceManifestSha256']==manifest['RuntimeBuildManifestSha256']=='4C9A79CD6E8BC18F68316D42B2F30DB171E67A242CA81CA84BB8A022E8BA5BFB'
remote=PurePosixPath(b['Stages'][0]['ReceiptPath']).parent.parent
assert remote.name=='nif-entry-original' and remote.parent.name=='artifacts'
def mapped(name):
    rel=PurePosixPath(name).relative_to(remote);assert '..' not in rel.parts
    q=e/rel.as_posix();assert q.resolve().is_relative_to(e.resolve());return q
assert PurePosixPath(b['RuntimeRoot'])==PurePosixPath(b['Prefix'])/'lib/erlang' and PurePosixPath(b['Prefix'])==remote.parent/'nif-entry-otp'
before=e/'runtime-build/source-before.json';after=e/'runtime-build/source-after.json'
assert m.H(before)==b['SourceBeforeSha256']==m.H(after)==b['SourceAfterSha256'] and before.read_bytes()==after.read_bytes()
inventory=m.J(before);assert inventory['Commit']==m.PIN and len(inventory['TrackedFiles'])==len({x['Path'] for x in inventory['TrackedFiles']})
# Independently hash every pinned Git source/symlink blob; do not execute the downloaded source inventory.
gitroot=m.R.parent/'otp'
tree=subprocess.check_output(['git','-C',str(gitroot),'ls-tree','-r','-z',m.PIN],timeout=30)
treeitems={}
for item in tree.split(b'\0'):
    if not item:continue
    meta,name=item.split(b'\t',1);mode,kind,oid=meta.split();assert kind==b'blob';treeitems[name.decode()]=(mode.decode(),oid.decode())
assert set(treeitems)=={x['Path'] for x in inventory['TrackedFiles']}
commands=Path(__file__).parent/'pinned-source-batch-input.txt';assert not commands.exists()
with commands.open('xb') as f:
    for row in inventory['TrackedFiles']:
        assert treeitems[row['Path']][0]==row['Mode'];f.write((treeitems[row['Path']][1]+'\n').encode())
with commands.open('rb') as stdin:
    proc=subprocess.Popen(['git','-C',str(gitroot),'cat-file','--batch'],stdin=stdin,stdout=subprocess.PIPE,stderr=subprocess.DEVNULL)
    for row in inventory['TrackedFiles']:
        header=proc.stdout.readline().split();assert len(header)==3 and header[1]==b'blob' and int(header[2])==row['Bytes'],row['Path']
        remaining=row['Bytes'];digest=hashlib.sha256()
        while remaining:
            raw=proc.stdout.read(min(remaining,1024*1024));assert raw;remaining-=len(raw);digest.update(raw)
        assert proc.stdout.read(1)==b'\n' and digest.hexdigest().upper()==row['Sha256'],row['Path']
    assert not proc.stdout.read(1) and proc.wait(timeout=30)==0
stages=[]
def stage(row):
    p=mapped(row['ReceiptPath']);assert m.H(p)==row['ReceiptSha256'];receipt=m.normal(p)
    stem=str(p).removesuffix('.process.json')
    for suffix,key in [('.stdout','StdoutSha256'),('.stderr','StderrSha256')]:assert m.H(stem+suffix)==row[key]
    stages.append(dict(Path=p.relative_to(e).as_posix(),Pid=receipt['OwnedPid'],Sha256=m.H(p)))
    return receipt
assert [x['Name'] for x in b['Stages']]==['configure','make','install']
for row in b['Stages']:stage(row)
assert len(b['InstalledFiles'])==len({x['Path'] for x in b['InstalledFiles']})>=11 and {'bin/erl','bin/erlc'}<={x['Path'] for x in b['InstalledFiles']}
assert any(x['Path'].endswith('/bin/beam.smp') for x in b['InstalledFiles'])

native=e/'native-build/native-build-receipt.json';n=m.J(native)
assert n['Status']=='PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD' and n['SubjectHead']==m.HEAD and n['PinnedOtpCommit']==m.PIN and n['SourceManifestSha256']==m.MANIFEST and n['RuntimeBuildReceiptSha256']==m.H(build)
assert not n['NifInitializationExecuted'] and not n['OriginalObservationsExecuted']
for row in n['HeaderInputs']:
    p=mapped(row['Path']);assert m.H(p)==row['Sha256'] and p.stat().st_size==row['Bytes']
for row in manifest['Headers']:
    q=e/'native-build/headers'/row['Name'];assert m.H(q)==row['Sha256'];raw=m.blobs([row['GitPath']],m.PIN,gitroot)[row['GitPath']];assert q.read_bytes()==raw
sizes=(e/'native-build/headers/erl_int_sizes_config.h').read_text()
for name,size in [('CHAR',1),('SHORT',2),('INT',4),('LONG',8),('LONG_LONG',8),('VOID_P',8)]:assert re.findall(r'^#define SIZEOF_'+name+r' (\d+)$',sizes,re.M)==[str(size)]
assert m.H(e/'native-build/compiler-version.stdout')==n['Compiler']['VersionStdoutSha256']
assert len(n['OwnedStages'])==33
for row in n['OwnedStages']:stage(row)
def elf(raw):
    assert raw[:6]==b'\x7fELF\x02\x01' and struct.unpack_from('<H',raw,16)[0]==3 and struct.unpack_from('<H',raw,18)[0]==62
    start=struct.unpack_from('<Q',raw,40)[0];entry,count=struct.unpack_from('<HH',raw,58);assert entry==64 and start+count*entry<=len(raw)
    sections=[struct.unpack_from('<IIQQQQIIQQ',raw,start+i*entry) for i in range(count)]
    symbols=[s for s in sections if s[1]==11];assert len(symbols)==1;s=symbols[0];strings=sections[s[6]];table=raw[strings[4]:strings[4]+strings[5]];defined=set();undefined=set();assert s[9]==24 and s[5]%24==0
    for offset in range(s[4],s[4]+s[5],24):
        name,info,other,index,value,size=struct.unpack_from('<IBBHQQ',raw,offset)
        if not name or info>>4 not in (1,2):continue
        end=table.index(0,name);name=table[name:end].decode()
        (undefined if index==0 else defined).add(name)
    return defined,undefined
images={x['Leaf']:x for x in n['Images']};assert len(images)==len(n['Images'])==8
for row in n['Images']:
    p=mapped(row['Path']);raw=p.read_bytes();assert len(raw)==row['Bytes'] and m.D(raw)==row['Sha256'];defined,undefined=elf(raw)
    assert defined==set(row['DefinedSymbols'])=={'nif_init'} and undefined==set(row['UndefinedSymbols']) and undefined<=set(manifest['PermittedUndefinedSymbols'])
    src=tool+row['SourcePath'].removeprefix('source/');assert m.D(source[src])==row['SourceSha256'] and (p.parent/Path(src).name).read_bytes()==source[src]
    compile=m.J(p.parent/'compile.process.json')['Arguments'];assert compile==[n['Compiler']['Path'],'-shared','-fPIC','-O2','-std=c11','-Wall','-Wextra','-Werror','-I',str(remote/'native-build/headers')]+['-D'+x for x in row['CompilerDefinitions']]+[Path(src).name,'-o',p.name]

cases=json.loads(pub[tool+'cases.json'])['Cases'];completion=m.J(e/'capture/completion.json');assert completion['Status']=='PASS_ORIGINAL_NATIVE_ENTRY_ELEVEN_SOURCE_CONTRACT_OBSERVATIONS' and not completion['SdkOrInteropComparisonExecuted'] and not completion['OrdinaryCorpusModified'] and completion['NoWholeNifCompatibilityClaim']
assert [x['Name'] for x in completion['Cases']]==[x['Name'] for x in cases] and len(cases)==11 and not (e/'capture/failure.json').exists()
inputs=m.J(e/'capture/inputs.json');assert inputs['SubjectHead']==m.HEAD and inputs['PinnedOtpCommit']==m.PIN and inputs['SourceManifestSha256']==m.MANIFEST and inputs['RuntimeBuildReceiptSha256']==m.H(build) and inputs['NativeBuildReceiptSha256']==m.H(native) and inputs['WorkflowRunId']==str(run['id']) and inputs['WorkflowRunAttempt']=='1'
managed=json.loads(pub['docs/validation/part-356/native-completion.json']);rawmanaged=json.loads(pub['docs/validation/part-356/native-raw.json'])
assert managed['Status']=='PASS_WINDOWS_ELEVEN_OWN_NATIVE_FIXTURES_VIA_PRODUCTION_CAPTURED_ROUTE' and len(managed['Cases'])==11 and managed['CanonicalFullExitSha256']==m.D(pub['docs/validation/part-356/full-exit.json']) and managed['CanonicalCapturedOutputsSha256']==m.D(pub['docs/validation/part-356/captured-outputs.json'])
wm={}
for row in rawmanaged['Files']:
    data=base64.b64decode(row['Base64'],validate=True);assert len(data)==row['Bytes'] and m.D(data)==row['Sha256'];assert row['Path'] not in wm;wm[row['Path']]=data
payload=T(A('payload'),('map',frozenset({(A('alpha'),L(I(17),('binary',bytes([1,2,3]))))})))
results=[]
for case,obs in zip(cases,completion['Cases'],strict=True):
    name=case['Name'];d=e/'capture'/name;row=images[case['NativeLeaf']];assert obs['Name']==name and obs['Module']==case['Module'] and obs['Status']=='OBSERVED_SOURCE_CONTRACT_AND_NORMAL_CLEANUP'
    for key,file in [('ReturnedSha256','returned.etf'),('OutcomeSha256','outcome.etf'),('ImagesSha256','images.etf'),('ProcessReceiptSha256','observe-vm.process.json')]:assert obs[key]==m.H(d/file)
    assert obs['NativeImageSha256']==row['Sha256'] and obs['SourceSha256']==case['ErlangSourceSha256'] and (d/'native'/Path(row['Path']).name).read_bytes()==mapped(row['Path']).read_bytes()
    src=tool+case['ErlangSource'].removeprefix('source/');assert m.D(source[src])==case['ErlangSourceSha256'] and (d/Path(src).name).read_bytes()==source[src] and (d/'entry_observer.erl').read_bytes()==source[tool+'original/entry_observer.erl']
    for phase,limit in [('compile-vm',90),('observe-vm',45)]:
        receipt=m.normal(d/(phase+'.process.json'));assert receipt['ElapsedSeconds']<limit+10 and receipt['Arguments'][:3]==[b['RuntimeRoot']+'/bin/erl','+S','2:2'] and '-start_epmd' in receipt['Arguments']
        state=m.J(d/(phase+'.stage.json'));assert state['Started'] and state['Failure'] is None and state['Arguments']==receipt['Arguments'];stages.append(dict(Path=(d/(phase+'.process.json')).relative_to(e).as_posix(),Pid=receipt['OwnedPid'],Sha256=m.H(d/(phase+'.process.json'))))
    assert etf(d/'compile.etf')==L(T(A('ok'),A('entry_observer')),T(A('ok'),A(case['Module']))) and etf(d/'compile-outcome.etf')==T(A('returned'),A('compiled'))
    for file in ['compile-runtime-root.etf','runtime-root.etf']:
        value=etf(d/file);assert value[0]=='tuple' and value[1][0]==A('runtime_root') and text(value[1][1])==text(value[1][2])==b['RuntimeRoot']
    metadata=etf(d/'metadata.etf');assert metadata[0]=='tuple' and len(metadata[1])==6 and metadata[1][0]==A('entry_runtime_metadata') and text(metadata[1][1])==b['RuntimeRoot'] and text(metadata[1][2])=='29' and metadata[1][5]==I(8) and 'linux' in text(metadata[1][4])
    identities=etf(d/'images.etf');assert identities[0]=='list' and len(identities[1])==2
    for identity,module in zip(identities[1],['entry_observer',case['Module']],strict=True):
        assert identity[0]=='tuple' and len(identity[1])==5;atom,loaded,obj,code_md5,file_md5=identity[1];assert atom==A(module) and text(loaded)==text(obj)==str(remote/'capture'/name/'ebin'/(module+'.beam')) and code_md5[0]=='binary' and len(code_md5[1])==16 and file_md5==('binary',hashlib.md5((d/'ebin'/(module+'.beam')).read_bytes()).digest())
    value=etf(d/'returned.etf');assert value[0]=='tuple' and len(value[1])==2;observed,cleanup=value[1];assert cleanup==T(A('cleanup'),A('deleted_and_purged')) and etf(d/'outcome.etf')==T(A('returned'),value) and not (d/'raised.etf').exists()
    assert observed[0]=='tuple' and len(observed[1])==5 and observed[1][:3]==(A('entry_case'),A(name),A(case['Module']))
    result,body=observed[1][3:];wr=next(x for x in managed['Cases'] if x['Name']==name);managedbytes=wm[name+'/observations/observed.etf'];assert m.D(managedbytes)==wr['OutcomeSha256'];mv=etf(managedbytes)
    receipt=json.loads(wm[name+'/process.exit.json']);assert m.D(wm[name+'/process.exit.json'])==wr['ReceiptSha256'] and receipt['ExitCode']==0 and receipt['Settled'] and not receipt['TimedOut'] and not receipt['Failures'] and not receipt['JobTerminationRequested'] and receipt['RemainingJobProcesses']==0
    assert mv[0]=='tuple' and mv[1][:3]==observed[1][:3]
    if case['SourceExpected']=='success':
        marker=41 if name=='alpha_no_declarations' else -1 if name=='beta_absent_load' else 73;third=L(*(I(ord(x)) for x in 'alpha')) if name=='alpha_no_declarations' else I(233)
        assert result==A('ok') and body==T(payload,I(marker),third,A('ordinary_untouched')) and mv==observed
        selected=dict(Category='ok',CompleteSelectedSuccessTermEqual=True,Marker=marker,Third='alpha' if name=='alpha_no_declarations' else 233)
    else:
        assert result[0]=='tuple' and result[1][0]==A('error') and result[1][1][0]=='tuple';tag,reason=result[1][1][1];assert tag==A(case['SourceExpected']) and body==T(A('stub_after_failure'),A('not_loaded'))
        assert mv[1][3][0]=='tuple' and mv[1][3][1][0]==A('error') and mv[1][3][1][1][1][0]==tag and mv[1][4]==body
        selected=dict(Category=tag[1],StubAfterFailureEqual=True,OriginalReason=text(reason),ManagedReason=text(mv[1][3][1][1][1][1]),ExactErrorReasonCompared=False)
    results.append(dict(Name=name,Module=case['Module'],Selected=selected,OriginalReturnedSha256=m.H(d/'returned.etf'),ManagedObservedSha256=m.D(managedbytes),NativeElfSha256=row['Sha256'],OriginalCodeCleanup= 'deleted_and_purged',NormalCompileAndObserve=True))
assert len(stages)==58
m.W(Path(__file__).parent/'original-report.json',dict(Status='PASS_PASSIVE_ORIGINAL356_ELEVEN_SOURCE_BOUND_NATIVE_AND_SELECTED_MANAGED_CONTRACTS',Head=m.HEAD,Run=run['id'],Job=job['id'],Artifact=artifact['id'],Passed=11,Failed=0,Cases=results,PinnedOtpCommit=m.PIN,AllPinnedGitSourceRowsRehashed=len(inventory['TrackedFiles']),OriginalBuildNormalStages=3,NativeBuildNormalStages=33,CaseNormalCompileObserveStages=22,NormalProcessReceipts=stages,NativeElfsRehashedAndDynamicSymbolsParsed=8,HeaderInputs=4,OriginalInstalledRuntimePayloadAbsentFromArtifact=True,InstalledRuntimeIdentityEvidence='Exact source-build/installed-hash producer receipt, actual VM roots, source-checked compile/observation commands and loaded module checksums; installed binaries not independently rehashed from capture.',PublishedLfManifestSha256=m.MANIFEST,LocalCrLfManifestSha256=m.H(m.R/tool/'manifest.json'),ManifestDifferenceOnlyCrLfToLf=True,OriginalRuntimeBuildReceiptSha256=m.H(build),NativeBuildReceiptSha256=m.H(native),OriginalCompletionSha256=m.H(e/'capture/completion.json'),CaptureManifestSha256=m.H(c/'download-manifest.json'),ManagedPublishedNativeCompletionSha256=m.D(pub['docs/validation/part-356/native-completion.json']),ExactErrorReasonParityClaimed=False,WholeNifOrNoPolicyCompatibilityClaimed=False,WindowsOtpComparisonClaimed=False,Historical355FailureReclassified=False,CurrentCaseCountDoesNotChangeOrdinaryCorpus=True,SdkNativeOrDownloadedCodeExecutedByReviewer=False,NetworkUsed=False,ReaderSha256=m.H(Path(__file__))))
print('PASS actual original eleven / 58 normal process receipts / eight parsed ELF images / selected managed contracts.')

"""Independent passive328 reallocation review. Never executes downloaded code/binaries."""
from pathlib import Path,PurePosixPath,PureWindowsPath
import hashlib,json,zipfile,tarfile,subprocess,sys,importlib.util,re,posixpath
sys.dont_write_bytecode=True
R=Path.cwd();O=R/'artifacts/part329-reallocation-new-independent';D=R/'artifacts/part329-remote-new-review/reallocation';E=D/'artifact-12045009296';N=E/'sdk-native';HEAD='d83ed875d0c77939e20049afcb46fa297fee82dd';PIN='ad05823719d77c8faee87348ea39513d4e2f99c5';PLAN='0A1FAE308FBFC653924ED02A99705DE8C4622D293A15197784C205FE664D778D';LEGACY='3187D751083A34B7434E8B6C229C6D631277CBC7F98612D1888FB27ECA56D342';B=[]
h=lambda b:hashlib.sha256(b).hexdigest().upper();sha=lambda p:h(p.read_bytes());read=lambda p:json.loads(p.read_bytes());canonical=lambda v:json.dumps(v,sort_keys=True,separators=(',',':')).encode()
def bind(p):B.append(dict(Path=p.relative_to(R).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)))
def blobs(names):
 names=list(dict.fromkeys(names));assert all(not PurePosixPath(n).is_absolute() and '..' not in PurePosixPath(n).parts and not any(c in n for c in '\0\r\n') for n in names);raw=subprocess.check_output(['git','--no-optional-locks','cat-file','--batch'],input=''.join(HEAD+':'+n+'\n' for n in names).encode());off=0;v={}
 for n in names:
  end=raw.index(b'\n',off);hdr=raw[off:end].split();assert len(hdr)==3 and hdr[1]==b'blob';z=int(hdr[2]);off=end+1;v[n]=raw[off:off+z];off+=z;assert raw[off:off+1]==b'\n';off+=1
 assert off==len(raw);return v
m=read(D/'run-map.json');run=read(D/'run.json');job=read(D/'jobs.json')['jobs'];api=read(D/'artifacts.json')['artifacts'];assert m['Head']==run['head_sha']==HEAD and m['Run']==run['id']==38899307374 and run['run_attempt']==m['Attempt']==1 and run['status']=='completed' and run['conclusion']=='success';assert len(job)==len(api)==1 and job[0]['id']==116967324775 and job[0]['head_sha']==HEAD and job[0]['run_id']==run['id'] and job[0]['conclusion']=='success' and api[0]['id']==12045009296 and api[0]['workflow_run']['head_sha']==HEAD
rows=read(D/'download-manifest.json');assert len(rows)==len({x['Path'] for x in rows});assert {p.relative_to(D).as_posix() for p in D.rglob('*') if p.is_file() and p.name!='download-manifest.json'}=={x['Path'].replace('\\','/') for x in rows}|{'run-map.json'}
for x in rows:
 p=D/x['Path'];assert p.stat().st_size==x['Bytes'] and sha(p)==x['Sha256']
zp=D/'artifact-12045009296.zip';assert zp.stat().st_size==api[0]['size_in_bytes'] and sha(zp)==api[0]['digest'][7:].upper()=='B4374A8860F7E06DAC07989879042EE48A3F4C70008D1AA003B3ECC51E3E1CEE'
with zipfile.ZipFile(zp) as z:
 names=z.namelist();assert len(names)==len(set(names));files=[n for n in names if not n.endswith('/')];assert len(files)==192
 for n in names:
  assert not PurePosixPath(n).is_absolute() and not PureWindowsPath(n).is_absolute() and '..' not in PurePosixPath(n).parts and '..' not in PureWindowsPath(n).parts
  if not n.endswith('/'):assert z.read(n)==(E/n).read_bytes()
for n in ['run-map.json','run.json','jobs.json','artifacts.json','download-manifest.json','raw-capture.json','job-116967324775.log']:bind(D/n)
prefix='tools/Erlang.NifReallocationObservations/';pname=prefix+'SOURCE-PLAN.json';legacy='tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';wf='.github/workflows/nif-reallocation-native.yml';published=blobs([pname,legacy,wf]);plan=json.loads(published[pname]);assert h(published[pname])==PLAN and len(plan['Files'])==1145 and h(published[legacy])==LEGACY and len(json.loads(published[legacy])['Files'])==1120;assert not b'\r' in published[pname] and not published[pname].startswith(b'\xef\xbb\xbf') and re.findall(rb'^\s*REVIEWED_REALLOCATION_PLAN_SHA256: ([A-F0-9]{64})$',published[wf],re.M)==[PLAN.encode()]
source=blobs([x['Path'] for x in plan['Files']]);assert len(source)==1145
for x in plan['Files']:
 b=source[x['Path']];assert x['HashMode'] in ['TextLF','BinaryExact'];b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b;assert len(b)==x['Bytes'] and h(b)==x['Sha256']
# Import ONLY independently hash-matched trusted current repository passive modules, never downloaded artifact code.
helpers=[prefix+n for n in ['review-results.py','reviewed_common.py','runtime_receipt_adapter.py','verify_native_build.py']]
for n in helpers:assert (R/n).read_bytes().replace(b'\r\n',b'\n')==source[n].replace(b'\r\n',b'\n');bind(R/n)
assert h(source[prefix+'review-results.py'].replace(b'\r\n',b'\n'))=='875F4EA8DC6AC00494D4F88F21CB6C18EDDF825FBBAE7E0C8926463B4E2CF031'
sys.path.insert(0,str(R/prefix));spec=importlib.util.spec_from_file_location('trusted328review',R/prefix/'review-results.py');rv=importlib.util.module_from_spec(spec);spec.loader.exec_module(rv);spec=importlib.util.spec_from_file_location('trusted328elf',R/prefix/'verify_native_build.py');nv=importlib.util.module_from_spec(spec);spec.loader.exec_module(nv)
build=read(E/'source-build/build-process-receipt.json');runtime=read(E/'runtime-receipt/source-build-receipt.json');review=read(E/'control/source-review.json');approval=read(E/'control/runtime-approval.json');closure=build['InstalledClosure'];assert build['Complete'] is True and build['Head']==HEAD and build['PlanSha256']==PLAN and build['PinnedOtpCommit']==PIN and build['Roles']==['configure','build','install'] and build['ApprovalIssued'] is False and build['ObservationVmExplicitlyInvoked'] is False;assert build['SourceBefore']==build['SourceAfter'] and build['SourceAfter']['PinnedOtpCommit']==PIN and build['SourceAfter']['TrackedClean'] is True
assert build['ProgramIdentitiesBefore']==build['ProgramIdentities'] and [x['Role'] for x in build['ProgramIdentities']]==['configure','make'];assert build['ProgramIdentities'][0]['CommandPath']==build['SourceRoot']+'/configure' and build['ProgramIdentities'][1]['CommandPath']=='/usr/bin/make'
assert runtime['InstalledClosure']==closure and runtime['SourceGitInspection']==build['SourceAfter'] and runtime['OriginalBuildProvenance']==read(E/'source-build/legacy-provenance.json') and runtime['VersionCommandExecuted'] is False and runtime['VmExecuted'] is False and runtime['BuiltFromPinnedSource'] is True
assert approval['Passed'] is True and approval['Head']==HEAD and approval['RuntimeFiles']==2646 and approval['ApprovedReviewSha256']==sha(E/'control/source-review.json') and approval['ApprovedSourceBuildReceiptSha256']==sha(E/'runtime-receipt/source-build-receipt.json') and approval['VerifierSha256']==h(source[prefix+'verify_source_build.py'].replace(b'\r\n',b'\n'))
note=json.loads(review['ReviewNote']);assert note['InstalledClosureSha256']==h(canonical(closure)) and note['BuildProcessReceiptSha256']==sha(E/'source-build/build-process-receipt.json');assert review['ProvenanceSha256']==sha(E/'source-build/legacy-provenance.json') and review['BuildLogSha256']==sha(E/'source-build/complete-build.log')
for role in ['configure','build','install']:
 p=E/('source-build/'+role+'.process.json');v=read(p);assert v['ExitCode']==0 and not v['TimedOut'] and v['ParentReaped'] and v['OutputsClosed'];bind(p)
assert closure['ExternalSystemLibrariesIncluded'] is False and closure['DirectorySymlinksAllowed'] is False
entries={x['Path']:x for x in closure['Entries']};assert len(entries)==len(closure['Entries']);full={x['Path']:x for x in closure['RuntimeFiles']};assert len(full)==2646;relative={str(PurePosixPath(x).relative_to(closure['InstalledPrefix'])):v for x,v in full.items()};headers={};count=0;links=0
with tarfile.open(E/'control/installed-runtime.tar.gz','r:gz') as tar:
 seen=set()
 for member in tar:
  n=member.name.removeprefix('./').rstrip('/')
  if n in ('','.'):continue
  assert not PurePosixPath(n).is_absolute() and '..' not in PurePosixPath(n).parts and n not in seen;seen.add(n);x=entries[n]
  if member.isfile():
   assert x['Kind']=='File';raw=tar.extractfile(member).read();v=relative[n];assert len(raw)==v['Bytes'] and h(raw)==v['Sha256'];count+=1
   if '/usr/include/' in n:headers[PurePosixPath(n).name]=h(raw)
  elif member.issym():
   assert x['Kind']=='FileSymlink' and member.linkname==x['LinkText'];target=n;walk=set()
   while entries[target]['Kind']=='FileSymlink':
    assert target not in walk;walk.add(target);link=entries[target]['LinkText']
    target=str(PurePosixPath(link).relative_to(closure['InstalledPrefix'])) if PurePosixPath(link).is_absolute() else posixpath.normpath(posixpath.join(posixpath.dirname(target),link))
    assert not target.startswith('../') and not target.startswith('/') and target in entries
   assert target==x['Target'] and entries[target]['Kind']=='File';links+=1
  else:assert member.isdir() and x['Kind']=='Directory'
 assert seen==set(entries)
assert count==closure['RegularFileCount']==2646 and sha(E/'control/installed-runtime.tar.gz')==(E/'control/installed-runtime.tar.gz.sha256').read_text().split()[0].upper()
for x in plan['Headers']:assert headers[x['Name']]==x['Sha256']
assert len(plan['Headers'])==3 and 'erl_int_sizes_config.h' in headers
roles=runtime['OriginalBuildProvenance']['Files'];assert len(roles)==4
for x in roles:
 p=build['SourceRoot']+'/'+x['Path']
 if p not in full:
  n=str(PurePosixPath(p).relative_to(closure['InstalledPrefix']));entry=entries[n];assert entry['Kind']=='FileSymlink';p=closure['InstalledPrefix']+'/'+entry['Target']
 assert full[p]['Bytes']==x['Bytes'] and full[p]['Sha256']==x['Sha256']
na=read(E/'control/native-approval.json');br=read(N/'build-receipt.json');sdk=read(N/'sdk-receipt.json');original=read(E/'original3/receipt.json');capture=read(E/'control/capture-approval.json');assert na['Passed'] and na['Head']==HEAD and na['PlanSha256']==PLAN and na['VerifierSha256']==h(source[prefix+'verify_native_build.py'].replace(b'\r\n',b'\n'));assert na['ApprovedBuildReceiptSha256']==sha(N/'build-receipt.json')==sdk['BuildReceiptSha256'];assert capture['Passed'] and capture['ApprovedOriginalReceiptSha256']==sha(E/'original3/receipt.json') and capture['ApprovedSdkReceiptSha256']==sha(N/'sdk-receipt.json');assert original['IndependentlyApprovedSourceBuildReceiptSha256']==approval['ApprovedSourceBuildReceiptSha256'] and (E/'original3/approved-source-build-receipt.json').read_bytes()==(E/'runtime-receipt/source-build-receipt.json').read_bytes()
checks=[]
for name in ['reallocation_fixture.so','reallocation_extension.so','legacy_fixture.so','legacy_bridge.so']:
 parsed=nv.elf(N/name);x=next(x for x in na['Binaries'] if x['Name']==name);assert parsed==x['Elf'] and sha(N/name)==x['Sha256'];stem=name.replace('.','-')
 for role in ['compile','defined','undefined','readelf']:
  p=N/(stem+'-'+role+'.process.json');v=read(p);assert v['ExitCode']==0 and not v['TimedOut'] and v['PipesClosed'] and v['ParentReaped'] and v['OwnedPid']>0 and not (N/(stem+'-'+role+'.stderr')).read_bytes();bind(p)
 imports={x.split()[-1].split('@')[0] for x in (N/(stem+'-undefined.stdout')).read_text().splitlines() if x.split()};exports={x.split()[-1] for x in (N/(stem+'-defined.stdout')).read_text().splitlines() if x.split()};assert imports==set(parsed['Imports']) and imports<=set(plan['NativeImports'][name]) and exports==set(parsed['Exports'])==set(plan['NativeExports'][name]);assert (N/(stem+'-readelf.stdout')).stat().st_size>0
 argv=read(N/(stem+'-compile.process.json'))['Arguments'];assert argv[0]=='/usr/bin/gcc' and argv[1:9]==['-shared','-fPIC','-O2','-std=c11','-Wall','-Wextra','-Werror','-I'] and '-fvisibility=hidden' in argv;checks.append(dict(Name=name,Sha256=x['Sha256'],Elf=parsed))
for role in ['build-managed','sdk']:
 v=read(N/(role+'.process.json'));assert v['ExitCode']==0 and not v['TimedOut'] and v['PipesClosed'] and v['ParentReaped'] and not (N/(role+'.stderr')).read_bytes();bind(N/(role+'.process.json'))
sp=read(N/'sdk.process.json');assert sp['Arguments'][0]==read(N/'build-managed.process.json')['Arguments'][0] and sp['Arguments'][2]=='--reallocation-probe'
for p,digest in zip(sp['Arguments'][3::2],sp['Arguments'][4::2]):
 if p.endswith('.so'):assert sha(N/PurePosixPath(p).name)==digest
assets=[]
for target in read(N/'managed/Erlang.NifNativeBackend.deps.json')['targets'].values():
 for library in target.values():
  for kind in ['runtime','resources','native']:
   for n in library.get(kind,{}):assert (N/'managed'/n).is_file();assets.append(n)
# In-process PURE passive trusted source reader. It parses receipts/JSON/ETF only; no downloaded script execution.
oldargv=sys.argv;sys.argv=['review-results.py','--repository',str(R),'--plan',str(R/pname),'--expected-head',HEAD,'--approved-plan-sha256',PLAN,'--original',str(E/'original3'),'--sdk',str(N),'--approved-original-receipt-sha256',sha(E/'original3/receipt.json'),'--approved-sdk-receipt-sha256',sha(N/'sdk-receipt.json'),'--output',str(O/'comparison.json')]
try:rv.main()
finally:sys.argv=oldargv
compared=read(O/'comparison.json');assert compared==read(E/'control/original-sdk-three-comparison.json') and compared['Passed'] and len(compared['Cases'])==3 and [x['ActualOriginalMask'] for x in compared['Cases']]==[1539,1601,2047] and all(x['ActualOriginalMask']==x['ActualSdkMask'] for x in compared['Cases'])
for n in ['source-build/build-process-receipt.json','source-build/legacy-provenance.json','source-build/complete-build.log','runtime-receipt/source-build-receipt.json','control/source-review.json','control/runtime-approval.json','control/native-approval.json','control/capture-approval.json','control/original-sdk-three-comparison.json','original3/receipt.json','sdk-native/sdk-receipt.json','sdk-native/build-receipt.json']:bind(E/n)
fullguard=json.loads(blobs(['docs/validation/part-329/final-main/full-guard.json'])['docs/validation/part-329/final-main/full-guard.json']);assert fullguard['Passed']==6747 and fullguard['Failed']==0 and fullguard['IntegrationResults']==11
report=dict(Status='PASS_INDEPENDENT_EXACT_HEAD329_PINNED_PREFIX_AND_ORIGINAL3_SDK3_REALLOCATION',Passed=True,Head=HEAD,Run=38899307374,Job=116967324775,Artifact=12045009296,Attempt=1,DownloadedManifestRows=198,ArtifactMemberFiles=192,RawZipSha256=sha(zp),OwnPlanInputs=1145,OwnPlanSha256=PLAN,LegacyPlanInputs=1120,LegacyPlanSha256=LEGACY,PublishedPlanInputsRehashed=1145,CanonicalPlanAndWorkflowPinExact=True,PinnedOtpCommit=PIN,ConfigureBuildInstallActualExitZero=True,InstalledPrefixRegularFilesRehashed=count,InstalledNamespaceEntries=len(entries),InstalledFileSymlinks=links,WholeInstalledPrefixClosureExact=True,OriginalFourRuntimeRolesVerified=True,PinnedHeaders=headers,RawNativeElfChecks=checks,SourceBuildNativeCaptureApprovalsLinked=True,ManagedDependencyAssetsPresent=len(assets),Original3Sdk3Exact=True,Cases=compared['Cases'],IndependentComparisonSha256=sha(O/'comparison.json'),PublishedComparisonSourceSha256='875F4EA8DC6AC00494D4F88F21CB6C18EDDF825FBBAE7E0C8926463B4E2CF031',PublishedMainFullSeparateScope=dict(Passed=6747,IntegrationResults=11),DownloadedCodeOrBinariesExecuted=False,ReviewerSdkVmNativeExecuted=False,PhysicalMemoryCompared=False,ActualAllocatorOom=False,DisabledProfileCompared=False,GeneralNifAbiComplete=False,MainMutated=False,NetworkOrPostExecuted=False,Bindings=B)
(O/'guard.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8',newline='\n');print(sha(O/'guard.json'))

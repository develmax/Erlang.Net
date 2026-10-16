from pathlib import Path,PurePosixPath
import json,hashlib,tarfile,importlib.util
R=Path.cwd();A=R/'artifacts';spec=importlib.util.spec_from_file_location('a',A/'pack323-audit-common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
mapping,d,run,job,items,raw=m.load('reallocation');assert run['conclusion']==job['conclusion']=='success';E=d/('artifact-'+str(items[0]['id']));N=E/'sdk-native';read=m.read;sha=m.sha
paths=['tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','tools/Erlang.NifReallocationObservations/run-reviewed.py'];blobs=m.blobs(mapping['Head'],paths);plan=json.loads(blobs[paths[0]]);assert sha(blobs[paths[0]])=='2A31DD136547D9F9395930CF6EC5C02CD28CB426C94BE385757F6696E02D89D5' and len(plan['Files'])==1133
allsource=m.blobs(mapping['Head'],[r['Path'] for r in plan['Files']])
for r in plan['Files']:
 b=allsource[r['Path']];b=b.replace(b'\r\n',b'\n') if r['HashMode']=='TextLF' else b;assert len(b)==r['Bytes'] and sha(b)==r['Sha256']
build=read(E/'source-build/build-process-receipt.json');closure=build['InstalledClosure'];runtime=read(E/'runtime-receipt/source-build-receipt.json');review=read(E/'control/source-review.json');approval=read(E/'control/runtime-approval.json')
assert build['Head']==mapping['Head'] and build['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and build['Complete'] and not build['ApprovalIssued'] and not build['ObservationVmExplicitlyInvoked']
assert build['SourceBefore']['TrackedClean'] and build['SourceAfter']['TrackedClean'] and build['ProgramIdentitiesBefore']==build['ProgramIdentities']
assert runtime['InstalledClosure']==closure and approval['Passed'] and approval['RuntimeFiles']==2646 and approval['ApprovedReviewSha256']==sha((E/'control/source-review.json').read_bytes()) and approval['ApprovedSourceBuildReceiptSha256']==sha((E/'runtime-receipt/source-build-receipt.json').read_bytes())
canonical=lambda v:json.dumps(v,sort_keys=True,separators=(',',':')).encode()
note=json.loads(review['ReviewNote']);assert note['InstalledClosureSha256']==sha(canonical(closure)) and note['BuildProcessReceiptSha256']==sha((E/'source-build/build-process-receipt.json').read_bytes())
assert review['ProvenanceSha256']==sha((E/'source-build/legacy-provenance.json').read_bytes()) and review['BuildLogSha256']==sha((E/'source-build/complete-build.log').read_bytes())
for role in ['configure','build','install']:
 p=read(E/('source-build/'+role+'.process.json'));assert p['ExitCode']==0 and not p['TimedOut'] and p['ParentReaped'] and p['OutputsClosed']
tar=E/'control/installed-runtime.tar.gz';assert sha(tar.read_bytes())==(E/'control/installed-runtime.tar.gz.sha256').read_text().split()[0].upper()
entries={r['Path']:r for r in closure['Entries']};files={str(PurePosixPath(r['Path']).relative_to(closure['InstalledPrefix'])):r for r in closure['RuntimeFiles']};headers={};verified=0
with tarfile.open(tar,'r:gz') as t:
 seen=set()
 for member in t:
  n=member.name.removeprefix('./').rstrip('/')
  if n in ['','.']:continue
  assert not PurePosixPath(n).is_absolute() and '..' not in PurePosixPath(n).parts and n not in seen;seen.add(n);r=entries[n]
  if member.isfile():
   assert r['Kind']=='File';b=t.extractfile(member).read();fr=files[n];assert len(b)==fr['Bytes'] and sha(b)==fr['Sha256'];verified+=1
   if '/usr/include/' in n:headers[PurePosixPath(n).name]=sha(b)
  elif member.issym():assert r['Kind']=='FileSymlink' and member.linkname==r['LinkText']
  else:assert member.isdir() and r['Kind']=='Directory'
 assert seen==set(entries)
assert verified==closure['RegularFileCount']==2646
import sys,importlib.util
contracts=A/'pack323-reallocation-published-passive-contracts';tool=contracts/'tools/Erlang.NifReallocationObservations';sys.path.insert(0,str(tool));spec=importlib.util.spec_from_file_location('passive_native319',tool/'verify_native_build.py');nv=importlib.util.module_from_spec(spec);spec.loader.exec_module(nv)
native_approval=read(E/'control/native-approval.json');build_receipt=read(N/'build-receipt.json');sdk=read(N/'sdk-receipt.json');original=read(E/'original3/receipt.json');capture=read(E/'control/capture-approval.json')
assert native_approval['Passed'] and native_approval['Head']==mapping['Head'] and native_approval['PlanSha256']==sha(blobs[paths[0]]) and native_approval['ApprovedBuildReceiptSha256']==sha((N/'build-receipt.json').read_bytes())==sdk['BuildReceiptSha256']
assert capture['Passed'] and capture['ApprovedOriginalReceiptSha256']==sha((E/'original3/receipt.json').read_bytes()) and capture['ApprovedSdkReceiptSha256']==sha((N/'sdk-receipt.json').read_bytes())
assert original['IndependentlyApprovedSourceBuildReceiptSha256']==approval['ApprovedSourceBuildReceiptSha256'] and (E/'original3/approved-source-build-receipt.json').read_bytes()==(E/'runtime-receipt/source-build-receipt.json').read_bytes()
native_checks=[]
for name in ['reallocation_fixture.so','reallocation_extension.so','legacy_fixture.so','legacy_bridge.so']:
 parsed=nv.elf(N/name);record=next(x for x in native_approval['Binaries'] if x['Name']==name);assert parsed==record['Elf'] and sha((N/name).read_bytes())==record['Sha256']
 for role in ['compile','defined','undefined','readelf']:
  stem=name.replace('.','-')+'-'+role;p=read(N/(stem+'.process.json'));assert p['ExitCode']==0 and not p['TimedOut'] and p['PipesClosed'] and p['ParentReaped'] and p['OwnedPid']>0 and not (N/(stem+'.stderr')).read_bytes()
 imports={x.split()[-1].split('@')[0] for x in (N/(name.replace('.','-')+'-undefined.stdout')).read_text().splitlines() if x.split()};exports={x.split()[-1] for x in (N/(name.replace('.','-')+'-defined.stdout')).read_text().splitlines() if x.split()};assert imports==set(parsed['Imports']) and imports<=set(plan['NativeImports'][name]) and exports==set(parsed['Exports'])==set(plan['NativeExports'][name])
 argv=read(N/(name.replace('.','-')+'-compile.process.json'))['Arguments'];assert argv[0]=='/usr/bin/gcc' and argv[1:9]==['-shared','-fPIC','-O2','-std=c11','-Wall','-Wextra','-Werror','-I'] and '-fvisibility=hidden' in argv
 native_checks.append({'Name':name,'Sha256':record['Sha256'],'Elf':parsed,'ActualCompileAndNmReadelfExitZero':True})
for role in ['build-managed','sdk']:
 p=read(N/(role+'.process.json'));assert p['ExitCode']==0 and not p['TimedOut'] and p['PipesClosed'] and p['ParentReaped'] and not (N/(role+'.stderr')).read_bytes()
sp=read(N/'sdk.process.json');assert sp['Arguments'][0]==read(N/'build-managed.process.json')['Arguments'][0] and sp['Arguments'][2]=='--reallocation-probe'
for path,h in zip(sp['Arguments'][3::2],sp['Arguments'][4::2]):
 if path.endswith('.so'):assert sha((N/PurePosixPath(path).name).read_bytes())==h
deps=read(N/'managed/Erlang.NifNativeBackend.deps.json');assets=[]
for target in deps['targets'].values():
 for library in target.values():
  for kind in ['runtime','resources','native']:
   for name in library.get(kind,{}):assert (N/'managed'/name).is_file();assets.append(name)
local=A/'pack323-reallocation-independent-published-comparison.json';lp=read(A/'pack323-reallocation-independent-published-comparison-process.json');assert lp['ExitCode']==0 and not lp['Stdout'] and not lp['Stderr'] and lp['ReportSha256']==sha(local.read_bytes())
compared=read(local);remote=read(E/'control/original-sdk-three-comparison.json');assert compared==remote and compared['Passed'] and len(compared['Cases'])==3 and [x['ActualOriginalMask'] for x in compared['Cases']]==[1539,1601,2047] and all(x['ActualOriginalMask']==x['ActualSdkMask'] for x in compared['Cases'])
report={'Status':'PASS_INDEPENDENT_EXACT_HEAD323_PINNED_SOURCE_FULL_PREFIX_FOUR_NATIVE_BUILDS_AND_ORIGINAL3_SDK3_COMPARISON','Head':mapping['Head'],'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'RawZipSha256':sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'RawJobSha256':sha((d/f'job-{job["id"]}.log').read_bytes()),'PublishedPlanInputsRehashed':1133,'PublishedPlanSha256':sha(blobs[paths[0]]),'ConfigureBuildInstallActualExitZero':True,'PinnedOtpCommit':build['PinnedOtpCommit'],'WholeInstalledPrefixFilesPassivelyRehashed':verified,'WholeNamespaceAndSymlinksExact':True,'SourceReviewRuntimeNativeCaptureApprovalReceiptHashesLinked':True,'RawElfAndCapturedSymbolsAndPoliciesVerified':native_checks,'AllFourActualNativeBuildsPassed':True,'ManagedDependencyAssetsPresent':len(assets),'ActualOriginalMetadataAndThreeCasesExitZero':True,'ActualSdkProbeExitZero':True,'PublishedPureComparisonIndependentlyReplayed':True,'ComparisonSourceSha256':lp['SourceVerifierSha256'],'IndependentComparisonSha256':sha(local.read_bytes()),'IndependentAndRemoteComparisonValuesExact':True,'Cases':compared['Cases'],'ActualAllocatorOom':False,'PhysicalMemoryCompared':False,'DisabledProfileCompared':False,'GeneralNifAbiComplete':False,'DefaultNativeLoadHandlerInstalled':False,'Previous315FirstImportFailureNotRelabeled':True,'NativeSdkVmExecutedByReviewer':False,'MainEdited':False,'PostExecuted':False}
out=A/'pack323-reallocation-independent-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Path':out.relative_to(R).as_posix(),'Sha256':sha(out.read_bytes()),'Status':report['Status']}))

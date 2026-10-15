from pathlib import Path,PurePosixPath
import json,hashlib,tarfile,importlib.util
R=Path.cwd();A=R/'artifacts';spec=importlib.util.spec_from_file_location('a',A/'pack315-audit-common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
mapping,d,run,job,items,raw=m.load('reallocation');assert run['conclusion']==job['conclusion']=='failure';E=d/('artifact-'+str(items[0]['id']));N=E/'sdk-native';read=m.read;sha=m.sha
paths=['tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','tools/Erlang.NifReallocationObservations/run-reviewed.py'];blobs=m.blobs(mapping['Head'],paths);plan=json.loads(blobs[paths[0]]);assert sha(blobs[paths[0]])=='D68B34BA634B104ACAC155DCE22E6DE7247CBE52017A4DAB3E8032B8DEC4C03E' and len(plan['Files'])==1090
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
raw_imports=(N/'legacy_fixture-so-undefined.stdout').read_text();imports={x.split()[-1].split('@')[0] for x in raw_imports.splitlines() if x.split()};extra=imports-set(plan['NativeImports']['legacy_fixture.so']);assert extra=={'__stack_chk_fail'} and '__stack_chk_fail@GLIBC_2.4' in raw_imports
for name in ['reallocation_fixture','reallocation_extension']:
 for role in ['compile','defined','undefined','readelf']:
  p=read(N/(name+'-so-'+role+'.process.json'));assert p['ExitCode']==0 and not p['TimedOut'] and p['ParentReaped'] and p['PipesClosed']
assert not (E/'original3').exists() and not (N/'sdk-receipt.json').exists() and not (N/'build-managed.process.json').exists() and not (N/'legacy_bridge-so-compile.process.json').exists()
oldmap,od,orr,oj,oi,_=m.load('native');OE=od/('artifact-'+str(oi[0]['id']));ON=OE/'nif-bounded-sdk';ob=read(ON/'source-binding.json');configured={r['Path']:r['Sha256'] for r in ob['ConfiguredHeaders']}
for n,h in configured.items():assert headers[n]==h,n
assert (ON/'gcc-version.stdout').read_bytes()==(N/'compiler-version.stdout').read_bytes()
bridge_raw=(ON/'nif_bridge-so-undefined.stdout').read_text();bridge_imports={x.split()[-1].split('@')[0] for x in bridge_raw.splitlines() if x.split()};assert bridge_imports-set(plan['NativeImports']['legacy_bridge.so'])=={'__stack_chk_fail'}
old_fixture=(ON/'fixture-so-undefined.stdout').read_text();assert {x.split()[-1] for x in old_fixture.splitlines() if x.split()}=={x.split()[-1] for x in raw_imports.splitlines() if x.split()}
oldrecipe=read(ON/'compile-veneer.process.json')['Arguments'];assert oldrecipe[1:10]==['-shared','-fPIC','-O2','-std=c11','-Wall','-Wextra','-Werror','-I',oldrecipe[9]] and '-fvisibility=hidden' in oldrecipe
assert [x.split('/tools/')[-1] for x in oldrecipe if x.endswith('.c')]==['Erlang.NifNativeBackend/native/nif_bridge_exports.c','Erlang.NifNativeBackend/native/nif_bridge_varargs.c']
log=(d/f'job-{job["id"]}.log').read_text(encoding='utf-8-sig');assert 'RuntimeError: Unexpected native imports: legacy_fixture.so' in log
report={'Status':'PRESERVED_FIRST_EXACT_HEAD315_NATIVE_BUILD_LEGACY_FIXTURE_IMPORT_REFUSAL','Head':mapping['Head'],'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'RawZipSha256':sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'RawJobSha256':sha((d/f'job-{job["id"]}.log').read_bytes()),'PublishedCanonicalPlanAnd1090InputsExact':True,'SourceConfigureBuildInstallExitZero':True,'PinnedOtpCommit':build['PinnedOtpCommit'],'WholeInstalledPrefixFilesPassivelyRehashed':verified,'WholePrefixNamespaceAndSymlinksExact':True,'IndependentSourceReviewAndRuntimeApprovalHashesBound':True,'FailedGate':'run-reviewed.py:61 native-build unexpected native imports legacy_fixture.so','ActualNewFixtureUnexpectedImports':sorted(extra),'ActualRawImport':'__stack_chk_fail@GLIBC_2.4','ActualOld315BridgeUnexpectedUnderNewPlan':sorted(bridge_imports-set(plan['NativeImports']['legacy_bridge.so'])),'Old315NativeRun':oldmap['Run'],'ConfiguredHeaderHashesSameAcrossBothRuns':True,'SameGccVersionBanner':True,'ActualOldBridgeSameSharedSourcesAndFlagsAsUnreachedNewRecipe':True,'NewLegacyBridgeExecuted':False,'ManagedBuildReached':False,'OriginalCasesExecuted':0,'SdkCasesExecuted':0,'NoImportPolicyModified':True,'NextHeadRequiresActualFourBuildsAndOriginalSdkExecution':True,'ReviewerNativeVmSdkExecution':False,'MainEdited':False,'PostExecuted':False,'Scope':'Source-build and static compilation advanced; no successful NIF runtime or original/SDK comparison can be inferred from this failed new workflow.'}
out=A/'pack315-reallocation-independent-failure-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Path':out.relative_to(R).as_posix(),'Sha256':sha(out.read_bytes()),'Status':report['Status']}))

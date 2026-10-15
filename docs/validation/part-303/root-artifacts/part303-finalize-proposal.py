"""Root-only; actual terminal validation, source and native gates precede every mutation."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-303';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *v:subprocess.check_output(['git',*v],cwd=r)
parent='ce3636a9d17a4d1e1c979c57b4df3d0800b72134';previous301='ca33514ba537688aaa8a265200b8d270a01f98fb';assert git('rev-parse','HEAD').decode().strip()==parent and not o.exists()
freeze=a/'root-part303-final-source-freeze.json';assert h(freeze.read_bytes())=='A76B100B3A281B9087B13D1ABCEB45F8EC188B1403906997C6476FD2C722F078';f=j(freeze)
assert f['ParentHead']==parent and f['SourcePaths']==len(f['Files'])==13 and f['CSharpPaths']==11 and f['TokensExactForCSharp'] and f['ReviewedSourceInputs']==1007 and not f['PublicMfaAdded'] and not f['PublicLoadNifActivated'] and not f['OriginalNewCasesExecuted'] and not f['GeneralNifAbiComplete']
paths={x['Path'] for x in f['Files']};assert len(paths)==13 and sum(p.endswith('.cs') for p in paths)==11
for row in f['Files']:
 data=(r/row['Path']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';assert h(plan.read_bytes())==f['ReviewedSourcePlanSha256']=='C8577299D35B72112F8FFAE3BEBD689A8BC9D7D8318F1601EBD75178D6B937E0' and len(j(plan)['Files'])==1007
sg=j(a/'root-part303-source-guard.json');assert sg['Status']=='PASS_ACTUAL_MAIN13_SOURCE_11_TOKENS_1007_INPUTS_AND_13_FOCUS' and sg['SourceFreezeSha256']==h(freeze.read_bytes()) and sg['MainFocusedGroupsPassed']==13
assert j(a/'root-part303-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part303-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-301/tests.json');names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']];added='runtime/direct-source-load-nif-exact-caller-capability'
assert (t['Passed'],t['Failed'])==(6622,0) and len(names)==len(set(names))==6622 and len(oldnames)==6621 and [n for n in names if n in set(oldnames)]==oldnames and set(names)-set(oldnames)=={added} and all(x['Status']=='Passed' for x in t['Tests'])
assert j(a/'integration-results.json')==j(r/'docs/validation/part-301/integration-results.json') and len(j(a/'integration-results.json'))==11
raw_path=a/'root-part303-supported-mfas-raw-export.json';raw=j(raw_path);derived=j(r/'docs/supported-mfas.json');published=json.loads(git('cat-file','blob',parent+':docs/supported-mfas.json'));key=lambda q:(q['Module'],q['Function'],q['Arity']);subset=('erlang','process_info',1)
assert len(raw)==len(derived)==503 and derived==published and [key(x) for x in raw]==[key(x) for x in derived]
for before,after in zip(raw,derived):assert (before==after if key(before)!=subset else {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'})
meta=j(a/'root-part303-supported-mfas-derived-metadata-receipt.json');assert meta['RawSha256']==h(raw_path.read_bytes()) and meta['DerivedSha256']==h((r/'docs/supported-mfas.json').read_bytes()) and meta['FullValidationLogSha256']==h((a/'root-part303-full-validation.log').read_bytes()) and meta['ActualFullTestReportSha256']==h((a/'tests.json').read_bytes()) and not meta['SourceProgramExporterModified']
ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6622 and ready['RegisteredMfas']==503 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-301/tests.json'
native=j(a/'root-part303-main-native-guard.json');assert native['Status']=='PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE' and native['ExitCode']==0 and native['SourcePaths']==13 and native['CSharpPaths']==11 and native['SourceHashesExact'] and native['SourceFreezeSha256']==h(freeze.read_bytes()) and native['ReviewedSourcePlanSha256']==h(plan.read_bytes())
assert native['NativeTrustedWindowsProfilePassed'] and native['ActualGeneratedCSharpRoutePassed'] and native['ActualNativeFenceConcurrencyPassed'] and native['NonDefaultNativeProviderRefusalPassed'] and not native['GeneralNifAbiComplete'] and native['GeneratedCSharpSha256']=='6E28E8B093F042C22ACAFD2C0213A55C7DCEFC2D162B486AC8443FC7C4E5F77A'
assert (a/'root-part303-main-native.log').read_text(encoding='utf-8-sig').splitlines()==(a/'root-part301-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert j(a/'root-part303-main-target-exit.json')['ExitCode']==0 and j(a/'root-part303-main-focus13-exit.json')=={'ActualMain':True,'Groups':13,'ExitCode':0}
focus=re.findall(r'^PASS ([^\s]+)\s*$',(a/'root-part303-main-focus13.log').read_text(encoding='utf-8-sig'),re.M);assert len(focus)==len(set(focus))==13 and 'SourceLoadNifHostChecks' in focus
isolated=a/'root-nif-source-caller303-isolated';assert j(isolated/'tokens11-folder2-before.json')==j(isolated/'tokens11-folder2-after.json') and j(isolated/'format11-folder2-guard.json')['TokensExact']
closed45='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6';assert h((r/'docs/changelog/0045.md').read_bytes())==closed45
volume=r/'docs/changelog/0046.md';entry=(a/'part303-changelog-draft.md').read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip('\r\n')+'\n';combined=volume.read_bytes()+b'\r\n'+entry.replace('\n','\r\n').encode();assert b'## Part 303' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400 and not combined.endswith(b'\r\n\r\n')
index=r/'CHANGELOG.md';ib=index.read_bytes();index_label='part302 (active)'.encode();assert ib.count(index_label)==1;updated_index=ib.replace(index_label,'parts302–303 (active)'.encode());assert all(isinstance(x,bytes) for x in [ib,combined,updated_index])
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:assert b'Part303:' not in (r/name).read_bytes() and re.search(br'\r?\n\r?\n',(r/name).read_bytes())
attrs=r/'.gitattributes';assert b'docs/validation/part-303/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert src.is_file() and dst.is_relative_to(o) and not {'bin','obj','__pycache__','.git'}.intersection(src.parts) and 'LICENSE.local.txt' not in src.parts
 data=src.read_bytes()
 if expected:assert h(data)==expected
 if size is not None:assert len(data)==size
 if dst in copies:assert copies[dst][1]==h(data)
 copies[dst]=(src,h(data))
def manifest(mp):
 for row in j(mp):select(r/row['SourcePath'],r/row['Destination'],row['Sha256'],row['Bytes'])
 select(mp,o/'staging-manifests'/mp.name)
ng=j(a/'pack301-native-combined-independent-guard.json');assert ng['Head']==previous301 and ng['Status']=='PASS_EXACT_HEAD301_LINUX_TRUSTED_SDK_AND_SEPARATE_ORIGINAL13' and ng['SdkSixArgumentNativePassed'] and ng['IndependentOriginal13TypedPassed'] and ng['SourceInputsVerified']==1003 and not ng['OriginalAndSdkThirteenCasesCompared'];manifest(a/'pack301-native-detached-staging-manifest-for303.json')
ordinary301=False;op=a/'pack301-original-38732672128-independent-guard.json';om=a/'pack301-original-detached-staging-manifest-for303.json'
if op.exists() or om.exists():
 assert op.exists() and om.exists();g=j(op);assert g['Head']==previous301 and g['Passed']==5854 and g['Failed']==0 and g['FdPassed']==8 and g['ApprovedHistoricalSourceGeneratedMigrations']==1 and g['NewCompiledCases']==8 and g['NineActualLocalGeneratedHashesMatch'] and not g['RawNormalization'];manifest(om);ordinary301=True
previous302=[];extra=a/'part303-additional-verified-manifests.json'
if extra.exists():
 for row in j(extra):
  mp=r/row['Manifest'];gp=r/row['Guard'];assert h(mp.read_bytes())==row['ManifestSha256'] and h(gp.read_bytes())==row['GuardSha256'];g=j(gp);assert g['Head']==parent and g['Status']==row['ExpectedStatus'] and 'PASS' in g['Status'];manifest(mp);previous302.append({'Status':g['Status'],'GuardSha256':h(gp.read_bytes()),'ManifestSha256':h(mp.read_bytes())})
 select(extra,o/'staging-manifests'/extra.name)
for pattern in ['root-part303-*','root-*303*.py','root-*303*.ps1','part303-*.py','part303-*.json','part303-changelog-draft.md']:
 for src in sorted(a.glob(pattern)):
  if src.is_file():select(src,o/'root-artifacts'/src.name)
for folder in ['nif-source-load-caller301-overlay','nif-source-load-caller301-readonly-review1','nif-source-load-caller301-coverage-revision2']:
 directory=a/folder;assert directory.is_dir()
 for src in sorted(directory.rglob('*')):
  if src.is_file() and not {'bin','obj','__pycache__','.git'}.intersection(src.parts):select(src,o/'candidate-provenance'/folder/src.relative_to(directory))
for src in sorted(isolated.iterdir()):
 if src.is_file():select(src,o/'isolated-records'/src.name)
for prefix in ['authored-preformat','inputs','promotion-recipe']:
 for src in sorted((isolated/prefix).rglob('*')):
  if src.is_file() and not {'bin','obj','__pycache__','.git'}.intersection(src.parts):select(src,o/'isolated-records'/src.relative_to(isolated))
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json'),(raw_path,o/'supported-mfas-raw-export.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 data=src.read_bytes();assert h(data)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data)
volume.write_bytes(combined);index.write_bytes(updated_index)
note='Part303: actual6622/full retains immutable301 all6621 ordered names plus one internal direct-source load_nif caller-capability HOST;13 focused groups and eleven integration/package results pass. No public MFA/default loader activation; registry503/erlang345 of351 unchanged. Actual generated managed entry, argument ordering/failure and exact image lease ownership are bounded prerequisites, not native on_load/BEAM instruction/general ABI parity. Native closure1007 and actual post-full Windows trusted-profile checks are preserved; previous301 Linux native plus separate original13 verified independently. Other previous301/302 terminal states are explicit in receipt. Readiness remains partial.'
for name in metadata:
 q=r/name;data=q.read_bytes();cut=re.search(br'\r?\n\r?\n',data).end();nl=b'\r\n' if b'\r\n' in data else b'\n';q.write_bytes(data[:cut]+note.encode()+nl+nl+data[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-303/** binary\n');assert h((r/'docs/changelog/0045.md').read_bytes())==closed45
receipt={'Status':'ACTUAL_MAIN6622_FULL_INTERNAL_SOURCE_CALLER_PREREQUISITE','ParentHead':parent,'SourcePaths':13,'CSharpPaths':11,'Passed':6622,'Failed':0,'Historical6621OrderedNamesExact':True,'Added':[added],'Registry':503,'RegistryDelta':0,'ErlangPresence':345,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'MainFocusedGroupsPassed':13,'ActualPostFullMainNativeWindowsPassed':True,'PublicLoadNifActivated':False,'GeneralNifAbiComplete':False,'ReviewedSourceInputs':1007,'ReviewedSourcePlanSha256':h(plan.read_bytes()),'Actual503RawExporterOutputPreserved':True,'OnlyExistingProcessInfo1LimitsRestored':True,'Previous301LinuxNativeSdkAndSeparateOriginal13Verified':True,'Previous301Original13SdkCompared':False,'Previous301Ordinary5854Fd8Verified':ordinary301,'Previous302VerifiedProofs':previous302,'SameHeadCI303Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0045Sha256':closed45}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'Passed':6622,'SourcePaths':13,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined),'Previous301OrdinaryVerified':ordinary301,'Previous302Proofs':previous302}))

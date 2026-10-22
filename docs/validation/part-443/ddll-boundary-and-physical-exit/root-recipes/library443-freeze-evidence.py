from pathlib import Path
import hashlib,json,shutil,subprocess
r=Path.cwd();out=r/'docs/validation/part-443/ddll-boundary-and-physical-exit'
assert not out.exists()
def read(p): return json.loads((r/p).read_bytes())
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,x): p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
original=read('artifacts/library443-ddll-boundary-original1/completion.json')
assert original['Passed'] and original['WholeRows']==60 and original['CompleteProviderScans']==2
managed=read('artifacts/library443-ddll-boundary-managed2/completion.json');assert managed['Passed']
comparison=read('artifacts/library443-ddll-boundary-managed2/observations/ddll-boundary443/comparison.json')
assert comparison['Passed'] and comparison['PhysicalExit'] and comparison['PhysicalCleanup'] and comparison['ChildCompletion']
retained=read('artifacts/library443-ddll-retained-managed2/observations/visibility-all.json')
assert retained['Passed'] and len(retained['Results'])==8 and not retained['Failures']
assert read('artifacts/library443-ddll-retained-managed2/observations/ddll-init-prefix435/comparison.json')['Passed']
failed=read('artifacts/library443-ddll-retained-managed1/observations/visibility438-PhysicalChildFailure/visibility.json')
assert not failed['Passed'] and any('TimeoutException' in s for s in failed['Failures'])
reg=read('artifacts/library443-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==195 and reg['ExactCoverage'] and reg['ImmutableInputs']
boot=read('artifacts/library443-standard79-final2/completion.json')
assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
ledger=read('docs/mfa-compatibility.json');previous=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'],cwd=r))
assert ledger['Entries']==previous['Entries'] and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert managed['RuntimeSha256']==boot['RuntimeSha256']=='19945E1D24B723FB92A0BFEF2B11573C58494B59877F0A461CD983590D3B65E3'
out.mkdir(parents=True)
def copy(src,dst):
    src=r/src;dest=out/dst;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(src,dest)
def tree(base,dest,skip=()):
    for p in sorted((r/base).rglob('*')):
        rel=p.relative_to(r/base)
        if p.is_file() and not set(rel.parts).intersection(skip) and p.suffix in {'.json','.log','.etf','.erl','.c','.h','.cs','.py','.md','.beam'}:
            copy(p.relative_to(r),Path(dest)/rel)
tree('artifacts/library443-ddll-boundary-original1','original60')
tree('artifacts/library443-ddll-boundary-managed2','managed60',{'harness'})
for attempt in [1,2]:
    tree(f'artifacts/library443-ddll-retained-managed{attempt}',f'retained-{attempt}',{'harness'})
tree('artifacts/library443-affected-run1','affected195')
tree('artifacts/library443-standard79-final2','boot79')
tree('artifacts/library443-style-readiness1','style-readiness')
tree('artifacts/library443-ddll-boundary-proposal3','recipe',{'authority'})
for name in ['library443-prepare-package.py','library443-prepare-package2.py','library443-select-affected.py','library443-run-standard79-final2.py','library443-final-guards-style.py','library443-freeze-evidence.py']:
    copy('artifacts/'+name,'root-recipes/'+name)
for folder in ['library443-driver-package2','binary-integration369/library443-current-build1','binary-integration369/library443-retirement-build2']:
    for p in (r/'artifacts'/folder).glob('*'):
        if p.is_file() and p.suffix in {'.json','.log'}:copy(p.relative_to(r),'preparation/'+Path(folder).name+'/'+p.name)
copy('artifacts/library443-driver-package2/package/erldrv-host-package.json','preparation/final-package-manifest.json')
copy('artifacts/library443-affected-selection1/selection.json','affected-selection.json')
save(out/'source-review.json',dict(SourceOnly=True,Reviewer='ddll416',NoBlockers=True,Scope='WhenAny before graceful drain; preserve child disposal, PhysicalExit, PhysicalCleanup, final drain observation and ordered lease release',Path='src/Erlang.Runtime/RuntimeDriverFrontend422.Retirement.cs',Sha256=sha(r/'src/Erlang.Runtime/RuntimeDriverFrontend422.Retirement.cs')))
save(out/'authority-references.json',read('artifacts/library443-ddll-boundary-proposal3/source-bindings.json'))
save(out/'completion.json',dict(Status='LOCAL_SCOPE_ACCEPTED_CI_PENDING',RuntimeSha256=managed['RuntimeSha256'],PackageSha256=managed['PackageSha256'],NewOriginalRows=60,RetainedOriginalRows=36,RetainedNativeGroups=8,AffectedPassed=195,StandardPassed=79,SourceGuards=1618,ApiEntries=708,VerifiedContracts=118,ApiDelta=0,VerifiedDelta=0,InitialRetainedFailurePreserved=True,WholeModuleClaim=False,PhysicalMemoryParityClaim=False,ProviderScansPerBatch=2,Merge442='b90a53e4d4e448109069500ed9952be87c98ae8b',Merge442GreenCi=39864425689))
oldtime=read('artifacts/library443-ddll-retained-managed1/actual-native-visibility438.exit.json')['ElapsedSeconds']
newtime=read('artifacts/library443-ddll-retained-managed2/actual-native-visibility438.exit.json')['ElapsedSeconds']
save(out/'execution-comparison.json',dict(InitialNativeBodySeconds=oldtime,CorrectedNativeBodySeconds=newtime,InitialPassed=False,CorrectedPassed=True,PerformanceBenchmark=False,OriginalScansSourcePlanBefore=9,OriginalScansExecuted=2,ManagedScansSourcePlanBefore=5,ManagedScansExecuted=2,UnexecutedPredecessorTimingClaim=False,OriginalProviderScans=read('artifacts/library443-ddll-boundary-original1/provider-batch-checks.json'),ManagedProviderScans=read('artifacts/library443-ddll-boundary-managed2/provider-batch-checks.json')))
(out/'README.md').write_text('''# Part443 — DDLL rollback boundaries and physical child exit

Pinned OTP29.1.1: ad05823719d77c8faee87348ea39513d4e2f99c5. Actual Windows/x64 native drivers execute on original OTP and our .NET runtime. All60 source-observer results, including complete error stacks/default,0,1,2,8 profiles, match in both whole typed terms and whole ETF files. Eight success/failure × retained/immediate/deeper/both prior-entry scenarios close the stated rollback-boundary gap. The shared Erlang observer explicitly filters loaded_drivers to its five fixture names; this is not a claim of equality for the complete VM driver namespace. There is no comparison-side normalization.

The current prefix rollback already agrees with OTP current dp->next after removals, so that algorithm is unchanged. A genuine retained native regression then failed: deliberate child death left a stable DDLL reader waiting until timeout. Preserve retained-1, including the timeout and actual faults. Retirement awaited drain, while pending linked initialization depended on later physical settlement. The four-line change waits for either drain or actual PhysicalExit; then preserves child.Dispose, PhysicalExit, PhysicalCleanup, pending-owner faulting, final drain observation and ordered environment/dispatcher release. Live-child graceful drain is unchanged; no timeout increase, synthetic process, expected-output rewrite, test exclusion or success conversion is used.

On final Runtime19945E1D, both whole new files/60 rows, retained whole36 comparisons and all8 genuine native/HOST groups pass. Affected195/195 in10 immutable blocks, standard79/79 and actual collector/persistent cleanup pass; build has zero warnings/errors and layout is clean. Physical native8 is own-backend evidence, not an original concurrent transcript. Readiness authority uses the explicitly historical8813 report; it is not relabelled current full execution. Source1618/default708/verified118 unchanged; both API/full-contract deltas are0. Exact publication-head CI and package status belong to the attached PR.

Original and managed batches each perform two complete6427-file provider scans (before and finally-after), while every stage retains bound-input hashing and process-settlement receipts. Values and stacks execute independently after successful preparation; failures aggregate. Earlier orchestration proposed9/5 scans but was not executed, so no before/after runtime speedup is claimed. Actual provider scan and failed/corrected native body durations are retained separately in execution-comparison.json.

Recipe payloads are archived unchanged, with original workspace-relative dependency paths. Licensed authority files and reused legacy helpers remain hash-referenced rather than duplicated; this archive is provenance, not a claim of a turnkey fresh-clone recipe. Source review is passive; all actual compilation/OTP/native execution is Root-owned. No full DDLL, arbitrary callback, platform/ABI or physical ERTS allocation parity is claimed. Our runtime reclaims disconnected worker leases rather than emulating leaked orphan native descriptors. Remaining work includes broader load/unload/reload/monitor/reference-owner partitions and other OS/native profiles. LICENSE.local.txt is excluded.
''',encoding='utf-8')
checkpoint='Part443 scoped local acceptance: original60 whole values/stacks and retained36 match; all8 native/HOST groups,195 affected,boot79 and actual cleanup pass on Runtime19945E1D. Retained first native failure records a PhysicalChildFailure reader timeout. Retirement now waits for drain OR actual PhysicalExit, preserving all physical cleanup/final-drain fences and errors. Source1618/default708/full118 unchanged, deltas0/0. Exact443 CI/packages pending in attached PR. Part442 merged main b90a53e4; merge CI39864425689 green8823/8823 and both packages. Root owns shared execution; LICENSE.local.txt excluded. Evidence:docs/validation/part-443/ddll-boundary-and-physical-exit.'
for name in ['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md']:
    p=r/name;p.write_text(checkpoint+'\n\n'+p.read_text(encoding='utf-8'),encoding='utf-8')
p=r/'docs/NATIVE_API_LIMITATIONS.md';text=p.read_text();text=text.replace('Failed outer initialization combined with removal of the previous list boundary; additional load/unload/reload/monitor partitions and additional platform/native profiles.','Part443 verifies failed/successful linked init with removal of immediate/deeper/both prior fixture boundaries (new original60); real physical-child-loss timeout corrected and retained native8 pass. Remaining broader load/unload/reload/monitor/reference-owner partitions and additional platform/native profiles.')
p.write_text(text+'\nPart443: original60 + retained36 whole comparisons, native8, affected195 and boot79 pass locally on Runtime19945E1D. Initial reader timeout preserved; physical child exit can advance retirement without weakening any cleanup fence. Exact-head443 CI tracked in PR. [Evidence](validation/part-443/ddll-boundary-and-physical-exit/README.md).\n')
p=r/'docs/remaining-work/INDEX.md';text=p.read_text();text=text.replace('**DDLL**: failed outer initialization combined with removing a previous-list boundary; remaining load/unload/reload/monitor partitions;', '**DDLL**: Part443 closes its immediate/deeper/both prior fixture-boundary rollback scope and physical-child-loss waiter timeout; remaining broader load/unload/reload/monitor/reference-owner partitions;')
p.write_text(text+'\nPart442 is merged as main b90a53e4; exact merge CI39864425689 passes8823/8823 and both packages. Part443 local scope and remaining DDLL boundaries: [evidence](../validation/part-443/ddll-boundary-and-physical-exit/README.md). Neither increment promotes whole-module contracts.\n')
p=r/'docs/changelog/0078.md';assert '## Part443 ' not in p.read_text()
entry='''

## Part443 — DDLL current rollback boundaries and physical child exit

- Merge completed442 PR3 into main b90a53e4 after checking PR head d492eff4 against green8823 exact-head CI. Merge CI39864425689 also passes8823/8823, integration and both package/tool consumers. This is Git publication/package validation, not a NuGet release.
- Audit pinned io.c: nested removal updates the outer descriptor current dp->next. Our surviving-prior-row prefix rollback already agrees for the tested mutations; do not invent a production patch for that algorithm. Add a genuine native start/control/stop fixture and identical compiled Erlang observer: eight success/failure × keep/immediate/deeper/both prior-entry controls plus two badarg cases across six profiles,60 complete observations. Compare both entire typed/raw files. The Erlang observer explicitly projects driver names to five fixture-owned names; no whole VM driver namespace equality or comparison normalization is claimed.
- Build a fresh current DriverHost with exact matching parent assemblies, authenticated real three-lane parent input and actual native bridge compile/source proof. First new60 and original provider6427 checks pass. Repeating the retained native8 exposes a genuine PhysicalChildFailure timeout: retirement waits for native owners which depend on later physical settlement. Preserve this failed report rather than excluding it or increasing its deadline.
- Change only the pre-disposal retirement wait to drain OR actual physical process exit. Keep all child disposal/PhysicalExit/PhysicalCleanup fences, final drain fault observation and ordered lease release. On Runtime19945E1D the actual native body passes all8 groups in4.328s; the preserved failing body took21.859s. These are diagnostic outcomes, not a performance benchmark. Retained36 and new60 match in whole typed/raw values/stacks; actual child completion, physical cleanup and zero owner leases pass.
- Reduce proposed original/managed provider scans from9/5 to2 per complete batch, with the after scan in finally. Keep per-stage input hashing, independent values/stacks, dependent preparation gates and aggregate failures. Predecessor recipes were not executed; claim no measured before/after orchestration speedup. All payload bytes and actual scan durations are retained.
- Final195/195 affected checks in10 immutable blocks, standard79/79 and actual collector/persistent cleanup pass. Build has zero warnings/errors, full layout is clean and source/evidence readiness passes using authentic historical8813 authority. Only one runtime source guard changes;1618 sources,708 registered APIs and118 verified contracts remain, deltas0/0. Existing8823 catalog is not relabelled local full execution; exact publication-head CI and package results are tracked in the attached PR.
- Publish [bounded evidence and the retained failure](../validation/part-443/ddll-boundary-and-physical-exit/README.md), current remaining-work index and module checkpoint. Remaining DDLL mode/reference-owner/platform/native profiles stay open; no whole-module or physical ERTS allocation parity promotion. Preserve LICENSE.local.txt uncommitted.
'''
assert len((p.read_text()+entry).encode('utf-8'))<32768
p.write_text(p.read_text()+entry)
files=[dict(Path=f.relative_to(out).as_posix(),Bytes=f.stat().st_size,Sha256=sha(f)) for f in sorted(out.rglob('*')) if f.is_file()]
save(out/'manifest.json',files)
print(json.dumps(dict(Passed=True,EvidenceFiles=len(files)+1,Affected=195,NewOriginal=60,RetainedOriginal=36,NativeGroups=8,Standard=79)))

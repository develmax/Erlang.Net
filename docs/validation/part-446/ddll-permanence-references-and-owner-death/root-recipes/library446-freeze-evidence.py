from pathlib import Path
import ast,gzip,hashlib,json,subprocess
r=Path.cwd();out=r/'docs/validation/part-446/ddll-permanence-references-and-owner-death';assert not out.exists()
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
read=lambda p:json.loads((r/p).read_bytes())
runtime='079C9FC8450B7AE109826283FE2257D1CA3271C06999ECA5133E16DCAEE81869'
original=read('artifacts/library446-ddll-frontier-original1/completion.json');assert original['Passed'] and original['WholeRows']==36 and original['NativeFixturesCompiled']==0
managed=read('artifacts/library446-ddll-frontier-managed1/completion.json');assert managed['Passed'] and managed['RuntimeSha256']==runtime and managed['CompleteProviderScans']==2
comparison=read('artifacts/library446-ddll-frontier-managed1/observations/ddll-frontier446/comparison.json');assert comparison['Passed'] and comparison['WholeMainRows']==36 and len(comparison['Groups'])==6
for row in comparison['Groups']:assert all(row[k] for k in ['Passed','WholeTypedEqual','WholeRawEqual','OwnProvenanceValid','PhysicalExit','PhysicalCleanup','ChildCompletion']) and not row['Failures']
assert read('artifacts/library446-retained445-managed1/observations/ddll-final-modes445/comparison.json')['Passed']
assert read('artifacts/library446-retained444-managed1/observations/ddll-modes444/comparison.json')['Passed']
assert read('artifacts/library446-ddll-retained-managed1/observations/ddll-init-prefix435/comparison.json')['Passed']
native=read('artifacts/library446-ddll-retained-managed1/observations/visibility-all.json');assert native['Passed'] and len(native['Results'])==8
reg=read('artifacts/library446-affected-run1/completion.json');assert reg['Passed'] and reg['Examined']==195 and reg['ExactCoverage'] and reg['ImmutableInputs']
boot=read('artifacts/library446-standard79-final1/completion.json');assert boot['ChildPassed'] and boot['RuntimeUnchanged'] and boot['RuntimeSha256']==runtime and boot['comparison.json']['Equal']==79 and boot['collector-lifecycle.json']['Passed'] and boot['persistent-seven-lifecycle.json']['Passed']
assert read('artifacts/library446-style-readiness1/completion.json')['Passed']
review=read('artifacts/library446-independent-final-review1/review.json');assert review['Passed'] and review['WholeRows']==36 and not review['ProductionMismatchFoundInThisCohort']
ledger=read('docs/mfa-compatibility.json');old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'],cwd=r));assert ledger==old and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
for row in ledger['VerifiedSourceFiles']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tests','tools'],cwd=r)
# Reuse only the exact already published, exercised lossless archive helpers.
helpers=r/'artifacts/library445-freeze-evidence.py';published=r/'docs/validation/part-445/ddll-permanent-reload-force/root-recipes/library445-freeze-evidence.py'
assert sha(helpers)==sha(published)
functions=[n for n in ast.parse(helpers.read_text()).body if isinstance(n,ast.FunctionDef) and n.name in {'save','copy','tree'}];assert len(functions)==3
exec(compile(ast.Module(body=functions,type_ignores=[]),str(helpers),'exec'))
out.mkdir(parents=True);archive=[]
for source,dest,skip in [
 ('artifacts/library446-ddll-frontier-original1','original36',{'drivers'}),
 ('artifacts/library446-ddll-frontier-managed1','managed36',{'harness'}),
 ('artifacts/library446-retained445-managed1','retained60',{'harness'}),
 ('artifacts/library446-retained444-managed1','retained72',{'harness'}),
 ('artifacts/library446-ddll-retained-managed1','retained36-native8',{'harness'}),
 ('artifacts/library446-affected-run1','affected195',set()),
 ('artifacts/library446-standard79-final1','boot79',set()),
 ('artifacts/library446-style-readiness1','style-readiness',set()),
 ('artifacts/library446-ddll-frontier-proposal1','pre-execution-draft1',{'authority'}),
 ('artifacts/library446-ddll-frontier-proposal2','lexical-successor2',{'authority'}),
 ('artifacts/library446-ddll-frontier-proposal3','executed-recipe3',{'authority'}),
 ('artifacts/library446-ddll-frontier-plan1','source-plan',set()),
 ('artifacts/library446-independent-review1','preparation-review',set()),
 ('artifacts/library446-independent-final-review1','independent-final-review',set())]:tree(source,dest,skip)
for name in ['library446-main445-green-receipt.json','library446-prepare-package1.py','library443-prepare-package.py','library446-prepare-capture3.py','library446-select-affected.py','library446-run-standard79.py','library446-style-readiness.py','library446-freeze-evidence.py','library446-stage-audit.py']:copy('artifacts/'+name,'root-recipes/'+name)
copy('artifacts/library446-affected-selection1/selection.json','affected-selection.json')
for folder in ['library446-driver-package1','binary-integration369/library446-current-build1']:
 for p in sorted((r/'artifacts'/folder).glob('*')):
  if p.is_file() and p.suffix in {'.json','.log'}:copy(p.relative_to(r),'preparation/'+Path(folder).name+'/'+p.name)
copy('artifacts/library446-driver-package1/package/erldrv-host-package.json','preparation/final-package-manifest.json')
save(out/'archive-map.json',archive)
save(out/'completion.json',dict(Status='LOCAL_SCOPE_ACCEPTED_EXACT_HEAD_CI_TRACKED_IN_PR',RuntimeSha256=runtime,PackageSha256=managed['PackageSha256'],NewOriginalRows=36,OwnRawRows=36,RetainedOriginalRows=168,RetainedNativeGroups=8,AffectedPassed=195,StandardPassed=79,SourceGuards=1618,ApiEntries=708,VerifiedContracts=118,ApiDelta=0,VerifiedDelta=0,ProductionFilesChanged=0,ReportedDifferences=0,NewCanonicalTestCount=0,WholeModuleClaim=False,Merge445='a72f69032c69796db263b04f11c3985de5e1bfe4',Merge445GreenCi=39922245233))
cases=read('artifacts/library446-ddll-frontier-proposal3/profile.json')['Cases'];assert len(cases)==6
table='\n'.join('| `'+name+'` | Matches in all six profiles |' for name in cases)
(out/'README.md').write_text('''# Part446 — DDLL permanence, reference origins and two-owner death

Pinned OTP29.1.1: ad05823719d77c8faee87348ea39513d4e2f99c5. Part445 PR6 exact head1bf3fa5d passes8823/8823 and both packages, and is merged as main a72f6903. Exact merge CI39922245233 also passes8823/8823, mandatory steps and both package/consumer checks. This is Git publication and CI package validation, not a NuGet release.

## All six prepared scenarios executed

'''+table+'''

The identical new Erlang observer is compiled by the admitted original compiler and executed unchanged on pinned OTP and our actual native parent/DriverHost runtime. All36 whole source results match in both complete typed terms and all six entire main ETF files (values/default/0/1/2/8). No discrepancy was found in this cohort; no forced production edit, source-guard change or compatibility promotion follows. The original capture and managed consumer each perform two complete uncached6427 provider scans, before/finally-after, plus immutable-input checks and independently settled profile routes after successful preparation.

Repeated permanence uses the genuine existing permanent_drv445.dll, authenticated against its actual Passed60 original445 receipt and compile evidence (SHA29AD30F4EC0985BBACD61E918280B41792866394484184029600B6B718C11C06). The echo DLL is authenticated against original420. Both byte-identical DLLs and stock facade are reused: zero new native compilations and no old228/60 original VM rerun. A helper locks its actual port twice, closes its last port, reports actual permanent state and exits. Only after its real DOWN does another actor open a distinct real port and call the native control again. Source port_count remains the atom permanent, never an invented numeric zero; permanent never waits for unloaded. Each profile owns a fresh real original VM and managed Runtime/DriverHost.

The queued reference roundtrip uses actual term_to_binary/binary_to_term BIFs and creator-owned demonitor; the image-generation case completes a real unload before loading the same exact DLL again and demonstrates an old completed monitor cannot remove the new one. This is native driver image generation, not VM creation/node-identity transition. The external-reference case decodes a well-formed explicitly fixed NEWER_REFERENCE_EXT input (node ddll446@external,creation7,words123/456/789). Its complete original badarg result, BIF argument/reference and full profile-dependent stacks match. At depth0 the actual empty stack is retained, not replaced. Only this source-declared fixed external identity may appear in main; moving VM refs/PIDs remain own-only. This is public protocol-input testing, not proof of a live remote producer or peer interoperability.

Before either owner-stop request, source info(processes) proves exactly two distinct loaded owners with one reference each; their actual ordered source-role/count rows enter both main results. Both stop messages are sent before joining either DOWN. The ordinary case proves the observer-connected real port survives via echo before final close/unloaded; the kill_ports case joins actual port EXIT and driver unloaded by role. Actual cross-producer arrival streams remain in complete own raw sidecars, without imposing a total-order promise. This is two real owner requests without an interposed wait, not proof of physically simultaneous native callbacks. Pinned EXIT_FREE DDLL cleanup precedes process DOWN; no sleeps or absence polling are used.

Each of six managed groups preserves complete actual main/own ETF and typed data before row assertions and proves PhysicalExit, PhysicalCleanup, Completion and actual zero leases. Independent review authenticates original receipts, source/BEAM/native identity, all raw equalities, owner prerequisites and cleanup. Complete own observations preserve actual refs/PIDs/ports/info/messages; their opaque bits are not compared across independent VMs. There is no golden construction, consumer projection/normalization or rewriting of expected values/errors/stacks.

## Final regression and delivery

Final Runtime079C9FC8 additionally passes prior60+72+36 complete original comparisons, all8 native/HOST groups, affected195/195 in10 immutable blocks and standard79/79 with actual collector/persistent cleanup. Thus204 source-profile results are replayed on final assemblies:36 new and168 retained. Build has zero CLR warnings/errors; original observer unused-helper warnings remain visible. Source layout needs zero changes. Source1618/default708/full118 and ledger classifications remain unchanged, API/full deltas0/0. These standalone36 are not36 new canonical test registrations; catalog remains8823. Historical8813 readiness authority is distinct from publication-head full CI, whose exact SHA/results are recorded in the attached PR.

## Preparation history, provenance and remaining scope

Draft1 was never compiled/executed; its Wire=<< lexical ambiguity was corrected before execution in immutable successor2. Root successor3 adds the genuine two-owner setup fence and source-role observations for the same two cases. Copied successor-delta.json describes historical successor2, not the entire proposal3 delta; root-pre-execution-review.json/README record proposal3 separately. No preparation correction is reported as a production bug or an observed compiler/VM failure. A stalled earlier helper was replaced; only Root executed compiler/native/VM/git/CI work.

Large JSON is stored losslessly as deterministic gzip. archive-map.json binds stored and original raw hashes/lengths; Git-index audit decompresses every receipt and verifies all source/evidence bytes. Vendor authorities and prior native binaries are hash-referenced rather than redundantly copied. Recipes retain workspace-relative authority/helper dependencies: this is provenance, not turnkey fresh-clone replay. LICENSE.local.txt remains excluded.

Not closed: actual VM node-creation transitions, live peer/inter-Runtime reference interoperability, held native teardown interleavings, broader reload/demonitor/force combinations and other OS/ABI/native-thread/physical profiles. Non-Windows/x64 and AOT/single-file ownership are outside the admitted current backend. Full DDLL contracts and adjacent erlang port APIs are not promoted by these six successful scenarios.
''',encoding='utf-8')
checkpoint='Part446 scoped local acceptance: all six prepared DDLL scenarios match pinned OTP in36 whole source-profile results, with complete fixed-external-reference stacks and separate own36 raw logs. Repeated genuine permanence/close/reopen, actual ref ETF roundtrip/image generation and two-owner death with/without kill_ports pass. Root before-stop snapshot proves two actual owners count1. No production/source-guard change or discrepancy. Retained60+72+36/native8, affected195,boot79 and real cleanup pass on Runtime079C9FC8. Source1618/default708/full118 unchanged, API/full deltas0/0; standalone36 are not36 canonical tests. Exact publication CI/packages tracked in attached PR. Part445 merged main a72f6903; merge CI39922245233 green8823/8823 and both packages. LICENSE.local.txt excluded. Evidence:docs/validation/part-446/ddll-permanence-references-and-owner-death.'
docs=['AGENTS.md','docs/WORK_PLAN.md','docs/API_DELIVERY_PACKAGES.md','docs/NATIVE_API_LIMITATIONS.md','docs/remaining-work/INDEX.md']
for name in docs[:3]:
 p=r/name;p.write_text(checkpoint+'\n\n'+p.read_text(encoding='utf-8'),encoding='utf-8')
p=r/docs[3];text=p.read_text().replace('checkpoints through Part445','checkpoints through Part446');oldrow=next(line for line in text.splitlines() if line.startswith('| `erl_ddll` |'))
text=text.replace(oldrow,'| `erl_ddll` | Part445 ordering fix retained; Part446 repeated permanence, real reference roundtrip/image generation, fixed external input and two-owner death | All36 new complete source-profile results/whole raw files match; prior60+72+36/native8, affected195 andboot79 pass locally with actual physical cleanup. No Part446 product change or API promotion. Part445 merge a72f6903 CI39922245233 green8823/8823 and packages; exact446 CI tracked in PR. | Actual VM creation/peer reference interoperability, held native teardown interleavings, broader option/reload/demonitor/force cross-products and additional OS/ABI/physical profiles. |')
p.write_text(text+'\nPart446 exact scope/results: [six DDLL frontier scenarios](validation/part-446/ddll-permanence-references-and-owner-death/README.md). Source/default/full classifications unchanged.\n',encoding='utf-8')
p=r/docs[4];text=p.read_text().replace('Published main ef3840c5 includes Parts442-444; exact merge CI39914974411 passes8823/8823 and both packages. Part445 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.','Published main a72f6903 includes Parts442-445; exact merge CI39922245233 passes8823/8823 and both packages. Part446 scoped local acceptance is recorded separately; its exact publication-head CI is linked from its pull request.')
text=text.replace('remaining broader generation/reference/owner races, repeated permanent transitions and additional force cross-products;', 'Part446 verifies its six repeated-permanence/reference-origin/image-generation/two-owner scenarios (new36 complete source-profile results); remaining actual VM node-creation/live-peer references, held native teardown interleavings and broader reload/demonitor/force combinations;')
p.write_text(text+'\nPart446 evidence: [six DDLL scenarios](../validation/part-446/ddll-permanence-references-and-owner-death/README.md). No full-module/API promotion.\n',encoding='utf-8')
entry='''

## Part446 — DDLL repeated permanence, reference origins and two-owner death

- Verify PR6 exact445 head1bf3fa5d green CI39920284895, merge into main a72f6903, then verify merge CI39922245233:8823/8823, mandatory steps and both package/consumer checks. Git publication/CI package validation is not a NuGet release.
- Execute all six prepared scenarios in six isolated original OTP VM and managed Runtime/DriverHost profiles: repeated actual native lock/last-port close/original-owner exit/reopen; real reference ETF roundtrip; completed monitor across genuine native unload/reload; a fixed external ETF reference with complete badarg stacks; two loaded owners stopping before either DOWN is joined, with and without persistent kill_ports. Source proves two actual owners count1 before either stop. Preserve actual per-role state and all own raw identities/arrival streams, not a cross-producer ordering promise.
- Reuse authenticated permanent445 and echo420 DLL bytes and stock facade, compiling only the new identical Erlang observer. All36 whole main results and all six entire ETF files match; each managed child proves physical exit/cleanup/completion and zero leases. Independent review verifies source/BEAM/native reuse, actual owner prerequisites and complete raw/stack comparisons. No DDLL difference or production edit is justified by this cohort.
- Preserve unexecuted preparation history: immutable successor2 fixes lexical spacing; Root successor3 adds the genuine two-owner setup fence. Neither is relabelled a product fix or observed execution failure. Only Root runs actual compiler/native/VM/git/CI work; replace a helper that did not start. Exactly two complete6427 provider scans bound each new batch, with immutable source checks and independent captures.
- Final Runtime079C9FC8 passes retained60+72+36 original comparisons, all8 native/HOST groups, affected195/195 in10 immutable blocks and standard79/79 with actual collector/persistent cleanup. CLR build has zero warnings/errors, source layout is clean, original unused-helper warnings remain visible. Historical8813 readiness evidence is not relabelled a current full run.
- Source1618/default708/verified118 and classifications remain unchanged; API/full deltas0/0, standalone36 add no canonical test registrations. Exact publication-head full CI/package outcomes are tracked in its PR. Publish [complete proof and bounded remaining scope](../validation/part-446/ddll-permanence-references-and-owner-death/README.md) with lossless JSON compression and stored/raw Git-index audit. Remaining actual VM/peer identity, held native interleavings, broader mode combinations and platform/ABI profiles stay separate. Preserve LICENSE.local.txt uncommitted.
'''
p=r/'docs/changelog/0078.md';assert '## Part446 ' not in p.read_text()
combined=p.read_text()+entry
if len(combined.encode('utf-8'))<=32768 and len(combined.splitlines())<=400:
 p.write_text(combined,encoding='utf-8');docs.append('docs/changelog/0078.md')
else:
 p=r/'docs/changelog/0079.md';assert not p.exists();p.write_text('# Detailed work changelog — volume0079\n'+entry,encoding='utf-8');docs.append('docs/changelog/0079.md')
 index=r/'CHANGELOG.md';text=index.read_text();old='- [0078.md](docs/changelog/0078.md) — part440 onward (active; readiness publication and remaining native contracts)';assert old in text
 text=text.replace(old,'- [0078.md](docs/changelog/0078.md) — parts440–445 (closed; readiness publication and native contracts)')
 index.write_text(text+'\n- [0079.md](docs/changelog/0079.md) — part446 onward (active; DDLL frontier contracts)\n',encoding='utf-8');docs.append('CHANGELOG.md')
save(r/'artifacts/library446-publication-paths.json',dict(DocumentPaths=docs,EvidenceDirectory=out.relative_to(r).as_posix(),LicenseLocalExcluded=True))
files=[dict(Path=p.relative_to(out).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(out.rglob('*')) if p.is_file()];save(out/'manifest.json',files)
print(json.dumps(dict(Passed=True,Files=len(files)+1,RawArchiveFiles=len(archive),OriginalNew=36,OriginalRetained=168,NativeGroups=8,Affected=195,Standard=79,CompressedJson=sum(x['Gzip'] for x in archive),RawBytes=sum(x['RawBytes'] for x in archive),StoredBytes=sum(x['Bytes'] for x in archive),Documents=docs)))

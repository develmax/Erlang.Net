from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();folder=r/'docs/remaining-work';target=folder/'architecture-and-compatibility-queue.md';assert not target.exists()
ledger_path=r/'docs/mfa-compatibility.json';ledger=json.loads(ledger_path.read_bytes());known=[x for x in ledger['Entries'] if x['Status']=='Known differences'];pending=[x for x in ledger['Entries'] if x['Status']=='Needs verification'];assert len(known)==31 and len(pending)==559
sha=hashlib.sha256(ledger_path.read_bytes()).hexdigest().upper();names=['%s:%s/%s'%(x['Module'],x['Function'],x['Arity']) for x in known]
table='\n'.join('| `'+name+'` | Open revalidation task; current reproduction not yet established |' for name in names)
text='''# Architecture and compatibility queue — recorded at the Part450 safe boundary

Status: **planned**, not implemented or newly verified by this documentation supplement. Root is the sole owner of shared runtime/build/project/git changes. This supplement records coordinated owner requirements without interrupting the completed450 functional gates or performing a bulk migration. It extends the existing backend-selection plan rather than replacing working alternatives.

## P0 — eliminate known observable semantic differences

The unchanged compatibility ledger currently classifies31 MFAs as Known differences,118 as Verified compatible and559 as Needs verification. This is a ledger inventory, not proof that every historical difference still reproduces on current binaries. Nearest owner operation after publishing450: revalidate each named row with actual backend, specific cause/input and minimal reproduction; bind pinned original/source evidence and current source/DLL hashes. Record resolved/stale/open/blocked with the exact reason and proof. A stale label must be demonstrated, not assumed.

Group the observations by shared runtime dependencies and first fix genuinely reproduced semantic errors against pinned OTP29.1.1. Maintain a permanent differential regression for every applicable C# and BEAM backend, including complete values, errors/stacks, side effects and lifecycle metadata. Newly discovered differences enter the same inventory and are corrected or retain a factual blocker. Unsupported valid behavior cannot simply be renamed a permissible limitation.

Do not remove cases, weaken comparisons, silently switch backend, delete alternatives or merely reclassify rows to obtain a cosmetic zero. Preserve unstable or VM-owned native data and use explicit source/own-state protocols where byte equality is not a meaningful contract; never fabricate equality.

Acceptance: **zero unresolved known, currently observed semantic differences in the revalidated inventory**, supported by current source/DLL and original proofs. This does not close the559 Needs verification entries and is not a whole-OTP claim. No row is resolved by this plan update.

### Named baseline awaiting current revalidation

The following rows are copied by identity only from `../mfa-compatibility.json`; their existing Scope/Limits/Evidence remain there. All tasks below are open; current reproductions and backend-specific evidence are still to be collected. Baseline ledger SHA256: `LEDGER_SHA`.

| MFA | Current queue state |
| --- | --- |
KNOWN_TABLE

## A1 — in-repository pinned original OTP snapshot

Already exists: selected original `.erl`/`.beam` files and `provenance.json` under `examples/HelloOtpLibraries/Otp`, additional source/image fixtures under `tests/Erlang.Tests/Resources`, and previous immutable validation evidence. These existing partial snapshots must be reused/audited, not duplicated blindly. They do not establish an audited complete self-contained closure for every loaded original module. A moving `../otp`, URL/latest reference, ignored artifact or download-only preparation is insufficient.

Planned: keep the actually used original sources and required dependency closure **tracked in this repository**, pinned to OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Record origin/upstream paths, applicable licenses/notices and SHA manifests. For every actually loaded BEAM choose tracked pinned image bytes or reproducible generation **from the tracked sources**, with pinned compiler/toolchain/options and loaded-image hash verification. Keep runtime/BIF/native dependency boundaries explicit.

Acceptance: a fresh checkout contains the source snapshot; drift/tamper is refused; reproducible compilation/load preserves exact code identity without reading a mutable upstream checkout. This full closure/build guarantee is planned, not credited by existing selected examples or450 tests.

## A2 — separate C# module libraries in this same repository

Already exists: `Erlang.Otp.Behaviours` and the current Runtime/Terms/Serialization/Compiler/MSBuild projects; explicit `RuntimeModuleSelection`, `RuntimeModuleBackend` and per-module startup ownership; `build/Erlang.Library.targets` and `ErlangLibrary` examples. These do not by themselves mean every C# module is already extracted into its own library.

Planned: define C# module library/project boundaries and runtime/native/host dependencies in **this repository**. Preserve existing implementations and reuse per-module selection/ErlangLibrary. One application/runtime may mix different BEAM and C# modules, while one module identity has exactly one explicitly selected pure backend. Refuse conflicting identities/dependencies deterministically; capture native stubs and provenance explicitly. No hidden fallback or implicit hot-state migration, and no promise that conflicting implementations of one identity execute simultaneously.

Acceptance: managed-only, BEAM-only and mixed consumers; conflict and transitive-dependency controls; API parity and cold PackageReference/package/publish validation. Extraction remains planned.

## A3 — incremental subsystem/project structure

Already exists: separate foundational projects, while many namespace-level source types remain directly in project roots. This supplement does not move them.

Planned: Root first records a target subsystem/subdirectory tree and dependency graph, then makes small reversible owned batches after the functional checkpoint. Update csproj/solution/imports/resources/generators, examples/documentation links and source guards for each actual batch. Preserve public namespaces, APIs, behavior, package contracts and alternatives; do not combine this work with speculative runtime changes.

Acceptance per real change: exact semantic delta0; build plus affected/full gates; clean/rebuild/incremental and cold PackageReference/publish; no cycles, lost resources or broken links. Update manifests only from the verified batch baseline. No bulk migration or project extraction is authorized by this documentation-only supplement.

## Owner order and boundaries

1. Finish450 publication/exact-head CI without interrupting its gates.
2. Root refreshes the31 named current reproductions/backend/source/DLL proofs, groups common dependencies and fixes observed semantic differences first. Helpers may audit isolated evidence; shared changes stay with Root.
3. Establish the canonical pinned source/image closure and reproducible load/build contract by reusing existing tracked snapshots.
4. Design/extract C# module libraries with explicit mixed-backend dependencies.
5. Carry out the recorded subsystem layout in small verified batches after the relevant functional checkpoint.

These are queued obligations, not implementation or compatibility credit. Existing full-contract classifications remain unchanged until their own acceptance is met.
'''.replace('LEDGER_SHA',sha).replace('KNOWN_TABLE',table)
target.write_text(text,encoding='utf-8')
work=r/'docs/WORK_PLAN.md';s=work.read_text();marker='## Post-450 owner priority and repository architecture — planned';assert marker not in s
section='''## Post-450 owner priority and repository architecture — planned

The four coordinated owner requirements are recorded in the [single architecture/compatibility queue](remaining-work/architecture-and-compatibility-queue.md), including existing facilities,31 named ledger identities and acceptance criteria. This is planning only:450 adds no complete OTP snapshot, module-library extraction, directory migration or resolved known difference.

- **P0: zero unresolved observed semantic differences.** Revalidate all31 Known differences by MFA/backend/cause/input/reproduction and pinned original/current source/DLL proofs; use resolved/stale/open/blocked status, group shared runtime dependencies and fix real semantic errors first. Add permanent applicable C#/BEAM differential tests for full values/errors/side effects/lifecycle metadata. No deleted cases, relaxed comparisons, hidden backend switch or cosmetic reclassification; handle own-VM data honestly. Zero revalidated observed differences does not close559 Needs verification.
- **A1: original sources in this repository.** Reuse existing selected tracked source/image examples, then pin the actual full required closure to OTP29.1.1/ad058237 with origin paths, licenses/notices and SHA manifests. Actually loaded BEAM must be tracked pinned bytes or reproducibly built from tracked sources with pinned compiler/options and loaded-hash checks. Fresh checkout, drift/tamper refusal and exact-identity load without mutable upstream access are required; external checkout/download-only is insufficient.
- **A2: separate C# libraries here.** Preserve implementations; define module/runtime/native/host project dependencies and reuse per-module selection/ErlangLibrary. Mix different C#/BEAM modules in one runtime, one explicit pure backend per identity; deterministic conflicts, explicit native/dependency/provenance capture, no hidden fallback/hot-state migration. Require managed-only/BEAM-only/mixed, parity and cold package/publish gates.
- **A3: subsystem layout in small batches.** Root owns target tree/dependency graph and reversible migration after the functional checkpoint; update project/solution/import/resource/generator/example/doc/guard references. Preserve public namespaces/API/behavior/package contracts and alternatives. Require semantic delta0, affected/full/clean/rebuild/incremental/cold package/publish, no cycles/lost resources/broken links and manifests updated only on verified baseline.

'''
work.write_text(section+s,encoding='utf-8')
index=r/'docs/remaining-work/INDEX.md';s=index.read_text();insert='''## Nearest owner queue after Part450

The [architecture and compatibility queue](architecture-and-compatibility-queue.md) is the authoritative single supplement for these four **planned** obligations. Current ledger count31 Known differences is confirmed, not newly reproduced or resolved; selected original source/image examples and backend selection already exist, while complete snapshot/library extraction/layout acceptance remains open.

1. Revalidate the31 named differences on current C#/BEAM backends with original/source/DLL evidence; group dependencies and fix observed semantic errors first. Target zero unresolved observed differences without weakening tests, hiding backend changes or claiming the559 unverified contracts complete.
2. Complete the tracked pinned OTP source/loaded-image closure and reproducible fresh-checkout/drift-refusal contract.
3. Extract explicit C# module libraries in this repository with managed-only/BEAM-only/mixed and cold package validation.
4. Migrate subsystem directories/project references in small Root-owned semantic-delta0 batches after the functional checkpoint.

No implementation or compatibility credit is granted by this queue entry;450 functional verification remains separate.

''';needle='## Immediate native scope';assert needle in s;s=s.replace(needle,insert+needle,1);index.write_text(s,encoding='utf-8')
p=r/'docs/changelog/0079.md';s=p.read_text();s+='\n- Documentation-only owner-plan supplement at the completed450 safe boundary: record the31 named ledger differences for first-priority current-backend revalidation/fixes, canonical in-repository OTP snapshot/BEAM provenance, C# module libraries in the same repository and incremental subsystem layout. Existing selected tracked source/image examples, foundational projects and selector/ErlangLibrary are acknowledged; complete closure/extraction/migration and difference resolution remain planned with explicit fresh-checkout/conflict/cold-build/semantic-delta0 acceptance. No code move or ledger status change. [Single queue](../remaining-work/architecture-and-compatibility-queue.md).\n';assert len(s.encode('utf-8'))<32768;p.write_text(s,encoding='utf-8')
receipt={'Passed':True,'DocumentationOnly':True,'KnownLedgerRows':31,'NeedsVerification':559,'MfaLedgerSha256':sha,'NewResolvedDifferences':0,'CodeMoves':0,'NewRuntimeImplementation':False,'Files':['docs/WORK_PLAN.md','docs/remaining-work/INDEX.md',str(target.relative_to(r)),'docs/changelog/0079.md'],'Sections':['P0','A1','A2','A3']}
(r/'artifacts/library450-plan-supplement.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))

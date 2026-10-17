"""Prepare an artifact overlay only, after Root accepts an independently audited full353.

Does not run the SDK, readiness, network, Git mutations, or modify Main/candidate.
The required explicit notice is an execution precondition, not a full-result substitute.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path, PurePosixPath
import subprocess

ROOT = Path(__file__).resolve().parents[2]
PARENT = '0e739220c2d5d7afc66b6fb94c1f2b15d156bd90'
COMPOSITION = 'artifacts/scheduler-affinity353-composition-plan2/composition-plan.json'
COMPOSITION_SHA = '3F8C5F95912959D7BE22BDDA3E6F39B696AC644EA98E5461C6B67C64771F06BC'
META_PLAN = 'artifacts/affinity353-metadata-overlay5/copy-plan.json'
META_SHA = 'EE35785321063171DAE0648AEA211C7D9B2D98808DA5525762EECAC663332BE1'
CANDIDATE = ROOT / 'artifacts/scheduler-affinity353-full-candidate2'
PRESERVE = ['docs/API_COMPATIBILITY_INDEX.md', 'docs/ERLANG_COMPATIBILITY.md']
REPRODUCTION_PINS = [{'Path': 'artifacts/root-affinity353-full.py', 'Bytes': 4481, 'Sha256': '104701AA7012E7AC120CEC668257F062CC0EA2AF8D10E723575255D511598400'}, {'Path': 'artifacts/spawn-priority351-preparation/execute-root.py', 'Bytes': 7652, 'Sha256': 'C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'}, {'Path': 'artifacts/scheduler-affinity353-full-independent/source-freeze.json', 'Bytes': 572, 'Sha256': 'E914C5D98CB5AF11A2787EAE8F33F3A5DB5E4562E75726950DE2DA197FAA57F1'}, {'Path': 'artifacts/scheduler-affinity353-full-independent/root-source-approval.json', 'Bytes': 476, 'Sha256': 'FF54B251A1EAD8D045FEA284510C4C6295A952F6A4A6E827FDF8B68243124252'}, {'Path': 'artifacts/scheduler-affinity353-full-independent/source-schema-followup.json', 'Bytes': 1854, 'Sha256': '1FC1DB8686373CD2CC6B71B0AC40311424FC9CFEA28FB9D37C343BB994A9193E'}, {'Path': 'artifacts/scheduler-affinity353-full-runner-review/report.json', 'Bytes': 2861, 'Sha256': 'BCCAA7D62E4D8641E431B2EC466E4FD6B5DEEB0E4ED2FE6C2A01C9828009F316'}, {'Path': 'artifacts/root-affinity353-full2.py', 'Bytes': 5721, 'Sha256': 'CC57902DB918ECD79474696EDD91FDFDB5985AFB0762D513392A5A62B720CB63'}]

def digest(b): return hashlib.sha256(b).hexdigest().upper()
def read_json(p): return json.loads(p.read_bytes())
def encoded(v): return (json.dumps(v, ensure_ascii=False, indent=2)+'\n').encode()
def safe(path):
    assert '\\' not in path and not PurePosixPath(path).is_absolute()
    assert '..' not in PurePosixPath(path).parts and ':' not in path
    return path
def source(row):
    path = ROOT / safe(row['SourcePath'])
    b = path.read_bytes()
    assert len(b) == row['Bytes'] and digest(b) == row['Sha256'], row['SourcePath']
    return b
def current_head():
    return subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--root-completion-notice', required=True, choices=['FULL353_2_TERMINAL_AND_INDEPENDENT_PASS_ACCEPTED'])
parser.add_argument('--independent-guard', type=Path, required=True)
parser.add_argument('--independent-guard-sha256', required=True)
parser.add_argument('--full-directory', type=Path, required=True)
parser.add_argument('--full2-reader', type=Path, required=True)
parser.add_argument('--full2-controls', type=Path, required=True)
parser.add_argument('--full2-controls-sha256', required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()

# No actual result is read until this script is explicitly invoked with Root's notice.
assert current_head() == PARENT
guard_path = args.independent_guard.resolve(strict=True)
full = args.full_directory.resolve(strict=True)
assert guard_path.is_relative_to(ROOT/'artifacts') and full.is_relative_to(ROOT/'artifacts')
assert digest(guard_path.read_bytes()) == args.independent_guard_sha256.upper()
guard = read_json(guard_path)
controls_path=args.full2_controls.resolve(strict=True)
assert controls_path.is_relative_to(ROOT/'artifacts') and digest(controls_path.read_bytes())==args.full2_controls_sha256.upper()
controls=read_json(controls_path)
assert controls['Files'] and len({r['Path'] for r in controls['Files']})==len(controls['Files'])
for row in controls['Files']:
    control=ROOT/safe(row['Path']);data=control.read_bytes()
    assert len(data)==row['Bytes'] and digest(data)==row['Sha256']
full2_reader=args.full2_reader.resolve(strict=True)
assert full2_reader.is_relative_to(ROOT/'artifacts') and digest(full2_reader.read_bytes())==guard['ReaderSha256']
assert any(r['Path']==full2_reader.relative_to(ROOT).as_posix() and r['Sha256']==guard['ReaderSha256'] for r in controls['Files'])
assert guard['Status'] == 'PASS_INDEPENDENT_ACTUAL_CORRECTED_CANONICAL353_FULL2_RAW_REGISTRY_RESTORATION_PENDING'
assert guard['Passed'] == 7313 and guard['Failed'] == 0
assert guard['CompositionPlanSha256'] == COMPOSITION_SHA
assert guard['OwnedPaths'] == 169 and guard['StableClosureRows'] == 3106
assert guard['Old7297ExactPrefix'] and len(guard['AddedTestNames']) == 16
assert len(guard['IntegrationResults']) == 11 and len(guard['Packages']) == 2
assert guard['PackageInputs'] == 6 and guard['RuntimeSourceGuards'] == 89
assert guard['RegistryRows'] == 587 and guard['TermOrderChildren'] == 12
assert guard['RegistryNonLimitsAndOrderPreserved'] and guard['ChildMatrixVerified']
assert guard['NativeOriginalExecuted'] is False and guard['MainModified'] is False
assert digest((ROOT/COMPOSITION).read_bytes()) == COMPOSITION_SHA
assert digest((ROOT/META_PLAN).read_bytes()) == META_SHA
plan = read_json(ROOT/COMPOSITION)
assert len(plan['OwnedPaths']) == 169 and len(set(plan['OwnedPaths'])) == 169
for row in plan['StableClosure']:
    b = (CANDIDATE/safe(row['Path'])).read_bytes()
    assert len(b) == row['Bytes'] and digest(b) == row['Sha256'], row['Path']
for name, field in [('full.exit.json','FullExitSha256'), ('full.stdout.log','FullStdoutSha256'), ('full.stderr.log','FullStderrSha256'), ('artifacts/tests.json','TestReportSha256'), ('captured-outputs.json','CapturedIndexSha256')]:
    assert digest((full/name).read_bytes()) == guard[field]
exit_receipt = read_json(full/'full.exit.json')
assert exit_receipt['Settled'] and exit_receipt['ExitCode'] == 0 and not exit_receipt['TimedOut']
assert digest(source(plan['RegistryBefore'])) == guard['RegistryBeforeSha256']
assert digest((full/'raw-supported-mfas.json').read_bytes()) == guard['RawRegistrySha256']

output = args.output.resolve()
assert output.is_relative_to(ROOT/'artifacts') and not output.exists()
overlay = output/'overlay'
rows = {}
def put(target, b, origin):
    safe(target)
    if target in rows:
        assert (overlay/target).read_bytes() == b, target
        return
    p = overlay/target
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes(b)
    parent = ROOT/target
    rows[target] = {'SourcePath':p.relative_to(ROOT).as_posix(), 'TargetPath':target,
        'Bytes':len(b), 'Sha256':digest(b), 'ParentSha256':digest(parent.read_bytes()) if parent.exists() else None,
        'Origin':origin}

by_path = {r['Path']:r for r in plan['Files']}
assert 'docs/ERLANG_COMPATIBILITY.md' in plan['OwnedPaths']
for path in plan['OwnedPaths']:
    if path == 'docs/ERLANG_COMPATIBILITY.md': continue
    row = by_path[path]
    b = source(row) if path == 'docs/supported-mfas.json' else (CANDIDATE/path).read_bytes()
    assert len(b) == row['Bytes'] and digest(b) == row['Sha256']
    put(path, b, 'Exact reviewed composition; registry restoration is overlay-only after independent PASS')
    if not path.startswith('docs/validation/'):
        put('docs/validation/part-353/tested-source/'+path, b, 'Exact owned source/config input snapshot; existing validation evidence is already preserved at its public path')

for path in PRESERVE:
    assert path not in rows
preserved = [{'Path':p,'Sha256':digest((ROOT/p).read_bytes())} for p in PRESERVE]
old_journal = (ROOT/'docs/changelog/0055.md').read_bytes()
entry = '''
## Part 353 — 2026-10-17 — Logical scheduler binding and online lanes

Implement logical `{scheduler,N}` creator options and `process_flag(scheduler,N)`: zero unbound, configured-range validation, last valid duplicate and previous-binding return. Resolve child binding before local, request and inbound connected publication. Preserve process-owner context while moving or parking scheduler objects. Add actual logical scheduler identity/count queries and admitted `schedulers_online` changes, including offline-lane quiescence and completion in the initiating owner. Initializers have an explicit actor role with real admission, recursive module-install reentry and retirement ordering. Logical lanes do not promise physical CPU/thread pinning, dirty scheduler execution, native reductions/preemption, timing or throughput parity.

The canonical full validation passed 7,313 tests, preserving all 7,297 prior ordered names and adding eleven HOST plus five portable contracts. Eleven integration outcomes, two packages with six exact resource inputs, and twelve bounded term-order child receipts/streams were independently checked. Restore the exact reviewed custom registry Limits only after that audit; retain the raw generated registry separately. Actual focused ParseAll 7/30 targets and the earlier permanent project build (exit 0, no warnings/errors, 30.406 seconds) remain distinct evidence. Preserve the earlier source/build/control/provenance refusals and their actual scope. The first full failed 7,312/7,313 on the unchanged owned-actor capacity HOST timeout, with all sixteen new tests passing. Its ten-phase diagnostic had seven passes and three failures. A source-reviewed five-line online-idle TrySteal/Select retry fixes the reachable idle-worker starvation path; it is not a captured historical queue snapshot. The corrected 40-target focus passed without changing original assertions or budgets; original Run took 2.216 seconds under its unchanged 15-second bound. This final full result belongs to the fresh corrected composition and does not relabel the failed first full.

Keep 587 registered MFAs and the same compatibility classifications: 16 commands 100% compatible, 24 with known differences and 547 needing verification. Preserve the sixteen complete-contract scopes and prior evidence; refresh/extend the source guard graph from 77 to 89 to bind owner, context, admission and lane dependencies. Correct thirteen narrow Limits descriptions, including stale connected-spawn/priority refusals. The four spawn_opt commands remain Known differences because heap/GC tuning and message_queue_data options remain unsupported. A passed full suite does not certify whole scheduler, spawn or erlang compatibility.

Archive original part 351: 6,316 semantic contracts plus eight fd scenarios, all previous 6,313 raw rows and three bounded priority witnesses. Retain its separate predeclared map stdout strict-equality refusal without normalization. Archive corrected part 352 CI: 7,297 tests and eleven outcomes. Initial c09 CI was successful at the API level but not semantically audited; retain the Root attribute-order promotion error and its correction without inventing a CI failure. Preserve the later published command index, current matrix labels and part 352 reporting appendix.

Document the priority/affinity CLR adaptations and unported behaviors in OTP-PORTS.md, preserving its historical register, and bind the new file as a package resource. Preserve exact reviewed attributes, raw collector sources and LF workflows; verify all 1,268 pending and 1,293 reallocation manifest rows and seals after final writes. The separate original profile builds pinned OTP and uses five fresh VMs with explicit +S 2:2; its provenance controls for tracked symlinks and ERL_ROOTDIR are source-reviewed. At this initial publication the new five-case native profile, updated ordinary original comparison and same-head CI remain pending: workflows can execute against the published head only afterward. No fresh native result or performance result is claimed here.
'''
sep = b'\r\n' if b'\r\n' in old_journal else b'\n'
journal = old_journal + (b'' if old_journal.endswith(b'\n') else sep) + entry.encode().replace(b'\n',sep)
assert journal.startswith(old_journal) and len(journal) <= 32768 and len(journal.splitlines()) <= 400
put('docs/changelog/0055.md',journal,'Append only to exact published reporting appendix')

for inventory in ['artifacts/part353-delivery-proposal/full2-history-proof-copy-plan.json']:
    for row in read_json(ROOT/inventory)['Files']: put(row['TargetPath'],source(row),'Bound lean source/history proof')
for reproduction in REPRODUCTION_PINS:
    path = ROOT/reproduction['Path']
    b = path.read_bytes()
    assert len(b)==reproduction['Bytes'] and digest(b)==reproduction['Sha256']
    put('docs/validation/part-353/root-delivery/full-reproduction/'+reproduction['Path'].removeprefix('artifacts/'),b,'Exact actual runner/owned helper and independently approved passive-reader source controls')
put('docs/validation/part-353/full/independent-report.json',guard_path.read_bytes(),'Independent actual full acceptance')
reader_bytes=full2_reader.read_bytes()
assert digest(reader_bytes)==guard['ReaderSha256']
put('docs/validation/part-353/full/independent-reader.py',reader_bytes,'Actual independently accepted full2 reader source')
put('docs/validation/part-353/root-delivery/full2-source-controls.json',controls_path.read_bytes(),'Root-reviewed actual full2 reader controls')
for control_row in controls['Files']:
    put('docs/validation/part-353/root-delivery/full2-source-controls/'+control_row['Path'].removeprefix('artifacts/'),(ROOT/control_row['Path']).read_bytes(),'Exact bound full2 source-control bytes')
put('docs/validation/part-353/root-delivery/readiness-after-full2.py',(ROOT/'artifacts/part353-delivery-proposal/readiness-after-full2.py').read_bytes(),'Root-only restored-registry readiness2 source proposal, not execution evidence')
put('docs/validation/part-353/root-delivery/finalize-after-pass.py',Path(__file__).read_bytes(),'Source-reviewed artifact-only delivery helper')
for p in sorted(full.rglob('*')):
    if p.is_file() and p.suffix.lower() in ['.json','.log','.txt','.md']:
        put('docs/validation/part-353/full/raw/'+p.relative_to(full).as_posix(),p.read_bytes(),'Declared text/JSON subset; package/DLL/ZIP binaries excluded and retained by digest')
captured = read_json(full/'captured-outputs.json')
for child in captured['Children']:
    receipt = CANDIDATE/safe(child['Path'])
    assert digest(receipt.read_bytes()) == child['Sha256']
    for p in [receipt,receipt.parent/'stdout.txt',receipt.parent/'stderr.txt']:
        put('docs/validation/part-353/full/children/'+p.relative_to(CANDIDATE/'artifacts/term-order-children').as_posix(),p.read_bytes(),'Actual bounded child receipt/streams')

# No attrs reconstruction; the composition's exact attrs must equal the final payload.
assert (overlay/'.gitattributes').read_bytes() == source(by_path['.gitattributes'])
def final_bytes(path):
    p=overlay/path
    return p.read_bytes() if p.exists() else (ROOT/path).read_bytes()
native = []
for path, workflow, seal, count in [
 ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1268),
 ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1293)]:
    b = final_bytes(path); manifest=json.loads(b); assert len(manifest['Files']) == count
    for row in manifest['Files']:
        raw=final_bytes(row['Path']); assert row['HashMode'] in ['TextLF','BinaryExact']
        data=raw.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else raw
        assert len(data)==row['Bytes'] and digest(data)==row['Sha256'],row['Path']
    assert (seal+': '+digest(b)).encode() in final_bytes(workflow)
    native.append({'Path':path,'Rows':count,'Sha256':digest(b),'EveryFinalRowVerified':True})
ledger=json.loads(final_bytes('docs/mfa-compatibility.json'))
assert len(ledger['VerifiedSourceFiles']) == 89
assert collections.Counter(r['Status'] for r in ledger['Entries']) == {'Verified compatible':16,'Known differences':24,'Needs verification':547}
for row in ledger['VerifiedSourceFiles']: assert digest(final_bytes(row['Path']))==row['Sha256']
for entry_row in ledger['Entries']:
    for proof in entry_row['Evidence']: assert digest(final_bytes('docs/'+proof['Path']))==proof['Sha256']
for resource in plan['PackageResources']: assert digest(final_bytes(resource['Path']))==resource['Sha256']
assert current_head()==PARENT
result={'Status':'ARTIFACT_ONLY_POST_INDEPENDENT_PASS_353_PROMOTION_PROPOSAL_NOT_APPLIED',
 'ParentHead':PARENT,'IndependentGuardSha256':digest(guard_path.read_bytes()),'CompositionPlanSha256':COMPOSITION_SHA,
 'Files':list(rows.values()),'FilesCount':len(rows),'CompositionOwnedSelected':168,'PreserveCurrentMain':preserved,
 'Original0055PrefixSha256':digest(old_journal),'Final0055Bytes':len(journal),'Final0055Lines':len(journal.splitlines()),
 'RegistryRestoredOnlyInProposedOverlay':True,'ReadinessCliExecuted':False,'RootActualReadinessStillRequired':True,
 'NativePlans':native,'PackageInputs':plan['PackageResources'],'Native5OrdinaryAndSameHeadCiPending':True,
 'ProofBinaryExclusions':'ZIP/nupkg/DLL/runtime binaries not copied; their producer/transport/package hashes remain in raw receipts.',
 'NoMainCandidateSdkNetworkGitMutation':True}
(output/'promotion-plan.json').write_bytes(encoded(result))
(output/'execution.json').write_bytes(encoded({'Status':'ARTIFACT_ONLY_HELPER_COMPLETED','PromotionPlanSha256':digest((output/'promotion-plan.json').read_bytes()),'HelperSha256':digest(Path(__file__).read_bytes())}))
print(digest((output/'promotion-plan.json').read_bytes()))

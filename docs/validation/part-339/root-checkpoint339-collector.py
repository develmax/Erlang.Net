from pathlib import Path
import hashlib
import json
import shutil
import subprocess

r = Path.cwd()
a = r / 'artifacts'
head = 'c8f46a6b60df1d0921df55855e617f8453adbb31'
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == head
assert not subprocess.check_output(['git', 'diff', '--name-only']).strip()
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only']).strip()
j = lambda p: json.loads(p.read_bytes())
h = lambda raw: hashlib.sha256(raw).hexdigest().upper()
proposal = a / 'connected-spawn-original-collector-proposal2'
freeze = j(proposal / 'source-freeze.json')
assert h((proposal / 'source-freeze.json').read_bytes()) == '2080B5FDAE3F93B731B97779AE13BDF33983685CBB0EE85ABDE5AEB05F0997A9'
peer = a / 'connected-spawn-original-collector-source-peer2/report.json'
assert h(peer.read_bytes()) == 'E4F824AE5EEBA8EDA5719B45CBC0F8360D3334D87F7A6509376611B0A4F134E9'
controls = j(a / 'root-connected-spawn-collector2-controls/guard.json')
assert controls['Controls'] == controls['DeclaredScenarios'] == 12 and controls['MainEdited'] is False
b = r / 'docs/validation/part-339'
assert not b.exists()
source = []
for row in freeze['Files']:
    p = proposal / 'source' / row['Path']
    raw = p.read_bytes()
    assert len(raw) == row['Bytes'] and h(raw) == row['Sha256']
    target = r / row['Path']
    assert not target.exists()
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(p, target)
    source.append(row['Path'])
attributes = r / '.gitattributes'
before = attributes.read_bytes()
assert b'ConnectedSpawnOriginalObservations' not in before
attributes.write_bytes(before.rstrip(b'\r\n') + b'\n\ntools/Erlang.ConnectedSpawnOriginalObservations/** -text whitespace=trailing-space,space-before-tab,cr-at-eol\n.github/workflows/connected-spawn-original.yml -text whitespace=trailing-space,space-before-tab,cr-at-eol\n')
source.append('.gitattributes')

def write(path, text):
    (r / path).write_text(text, encoding='utf-8', newline='\n')

def prepend(path, text):
    first, rest = (r / path).read_text(encoding='utf-8').split('\n', 1)
    write(path, first + '\n\n' + text + '\n\n' + rest.lstrip('\n'))

summary = ('Part339 adds a standalone original-only connected-spawn collector with12 fresh-VM controller scenarios: six plain/TT/monitor-option cases and six alias33/34 admission/send/lifecycle cases. '
           'Pinned source/installed-runtime/module/fixture provenance, exact reviewed source manifest, per-case raw ETF/packets and observed normal actor cleanup are mandatory. '
           'Independent source review and12 actual passive positive/refusal controls pass; original execution and SDK semantic comparison remain pending publication. '
           'No runtime, registry, fixture expectation or compatibility-count change:576 APIs across12 modules, proc_lib33/39;338 tests7105 and separate11 integrations remain the current local authority. '
           'Injected controller observations are not physical TCP interoperability. Continue six proc_lib node overloads and real RPC label queries; known full-module/physical/logger/performance gaps remain explicit.')
for path in ['AGENTS.md', 'docs/PROGRESS.md', 'docs/PROC_LIB_COMPATIBILITY.md', 'docs/NEXT_STEPS.md']:
    prepend(path, summary)
entry = '''
## Part339 — 2026-10-16 — Original connected-spawn and alias observations

**Part status:** completed source-only observation tooling; original execution awaits publication.
**Objective:** obtain independent raw OTP evidence for the connected-spawn dependency of all six remaining proc_lib node overloads and remote process-label queries.

### Changes and decisions

- Add a manually dispatched collector using exact OTP29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. Build the original once, then compile and execute each of12 reviewed scenarios in its own bounded VM.
- Observe incoming/outgoing plain and TT spawn packets, monitor priority/custom tags/aliases, incoming alias33/34 delivery before and after alias operations, and actual public alias sends. Preserve raw packets, ETF results, trace events, process identities, compiler/VM receipts and loaded-image provenance. Never create SDK-derived expected values.
- Reuse the exact existing owned-process and passive-ETF helpers, bound as external dependencies. Require successful stage/outcome records plus normal controller/tracer/actual child DOWN cleanup; VM exit0 alone does not prove successful observation or compatibility. A dynamic trace barrier retains controller-RPC trace events and does not invent exact token serials.
- Seal all source, workflow and external-dependency bytes with the reviewed manifest. Preserve source bytes in Git explicitly. Use existing infrastructure without adding a production actor/transport package.

### Validation and limits

- Independent source peer review and12 actual passive source/refusal controls pass. Controls reject wrong manifest/head, changed workflow/dependency/fixture, undeclared files and escaping paths, then verify the restored exact closure. Python syntax and twelve-case/raw-file cardinalities are checked.
- No Erlang compiler, original VM, SDK or network was executed by those controls. Source-only checks establish neither observed native values nor SDK semantic compatibility. The controller simulates a peer; physical TCP interoperability remains separate.
- Runtime, registry and tests are unchanged from338:576 APIs, proc_lib33/39,7105 actual passed tests plus a separately completed11-check readiness/integration continuation. The initial338 missing-input failure remains preserved. Its CI and6216-case original run are pending separate terminal evidence; no new full pipeline is claimed for this tooling-only increment.
- The first six-case proposal and its controls remain immutable; this revision adds six alias cases. Keep actual raw failures and process receipts if a compiler/scenario/cleanup operation fails.

### Handoff

Publish the tooling and dispatch its exact reviewed source manifest once. Continue real connected spawning, RPC/erpc label queries and six node-qualified proc_lib overloads; compare actual SDK and independent original observations before declaring compatibility. Keep LICENSE.local.txt and unrelated caches uncommitted; do not publish a package.
'''
log = r / 'docs/changelog/0053.md'
combined = log.read_text(encoding='utf-8') + entry
assert len(combined.encode()) <= 32768 and len(combined.splitlines()) <= 400
write('docs/changelog/0053.md', combined)
index = (r / 'CHANGELOG.md').read_text(encoding='utf-8')
assert 'part338 (active)' in index
write('CHANGELOG.md', index.replace('part338 (active)', 'parts338–339 (active)'))
metadata = ['AGENTS.md', 'docs/PROGRESS.md', 'docs/PROC_LIB_COMPATIBILITY.md', 'docs/NEXT_STEPS.md', 'docs/changelog/0053.md', 'CHANGELOG.md']
b.mkdir(parents=True)
(b / '.gitattributes').write_bytes(b'* binary\n')
for directory, label in [(proposal, 'collector-source'), (a / 'connected-spawn-original-collector-source-peer2', 'source-peer')]:
    for p in sorted(directory.rglob('*')):
        if p.is_file() and p.suffix in ['.json', '.py', '.erl', '.yml', '.md']:
            target = b / label / p.relative_to(directory)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(p, target)
for name in ['root-checkpoint339-collector.py', 'root-control-connected-spawn-collector2.py', 'root-prepare-collector2-controls.py', 'root-part338-stage-guard.json', 'root-part338-ordinary-dispatch-attempt.json', 'root-part338-ordinary-dispatch-result.json']:
    shutil.copyfile(a / name, b / name)
shutil.copyfile(a / 'root-connected-spawn-collector2-controls/guard.json', b / 'source-controls.json')
rows = [dict(Path=p.relative_to(r).as_posix(), Bytes=p.stat().st_size, Sha256=h(p.read_bytes())) for p in sorted(b.rglob('*')) if p.is_file()]
(b / 'evidence-index.json').write_text(json.dumps(rows, indent=2) + '\n')
receipt = dict(Status='SOURCE_ONLY339_COLLECTOR12_PROMOTED_PENDING_CLI_AND_EXACT_STAGE',
               Parent=head, RuntimeChanged=False, Registry=576, ProcLib=33, Scenarios=12,
               ManifestSha256=freeze['SourceManifestSha256'], OriginalExecuted=False,
               PassiveSourceControls=12, ProofFiles=len(rows), ProofBytes=sum(x['Bytes'] for x in rows))
(b / 'evidence-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
proof = [p.relative_to(r).as_posix() for p in sorted(b.rglob('*')) if p.is_file()]
paths = sorted(source + metadata + proof)
assert len(paths) == len(set(paths))
(a / 'root-part339-stage-plan.json').write_text(json.dumps(dict(Paths=paths, Source=source, Metadata=metadata, Proof=proof), indent=2) + '\n')
(a / 'root-part339-stage-paths.txt').write_text('\n'.join(paths) + '\n')
print(json.dumps(receipt))

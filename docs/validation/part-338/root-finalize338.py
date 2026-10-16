from pathlib import Path
import hashlib
import json
import shutil

r = Path.cwd()
a = r / 'artifacts'
b = r / 'docs/validation/part-338'
j = lambda p: json.loads(p.read_bytes())
h = lambda raw: hashlib.sha256(raw).hexdigest().upper()
promoted = j(a / 'root-part338-promotion.json')
ready = j(a / 'root-part338-readiness/module-readiness.json')
assert (ready['LocalPassedTests'], ready['RegisteredMfas'], ready['PreviousRegisteredMfas'], ready['RegisteredMfaDelta']) == (promoted['Passed'], 576, 540, 36)
assert ready['ImplementedApiModules'] == 12
proc = next(x for x in ready['Modules'] if x['Name'] == 'proc_lib')
assert (proc['RegisteredMfas'], proc['ReferenceDeclarations']) == (33, 39)
for name in ['MODULE_READINESS.md', 'module-readiness.json']:
    shutil.copyfile(a / 'root-part338-readiness' / name, r / 'docs' / name)
    shutil.copyfile(r / 'docs' / name, b / name)

def archive(source, label, suffixes=None):
    assert source.is_dir(), source
    for p in sorted(source.rglob('*')):
        if p.is_file() and '__pycache__' not in p.parts and p.suffix not in ['.zip', '.dll', '.pdb', '.exe'] and (suffixes is None or p.suffix in suffixes):
            out = b / label / p.relative_to(source)
            out.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(p, out)

archive(a / 'part337-remote-new-review', 'prior337', {'.json', '.py', '.log'})
for p in sorted(a.glob('proc-lib338-actual*')):
    if p.is_dir():
        archive(p, 'development/' + p.name, {'.json', '.log', '.cs', '.py'})
for name in ['proc-lib338-direct-host1', 'proc-lib338-source', 'proc-lib338-term-limit-fix', 'proc-lib338-permanent-tests3', 'proc-lib338-integration-proposal']:
    archive(a / name, 'source-history/' + name, {'.json', '.log', '.cs', '.py', '.erl', '.md'})
for name in ['root-checkpoint338.py', 'root-finalize338.py', 'root-stage338.py', 'root-part338-promotion.json', 'root-review337-ci.py', 'root-check337-report-catalog.py', 'root-337-report-catalog-guard.json', 'root-review338-physical-bridge.py', 'root-338-physical-bridge-source-review.json', 'root-check338-archive-line-endings.py', 'root-check338-archive-line-endings2.py', 'root-prepare338-line-endings-reader2.py', 'root-338-unowned-line-endings2.json', 'proc-lib338-finalize-composite.py', 'proc-lib338-readiness-integration-continuation.ps1']:
    shutil.copyfile(a / name, b / name)
rows = [dict(Path=p.relative_to(r).as_posix(), Bytes=p.stat().st_size, Sha256=h(p.read_bytes())) for p in sorted(b.rglob('*')) if p.is_file() and p.name not in ['evidence-index.json', 'evidence-receipt.json']]
index = b / 'evidence-index.json'
assert not index.exists()
index.write_text(json.dumps(rows, indent=2) + '\n')
receipt = dict(Status='LOCAL338_FROZEN_TESTS_AND_SEPARATE_INTEGRATION_CONTINUATION_COMPLETE',
               Passed=promoted['Passed'], Registry=576, ProcLib=33,
               Previous337Ci=7017, Previous337Original=6171, Previous337Fd=8,
               LocalCrashPhysicalFieldsExplicitlyUnavailable=['heap_size', 'stack_size'],
               Current338CiVerified=False, Current338OriginalVerified=False, SingleUninterruptedFullPass=False,
               CopiedFiles=len(rows), CopiedBytes=sum(x['Bytes'] for x in rows), EvidenceIndexSha256=h(index.read_bytes()))
(b / 'evidence-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
source = [x['Path'] for x in j(b / 'final-candidate/source-freeze.json')['Files']]
metadata = ['AGENTS.md', 'CHANGELOG.md', 'docs/PROGRESS.md', 'docs/COMPATIBILITY.md', 'docs/PROC_LIB_COMPATIBILITY.md', 'docs/ERLANG_COMPATIBILITY.md', 'docs/semantic-differences.md', 'docs/DECISIONS.md', 'docs/dependency-decisions.md', 'docs/DEPENDENCY_PRIORITIES.md', 'docs/NEXT_STEPS.md', 'docs/module-readiness-scope.json', 'docs/module-readiness.json', 'docs/MODULE_READINESS.md', promoted['ChangelogVolume'], 'docs/supported-mfas.json']
proof = [p.relative_to(r).as_posix() for p in sorted(b.rglob('*')) if p.is_file()]
paths = sorted(source + metadata + proof)
assert len(paths) == len(set(paths))
assert not any('LICENSE.local' in x or '__pycache__' in x for x in paths)
(a / 'root-part338-stage-plan.json').write_text(json.dumps(dict(Paths=paths, Metadata=metadata, Source=source, Proof=proof), indent=2) + '\n')
(a / 'root-part338-stage-paths.txt').write_text('\n'.join(paths) + '\n')
print(json.dumps(dict(**receipt, StagePaths=len(paths))))

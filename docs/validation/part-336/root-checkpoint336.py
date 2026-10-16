from pathlib import Path
import hashlib
import json
import shutil
import subprocess

r = Path.cwd()
a = r / 'artifacts'
b = r / 'docs/validation/part-336'
j = lambda path: json.loads(path.read_bytes())
h = lambda data: hashlib.sha256(data).hexdigest().upper()
assert subprocess.check_output(['git', 'rev-parse', 'HEAD']).decode().strip() == '0f357f55d016936fe7df7cf38408c3009887d382'
freeze = a / 'proc-lib-format-original-proposal/source-freeze.json'
assert h(freeze.read_bytes()) == '3FCBF2B9401541E036B20FC5673DB83C65CB5F5E04C47D9FD85876E1515FFD32'
f = j(freeze)
assert len(f['Files']) == 85
for row in f['Files']:
    source, target = r / row['SourcePath'], r / row['TargetPath']
    assert len(source.read_bytes()) == row['Bytes'] and h(source.read_bytes()) == row['Sha256']
    assert not target.exists()
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, target)
tool = r / 'tools/Erlang.ProcLibFormatOriginalObservations'
manifest = tool / 'source-manifest.json'
assert h(manifest.read_bytes()) == f['SourceManifestSha256']
controls = subprocess.run(['C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe', '-B', str(tool/'check-source-controls.py')], capture_output=True)
assert controls.returncode == 0, controls.stderr.decode()
actual = json.loads(controls.stdout)
assert actual['Controls'] == 16 and actual['OriginalExecuted'] is actual['SdkExecuted'] is False
b.mkdir(parents=True, exist_ok=False)
(b/'.gitattributes').write_bytes(b'* binary\n')
(b/'source-controls.json').write_bytes(controls.stdout)
(b/'source-controls.stderr').write_bytes(controls.stderr)
shutil.copyfile(freeze, b/'collector-source-freeze.json')
guard = a/'part334-remote-new-review/ordinary-semantic-independent-guard.json'
assert h(guard.read_bytes()) == '345C2CEC8459B9C34C3AE2EFE09A7D30019BB0F2EF891712C96A83501B392682'
assert j(guard)['Passed'] == 6085 and j(guard)['FdPassed'] == 8
for source in sorted((a/'part334-remote-new-review').rglob('*')):
    if source.is_file() and source.suffix in ['.json', '.py']:
        target = b/'prior334-original'/source.relative_to(a/'part334-remote-new-review')
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
shutil.copyfile(a/'proc-lib336-sdk-capture/report.json', b/'unpublished-sdk-formatter-observation-status.json')
note = 'Part336 publishes a standalone original formatter observation collector, not a runtime increment. Published334 original passes6085/6085 plusfd8 with all6042 previous ordered contracts and43 new metadata/label cases verified; one predeclared map stdout-order variance remains a separate strict refusal. Published335 remains the runtime baseline:6952 local tests/11 integrations,536 APIs in9 modules, proc_lib25/39 and sys2/36. Current335 CI/original are pending. The private formatter has57 SDK report observations,19 primitive observations,28 source-derived backend assertions and an actual callback-await control; no original formatter match or four new exports is claimed. Continue the real lifecycle/crash-report and connected-node dependencies.'
for name in ['AGENTS.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/PROC_LIB_COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/semantic-differences.md']:
    path = r/name
    text = path.read_text(encoding='utf-8')
    heading, rest = text.split('\n', 1)
    path.write_text(heading+'\n\n'+note+'\n'+rest, encoding='utf-8', newline='\n')
for name, paragraph in {
    'docs/DECISIONS.md': 'Part336 — Obtain independently captured original outcomes before assigning formatter expectations. Use existing Python standard-library development-observer infrastructure, exact pinned source-built OTP and isolated owned processes; do not add a runtime BEAM or third-party package dependency. Decode ETF terms for later comparison while retaining text bytes and nested lists; never infer expected values from SDK output.',
    'docs/dependency-decisions.md': 'Part336 — Inspected the existing differential workflow and source-capture observer tools. Reuse their pinned OTP build/configuration, Python standard-library process and hashing facilities, with fresh erlc/VM percase. Python is development observation infrastructure only; production/runtime formatter remains C#. No package or public license decision changes; existing third-party notices and owner-local uncommitted license are preserved.',
    'docs/DEPENDENCY_PRIORITIES.md': 'Part336 — Capture original57 report and19 primitive results first; compare with private SDK observations, fix actual differences and only then publish four report exports. In parallel complete initializer/hibernate with real crash-report/logger dependencies; six node overloads also require actual connected spawning.',
    'docs/NEXT_STEPS.md': 'Part336: publish the source-reviewed standalone collector, dispatch exactly once with its manifest SHA, then compare actual original raw returned terms against private SDK observations. Finish exact335 CI/original audits. Continue all14 missing proc_lib exports and label remote dependencies; no fake logger or disconnected-only completion claim.'
}.items():
    path = r/name
    text = path.read_text(encoding='utf-8')
    heading, rest = text.split('\n', 1)
    path.write_text(heading+'\n\n'+paragraph+'\n'+rest, encoding='utf-8', newline='\n')
scope = j(r/'docs/module-readiness-scope.json')
scope['ComparisonTestReport'] = 'validation/part-335/final-candidate/tests.json'
component = next(row for row in scope['Components'] if row['Id'] == 'proc_lib adapters')
component['Implemented'].append('Part336: published334 original6085/fd8 verifies43 introspection/label cases and6042 previous contracts. Standalone formatter collector is infrastructure only; private57+19 SDK observations are not original matches or exports.')
(r/'docs/module-readiness-scope.json').write_text(json.dumps(scope,indent=2)+'\n', encoding='utf-8', newline='\n')
entry = '''
## Part336 — 2026-10-16 — Independent original proc_lib formatter collector

**Part status:** completed development tooling; original formatter observations await publication and dispatch.
**Objective and scope:** establish independent original outcomes before promoting the private report formatter. Runtime API presence remains unchanged.

### Changes

- Add the standalone Python development collector and manual workflow with57 exact report sources (45 text/exception,12 raw iolist) and19 separate original-only primitive sources. Freeze83 tool source files, the manifest and workflow by SHA256.
- Build the exact pinned OTP29.1.1 commit using the existing oracle configuration. Verify installed version, clean tracked reference source, runtime/compiler/module identities and12 pinned source/data references.
- Compile each unchanged fixture in its own directory with compiler-server use disabled; execute each in a fresh bounded original VM. Preserve returned/raised ETF, outcome envelope, source/BEAM hashes, arguments, stdout/stderr and settled process receipts. Timeout cleanup targets only its owned process group. Capture failures remain distinct from guest outcomes; no result flattening occurs in the collector.
- Archive the completed published334 original audit:6085/6085 plusfd8; all6042 old ordered contracts and43 new source/local/original cases verified. The predeclared map stdout-order change remains a separate strict raw refusal without normalization.

### Decisions and compatibility

Reuse Python standard-library observation infrastructure and the existing source-built original workflow; add no production BEAM or third-party package dependency. Production formatter code remains C# and isolated.57 SDK report observations,19 SDK primitive observations,28 source-derived pretty assertions and the real callback suspension/release control exist privately; they are not original comparisons or public exports. Preserve the failed compile/test-setup receipts. Labels are locally implemented, with remote channels still partial. Logging, lifecycle and connected spawning remain genuine dependencies for completing proc_lib.

### Validation

- Root independently rehashes85 promoted source paths and reruns16 source/schema/CLI controls, all pass. Python syntax, exact source closure, wrong hashes, invalid paths, malformed declarations/catalogs and pre-launch refusal paths are checked. These controls do not execute OTP or SDK. Linux normal/raw-byte and owned-group timeout controls will run in the workflow; they are not claimed locally.
- [Original334 semantic guard](../validation/part-336/prior334-original/ordinary-semantic-independent-guard.json) and distinct strict refusal bind exact8f27347c/run38933888536/job117084401981/artifact12060519304. Raw report and immutable download/source bindings are archived. Two reader-adaptation mistakes were corrected and disclosed; raw observations were unchanged.
- Historical runtime validation remains6952/6952 and11 integration/package outcomes at0f357f55. No redundant full runtime test is run for this Python-only observer. Regenerate readiness from the actual passed335 report against immutable335:536 APIs, delta0, proc_lib25/39, sys2/36. This is presence, not whole-module compatibility. Current335 CI/original remain pending.

### Handoff

Publish this tooling, dispatch the reviewed manifest exactly once, and compare all76 independently captured terms before promoting the report formatter. Continue initializer/hibernate/crash reporting and node overloads. Update progress, compatibility, semantic differences, decisions, readiness and executable priorities. Keep LICENSE.local.txt and unrelated caches uncommitted. No public package publication is authorized or performed.
'''
log = r/'docs/changelog/0052.md'
value = log.read_text(encoding='utf-8') + entry
assert len(value.encode()) <= 32768 and len(value.splitlines()) <= 400
log.write_text(value,encoding='utf-8',newline='\n')
index = r/'CHANGELOG.md'
text = index.read_text(encoding='utf-8').replace('parts 332–335','parts 332–336').replace('parts332–335','parts332–336')
index.write_text(text,encoding='utf-8',newline='\n')
(a/'root-part336-promotion.json').write_text(json.dumps(dict(Status='COLLECTOR85_SOURCE_AND16_CONTROLS_PASS',SourceManifestSha256=f['SourceManifestSha256'],RuntimeMfaDelta=0,OriginalFormatterExecuted=False,Files=f['Files']),indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Status='TOOL336_PROMOTED_PENDING_READINESS_AND_STAGE',SourceFiles=85,Controls=16,Prior334Original=6085,RuntimeMfas=536)))

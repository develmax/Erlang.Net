from pathlib import Path
import collections
import hashlib
import json
import shutil
import subprocess

r = Path.cwd()
d = r / 'docs/validation/part-370'
c = d / 'local-completion'
def load(p):
    return json.loads(p.read_bytes())
def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def write(p, value):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')

full = load(c / 'canonical-execution.exit.json')
suffix_dir = r / 'artifacts/binary-integration369/erlang370-final-metadata1'
suffix = load(suffix_dir / 'execution.exit.json')
for receipt in [full, suffix]:
    assert receipt['ExitCode'] == 0 and receipt['Settled'] and not receipt['TimedOut']
tests = load(r / 'artifacts/tests.json')
prior = load(r / 'docs/validation/part-369/local-completion/canonical-tests.json')
assert (tests['Passed'], tests['Failed']) == (7642, 0)
assert [x['Name'] for x in tests['Tests'][:7639]] == [x['Name'] for x in prior['Tests']]
ledger = load(r / 'docs/mfa-compatibility.json')
baseline = load(r / 'artifacts/erlang-integration370/baseline/docs/mfa-compatibility.json')
assert ledger['Entries'] == baseline['Entries']
assert len(ledger['VerifiedSourceFiles']) == 462
for item in ledger['VerifiedSourceFiles']:
    assert digest(r / item['Path']) == item['Sha256'], item['Path']
assert load(r / 'docs/supported-mfas.json') == load(r / 'artifacts/erlang-integration370/baseline/docs/supported-mfas.json')
statuses = dict(collections.Counter(x['Status'] for x in ledger['Entries']))
assert statuses == {'Verified compatible': 107, 'Needs verification': 541, 'Known differences': 30}
for packet in ['old', 'new']:
    closure = load(r / f'artifacts/erlang-integration370-final-{packet}1/capture1/main-dll-closure.json')
    for item in closure['Files']:
        assert digest(r / item['Path']) == item['Sha256'], item['Path']
for packet, count in [('old', 9582), ('new', 264)]:
    result = load(d / f'comparison/final-{packet}/acceptance.json')
    assert (result['Rows'], result['Strict'], result['Relations'], result['Differences']) == (count, count, 0, 0)

names = ['MODULE_READINESS.md', 'API_COMPATIBILITY.md', 'module-readiness.json', 'native-infrastructure.json', 'native-infrastructure.md']
for name in names:
    current = r / 'artifacts/readiness' / name
    packaged = r / 'artifacts/tool-readiness' / name
    assert current.read_bytes() == packaged.read_bytes(), name
    shutil.copyfile(current, c / name)
    if name != 'native-infrastructure.json':
        shutil.copyfile(current, r / 'docs' / name)
readiness = load(c / 'module-readiness.json')
assert (readiness['RegisteredMfas'], readiness['PreviousRegisteredMfas'], readiness['RegisteredMfaDelta'], readiness['VerifiedCompatibleMfas'], readiness['LocalPassedTests']) == (678, 678, 0, 107, 7642)
for module, count in [('binary', 31), ('queue', 38), ('proplists', 22)]:
    row = next(x for x in readiness['Modules'] if x['Name'] == module)
    assert row['VerifiedCompatibleMfas'] == row['RegisteredMfas'] == count
assert len(load(r / 'artifacts/integration-results.json')) == 11
for p in suffix_dir.iterdir():
    if p.is_file():
        shutil.copyfile(p, c / ('metadata-suffix-' + p.name))
shutil.copyfile(r / 'artifacts/erlang-integration370/final-metadata/composition.json', c / 'metadata-suffix-composition.json')
shutil.copyfile(r / 'artifacts/integration-results.json', c / 'final-integrations.json')
shutil.copyfile(r / 'docs/supported-mfas.json', c / 'supported-mfas.json')
shutil.copyfile(r / 'docs/mfa-compatibility.json', c / 'mfa-compatibility.json')
shutil.copyfile(r / 'docs/module-readiness-scope.json', c / 'module-readiness-scope.json')

current = ('Part370 complete locally; unpublished. Fixed the current erlang value/full-stack packet: '
           '9582/9582 old and264/264 new observations match pinned OTP strictly (9846 total,0 differences,0 identity relations). '
           'Repairs cover dynamic apply tail frames, size-error caller locations, tuple limits, local-FUN ordering versus exact identity, '
           'and conservative direct/one-hop compiler continuation contracts. Final-source canonical validation passes7642/7642 tests '
           '(+3; all7639 previous ordered names retained),11 integration outcomes and2 local packages. The full pipeline succeeds '
           'uninterrupted in383.125s; an additional unchanged-code metadata/package suffix restores123 emitter-reset Limits notes and '
           'advances the comparison baseline to369. Current/installed CLI readiness matches across5 files;462 source guards are exact. '
           'API delta0; complete-contract delta0: erlang351 present,16 complete (4.6%),13 Known,322 pending. '
           'The57-MFA packet is not whole-module closure;294 erlang APIs are outside it. Global678 APIs/107 complete/30 Known/541 pending. '
           'Binary31/31,queue38/38 andproplists22/22 complete classifications are retained after final dependency regression. '
           '[Evidence](validation/part-370/local-completion/README.md).')
for name in ['WORK_PLAN.md', 'PROGRESS.md', 'NEXT_STEPS.md', 'ERLANG_COMPATIBILITY.md']:
    p = r / 'docs' / name
    text = p.read_text(encoding='utf-8')
    start = text.index('Part370 current repair stage:')
    end = text.index('\n\n', start)
    text = text[:start] + current + text[end:]
    text = text.replace('Part370 active: erlang351/351 present;', 'Historical Part370 initial checkpoint: erlang351/351 present;')
    p.write_text(text, encoding='utf-8')
p = r / 'docs/COMPATIBILITY.md'
text = p.read_text(encoding='utf-8')
pos = text.index('\n') + 1
p.write_text(text[:pos] + '\n' + current + '\n' + text[pos:], encoding='utf-8')
p = r / 'docs/changelog/0060.md'
text = p.read_text(encoding='utf-8').replace('## Part370 — 2026-10-18 — erlang complete-contract validation (in progress)', '## Part370 — 2026-10-18 — erlang value/full-stack packet (complete locally; unpublished)')
text += ('\nFinal local acceptance: canonical tools/validate.ps1 completes uninterrupted with exit0 in383.125s: '
         '7642/7642 tests,11 integration outcomes,2 local packages, actual PackageReference consumer and installed CLI. '
         'All7639 prior ordered test names and all678 ledger entries/statuses are unchanged. '
         'All462 reviewed guards and the5 current DLL hashes match both final SDK captures. '
         'Restore123 existing registry Limits notes reset by the test emitter; advance previous test/API report from368 to369. '
         'An additional exact readiness/integration/package suffix on unchanged production/tests passes; no redundant full-test repetition. '
         'Current and installed CLI readiness are byte-equal across5 files; local delta0 APIs/0 complete contracts/+3 tests. '
         'The current57-MFA witness packet is9846/9846 strict with0 differences/0 applied identity relations; '
         'erlang remains16/351 complete,13 Known and322 pending. Preserve the original failed producer, retrospective admission, '
         'all intermediate failed attempts and both successful test-only CI runs. Durable evidence is docs/validation/part-370; '
         'Main HEAD/index and the uncommitted owner-license draft are unchanged. Next60/40 work targets APIs outside the first57 '
         'plus source-contract closure and bounded affected regression. No Main commit, push, PR or package publication.\n')
p.write_text(text, encoding='utf-8')

(d / 'source-attribution.md').write_text('''# Part370 source attribution and boundaries

Authority: Erlang/OTP OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. Existing OTP license and port register remain in docs/OTP-LICENSE.txt and docs/OTP-PORTS.md. Attribution is centralized; no per-file license banners were added. LICENSE.local.txt is not committed or copied here.

The port follows pinned erts/emulator/beam/erl_term.h and erl_bif_lists.c/erl_bif_guard.c tuple/type primitives; beam_common.c and emu/instrs.tab apply validation, tail transfer and error-BIF continuation rules; utils.c/erl_fun.h local-FUN term ordering and identity; and lib/compiler/src/beam_call_types.erl, beam_ssa_type.erl and beam_types.erl element return-type/continuation analysis. The retained source proof identifies exact native files and hashes. Embedded erlang provenance is derived from pinned erts/preloaded/ebin/erlang.beam plus make_preload and BEAM stripping, rather than equating a later installed companion image with the loaded image.

C# reuses the existing source frame/tail loop, immutable terms, explicit comparison stack and BCL AsyncLocal/ConditionalWeakTable. No third-party dependency or production BEAM interpreter is added. The compiler rule is deliberately conservative for direct literal indices and one-hop variable-only forwarders; this does not claim complete OTP SSA optimization parity, general compiler/physical heap parity, timings, or full erlang closure. Runtime canonical selection is bound atomically to its actual dispatch body/image; real replacement declines the continuation optimization.

All raw original statuses, source plans, exact fixtures, full-stack captures and intermediate failures are retained. The original failed producer remains failed; its raw observations have separate retrospective source admission. Successful closure CI and final actual Windows Main are distinct proofs. See local-completion/README.md for final gates and status counts.
''', encoding='utf-8')

(c / 'README.md').write_text('''# Part370 final local acceptance

Current value/full-stack packet: **9846/9846 strict comparisons**, zero differences and zero applied identity relations (9582 old +264 new). This is a57-MFA witness corpus, not all351 erlang contracts. Final actual Windows Main captures match pinned original OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5; final5 DLL hashes are unchanged through canonical regression.

Original closure [CI39108122243](https://github.com/develmax/Erlang.Net/actions/runs/39108122243) and same-test-head [CLR39108119699](https://github.com/develmax/Erlang.Net/actions/runs/39108119699) succeed. Original39106192411 remains failed: independent retrospective admission validates its preserved raw packet under the pinned embedded-preload policy. No producer status or raw difference is rewritten. Final comparison receipts are ../comparison/final-old/acceptance.json and ../comparison/final-new/acceptance.json.

Final-source full canonical pipeline exits0 in383.125s: **7642/7642 tests**,11 integration outcomes,2 local packages, PackageReference consumer and installed CLI. All7639 prior ordered test names are retained;3 permanent HOST groups add deep dynamic apply, size caller metadata and13 real large-tuple continuation/protection/registry-replacement executions. An additional metadata/package suffix restores123 pre-existing registry notes reset by the emitter and advances the previous baseline to369; production/tests remain unchanged and the full functional suite is not repeated. Both receipts are retained separately. Current/installed CLI readiness is byte-equal across5 files.

| Module | Present APIs | Complete source contracts |
|---|---:|---:|
| erlang |351/351|16/351 (4.6%);13 Known,322 pending|
| binary |31/31|31/31|
| queue |38/38|38/38|
| proplists |22/22|22/22|
| lists |91/91|0/91|
| maps |34/34|0/34|
| proc_lib |39/39|0/39;17 Known|

Delta:0 APIs,0 complete contracts,+3 tests. Global678 APIs/107 complete/30 Known/541 pending. All678 classifications and462 reviewed current source guards are retained. General HAMT construction, wider compiler SSA, resource/distribution/native/physical behavior and full-module closure remain separate work. No completion percentage is inferred from a finite witness corpus. Next60/40 work addresses the294 APIs outside this packet and source-contract/affected-regression gaps.

Evidence files are copied byte-identically under the guarded copy plan; original source paths inside historical receipts remain unchanged. The top-level README is a preserved pre-gate draft; this file records final completion. See ../source-attribution.md, final-verification.json and ../final-seal.json. No Main implementation commit/push/PR/package publication; index remains empty and LICENSE.local.txt untracked. Part369 evidence remains immutable.
''', encoding='utf-8')

assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip() == 'da8c65f79c80cfbce5f4d3eca44479d0490690e9'
assert not subprocess.check_output(['git', 'diff', '--cached', '--name-only'], text=True).strip()
assert subprocess.run(['git', 'ls-files', '--error-unmatch', 'LICENSE.local.txt'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode != 0
files = []
for folder in ['src', 'tools', 'tests', 'benchmarks', 'examples']:
    for p in sorted((r / folder).rglob('*')):
        if p.is_file() and not {'bin', 'obj', '__pycache__', '.git'}.intersection(p.relative_to(r).parts):
            files.append(dict(Path=p.relative_to(r).as_posix(), Bytes=p.stat().st_size, Sha256=digest(p)))
write(c / 'final-source-closure.json', dict(Files=files, SourceGuards=462))
write(c / 'final-verification.json', dict(Status='PASS_FINAL_ERLANG370_LOCAL_UNPUBLISHED', StrictComparisons=9846, Differences=0, IdentityRelations=0, CorpusMfas=57, WholeErLangClosure=False, RegisteredApis=678, CompleteContracts=107, ApiDelta=0, CompleteContractDelta=0, TestDelta=3, Tests=7642, Integrations=11, Packages=2, SourceGuards=462, CurrentPackagedFilesByteEqual=5, FullCanonicalSeconds=full['ElapsedSeconds'], MetadataSuffixSeconds=suffix['ElapsedSeconds'], Previous7639NamesPreserved=True, Previous678LedgerEntriesPreserved=True, ImplementationPublished=False, IndexEmpty=True, LicenseCommitted=False))
shutil.copyfile(Path(__file__), c / 'finalize-local.py')
sealed = [dict(Path=p.relative_to(d).as_posix(), Bytes=p.stat().st_size, Sha256=digest(p)) for p in sorted(d.rglob('*')) if p.is_file() and p.name != 'final-seal.json']
write(d / 'final-seal.json', dict(Status='SEALED_LOCAL_UNPUBLISHED_ERLANG370', Files=sealed, ImplementationPublished=False))
print(json.dumps(dict(Status='PASS_FINAL_ERLANG370_LOCAL_UNPUBLISHED', EvidenceFiles=len(sealed), SourceFiles=len(files), Tests=7642, StrictComparisons=9846, Guards=462, ApiDelta=0, CompleteContractDelta=0)))

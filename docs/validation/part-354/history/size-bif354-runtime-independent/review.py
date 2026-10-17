"""Independent source review/checks only; no build, VM, Main or network mutation."""
import collections
import difflib
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
PARENT = '50e756f73437eb8d2882a19f729bccb09af74f3d'
OLD = ROOT / 'artifacts/size-bif354-integration-proposal'
NEW = ROOT / 'artifacts/size-bif354-integration-proposal2'
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def sha(data): return hashlib.sha256(data).hexdigest().upper()
def read(path): return json.loads(path.read_bytes())
def need(value, message):
    if not value: raise ValueError(message)
def fresh(path, value):
    with path.open('xb') as stream: stream.write((json.dumps(value, indent=2) + '\n').encode())
def git(root, *args): return subprocess.check_output(['git', '-C', str(root), *args], timeout=60)
def key(row): return row['Module'], row['Function'], row['Arity']


def main():
    need(sha((OLD/'source-freeze.json').read_bytes()) == 'A7FD4E2BEA3E5E95798FEE8D9BC9E43D4575ECD7946CEE109E5D2B58D8A77753', 'initial freeze')
    need(sha((NEW/'source-freeze.json').read_bytes()) == '178B0C9D17EDD2D0F435622DF37B4E71CDB15925F64A6885A5A942F89FEB445C', 'latest freeze')
    verified_rows = 0
    for directory in (OLD, NEW):
        freeze = read(directory/'source-freeze.json')
        need(freeze['ParentHead'] == PARENT, 'parent')
        for row in freeze['Files']:
            data = (directory/row['Path']).read_bytes()
            need(len(data) == row['Bytes'] and sha(data) == row['Sha256'], 'frozen row ' + row['Path'])
            verified_rows += 1
        for row in freeze['UpstreamFreezes']:
            need(sha((ROOT/row['Path']).read_bytes()) == row['Sha256'], 'upstream pin')
    owned = read(NEW/'owned-source.json')['Files']
    runtime = [row for row in owned if row['Path'].startswith('src/Erlang.Runtime/')]
    need(len(runtime) == 9, 'nine runtime files')
    diff_rows = []
    for row in runtime:
        path = row['Path']
        data = (NEW/'source'/path).read_bytes()
        need(data == (OLD/'source'/path).read_bytes() and sha(data) == row['Sha256'], 'runtime unchanged between integrations')
        if row['ParentSha256'] is not None:
            before = git(ROOT, 'show', PARENT + ':' + path)
            need(sha(before) == row['ParentSha256'], 'exact parent runtime')
            diff = ''.join(difflib.unified_diff(before.decode().splitlines(True), data.decode().splitlines(True),
                fromfile=PARENT+'/'+path, tofile='proposal2/'+path))
            target = OUT/(Path(path).name + '.diff')
            with target.open('xb') as stream: stream.write(diff.encode())
            diff_rows.append({'Path': path, 'BeforeSha256': sha(before), 'AfterSha256': sha(data), 'DiffSha256': sha(target.read_bytes())})
    need({r['Path'] for r in diff_rows} == {'src/Erlang.Runtime/CoreModules.cs', 'src/Erlang.Runtime/ProcessRuntime.cs',
         'src/Erlang.Runtime/SequenceTraceRegistrations.cs'}, 'only three existing runtime changes')
    parent = json.loads(git(ROOT, 'show', PARENT + ':docs/mfa-compatibility.json'))
    ledger = read(NEW/'source/docs/mfa-compatibility.json')
    need(len(parent['VerifiedSourceFiles']) == len(ledger['VerifiedSourceFiles']) == 89, 'guard count')
    changed = []
    for old, new in zip(parent['VerifiedSourceFiles'], ledger['VerifiedSourceFiles']):
        need(old['Path'] == new['Path'], 'guard order')
        before = git(ROOT, 'show', PARENT + ':' + old['Path'])
        need(sha(before) == old['Sha256'], 'old source guard')
        candidate = NEW/'source'/new['Path']
        after = candidate.read_bytes() if candidate.is_file() else before
        need(sha(after) == new['Sha256'], 'proposed source guard')
        if old != new: changed.append(new['Path'])
    need(set(changed) == {r['Path'] for r in diff_rows}, 'exact three guard refreshes/86 raw preserved')
    verified = []
    classification = []
    for old, new in zip(parent['Entries'], ledger['Entries']):
        need(key(old) == key(new), 'entry order')
        if old['Status'] == 'Verified compatible':
            need(all(old[k] == new[k] for k in ('Status', 'Scope', 'Limits'))
                 and new['Evidence'][:len(old['Evidence'])] == old['Evidence'], 'verified scope/evidence prefix preserved')
            verified.append(':'.join((old['Module'], old['Function'])) + '/' + str(old['Arity']))
        if old['Status'] != new['Status']:
            need(old['Status'] == 'Needs verification' and new['Status'] == 'Known differences', 'no promotion')
            classification.append(key(new))
        elif old['Status'] != 'Verified compatible':
            need(all(old[k] == new[k] for k in ('Status', 'Scope', 'Limits')), 'other status/scope/limits preserved')
    need(len(parent['Entries']) == len(ledger['Entries']) == 587 and len(verified) == 16, 'registry/verified cardinality')
    need(set(classification) == {('erlang','system_flag',2)} | {('erlang','spawn_request',n) for n in (2,3,4,5)}, 'exact five known transitions')
    need(dict(collections.Counter(e['Status'] for e in ledger['Entries'])) == {'Verified compatible':16, 'Known differences':29, 'Needs verification':542}, 'proposed counts')
    anchors = []
    for path, ranges in (
        ('erts/emulator/beam/bif.c', [(5159,5166)]),
        ('erts/emulator/beam/beam_common.c', [(877,932),(993,1006),(1028,1053)]),
        ('erts/emulator/beam/erl_term.h', [(259,270)]),
        ('erts/emulator/beam/error.h', [(206,206)]),
        ('erts/emulator/beam/erl_map.c', [(146,154)])):
        data = git(ROOT.parent/'otp', 'show', PIN+':'+path)
        anchors.append({'Path':path,'Commit':PIN,'BlobSha256':sha(data),'LineRanges':ranges})
    focused = read(NEW/'focused-execution-plan.json')
    need(len(focused['ParseAll']) == 21 and len(set(focused['ParseAll'])) == 21
         and len(focused['AssertedCompiledCases']) == 16 and focused['ExpectedManagedChecks'] == 17, 'latest parse/focus plan')
    report = {'Status':'PASS_CONDITIONAL_SOURCE354_RUNTIME_AND_SIXTEEN_VERIFIED_PRESERVATION_BOUNDED_FOCUS_ONLY',
        'ParentHead':PARENT,'SourceFreezeSha256':sha((NEW/'source-freeze.json').read_bytes()),
        'FrozenRowsRehashedAcrossTwoRevisions':verified_rows,'RuntimeFiles':9,'RuntimeBytesSameAcrossIntegrations':True,
        'ChangedExistingRuntimeFiles':diff_rows,'CurrentGuards':89,'UnchangedGuardsRawExact':86,'VerifiedContracts':verified,
        'SixteenVerifiedStatusScopeLimitsAndPriorEvidencePrefixesPreserved':True,'OnlyFivePendingToKnown':classification,
        'ConditionalThreeSourceGuardRefreshAdmitted':True,'PinnedAnchors':anchors,
        'ReviewedRuntimeReasoning':[
          'CoreModules changes only four invalid size branches; successful size paths and all predicate/self/node bodies unchanged.',
          'Fresh explicit error factory supplies only builtin MFA arguments and module erl_erts_errors metadata; it does not rewrite existing custom/raised errors.',
          'BacktraceDepth state still defaults8 per runtime; volatile field and atomic exchange add no scheduling/callback side effects.',
          'Exact system_flag backtrace_depth dispatch precedes unchanged scheduler/topology branches; small nonnegative guard and clamp64 match pinned 64-bit OTP.',
          'Existing CaptureErrorStack minimum1/current BIF and depth-minus-one continuation shape follows pinned save_stacktrace; explicit flag prevents apply/compiler duplicate frames.'
        ],
        'ConcreteExistingRuntimeGap':'Terms.ErlangException.WithFrame is not capped by Runtime.BacktraceDepth; generic hd/error routes may differ. Added generic_depth_error/hd assertions must record actual RED without weakening.',
        'MetadataPublicationBlockers':[
          'Combined system_flag Scope/Limits/source-known evidence still describe backtrace_depth as unintegrated/currently refused; update to current combined behavior before publication.',
          'Four changed spawn_request Limits retain stale parent326 present-tense connected/priority refusal wording; correct those affected rows while preserving historical evidence.'
        ],
        'ConditionalAdmission':'Root-owned fresh build/parse21/16compiled+1HOST diagnostic focus only; expected generic RED is not runtime completion. No full/readiness/publication admission and no system_flag/four-size/full-tail compatibility claim.',
        'NativeStackMetadataObservationPending':True,'NoVerifiedPromotion':True,'SdkExecuted':False,'VmExecuted':False,
        'NetworkUsed':False,'MainOrGitMutated':False,'ProductionAuthorship':'Reviewer did not author the nine runtime files. Prior future-size audit informed the scope; implementation authored by other agents.'}
    fresh(OUT/'report.json',report)
    print(sha((OUT/'report.json').read_bytes()))


if __name__ == '__main__': main()

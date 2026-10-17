import hashlib
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
PREP = ROOT / 'artifacts/spawn-priority351-corrected-preparation'
RAW = ROOT / 'artifacts/spawn-priority351-corrections-focus-actual1'
OUT = Path(sys.argv[1])

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def split(text, separator=','):
    result, start, nesting, quoted, escaped = [], 0, 0, False, False
    for index, character in enumerate(text):
        if quoted:
            if escaped:
                escaped = False
            elif character == '\\':
                escaped = True
            elif character == '"':
                quoted = False
        elif character == '"':
            quoted = True
        elif character in '({[':
            nesting += 1
        elif character in ')}]':
            nesting -= 1
        elif character == separator and nesting == 0:
            result.append(text[start:index].strip())
            start = index + 1
    assert not quoted and nesting == 0
    return result + [text[start:].strip()]

def csharp_expression(text, variables, count):
    result = ''
    for token in split(text, '+'):
        if token.startswith('"'):
            result += json.loads(token)
        elif token == 'cases.Count':
            result += str(count)
        else:
            assert token in variables, token
            result += variables[token]
    return result

def add_call(text):
    assert text.startswith('Add(')
    quoted, escaped, depth = False, False, 1
    for index in range(4, len(text)):
        char = text[index]
        if quoted:
            if escaped:
                escaped = False
            elif char == '\\':
                escaped = True
            elif char == '"':
                quoted = False
        elif char == '"':
            quoted = True
        elif char == '(':
            depth += 1
        elif char == ')':
            depth -= 1
            if depth == 0:
                assert text[index + 1] == ';'
                return split(text[4:index]), text[index + 2:]
    raise AssertionError('Unclosed Add')

def constructed_cases(path, prefix, module):
    text = path.read_text(encoding='utf-8')
    assert 'CompiledContractOutcomeNames.Success' in text and 'CompiledContractOutcomeNames.Completion' in text
    text = text[text.index('        const string '):]
    rows, variables = [], {}
    while True:
        text = text.lstrip()
        if text.startswith('return cases;'):
            break
        constant = re.match(r'const string (\w+) = ("(?:\\.|[^"\\])*");', text)
        if constant:
            variables[constant[1]] = json.loads(constant[2])
            text = text[constant.end():]
            continue
        loop = re.match(r'foreach \(string (\w+) in new\[\] \{ ((?:"(?:\\.|[^"\\])*"(?:, )?)+) \}\)', text)
        if loop:
            args, text = add_call(text[loop.end():].lstrip())
            for raw in split(loop[2]):
                environment = variables | {loop[1]: json.loads(raw)}
                assert len(args) == 2
                name, body = [csharp_expression(arg, environment, len(rows)) for arg in args]
                rows.append((prefix + name, '-module(' + module + '). -export([run/0]). run()->' + body + ',done.'))
        else:
            args, text = add_call(text)
            assert len(args) == 2
            name, body = [csharp_expression(arg, variables, len(rows)) for arg in args]
            rows.append((prefix + name, '-module(' + module + '). -export([run/0]). run()->' + body + ',done.'))
    return rows

freeze = read(PREP / 'focus-source-freeze.json')
candidate = Path(freeze['Candidate'])
assert len(freeze['Files']) == 3004
assert len({row['Path'] for row in freeze['Files']}) == 3004
for row in freeze['Files']:
    file = candidate / row['Path']
    assert file.stat().st_size == row['Bytes'] and sha(file) == row['Sha256'], row['Path']
assert sha(PREP / 'targets.json') == freeze['TargetsSha256']
targets = read(PREP / 'targets.json')['Targets']
hosts = [
    'host:HaltLifecycleHostChecks:Run',
    'host:PriorityPermissionHostChecks:Run',
    'host:RuntimeActorDispatchHostChecks:Run',
    'host:SpawnAsyncDistributionHostChecks:RunDeadParent',
    'host:SpawnPriorityAdmissionHostChecks:AllReadyPriorityOrder',
    'host:SpawnPriorityGuestReentryHostChecks:EscapedCallbackMustReadmitOwner',
    'host:SpawnPrioritySynchronousBorrowHostChecks:SameTurnCallbackStaysSynchronous',
    'host:SpawnPriorityEpochHostChecks:LowPaysOncePerLogicalReduction',
    'host:SpawnPriorityWaitingHostChecks:WaitingHighDoesNotHoldCapacity',
    'host:SpawnPriorityQueuedChangeHostChecks:ReprioritizesAlreadyQueuedOwner',
    'host:SpawnPriorityRetirementHostChecks:HaltSettlesEscapedCallbackWithoutInvokingTarget',
    'host:SpawnPriorityRetirementHostChecks:KilledOwnerStillSettlesQueuedCleanup',
]
for target in hosts:
    _, owner, method = target.split(':')
    source = (candidate / ('tests/Erlang.Tests/' + owner + '.cs')).read_text(encoding='utf-8')
    assert re.search(r'\b(?:Task|ValueTask)\s+' + method + r'\s*\(', source), target
differential = candidate / 'tools/Erlang.Differential'
pressure = constructed_cases(differential / 'PortPressureCases.cs', 'erlang-port-pressure-', 'port_pressure')
native = constructed_cases(differential / 'PortNativeOptionCases.cs', 'erlang-port-native-option-', 'port_native_option')
assert len(pressure) == 19 and len(native) == 25
names = dict(re.findall(r'public const string (\w+) = "([^"]*)";', (differential / 'SpawnPriorityCaseNames.cs').read_text(encoding='utf-8')))
rawsources = (differential / 'SpawnPriorityCaseSources.cs').read_text(encoding='utf-8')
priority = []
for key in ['FirstObservation', 'FlagOldAndDefault', 'DuplicateInvalid']:
    match = re.search(r'public const string ' + key + r' = """\n(.*?)\n        """;', rawsources, re.S)
    assert match
    source = '\n'.join(line[8:] if line else '' for line in match[1].split('\n'))
    priority.append((names[key], source))
fixtures = pressure + native + priority
assert targets == hosts + [name for name, _ in fixtures] and len(set(targets)) == 59
defaults = (differential / 'CompiledModuleCase.cs').read_text(encoding='utf-8')
assert 'CallerContext = CompiledModuleCallerContext.UntrappedFresh' in defaults
assert 'CompilerPolicy = CompiledModuleCompilerPolicy.Default' in defaults
fixture_map = dict(fixtures)

def receipt(stem):
    result = read(RAW / (stem + '.exit.json'))
    assert result['ExitCode'] == 0 and result['Settled'] is True and result['TimedOut'] is False and result['Failures'] == [], stem
    assert isinstance(result['Pid'], int) and result['Pid'] > 0
    assert sha(RAW / (stem + '.stdout.log')) == result['StdoutSha256']
    assert sha(RAW / (stem + '.stderr.log')) == result['StderrSha256']
    assert (RAW / (stem + '.stderr.log')).stat().st_size == 0
    live = read(RAW / (stem + '.live.json'))
    assert live['Pid'] == result['Pid'] and live['Command'] == result['Command']
    assert isinstance(live['StartedUnixSeconds'], (int, float)) and live['StartedUnixSeconds'] > 0
    result['StartedUnixSeconds'] = live['StartedUnixSeconds']
    return result

build = receipt('build')
log = (RAW / 'build.stdout.log').read_text(encoding='utf-8-sig')
for patterns in [[r'^\s*(\d+) Warning\(s\)\s*$', r'^\s*Предупреждений:\s*(\d+)\s*$'], [r'^\s*(\d+) Error\(s\)\s*$', r'^\s*Ошибок:\s*(\d+)\s*$']]:
    values = [int(value) for pattern in patterns for value in re.findall(pattern, log, re.M)]
    assert values and all(value == 0 for value in values)
summary = read(RAW / 'summary.json')
assert summary['Passed'] == 59 and summary['Failed'] == 0
assert [row['Name'] for row in summary['Tests']] == targets
expected_assemblies = {'Erlang.Priority351.Corrections.dll', 'Erlang.Terms.dll', 'Erlang.Differential.dll', 'Erlang.Tests.dll', 'Erlang.Runtime.dll', 'Erlang.Compiler.dll', 'Erlang.Otp.Behaviours.dll'}
pids, loaded_set, observations = [build['Pid']], {}, []
loaded_cardinalities = {}
for index, target in enumerate(targets):
    stem = f'case-{index:03d}'
    row = read(RAW / (stem + '.json'))
    life = receipt(stem)
    pids.append(life['Pid'])
    assert summary['Tests'][index]['Passed'] is True and summary['Tests'][index]['ExecutionFailure'] is None
    assert summary['Tests'][index]['Result'] == stem + '.json'
    assert row['Name'] == target and row['Passed'] is True and row['Error'] is None
    assert life['Command'][-2:] == [target, str((RAW / (stem + '.json')).resolve())]
    observed = [(item['Location'], item['Sha256']) for item in row['Loaded']]
    wanted = expected_assemblies if target == hosts[0] else (expected_assemblies - {'Erlang.Compiler.dll', 'Erlang.Otp.Behaviours.dll'} if target in hosts else expected_assemblies | {'Erlang.Serialization.dll'})
    assert len(observed) == len(wanted) and {Path(location).name for location, _ in observed} == wanted
    loaded_cardinalities[target] = len(observed)
    current = {location: digest for location, digest in observed}
    assert len(current) == len(wanted)
    for location, digest in observed:
        assert Path(location).is_relative_to(candidate) and sha(Path(location)) == digest
    for location, digest in current.items():
        if location in loaded_set:
            assert loaded_set[location] == digest
        loaded_set[location] = digest
    if target in fixture_map:
        source_hash = hashlib.sha256(fixture_map[target].encode('utf-8')).hexdigest().upper()
        assert row['Expected'] == row['Actual'] == '{ok,done}'
        assert row['SourceSha256'] == source_hash, target
        assert re.fullmatch('[0-9A-F]{64}', row['GeneratedSha256'])
    else:
        assert all(row[key] is None for key in ['Actual', 'Expected', 'SourceSha256', 'GeneratedSha256'])
    observations.append({'Name': target, 'ReceiptSha256': sha(RAW / (stem + '.exit.json')), 'ResultSha256': sha(RAW / (stem + '.json')), 'Pid': life['Pid']})
identities = []
intervals = {}
for stem in ['build'] + [f'case-{index:03d}' for index in range(59)]:
    life = receipt(stem)
    identity = (life['Pid'], life['StartedUnixSeconds'])
    identities.append(identity)
    intervals.setdefault(life['Pid'], []).append((life['StartedUnixSeconds'], life['StartedUnixSeconds'] + life['ElapsedSeconds']))
assert len(set(identities)) == 60
for segments in intervals.values():
    segments.sort()
    assert all(previous[1] <= following[0] for previous, following in zip(segments, segments[1:]))
assert not OUT.exists()
result = {'Status': 'PASS_ACTUAL_CORRECTION_FOCUSED_59_INDEPENDENT_PASSIVE', 'Passed': 59, 'Failed': 0, 'HostTests': 12, 'PressureCompiled': 19, 'NativeOptionCompiled': 25, 'PriorityCompiled': 3, 'SourceClosureRows': 3004, 'SourceFreezeSha256': sha(PREP / 'focus-source-freeze.json'), 'TargetsSha256': sha(PREP / 'targets.json'), 'SummarySha256': sha(RAW / 'summary.json'), 'SettledProcessReceipts': 60, 'DistinctNumericPids': len(set(pids)), 'DistinctPidStartReceiptIdentities': len(set(identities)), 'ReusedNumericPidsHaveNonoverlappingSettledReceiptIntervals': True, 'LoadedAssembliesByTarget': loaded_cardinalities, 'LoadedAssemblyUnionCount': len(loaded_set), 'All59LazyLoadedSetsExactExpectedClassAndCurrentOutputDllHashes': True, 'InitialBuiltDllManifestCaptured': False, 'SourceAndExpectedIndependentlyReconstructedFromFrozenCSharpCatalog': True, 'CallerContextAndPolicy': 'UntrappedFresh/Default; actual reviewed harness passes fixture.CallerContext and rejects nondefault policy. These values are source-proven, not separately emitted metadata fields.', 'GeneratedHashes': 'Each actual observation has a valid64hex generated CSharp hash; compilation deterministic reexecution not performed by passive reviewer.', 'Scope': 'Managed bounded focused execution only; no original351 comparison, full pass, API compatibility promotion or performance claim.', 'Authorship': 'Reviewer authored three HOST migrations and independently reviewed two runtime patches; Root executed actual build/cases.', 'ReaderHistory': 'Initial reader5992 refused uniform Loaded7 assumption; actual reviewed harness lazy loads halt7, otherHOST5, compiled8. Original reader and failure receipt preserved; raw results unchanged. Reader2 later refused numericPID uniqueness; reader3 instead verifies distinct PID/start receipt identities and nonoverlapping reuse intervals, without claiming OS startticks capture.', 'Observations': observations}
OUT.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(sha(OUT))

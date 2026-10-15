"""Source-only metadata reseal. No SDK, VM, native process, Git command or import of runners."""
import argparse
import ast
import copy
import hashlib
import json
import pathlib
import re
import types

HERE = pathlib.Path(__file__).resolve().parent
REPOSITORY = HERE.parent.parent
CONFIG_SHA = '7C1406C9B3AAB4ECCADB2F9301992D3CF26D235B77AC2610BD3E048545446E02'
FOCUS_SHA = '346BAEDF611C1FA69B77CFF0EBF6C6F238A2967D9D95158D02E29D65984234F3'
LEGACY = 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'
OWN = 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'
LEGACY_WORKFLOW = '.github/workflows/nif-pending-native.yml'
OWN_WORKFLOW = '.github/workflows/nif-reallocation-native.yml'
ATTRIBUTE = b'docs/validation/part-317/** binary\n'


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(raw):
    return hashlib.sha256(raw).hexdigest().upper()


def lf(raw):
    return raw.replace(b'\r\n', b'\n')


def json_bytes(value):
    return (json.dumps(value, indent=2) + '\n').encode('utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate', required=True, type=pathlib.Path)
    parser.add_argument('--source-freeze', required=True, type=pathlib.Path)
    parser.add_argument('--focus-freeze', required=True, type=pathlib.Path)
    parser.add_argument('--tokens-before', required=True, type=pathlib.Path)
    parser.add_argument('--tokens-after', required=True, type=pathlib.Path)
    parser.add_argument('--output', required=True, type=pathlib.Path)
    parser.add_argument('--apply', action='store_true', help='Write only the five declared metadata files in the isolated candidate.')
    args = parser.parse_args()
    candidate = args.candidate.resolve()
    require(candidate.is_relative_to(REPOSITORY / 'artifacts') and candidate.is_dir(), 'Isolated artifact candidate required')
    require(not (candidate / '.git').exists(), 'No Git checkout mutation is permitted')
    output = args.output.resolve()
    require(output.is_relative_to(REPOSITORY / 'artifacts') and not output.is_relative_to(candidate) and not output.exists(), 'Fresh external artifact output required')
    config_raw = (HERE / 'configuration.json').read_bytes()
    require(sha(config_raw) == CONFIG_SHA, 'Proposal configuration changed')
    config = json.loads(config_raw)
    base = HERE / 'published316'
    for row in config['PublishedInputs']:
        raw = (base / row['Path']).read_bytes()
        require(len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], 'Published input changed: ' + row['Path'])
    parent_raw = (HERE / 'parent-source-resource-closure.json').read_bytes()
    require(sha(parent_raw) == config['WorkingParentClosureSha256'], 'Parent closure changed')
    parent = {row['Path']: row for row in json.loads(parent_raw)['Files']}
    owned = set(config['OwnedCSharpPaths'])
    focus_raw = args.focus_freeze.read_bytes()
    require(sha(focus_raw) == FOCUS_SHA, 'Exact corrected compiled focus authority required')
    focus = json.loads(focus_raw)
    require({row['Path'] for row in focus['Files']} == owned, 'Focus ownership changed')
    focus_candidate = args.focus_freeze.resolve().parent / 'candidate'
    for row in focus['Files']:
        raw = (focus_candidate / row['Path']).read_bytes()
        require(len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], 'Compiled focus source changed: ' + row['Path'])

    def token_map(path):
        result = {}
        for row in json.loads(path.read_bytes()):
            name = row['Path'].replace('\\', '/')
            matches = [key for key in owned if name == key or name.endswith('/' + key)]
            require(len(matches) == 1 and matches[0] not in result, 'Token path/duplicate changed')
            result[matches[0]] = row['Tokens']
        require(set(result) == owned, 'All thirty-three token streams required')
        return result

    require(token_map(args.tokens_before) == token_map(args.tokens_after), 'Formatting changed C# token streams')
    source_raw = args.source_freeze.read_bytes()
    source = json.loads(source_raw)
    require(source['ParentHead'] == config['Published316Head'] and {row['Path'] for row in source['Files']} == owned, 'Source ownership/parent changed')
    for row in source['Files']:
        raw = (candidate / row['Path']).read_bytes()
        require(len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], 'Formatted source changed: ' + row['Path'])
    metadata = {LEGACY, OWN, LEGACY_WORKFLOW, OWN_WORKFLOW, '.gitattributes'}
    for name, row in parent.items():
        if name not in owned | metadata:
            raw = (candidate / name).read_bytes()
            require(len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], 'Undeclared parent change: ' + name)
    keys = set(parent) | owned
    require(len(keys) == config['ExpectedComposedSourceFiles'], 'Unexpected source closure size')

    # Execute ONLY the exact published reviewed_inputs function AST. Its one Git
    # read is replaced by the already reviewed parent source-path list. No runner
    # top-level statements, subprocess, compiler or binary can execute here.
    selector_raw = (base / 'tools/Erlang.NifPendingVarargsObservations/run-reviewed.py').read_bytes()
    selector = next(node for node in ast.parse(selector_raw).body if isinstance(node, ast.FunctionDef) and node.name == 'reviewed_inputs')
    tracked = [name for name in parent if name.startswith('src/') or '/' not in name and (name.startswith('Directory.') or name in ('global.json', 'NuGet.Config', 'nuget.config'))]

    def tracked_inputs(command, cwd):
        require(command == ['git', 'ls-files', '-z', '--', 'src', 'global.json', 'Directory.*', 'NuGet.Config', 'nuget.config'] and cwd == candidate, 'Unexpected selector external action')
        return ('\0'.join(tracked) + '\0').encode()

    environment = {'pathlib': pathlib, 'subprocess': types.SimpleNamespace(check_output=tracked_inputs)}
    exec(compile(ast.Module(body=[selector], type_ignores=[]), '<published-reviewed-input-selector>', 'exec'), environment)
    selected = environment['reviewed_inputs'](candidate, candidate / LEGACY)
    original_legacy = json.loads((base / LEGACY).read_bytes())
    expected = {row['Path'] for row in original_legacy['Files']} | set(config['NativePlanAddedCompilePaths'])
    require(selected == expected, 'Native selector additions/removals differ from reviewed source audit')
    require(not set(config['ExcludedNewCompilePaths']) & selected, 'Unrelated Tests/Differential inputs entered native closure')

    proposed = {}
    original_attributes = lf((base / '.gitattributes').read_bytes())
    attributes = original_attributes.rstrip(b'\n') + b'\n' + ATTRIBUTE
    require(lf((candidate / '.gitattributes').read_bytes()) in (original_attributes, attributes), 'Unreviewed attribute change')
    proposed['.gitattributes'] = attributes

    def record(name, mode):
        raw = proposed[name] if name in proposed else (candidate / name).read_bytes()
        require(mode in ('TextLF', 'BinaryExact'), 'Unknown source hash mode')
        raw = lf(raw) if mode == 'TextLF' else raw
        return {'Path': name, 'Bytes': len(raw), 'Sha256': sha(raw), 'HashMode': mode}

    reports = []
    for plan_name, workflow, pin_name, count in [
        (LEGACY, LEGACY_WORKFLOW, 'REVIEWED_SOURCE_PLAN_SHA256', config['ExpectedLegacyInputs']),
        (OWN, OWN_WORKFLOW, 'REVIEWED_REALLOCATION_PLAN_SHA256', config['ExpectedOwnInputs']),
    ]:
        baseline_raw = (base / plan_name).read_bytes()
        require(lf((candidate / plan_name).read_bytes()) == baseline_raw, 'Plan already changed before this reseal: ' + plan_name)
        baseline = json.loads(baseline_raw)
        updated = copy.deepcopy(baseline)
        prior = {row['Path']: row for row in baseline['Files']}
        require(len(prior) == len(baseline['Files']), 'Duplicate baseline plan rows')
        names = set(prior) | set(config['NativePlanAddedCompilePaths'])
        require(len(names) == count and plan_name not in names and OWN_WORKFLOW not in names, 'Plan count/self-hash boundary changed')
        updated['Files'] = [record(name, prior[name]['HashMode'] if name in prior else 'TextLF') for name in sorted(names)]
        require({k: v for k, v in updated.items() if k != 'Files'} == {k: v for k, v in baseline.items() if k != 'Files'}, 'Non-Files policy changed')
        raw = json_bytes(updated)
        require(b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf') and raw.decode('utf-8').encode('utf-8') == raw, 'Noncanonical source plan')
        proposed[plan_name] = raw
        workflow_raw = lf((candidate / workflow).read_bytes())
        require(workflow_raw == (base / workflow).read_bytes(), 'Workflow changed before reseal')
        pattern = rb'(?m)^(\s*' + pin_name.encode() + rb': )[A-F0-9]{64}$'
        updated_workflow, matches = re.subn(pattern, lambda match: match[1] + sha(raw).encode(), workflow_raw)
        require(matches == 1, 'Exactly one literal workflow pin required')
        proposed[workflow] = updated_workflow
        reports.append({'Path': plan_name, 'Inputs': count, 'Sha256': sha(raw), 'Added': sorted(names - set(prior)), 'NonFilesFieldsUnchanged': True})
    for name in (LEGACY, OWN):
        for row in json.loads(proposed[name])['Files']:
            require(record(row['Path'], row['HashMode']) == row, 'Final dependency reseal mismatch: ' + row['Path'])
    output.mkdir(parents=True)
    for name, raw in proposed.items():
        destination = output / 'metadata-overlay' / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(raw)
        if args.apply:
            (candidate / name).write_bytes(raw)
    closure = {'Files': [{'Path': name, 'Bytes': len(raw), 'Sha256': sha(raw)} for name in sorted(keys) for raw in [proposed[name] if name in proposed else (candidate / name).read_bytes()]]}
    closure_raw = json_bytes(closure)
    (output / 'source-resource-closure.json').write_bytes(closure_raw)
    receipt = {'SourceOnly': True, 'CandidateMetadataApplied': args.apply, 'PublishedParentHead': config['Published316Head'], 'CorrectedCompiledFocusFreezeSha256': FOCUS_SHA, 'FinalFormattedSourceFreezeSha256': sha(source_raw), 'TokensBeforeSha256': sha(args.tokens_before.read_bytes()), 'TokensAfterSha256': sha(args.tokens_after.read_bytes()), 'All33TokenStreamsEqual': True, 'Plans': reports, 'SourceClosureFiles': len(keys), 'FinalSourceClosureSha256': sha(closure_raw), 'OwnedSourcePaths': len(owned | metadata), 'CSharpPaths': len(owned), 'ChangedMetadataPaths': sorted(proposed), 'AllNonFilesImportAbiPolicyFieldsUnchanged': True, 'SdkNativeVmOrBuildExecuted': False}
    (output / 'receipt.json').write_bytes(json_bytes(receipt))
    print(json.dumps(receipt, indent=2))


if __name__ == '__main__':
    main()

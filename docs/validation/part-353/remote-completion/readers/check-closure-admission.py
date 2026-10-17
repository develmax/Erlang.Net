"""Read-only actual3106/142 source admission and narrow negative controls; no remote reader execution."""
import argparse
import ast
import copy
import importlib.util
import json
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch
import common as m

directory = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('ci_closure_control', directory / 'review-ci.py')
ci = importlib.util.module_from_spec(spec)
spec.loader.exec_module(ci)


def refusal(operation):
    try: operation()
    except ValueError: return
    raise AssertionError('required source closure refusal missing')


def main(args):
    for name in ('common.py', 'review-ci.py', 'review-ordinary.py', 'root-transport-with-log.py',
                 'root-discover-runs.py', 'check-closure-admission.py'):
        ast.parse((directory / name).read_bytes(), name)
    root = args.repository.resolve()
    subject = SimpleNamespace(repository=root, expected_head='50e756f73437eb8d2882a19f729bccb09af74f3d',
        promotion_plan=root/'artifacts/part353-delivery-final4/promotion-plan.json',
        stage_guard=root/'artifacts/root-part353-final-stage/staged-guard.json',
        stage_guard_sha256='54ED0A88E11466C8BC7BB347ECE214E01734DF7ECFB2D901F15A7D0F91CE3B1A', publication_deltas=None)
    plan, published, stage_rows = m.publication(subject)
    composition = json.loads(published['docs/validation/part-353/full/raw/composition-plan.json'])
    differences = ci.verify_stable_source(subject, composition, published)
    packages = ci.verify_package_inputs(subject, composition['PackageResources'])
    original_read, original_blobs = m.read, m.blobs
    inventory_path = root / ci.CLOSURE_INVENTORY
    inventory = original_read(inventory_path)
    negative = 0
    for flag in ('RawFrozenToLfExact', 'PublishedRawSameParent'):
        modified = copy.deepcopy(inventory)
        next(r for r in modified['Differences'] if not r['Staged'])[flag] = False
        def read(path): return modified if Path(path) == inventory_path else original_read(path)
        with patch.object(m, 'read', read):
            refusal(lambda: ci.verify_stable_source(subject, composition, published))
            negative += 1
    def wrong_parent(repository, head, paths):
        values = original_blobs(repository, head, paths)
        if head == inventory['Parent']:
            values['.github/workflows/allocation-diagnostic.yml'] += b'changed'
        return values
    with patch.object(m, 'blobs', wrong_parent):
        refusal(lambda: ci.verify_stable_source(subject, composition, published))
        negative += 1
    def wrong_code(repository, head, paths):
        values = original_blobs(repository, head, paths)
        if head == subject.expected_head and 'src/Erlang.Runtime/CoreModules.cs' in values:
            values['src/Erlang.Runtime/CoreModules.cs'] += b'\n'
        return values
    with patch.object(m, 'blobs', wrong_code):
        refusal(lambda: ci.verify_stable_source(subject, composition, published))
        negative += 1
    m.fresh(args.report, {'Status':'PASS_ACTUAL_CLOSED3106_SOURCE_REPRESENTATION_PREFLIGHT_NOT_REMOTE_CI_READER',
        'Head':subject.expected_head,'StableRows':len(composition['StableClosure']),
        'ActualDifferences':len(differences),'UnchangedParentRawExactUtf8Lf':134,'PriorClosedStagedDifferences':8,
        'GenericCSharpPermission':False,'AdditionalCSharpDifferencesBeyondPriorFiveBlankLineProof':0,
        'InventorySha256':m.sha(inventory_path),'StageRows':stage_rows,'PackageDeclaredLfPaths':packages,
        'NegativeRefusals':negative,'RemoteCiReaderExecuted':False,'SdkExecuted':False,'NativeExecuted':False,
        'NetworkUsed':False,'MainOrGitMutated':False})


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    main(parser.parse_args())

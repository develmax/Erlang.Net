"""Atomic fresh source plan; never starts a VM or rewrites evidence."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Runtime/Erlang.OwnedGuest506Observations'
WORKFLOWS = ['.github/workflows/owned-guest506-original.yml', '.github/workflows/validate.yml']
DEPENDENCIES = [
    'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py',
    'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/bounded_inventory.py',
    'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py',
    'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py',
    'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/build-original.py',
    'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/manifest.json',
    'tools/prepare-regex-provider.ps1',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuestOriginal506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuestOriginal506Names.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedGuest506RuntimeFactory.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedShutdown506Checks.cs',
    'tests/Erlang.Tests/Runtime/Memory/OwnedShutdown506Names.cs',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/observed.etf',
    'tests/Erlang.Tests/Resources/OwnedGuestOriginal506/observed-deterministic.etf',
]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--staged-tree', type=Path)
    parser.add_argument('--replace-plan-sha256')
    args = parser.parse_args()
    repo = args.repository.resolve()
    tree = args.staged_tree.resolve() if args.staged_tree else repo
    tool = Path(__file__).resolve().parent
    if tool != tree/TOOL:
        raise RuntimeError('Sealer must be under the declared tracked tool path')
    plan_path = tool/'source-plan.json'
    if plan_path.exists():
        actual = hashlib.sha256(plan_path.read_bytes()).hexdigest().upper()
        if not args.replace_plan_sha256 or actual != args.replace_plan_sha256.upper():
            raise RuntimeError('Replacing a plan requires its current SHA; immutable history belongs outside the tool')
    rows = []
    allowed_root = {'.gitattributes', 'collect.py', 'seal.py', 'jobs.json', 'README.md'}
    for path in sorted(tool.rglob('*')):
        if not path.is_file() or path == plan_path:
            continue
        relative = path.relative_to(tool)
        if relative.parts[0] not in {'fixtures', 'golden'} and relative.as_posix() not in allowed_root:
            raise RuntimeError('Live output/unlisted source cannot enter plan: '+str(relative))
        data = path.read_bytes()
        rows.append(dict(Scope='Tool', Path=relative.as_posix(), HashMode='Raw',
                         Sha256=hashlib.sha256(data).hexdigest().upper(), Bytes=len(data)))
    for name in [*DEPENDENCIES, *WORKFLOWS]:
        path = (tree if name in WORKFLOWS else repo)/name
        mode = 'Raw' if path.suffix in {'.etf', '.beam', '.cs'} else 'TextLF'
        data = path.read_bytes()
        if mode == 'TextLF':
            data = data.replace(b'\r\n', b'\n')
        rows.append(dict(Scope='Repository', Path=name, HashMode=mode,
                         Sha256=hashlib.sha256(data).hexdigest().upper(), SealedBytes=len(data)))
    plan = dict(State='SEALED_SOURCE_NOT_EXECUTION_RECEIPT', PinnedCommit=PIN, ToolPath=TOOL,
                Inputs=rows, Excludes=['source-plan.json', 'all live artifact output'],
                WholeContractsClosed=0, GoldenProfile='Historical genuinely acquired paired335 raw whole protocol',
                ProviderProfiles=['official Windows library376 admission', 'existing pinned Linux build receipt'])
    data = (json.dumps(plan, indent=2)+'\n').encode('utf-8')
    descriptor, temporary = tempfile.mkstemp(prefix='.source-plan-', suffix='.tmp', dir=tool)
    try:
        with os.fdopen(descriptor, 'wb') as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, plan_path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)
    print(json.dumps(dict(SourcePlanSha256=hashlib.sha256(data).hexdigest().upper(), Inputs=len(rows),
                          Executed=False, WholeContractsClosed=0)))


if __name__ == '__main__':
    main()

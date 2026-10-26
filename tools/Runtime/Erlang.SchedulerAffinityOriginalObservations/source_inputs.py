import hashlib
import json
import os
from pathlib import Path
import subprocess

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL_PATH = 'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations'


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def write(path, value):
    Path(path).write_bytes((json.dumps(value, indent=2) + '\n').encode())


def require_normal_receipt(receipt):
    assert receipt['ExitCode'] == 0 and receipt['ParentReaped'] and receipt['StreamsClosed']
    assert not receipt['TerminationRequested'] and not receipt['RemainingGroupKillRequested']
    assert receipt['CleanupFailure'] is None


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], timeout=30)


def verify(repository, manifest_sha256, expected_head):
    repository = Path(repository).resolve()
    tool = Path(__file__).resolve().parent
    assert tool == repository / TOOL_PATH
    assert git(repository, 'rev-parse', 'HEAD').decode().strip() == expected_head
    path = tool / 'manifest.json'
    assert digest(path) == manifest_sha256.upper()
    manifest = json.loads(path.read_bytes())
    assert manifest['OriginalCommit'] == PIN
    for row in manifest['Files']:
        assert digest(tool / row['Path']) == row['Sha256'], row
    for row in manifest['Dependencies'] + [manifest['Workflow']]:
        assert digest(repository / row['Path']) == row['Sha256'], row
    return repository, tool, manifest


def source_identity(source):
    source = Path(source).resolve()
    assert git(source, 'rev-parse', 'HEAD').decode().strip() == PIN
    assert not git(source, 'status', '--porcelain', '--untracked-files=no').strip()
    entries = parse_index_inventory(git(source, 'ls-files', '--stage', '-z'))
    rows = [source_file_identity(source, path, mode) for mode, path in entries]
    return {'Commit': PIN, 'TrackedFiles': rows}


def parse_index_inventory(data):
    entries = sorted(value for value in data.split(b'\0') if value)
    result = []
    for entry in entries:
        metadata, name = entry.split(b'\t', 1)
        mode, object_id, stage = metadata.split(b' ')
        assert stage == b'0' and mode in (b'100644', b'100755', b'120000'), entry
        result.append((mode.decode('ascii'), name.decode('utf-8')))
    return result


def source_file_identity(source, name, mode):
    path = source / name
    assert mode in ('100644', '100755', '120000'), mode
    if mode == '120000':
        assert path.is_symlink(), name
        # Git's symlink blob stores the link text, even for directory/dangling targets.
        data = os.fsencode(os.readlink(path))
    else:
        assert path.is_file() and not path.is_symlink(), name
        data = path.read_bytes()
    return {'Path': name, 'Mode': mode, 'Bytes': len(data), 'Sha256': hashlib.sha256(data).hexdigest().upper()}


def sanitized_environment(environment):
    result = dict(environment)
    for key in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERL_ROOTDIR']:
        result.pop(key, None)
    return result

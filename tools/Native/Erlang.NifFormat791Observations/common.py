"""Shared source-bound orchestration. Importing this module executes no child."""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
PACKAGE = Path(__file__).resolve().parent
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
HELPER = 'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py'
PASSIVE = 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/passive_etf.py'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()


def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def require(value, message):
    if not value:
        raise RuntimeError(message)


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def check(plan):
    require(plan['Pin'] == PIN and plan['Platform'] == 'windows-x64', 'Wrong plan pin/platform')
    require(sha(plan['DotnetPath']) == plan['DotnetSha256'], 'Dotnet host changed')
    for row in plan['Inputs']:
        require(sha(row['Path']) == row['Sha256'], 'Sealed input changed: ' + row['Path'])
    require(snapshot(Path(plan['ManagedHost']).parent) == plan['ManagedInputs'], 'Managed host/native asset closure changed')


def snapshot(directory):
    return [{'Path': str(path), 'Sha256': sha(path)} for path in sorted(directory.rglob('*'))
            if path.is_file() and path.suffix.lower() in ('.dll', '.json', '.pdb', '.exe')]


def validate_rows(repo, path, subject):
    decoder = load(repo / PASSIVE, 'functional_printf_mutex_passive')
    value = decoder.decode_exact(Path(path).read_bytes())
    require(value[0] == 'tuple' and len(value[1]) == 2 and value[1][0] == ('atom', 'loaded'), 'Observer did not load/run: ' + str(path))
    rows = value[1][1]
    if subject['Kind'] == 'snprintf':
        require(rows[0] == 'list' and len(rows[1]) == 41, 'Expected all41 printf rows')
        names = json.loads((PACKAGE / 'snprintf-row-names.json').read_bytes())
        for index, row in enumerate(rows[1]):
            require(row[0] == 'tuple' and len(row[1]) == 5, 'Printf row shape')
            fields = row[1]
            require(fields[0] == ('atom', names[index]), 'Printf row name/order differs')
            require(fields[1][0] == fields[2][0] == fields[4][0] == 'integer', 'Printf integer columns')
            require(fields[3][0] == 'binary' and len(fields[3][1]) == 128, 'Full128-byte printf capture required')
        return 41
    require(rows[0] == 'tuple' and len(rows[1]) == 8 and all(item[0] == 'integer' for item in rows[1]), 'Expected exact8-int mutex tuple')
    return 8

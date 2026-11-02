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



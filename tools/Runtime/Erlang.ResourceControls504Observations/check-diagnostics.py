"""Pure counter-tamper controls. Never starts a VM, compiler, or subprocess."""
import importlib.util
from pathlib import Path
from unittest.mock import patch


def integer(value):
    return ('integer',value)


def numbers(values):
    return ('tuple',tuple(integer(value) for value in values))


def row(label, queued, status, details):
    return ('tuple', (('atom',label),integer(0),('tuple',(numbers(queued),numbers(status),numbers(details)))))


def diagnostic(rows):
    return ('tuple',(('atom','diagnostics'),('list',tuple(rows)),('list',())))


def main():
    path = Path(__file__).resolve().parent / 'collect.py'
    spec = importlib.util.spec_from_file_location('resource504_counter_controls',path)
    collector = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(collector)
    held = row('held',(0,0),(1,1,0,0),(1,1,0,0,0,0,0,0))
    queued = row('queued',(1,1),(1,1,0,0),(1,1,0,0,0,0,0,0))
    # A cancelled queued owner need never enter its native body. Deliberately
    # unequal CPU/IO totals prevent an accidental hardcoded two-per-kind check.
    cleanup = row('cleanup',(0,0),(2,1,2,1),(2,1,2,1,0,0,1,2))
    accepted = collector.check_native_diagnostics(diagnostic([held,queued,cleanup]))
    if not accepted['PhysicalClosureConfirmed'] or accepted['EnteredCpu'] != 2 or accepted['EnteredIo'] != 1:
        raise RuntimeError('real per-kind completion was replaced by a fixed body count')
    refused = []
    for field,value in [(4,1),(5,1),(6,0),(6,2),(7,0),(2,1),(2,3),(3,0),(3,2)]:
        values = [2,1,2,1,0,0,1,2]
        values[field] = value
        refused.append(diagnostic([held,queued,row('cleanup',(0,0),values[:4],values)]))
    refused += [diagnostic([]),diagnostic([queued,cleanup]),diagnostic([held,cleanup]),
        diagnostic([held,queued]),diagnostic([held,queued,row('cleanup',(1,0),(2,1,2,1),(2,1,2,1,0,0,1,2))]),
        diagnostic([held,queued,row('cleanup',(0,0),(2,1,1,1),(2,1,2,1,0,0,1,2))]),
        diagnostic([held,queued,row('cleanup_failed',(0,0),(2,1,2,1),(2,1,2,1,0,0,1,2)),cleanup]),
        diagnostic([row('held',(0,0),(1,1,0,0),(1,1,0,0,1,0,0,0)),queued,cleanup]),
        diagnostic([held,row('queued',(0,0),(1,1,0,0),(1,1,0,0,0,0,0,0)),cleanup])]
    with patch('subprocess.Popen',side_effect=RuntimeError('child execution is forbidden in pure controls')):
        for index,value in enumerate(refused):
            try:
                collector.check_native_diagnostics(value)
            except (RuntimeError,ValueError,TypeError,IndexError):
                continue
            raise RuntimeError('tampered diagnostic accepted: ' + str(index))
    print('PASS pure physical-counter controls: one unequal-kind success, ' + str(len(refused)) + ' rejected mutations; no original/SDK execution')


if __name__ == '__main__':
    main()

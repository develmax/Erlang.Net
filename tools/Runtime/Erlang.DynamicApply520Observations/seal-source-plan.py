"""Hash source inputs only; never compiles or invokes an Erlang VM."""
from pathlib import Path
import hashlib
import json
repo = Path.cwd().resolve()
tool = Path(__file__).resolve().parent
paths = [
    tool / 'collect-original.py',
    tool / 'seal-source-plan.py',
    *sorted((tool / 'fixtures').glob('*.erl')),
    repo / 'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py',
    repo / 'tools/Native/Erlang.NifMapFamily504Observations/diagnostic_etf.py',
    repo / 'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py',
    repo / 'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py',
    repo / 'tests/Erlang.Tests/Resources/ApplyOriginal477/odbc.beam',
    repo / 'tests/Erlang.Tests/Resources/ApplyOriginal477/fixture.json',
    *sorted((repo / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts').glob('*.py')),
]

rows = []
for path in paths:
    mode = 'Raw' if path.is_relative_to(tool) or path.suffix in ('.beam', '.etf') else 'TextLF'
    data = path.read_bytes()
    if mode == 'TextLF':
        data = data.replace(b'\r\n', b'\n')
    rows.append(dict(
        Path=path.relative_to(repo).as_posix(),
        HashMode=mode,
        Sha256=hashlib.sha256(data).hexdigest().upper(),
    ))

out = tool / 'source-plan.json'
payload = dict(
    Pin='ad05823719d77c8faee87348ea39513d4e2f99c5',
    State='SOURCE_ONLY_NOT_EXECUTED',
    Inputs=rows,
)
out.write_bytes((json.dumps(payload, indent=2) + '\n').encode('utf-8'))
print(hashlib.sha256(out.read_bytes()).hexdigest().upper())

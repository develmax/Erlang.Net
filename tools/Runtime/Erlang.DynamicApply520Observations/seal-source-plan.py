"""Hash source inputs only; never compiles or invokes an Erlang VM."""
from pathlib import Path
import hashlib,json
repo=Path.cwd().resolve();tool=Path(__file__).resolve().parent
paths=[tool/'collect-original.py',tool/'seal-source-plan.py',*sorted((tool/'fixtures').glob('*.erl')),
 repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py',
 repo/'tools/Native/Erlang.NifMapFamily504Observations/diagnostic_etf.py',
 repo/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py',
 repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py',
 repo/'tests/Erlang.Tests/Resources/ApplyOriginal477/odbc.beam',
 repo/'tests/Erlang.Tests/Resources/ApplyOriginal477/fixture.json',
 *sorted((repo/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts').glob('*.py'))]
rows=[dict(Path=p.relative_to(repo).as_posix(),Sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()) for p in paths]
out=tool/'source-plan.json';out.write_text(json.dumps(dict(Pin='ad05823719d77c8faee87348ea39513d4e2f99c5',State='SOURCE_ONLY_NOT_EXECUTED',Inputs=rows),indent=2)+'\n');print(hashlib.sha256(out.read_bytes()).hexdigest().upper())

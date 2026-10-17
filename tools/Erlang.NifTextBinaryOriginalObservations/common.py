"""Reuse the published owned/provenance helpers; bind this separate fixture closure."""
import importlib.util
from pathlib import Path
BASE = Path(__file__).resolve().parents[1] / 'Erlang.NifEntryOriginalObservations'
spec = importlib.util.spec_from_file_location('term_original_base', BASE / 'common.py')
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)
for key in dir(base):
    if not key.startswith('_'):
        globals()[key] = getattr(base, key)
TOOL = 'tools/Erlang.NifTextBinaryOriginalObservations'

def verify(repository, expected, manifest_sha):
    assert sys.platform.startswith('linux') and os.uname().machine == 'x86_64'
    root = Path(repository).resolve()
    tool = root / TOOL
    assert tool == Path(__file__).resolve().parent
    assert subprocess.check_output(['git','--no-optional-locks','-C',str(root),'rev-parse','HEAD'],text=True,timeout=20).strip() == expected
    assert not os.environ.get('GITHUB_SHA') or os.environ['GITHUB_SHA'] == expected
    assert digest(tool/'manifest.json') == manifest_sha.upper()
    plan = read(tool/'manifest.json')
    assert plan['PinnedOtpCommit'] == PIN
    for prefix, rows in [(tool,plan['Files']),(root,plan['Dependencies'])]:
        assert len(rows) == len({x['Path'] for x in rows})
        for row in rows:
            path = (prefix/row['Path']).resolve()
            assert path.is_relative_to(prefix) and path.stat().st_size == row['Bytes'] and digest(path) == row['Sha256']
    assert digest(root/plan['Workflow']['Path']) == plan['Workflow']['Sha256']
    identity = module(root/'tools/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','term_identity')
    owned = module(root/'tools/Erlang.GuestConnectedSendObservations/owned_process.py','term_owned')
    decoder = module(root/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py','term_decoder')
    return root,tool,plan,identity,owned,decoder

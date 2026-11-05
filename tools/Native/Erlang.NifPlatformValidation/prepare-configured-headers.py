"""Copy pinned public headers plus the actual target-configured integer sizes."""
import argparse
import hashlib
import json
import platform
import shutil
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--repository', type=Path, required=True)
parser.add_argument('--otp-source', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.repository.resolve()
otp = args.otp_source.resolve()
output = args.output.resolve()
assert output.is_relative_to(root / 'artifacts') and not output.exists()
baseline = json.loads((root / 'reference-baseline.json').read_text())
assert subprocess.check_output(['git', '-C', str(otp), 'rev-parse', 'HEAD'], text=True).strip() == baseline['commit']
configured = list((otp / 'erts/include').glob('*/erl_int_sizes_config.h'))
assert len(configured) == 1, configured
output.mkdir(parents=True)
for name in ('erl_nif.h', 'erl_nif_api_funcs.h', 'erl_drv_nif.h'):
    original = subprocess.check_output(['git', '-C', str(otp), 'show', baseline['commit'] + ':erts/emulator/beam/' + name])
    (output / name).write_bytes(original)
shutil.copyfile(configured[0], output / 'erl_int_sizes_config.h')
receipt = {'OtpBaseline': baseline, 'HostOS': platform.system(), 'HostArchitecture': platform.machine(),
           'GeneratedSizesSource': str(configured[0]),
           'Headers': {p.name: hashlib.sha256(p.read_bytes()).hexdigest().upper() for p in output.glob('*.h')},
           'OriginalVmExecuted': False}
(output / 'headers-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')

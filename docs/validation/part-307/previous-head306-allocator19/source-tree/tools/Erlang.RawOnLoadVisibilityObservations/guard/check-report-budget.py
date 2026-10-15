import importlib.util
import hashlib
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
root = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('synthetic_guard_report_writer', root / 'verify-original9.py')
guard = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guard)
output = Path(sys.argv[1])
assert not output.exists()
output.mkdir()
small = output / 'small.json'
guard.write_report(small, {'Status': 'SYNTHETIC', 'Value': 1}, 128)
assert json.loads(small.read_bytes())['Value'] == 1 and not Path(str(small) + '.partial').exists()
large = output / 'refused.json'
try:
    guard.write_report(large, {'Values': list(range(100))}, 64)
except ValueError as error:
    assert str(error) == 'Aggregate serialized report byte budget'
else:
    raise AssertionError('Oversized report accepted')
partial = Path(str(large) + '.partial')
assert not large.exists() and partial.is_file() and partial.stat().st_size <= 64
try:
    guard.write_report(large, {}, 64)
except ValueError as error:
    assert str(error) == 'Refuse prior partial report'
else:
    raise AssertionError('Prior partial overwritten')
report = {'Status': 'SYNTHETIC_REPORT_SERIALIZATION_BUDGET_PASS', 'PositiveReportPassed': True, 'ByteBudgetRefusalPassed': True, 'PartialEvidenceRetained': True, 'PartialOverwriteRefused': True, 'OriginalExecuted': False, 'SdkCompared': False, 'NativeExecuted': False, 'GuardSha256': hashlib.sha256((root / 'verify-original9.py').read_bytes()).hexdigest().upper()}
(output / 'report.json').write_bytes((json.dumps(report, indent=2) + '\n').encode())
print(json.dumps(report))

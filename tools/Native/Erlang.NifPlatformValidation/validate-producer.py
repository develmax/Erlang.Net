"""Bind an original-OTP CI artifact to the current fixture and bridge sources."""
import hashlib
import json
import platform
from pathlib import Path

root = Path.cwd()
area = root / 'artifacts/nif-platform-ci'
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
receipt = json.loads((area / 'native/build-receipt.json').read_text())
assert receipt['OtpBaseline'] == json.loads((root / 'reference-baseline.json').read_text())
assert receipt['HostOS'] == platform.system()
def architecture(value):
    return {'aarch64': 'arm64', 'amd64': 'x86_64'}.get(value.lower(), value.lower())
assert architecture(receipt['HostArchitecture']) == architecture(platform.machine())
for name, expected in receipt['Sources'].items():
    assert digest(root / name) == expected.upper(), name
assert digest(area / 'native/Erlang.NifBridge.so') == receipt['LibrarySha256'].upper()
image = json.loads((area / 'image/build-receipt.json').read_text())
assert image['OriginalExecuted'] is True
assert digest(root / 'tools/Native/Erlang.NifPlatformValidation/nif_platform_fixture.c') == image['SourceSha256'].upper()
assert digest(area / 'image/ebin/nif_platform_fixture.beam') == image['BeamSha256'].upper()
assert digest(area / 'image/original.etf') == image['OriginalEtfSha256'].upper()
observed = {'HostOS': platform.system(), 'HostArchitecture': platform.machine(), 'ProducerBoundToCurrentSources': True, 'OriginalLibrarySha256': digest(area / 'image/original/nif_platform_fixture.so'), 'BeamSha256': image['BeamSha256'], 'OriginalEtfSha256': image['OriginalEtfSha256']}
(area / 'image/unmodified-provenance.json').write_text(json.dumps(observed, indent=2) + '\n')

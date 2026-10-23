from pathlib import Path
r=Path.cwd()
s=(r/'artifacts/library449-build-focus4.py').read_text().replace('library449-build-focus4','library450-build-focus1').replace('runtime/distribution449/remote-epoch-keeps-old-reference-and-rejects-stale-input','runtime/distribution450/losing-candidate-preserves-live-queue-and-generation');(r/'artifacts/library450-build-focus1.py').write_text(s)
s=(r/'artifacts/library449-prepare-package1.py').read_text().replace('library449-driver-package1','library450-driver-package1');(r/'artifacts/library450-prepare-package1.py').write_text(s)

"""Compose a successor passive reader using the exact previously reviewed local transport routines."""
from pathlib import Path
import hashlib
R=Path.cwd();O=R/'artifacts/successor346-ci-independent';O.mkdir(exist_ok=True)
p=R/'artifacts/successor345-ci-independent/review.py';b=p.read_bytes();assert hashlib.sha256(b).hexdigest().upper()=='490531D8F31A8988C432BD7D1D6683D20B5747184C18D42C446624F19893FFBA'
prefix=b.decode().split("head='fe3dc9582997ee1365289e8f08ff20da14e74cb0';",1)[0].replace("O=A/'successor345-ci-independent'","O=A/'successor346-ci-independent'")
body=(O/'body.txt').read_text(encoding='utf-8');(O/'review.py').write_text(prefix+body,encoding='utf-8')

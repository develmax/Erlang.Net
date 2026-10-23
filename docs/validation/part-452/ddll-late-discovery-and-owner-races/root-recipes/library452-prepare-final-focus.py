from pathlib import Path
import hashlib,json
r=Path.cwd();p=r/'tests/Erlang.Tests/RetiredCandidateHostChecks452.cs';after=p.read_bytes()
before=after.replace(b'                    Require(!adoption.IsCompleted);\n',b'').replace(b'            Require(!obsolete.IsCompleted);\n',b'')
selection=json.loads((r/'artifacts/library452-affected-selection1/selection.json').read_bytes())
assert hashlib.sha256(before).hexdigest().upper()==selection['Bindings'][p.relative_to(r).as_posix()]
receipt={'OnlyTestGuardsChanged':True,'GuardPurpose':'Prove adoption is actually deferred, not an immediate badarg, before retiring the old generation','PriorSha256':hashlib.sha256(before).hexdigest().upper(),'FinalSha256':hashlib.sha256(after).hexdigest().upper(),'RuntimeBefore':hashlib.sha256((r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()).hexdigest().upper(),'Prior591Passed':True,'FinalWholeSourceCiRequired':True}
(r/'artifacts/library452-final-test-guards.json').write_text(json.dumps(receipt,indent=2)+'\n')
s=(r/'artifacts/library452-build-focus3.py').read_text().replace('library452-build-focus3','library452-build-focus4');(r/'artifacts/library452-build-focus4.py').write_bytes(s.encode())

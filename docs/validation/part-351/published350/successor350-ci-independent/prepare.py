from pathlib import Path
import ast,json,hashlib
R=Path.cwd();O=R/'artifacts/successor350-ci-independent';prior=R/'artifacts/successor346-ci-independent/review3.py';s=prior.read_text().split("head='83dde925",1)[0].replace("O=A/'successor346-ci-independent'","O=A/'successor350-ci-independent'")+(O/'body.txt').read_text();ast.parse(s)
(O/'review.py').write_text(s,encoding='utf-8',newline='\n');sha=lambda b:hashlib.sha256(b).hexdigest().upper()
freeze={'Status':'SOURCE_ONLY_PASSIVE350_CI_READER_BEFORE_EXECUTION','Files':[{'Path':n,'Sha256':sha((O/n).read_bytes())} for n in ['prepare.py','body.txt','review.py']],'StageGuardSha256':sha((R/'artifacts/root-part350-stage-guard.json').read_bytes()),'ObservedHead':'c1f219f31726fc93e537080f400651d300c47d72','ObservedRun':38983525835,'ObservedJob':117249607117,'ObservedArtifact':12082372762,'CaptureExecuted':False,'Sdk':False,'Network':False,'Main':False}
(O/'source-freeze.json').write_text(json.dumps(freeze,indent=2)+'\n',encoding='utf-8');print(sha((O/'source-freeze.json').read_bytes()))

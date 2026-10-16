from pathlib import Path
import json,hashlib,ast
R=Path.cwd();O=R/'artifacts/successor349-ci-independent';prior=R/'artifacts/successor346-ci-independent/review3.py';prefix=prior.read_text().split("head='83dde925",1)[0].replace("O=A/'successor346-ci-independent'","O=A/'successor349-ci-independent'")
s=prefix+(O/'body.txt').read_text();ast.parse(s);(O/'review.py').write_text(s,encoding='utf-8',newline='\n');sha=lambda b:hashlib.sha256(b).hexdigest().upper()
g={'Status':'SOURCE_ONLY_PASSIVE349_CI_READER_DRAFT','ExpectedHead':'712933b97a654a3b6ed42250902017bde0740f17','ExpectedRun':38981313422,'FutureJobArtifactRequiredCli':True,'SourceFiles':[{'Path':p,'Sha256':sha((O/p).read_bytes())} for p in ['prepare.py','body.txt','review.py']],'ExactStageGuardSha256':sha((R/'artifacts/root-part349-stage-guard.json').read_bytes()),'CaptureExecuted':False,'Sdk':False,'Network':False,'Main':False}
(O/'source-freeze.json').write_text(json.dumps(g,indent=2)+'\n',encoding='utf-8');print(sha((O/'source-freeze.json').read_bytes()))

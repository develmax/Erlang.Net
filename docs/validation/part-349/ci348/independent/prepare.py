from pathlib import Path
import ast,hashlib,json
R=Path.cwd();O=R/'artifacts/successor348-ci-independent'
prior=R/'artifacts/successor346-ci-independent/review3.py'
prefix=prior.read_text(encoding='utf-8').split("head='83dde925",1)[0]
prefix=prefix.replace("O=A/'successor346-ci-independent'","O=A/'successor348-ci-independent'")
body=R/'artifacts/successor348-ci-independent/body.txt'
text=prefix+body.read_text(encoding='utf-8');ast.parse(text)
target=O/'review.py';assert not target.exists();target.write_text(text,encoding='utf-8',newline='\n')
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
report={'Status':'SOURCE_ONLY_PASSIVE348_CI_READER_DRAFT_AST_VALID','PreviousTransportSourceSha256':sha(prior.read_bytes()),'TransportAndBlobFunctionsReusedWithoutChanges':True,'ExpectedHead':'d1bd571fc1f655abf81bdc0336b7133f3d023fac','ExpectedRun':38979387792,'FutureJobAndArtifactAreRequiredCliArguments':True,'CaptureExecuted':False,'Sdk':False,'Network':False,'MainChanged':False,'Files':[{'Path':n,'Sha256':sha((O/n).read_bytes())} for n in ['prepare.py','body.txt','review.py']]}
(O/'source-freeze.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(sha((O/'source-freeze.json').read_bytes()))

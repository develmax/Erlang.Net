from pathlib import Path
import hashlib, json, ast
r=Path.cwd();a=r/'artifacts';src=a/'root-compose328-historical.py';d=a/'root-compose328-historical-v2.py'
h=lambda q:hashlib.sha256(q.read_bytes()).hexdigest().upper()
f=a/'undefined-lambda328-candidate1/source-freeze.json'
assert h(f)=='F4CDE0FBFAC4674201D2E1F13D968CA3DA590A85B1D020294D2A0D14B8A53BCD'
expected=json.loads(f.read_bytes())['BaselineZipSha256']
assert h(a/'undefined-lambda328-candidate1/baseline-source-resources.zip')==expected
old="assert sha(bz)=='F4E18B91C93903112B9DA76C0CFD9CC55C8C5C4AE040688120C66C0E89A562'"
text=src.read_text();assert text.count(old)==1 and not d.exists()
text=text.replace(old,"assert sha(baseline/'source-freeze.json')=='F4CDE0FBFAC4674201D2E1F13D968CA3DA590A85B1D020294D2A0D14B8A53BCD'\nassert sha(bz)==json.loads((baseline/'source-freeze.json').read_bytes())['BaselineZipSha256']")
ast.parse(text);d.write_text(text)
receipt={'Status':'SOURCE_PRECONDITION_LITERAL_SHA_TRANSCRIPTION_CORRECTED_BEFORE_OUTPUT_OR_SDK','InitialComposerSha256':h(src),'CorrectedComposerSha256':h(d),'InitialExitCode':1,'InitialOutputCreated':False,'BaselineZipChanged':False,'ExpectedZipSha256FromVerifiedAuthorFreeze':expected,'SdkOrNativeExecuted':False}
(a/'root328-historical-composer-precondition.json').write_text(json.dumps(receipt,indent=2)+'\n')
print(json.dumps(receipt))

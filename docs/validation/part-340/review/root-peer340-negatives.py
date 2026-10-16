from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();d=r/'artifacts/connected-negative-fixtures340-proposal2';c=r/'artifacts/connected-remote-completion-candidate'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
assert h(d/'source-freeze.json')=='C8DBE4CB6DC7FF4CE6795727037F797006FCD958070138FF6A2810F7DEA813EB'
f=j(d/'source-freeze.json')
for x in f['Files']:
 p=d/'source'/x['Path'];assert p.stat().st_size==x['Bytes'] and h(p)==x['Sha256']
for x in f['Parents']:
 p=r/x['Path'];assert p.stat().st_size==x['Bytes'] and h(p)==x['Sha256']
old=j(r/'artifacts/connected-remote-completion-full-actual3/source-freeze.json')
production=[x for x in old['Files'] if x['Path'].startswith('src/')]
for x in production:
 p=c/x['Path'];assert p.stat().st_size==x['Bytes'] and h(p)==x['Sha256']
assert len(production)==56
pin=f['PinnedOtpCommit'];native=subprocess.check_output(['git','-C','../otp','show',pin+':erts/emulator/beam/dist.h'])
assert b'DOP_ALTACT_SIG_SEND     = 37' in native and b'ERTS_DOP_ALTACT_SIG_FLG_ALIAS   (1 << 2)' in native
report=dict(Status='PASS_SOURCE_PEER_TWO_HISTORICAL_NEGATIVE_FIXTURE_MIGRATIONS',ProposalSha256=h(d/'source-freeze.json'),PinnedOtp=pin,ProductionFilesUnchanged=56,ChangedTestFiles=3,OldNamesPreserved=f['OldNamesPreserved'],Checks=['ALIAS capability absent remains NotSupported','Negotiated ALIAS with wrong arity remains ArgumentException','ALTACT Alias bit with PID target is malformed, not unknown flag','Truly unsupported raw ALTACT PID frame replaces newly implemented spawn request','All41 raw input assertions retained'],NativeHeaderSha256=hashlib.sha256(native).hexdigest().upper(),MainEdited=False,SdkExecuted=False,PreviousFullPassed=7191,PreviousFullFailed=2)
out=r/'artifacts/root-340-negative-fixtures-source-review.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n');print(h(out))

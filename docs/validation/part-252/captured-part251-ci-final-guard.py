from pathlib import Path
import hashlib,json
r=Path.cwd()
g=json.loads((r/'artifacts/part251-ci-guard.json').read_text(encoding='utf-8-sig'))
p=json.loads((r/'artifacts/part-251-ci-latest-provenance.json').read_text(encoding='utf-8-sig'))
assert p['Run']['head_sha']==g['Head'] and p['Job']['head_sha']==g['Head']
assert p['Run']['id']==g['Run'] and p['Job']['id']==g['Job']
assert p['Run']['conclusion']==p['Job']['conclusion']=='success'
assert p['Job']['labels']==['windows-latest']
assert all(s['status']=='completed' and s['conclusion']=='success' for s in p['Job']['steps'])
raw=(r/'artifacts/part-251-ci-job.log').read_bytes()
digest=json.loads((r/'artifacts/part-251-ci-job.log.digest.json').read_text(encoding='utf-8-sig'))
assert hashlib.sha256(raw).hexdigest().upper()==digest['SHA256']
log=raw.decode('utf-8-sig')
assert '6468/6468 passed' in log and 'Validation passed' in log
assert 'FAIL oracle/' not in log
g.update(ProvenanceLogPending=False,JobLogSha256=digest['SHA256'],AllActualJobStepsSuccess=True,SafeRawLogMarkers=['6468/6468 passed','Validation passed'])
(r/'artifacts/part251-ci-final-guard.json').write_text(json.dumps(g,indent=2)+'\n',encoding='utf-8')
print('PASS exact-head251 CI6468/full API/archive/reports/raw-log provenance')

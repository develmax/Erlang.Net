from pathlib import Path
import json,hashlib
r=Path('artifacts');j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));p=j(r/'part-266-ci-latest-provenance.json');g=j(r/'part266-ci-report-guard.json');d=j(r/'part-266-ci-job.log.digest.json');assert p['Run']['head_sha']==g['Head'] and p['Job']['conclusion']=='success' and p['Job']['labels']==['windows-latest']
assert all(x['status']=='completed' and x['conclusion']=='success' for x in p['Job']['steps']);b=(r/'part-266-ci-job.log').read_bytes();assert hashlib.sha256(b).hexdigest().upper()==d['SHA256'];s=b.decode('utf-8-sig');assert '6483/6483 passed' in s and 'Validation passed' in s
assert p['Run']['id']==38563967326 and p['Job']['id']==115851824835
g.update(Prior257AllocationFailurePreserved=True,Prior257AllocationCauseEstablished=False,RawJobLogAuditPending=False,JobLogSha256=d['SHA256'],AllJobStepsSuccess=True);(r/'part266-ci-final-guard.json').write_text(json.dumps(g,indent=2)+'\n');print('PASS exactheadCI6483/full with rawjob provenance')

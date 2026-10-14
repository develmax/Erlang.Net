from pathlib import Path
import json,hashlib
r=Path('artifacts');j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));p=j(r/'part-265-ci-latest-provenance.json');g=j(r/'part265-ci-report-guard.json');d=j(r/'part-265-ci-job.log.digest.json');assert p['Run']['head_sha']==g['Head'] and p['Job']['conclusion']=='success' and p['Job']['labels']==['windows-latest']
assert all(x['status']=='completed' and x['conclusion']=='success' for x in p['Job']['steps']);b=(r/'part-265-ci-job.log').read_bytes();assert hashlib.sha256(b).hexdigest().upper()==d['SHA256'];s=b.decode('utf-8-sig');assert '6482/6482 passed' in s and 'Validation passed' in s
assert p['Run']['id']==38560476440 and p['Job']['id']==115840027671
g.update(Prior257AllocationFailurePreserved=True,Prior257AllocationCauseEstablished=False,RawJobLogAuditPending=False,JobLogSha256=d['SHA256'],AllJobStepsSuccess=True);(r/'part265-ci-final-guard.json').write_text(json.dumps(g,indent=2)+'\n');print('PASS exactheadCI6482/full with rawjob provenance')

from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';mp=a/'pack297-ci-detached-staging-manifest.json'
assert hashlib.sha256(mp.read_bytes()).hexdigest().upper()=='D8958DE60CFBF696212E9C670FB0A482DC9C3F9AA1DA986771B334B6A45D849A'
rows=json.loads(mp.read_bytes());assert len(rows)==12
for x in rows:
 b=(r/x['SourcePath']).read_bytes();assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256'];assert x['Destination'].startswith('docs/validation/part-298/')
g=json.loads((a/'pack297-ci-38714602504-independent-guard.json').read_bytes())
assert g['Head']=='26e158fc1440002b9269896d0ba1a9cf5b1bbdd5' and g['Status']=='PASS_EXACT_HEAD297_CI6609_FULL'
assert (g['Passed'],g['Failed'])==(6609,0) and g['OrderedNamesExact'] and len(g['Integration'])==11
(a/'root-part298-previous297-ci-manifest-guard.json').write_text(json.dumps({'Status':'PASS','Files':12,'Head':g['Head'],'Passed':6609,'OrderedNamesExact':True,'IntegrationGroups':11},indent=2)+'\n')
print('PASS previous297 CI manifest12/exact6609/full')

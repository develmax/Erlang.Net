from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';mp=a/'pack296-original-detached-staging-manifest-for298.json'
assert hashlib.sha256(mp.read_bytes()).hexdigest().upper()=='7D4A9A150915E77C9823C4FA1FD674218208F90B8B927DC2F412BA13F743D9EB'
rows=json.loads(mp.read_bytes());assert len(rows)==18
for x in rows:
 p=r/x['SourcePath'];b=p.read_bytes();assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256']
 assert x['Destination'].startswith('docs/validation/part-298/') and 'LICENSE.local' not in x['Destination']
g=json.loads((a/'pack296-original-38710732869-semantic-guard.json').read_bytes())
assert g['Head']=='1d98eade866ff83ed3e1496169520fcf3d1e0c6b' and g['Attempt']==1
assert (g['Passed'],g['Failed'],g['FdPassed'])==(5846,0,8)
assert not g['Historical4903Module778Expression165DiagnosticRowsExact']
assert g['PredeclaredNativeMapStdoutVarianceRows']==1 and not g['RawNormalization']
assert g['HistoricalSourceExpectedSdkGeneratedAndPoliciesExact'] and g['AllHistoricalExpressionAndDiagnosticRowsExact'] and g['SixExclusionsContextPolicyExact']
(a/'root-part298-previous296-manifest-guard.json').write_text(json.dumps({'Status':'PASS','Files':18,'PreviousHead':g['Head'],'SemanticPassed':5846,'FdPassed':8,'StrictRawHistoryExact':False,'PredeclaredMapVarianceRows':1,'RawNormalized':False},indent=2)+'\n')
print('PASS previous296 manifest18, semantic5846/fd8; one predeclared raw map variance retained')

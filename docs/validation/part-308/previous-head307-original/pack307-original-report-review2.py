"""Correct conservative report metadata; retain the original report/manifest unchanged."""
from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();a=r/'artifacts';h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
p=a/'pack307-original-38757595924-semantic-guard.json';v=j(p);strict=a/'pack307-original-strict-audit-attempt.json';s=j(strict)
assert s['ExitCode']==0 and s['Stderr']=='' and v['HistoricalRawRowsExact'] and v['PredeclaredNativeMapStdoutVarianceRows']==0 and not v['RawNormalization']
d=a/'pack307-watch-38757595924-terminal';items=j(d/'artifacts.json')['artifacts'];assert len(items)==1
actual=j(d/f'artifact-{items[0]["id"]}/differential.json')
path='docs/validation/part-305/previous-head303-original/pack303-watch-38736737943-terminal/artifact-11974851398/differential.json'
baseline=json.loads(subprocess.check_output(['git','cat-file','blob','8b83560372b63b4760604ac088802fb74ac1c112:'+path],cwd=r))
for key,count in [('Results',778),('ModuleResults',4911),('DiagnosticResults',165)]:assert actual[key]==baseline[key] and len(actual[key])==count
v.update({'Historical4911Module778Expression165DiagnosticRowsExact':True,'HistoricalExactBaseline':'published303 capture retained at '+path,'RawStrictAuditPassed':True,'MetadataCorrectionOnly':True,'NoEvidenceBytesChanged':True,'PriorConservativeReportSha256':h(p),'StrictAttemptSha256':h(strict),'OriginalRunReexecuted':False})
out=a/'pack307-original-38757595924-semantic-guard-review2.json';assert not out.exists();out.write_text(json.dumps(v,indent=2)+'\n')
mp=a/'pack307-original-detached-staging-manifest.json';rows=j(mp)
for q in [out,Path(__file__).resolve()]:
 relative=q.relative_to(a.resolve()).as_posix() if q.is_absolute() else q.relative_to(a).as_posix()
 rows.append({'SourcePath':'artifacts/'+relative,'Destination':'docs/validation/part-308/previous-head307-original/'+relative,'Bytes':q.stat().st_size,'Sha256':h(q)})
target=a/'pack307-original-detached-staging-manifest-review2.json';assert not target.exists();target.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps({'Status':'METADATA_REVIEW2_STRICT_RAW303_BASELINE_EXACT','Guard':out.name,'Manifest':target.name,'Files':len(rows)}))

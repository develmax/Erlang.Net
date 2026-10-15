"""Root-only metadata proposal. Uses historical published293 report; runs no tests."""
from pathlib import Path
import json
r=Path.cwd();p=r/'docs/module-readiness-scope.json';s=json.loads(p.read_bytes());assert not any(x['Id']=='PressureObservations' for x in s['Components'])
s['ComparisonTestReport']='validation/part-293/tests.json'
s['Components'].append({'Id':'PressureObservations','Project':'tools/Erlang.DistributionPressureObservations','Status':'Partially compatible','Implemented':['Part294: actual standalone Main tool target,14 pre-write CLI refusals and two supplied-DLL raw capture/owned-child cleanup controls pass. Two owned C# token streams and13 source-linked framework files are bound. These are infrastructure/tool checks, not new Main tests or original pressure results.'],'Remaining':['Pinned original probe compilation and three fresh-VM observations pending; typed semantics require independent review. Native soft busy versus SDK hard queue limits, two-channel terminal ordering, controller death, generation replacement, physical accounting and broad shutdown remain incomplete.'],'TestPrefixes':[]})
p.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Added partial PressureObservations; historical published293 comparison; no tests run')

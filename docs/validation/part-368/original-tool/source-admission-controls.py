"""Passive malformed-gate regression controls; never invokes collector main/SDK/VM."""
from pathlib import Path
import importlib.util,json,copy,hashlib
T=Path(__file__).resolve().parent;R=T.parents[1]
def load(p,n):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
m=load(T/'collect-original-corrected.py','worker_gate_controls');profile=json.loads((T/'profile.json').read_bytes())
paths=[(T if x['Location']=='tool' else R)/x['Path'] for x in profile['FixtureSources']]
valid={'Status':'PASS_FIXTURE_PARSE_GATE','Fixtures':[dict(Path=str(p.resolve()),Sha256=m.digest(p),Passed=True) for p in paths]}
controls=[]
def rejected(name,change):
 x=copy.deepcopy(valid);change(x)
 try:m.require_parse_gate(x,paths)
 except (AssertionError,ValueError,KeyError):controls.append(name);return
 raise AssertionError('Invalid gate accepted: '+name)
m.require_parse_gate(valid,paths);controls.append('actual_unique_named_source_count_accepted')
rejected('failed_fixture_row_rejected',lambda x:x['Fixtures'][0].update(Passed=False))
rejected('error_field_on_pass_row_rejected',lambda x:x['Fixtures'][0].update(Error='retained failure'))
rejected('duplicate_source_path_rejected',lambda x:x['Fixtures'].__setitem__(1,dict(x['Fixtures'][0])))
rejected('wrong_source_hash_rejected',lambda x:x['Fixtures'][0].update(Sha256='0'*64))
rejected('wrong_source_path_rejected',lambda x:x['Fixtures'][0].update(Path=str(T/'not-a-fixture.erl')))
rejected('missing_source_rejected',lambda x:x['Fixtures'].pop())
rejected('stale_three_source_gate_rejected',lambda x:x.update(Fixtures=x['Fixtures'][:3]))
rejected('wrong_gate_status_rejected',lambda x:x.update(Status='FAIL_FIXTURE_PARSE_GATE'))
original=R.parents[1]/'proplists22-original-ci-capture1/run-39050410387/artifact-12107706600/capture/old59/stack-observations.etf'
# Old12 count/schema control uses synthetic complete rows, not managed expected stacks.
labels=[(('integer',arity),('atom',mode),('integer',depth)) for arity in [2,3] for mode in ['tail','non_tail'] for depth in [0,1,2]]
rows=('list',tuple(('tuple',label+(('tuple',(('atom','error'),('atom','function_clause'),('list',()))),)) for label in labels))
m.require_old59_stack_rows(rows);controls.append('unchanged_old59_twelve_schema_accepted')
try:m.require_old59_stack_rows(('list',rows[1]+rows[1][:6]))
except AssertionError:controls.append('obsolete_old59_eighteen_schema_rejected')
else:raise AssertionError('Old18 count admitted')
print(json.dumps({'Status':'PASS_PASSIVE_WORKER_SOURCE_GATE_CONTROLS','Controls':controls,'UniqueSources':len(paths)},indent=2))

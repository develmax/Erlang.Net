from pathlib import Path
import json,hashlib,ast
O=Path(__file__).resolve().parent;R=O.parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
stages=[]
def add(name,script,args):
 stages.append({'Name':name,'Script':script,'ScriptSha256':sha(R/script),'Arguments':args})
add('erlang-old','artifacts/erlang370-retrospective-peer1/compare.py',['--native','artifacts/erlang370-original-failure1/raw/capture','--native-admission','artifacts/erlang370-retrospective-peer1/acceptance.json'])
add('erlang-new','artifacts/erlang370-closure-admission-proposal1/compare.py',['--native','artifacts/erlang-original370-closure-remote1/run-39108122243/artifact-12129576088/capture','--native-admission','artifacts/erlang370-closure-admission-proposal1/actual1/acceptance.json'])
for name,script,tool,native in [
 ('primary','binary369-comparison-successor1/compare.py','binary-original369-original1/tool','binary369-original-remote1/run-39083021286/artifact-12119578493/capture'),
 ('supplement','binary369-supplement-execution-proposal2/compare-binary.py','binary-original369-supplement1/tool','binary369-supplement-remote1/run-39083564610/artifact-12120102622/capture'),
 ('closure','binary369-closure-execution-proposal1/compare-binary.py','binary-original369-closure1/tool','binary369-closure-remote1/run-39084998283/artifact-12120398030/capture'),
 ('insert','binary369-insert-execution-proposal1/compare-binary.py','binary-original369-insert1/tool','binary369-insert-remote1/run-39085952486/artifact-12121017293/capture')]:
 args=['--tool','artifacts/'+tool,'--native','artifacts/'+native]
 if name=='primary':args+=['--native-admission','artifacts/binary369-original-admission-proposal2/actual1/acceptance.json']
 add('binary-'+name,'artifacts/'+script,args)
for name,script,admit,tool in [('erlang371','artifacts/erlang371-root/compare-final-erlang.py','artifacts/erlang371-passive-proposal3/actual1/acceptance.json','artifacts/erlang371-next-original-proposal1/tool'),('ordsets371','artifacts/ordsets371-full-passive-proposal1/compare.py','artifacts/ordsets371-full-passive-proposal1/actual1/acceptance.json','artifacts/ordsets371-remaining-original-proposal3/tool')]:
 admission=J(R/admit)
 add(name,script,['--native',admission['NativeCapture'],'--native-admission',admit,'--native-admission-sha256',sha(R/admit),'--tool',tool,'--expected-head',admission['Head'],'--manifest-sha256',admission['ManifestSha256']])
(O/'comparison-inputs.json').write_text(json.dumps({'Stages':stages,'NoPolicyChanged':True,'FreshFinalCapturedAndLiveDllEqualityRequired':True},indent=2)+'\n')

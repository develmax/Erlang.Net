from pathlib import Path
import subprocess,json,sys
r=Path.cwd();py=sys.executable
stages=[
('primary','binary369-comparison-successor1/compare.py','binary-original369-original1/tool','binary369-original-remote1/run-39083021286/artifact-12119578493/capture'),
('supplement','binary369-supplement-execution-proposal2/compare-binary.py','binary-original369-supplement1/tool','binary369-supplement-remote1/run-39083564610/artifact-12120102622/capture'),
('closure','binary369-closure-execution-proposal1/compare-binary.py','binary-original369-closure1/tool','binary369-closure-remote1/run-39084998283/artifact-12120398030/capture'),
('insert','binary369-insert-execution-proposal1/compare-binary.py','binary-original369-insert1/tool','binary369-insert-remote1/run-39085952486/artifact-12121017293/capture')]
rows=[]
for name,script,tool,native in stages:
 out='artifacts/erlang371-binary-'+name+'-comparison1'
 cmd=[py,'-B','artifacts/'+script,'--tool','artifacts/'+tool,'--native','artifacts/'+native,'--sdk','artifacts/erlang371-binary-retained-main1/binary-'+name+'/capture1/sdk','--output',out]
 if name=='primary':cmd+=['--native-admission','artifacts/binary369-original-admission-proposal2/actual1/acceptance.json']
 result=subprocess.run(cmd,capture_output=True)
 log=r/'artifacts/erlang371-root'/('binary-'+name+'-comparison1');log.mkdir(exist_ok=False)
 (log/'stdout.log').write_bytes(result.stdout);(log/'stderr.log').write_bytes(result.stderr)
 row=dict(Name=name,Command=cmd,ExitCode=result.returncode);rows.append(row);(log/'execution.json').write_text(json.dumps(row,indent=2)+'\n');print(json.dumps(row))
(r/'artifacts/erlang371-root/binary-comparisons-settlement1.json').write_text(json.dumps(rows,indent=2)+'\n')

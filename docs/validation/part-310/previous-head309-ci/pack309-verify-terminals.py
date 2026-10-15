"""Independent passive audits. Preserve every attempt, never dispatch or run Erlang."""
from pathlib import Path
import json,hashlib,subprocess,sys
a=Path.cwd()/'artifacts'
def execute(label,script,*args):
 out=a/f'pack309-{label}-audit-attempt.json'
 if out.exists():return json.loads(out.read_bytes())['ExitCode']==0
 command=[sys.executable,'-B',str(a/script),*map(str,args)];p=subprocess.run(command,capture_output=True,text=True)
 record={'Arguments':command,'ScriptSha256':hashlib.sha256((a/script).read_bytes()).hexdigest().upper(),'ExitCode':p.returncode,'Stdout':p.stdout,'Stderr':p.stderr,'NativeExecuted':False,'OriginalCompilerExecuted':False,'MainEdited':False,'PostExecuted':False}
 out.write_text(json.dumps(record,indent=2)+'\n');print(json.dumps({'Audit':label,'ExitCode':p.returncode}));return p.returncode==0
for kind,rid in [('ci',38766513916),('beam',38766554804)]:
 d=a/f'pack309-watch-{rid}-terminal'
 if not (d/'download-manifest.json').exists():continue
 if not execute(f'terminal-{rid}','pack309-terminal-envelope.py',rid):continue
 if json.loads((d/'run.json').read_bytes())['conclusion']=='success':
  if kind=='ci':execute('ci','pack309-ci-terminal-guard.py',rid,'ci')
  else:execute('beam','pack309-beam-terminal-guard.py')
 if not (a/f'pack309-{kind}-part310-detached-staging-manifest.json').exists():execute(f'{kind}-part310-staging','pack309-detached-staging.py',kind)

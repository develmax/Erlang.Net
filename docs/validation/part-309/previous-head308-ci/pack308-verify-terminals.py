"""Detached read-only audits, no native execution, dispatch or source edits."""
from pathlib import Path
import json,subprocess,sys,hashlib
r=Path.cwd();a=r/'artifacts';sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def execute(label,script,*args):
 out=a/f'pack308-{label}-audit-attempt.json'
 if out.exists():return json.loads(out.read_bytes())['ExitCode']==0
 command=[sys.executable,'-B',str(a/script),*map(str,args)]
 result=subprocess.run(command,capture_output=True,text=True)
 record={'Arguments':command,'VerifierSha256':sha(a/script),'ExitCode':result.returncode,'Stdout':result.stdout,'Stderr':result.stderr,'NativeExecuted':False,'MainEdited':False,'PostExecuted':False}
 out.write_text(json.dumps(record,indent=2)+'\n');print(json.dumps({'Audit':label,'ExitCode':result.returncode}))
 return result.returncode==0
for kind,rid in [('ci',38763479716),('original',38763659694),('native',38763668519)]:
 d=a/f'pack308-watch-{rid}-terminal'
 if not (d/'download-manifest.json').is_file():continue
 if not execute(f'terminal-{rid}','pack308-terminal-envelope.py',rid):continue
 conclusion=json.loads((d/'run.json').read_bytes())['conclusion']
 if kind=='ci' and conclusion=='success':execute('ci','pack308-ci-terminal-guard.py',rid,'ci')
 if kind=='original' and conclusion=='success':
  execute('original-strict','pack308-original-strict-guard.py',rid,'original')
  execute('original-semantic','pack308-original-semantic-guard.py',rid,'original')
 if kind=='native':
  passed=execute('native-envelope-sdk','pack308-native-terminal-guard.py',rid)
  if passed and conclusion=='success':
   typed=execute('original13-typed','pack308-original13-detached-capture.py')
   cross=execute('native-generated-cross-platform','pack308-native-cross-platform-generated-source-guard.py')
   if typed and cross:execute('native-combined','pack308-native-join-guard.py')
 if not (a/f'pack308-{kind}-detached-staging-manifest.json').exists():execute(f'{kind}-detached-staging','pack308-detached-staging.py',kind)

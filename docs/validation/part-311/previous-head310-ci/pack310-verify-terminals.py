"""Capture each passive audit once, including all failures. Never invokes native tools."""
from pathlib import Path
import hashlib,json,subprocess,sys
a=Path.cwd()/'artifacts';j=lambda p:json.loads(p.read_bytes())
def execute(label,script,*args):
 out=a/f'pack310-{label}-audit-attempt.json'
 if out.exists():return j(out)['ExitCode']==0
 command=[sys.executable,'-B',str(a/script),*map(str,args)];p=subprocess.run(command,capture_output=True,text=True);record={'Arguments':command,'ScriptSha256':hashlib.sha256((a/script).read_bytes()).hexdigest().upper(),'ExitCode':p.returncode,'Stdout':p.stdout,'Stderr':p.stderr,'NativeExecuted':False,'OriginalCompilerExecuted':False,'MainEdited':False,'PostExecuted':False};out.write_text(json.dumps(record,indent=2)+'\n');print(json.dumps({'Audit':label,'ExitCode':p.returncode}));return p.returncode==0
for kind in ['ci','native']:
 mp=a/f'pack310-{kind}-run-map.json'
 if not mp.exists():continue
 mapping=j(mp);d=a/f'pack310-watch-{mapping["Run"]}-terminal'
 if not (d/'download-manifest.json').is_file():continue
 if not execute(f'{kind}-terminal','pack310-terminal-envelope.py',kind):continue
 if kind=='ci' and j(d/'run.json')['conclusion']=='success':execute('ci','pack310-ci-terminal-guard.py')
 if kind=='native':
  sdk=execute('native-sdk','pack310-native-terminal-guard.py')
  if sdk and j(d/'run.json')['conclusion']=='success':
   original=execute('original13-typed','pack310-original13-detached-capture.py')
   if original:execute('native-combined','pack310-native-join-guard.py')
 if not (a/f'pack310-{kind}-part311-detached-staging-manifest.json').exists():execute(f'{kind}-part311-staging','pack310-detached-staging.py',kind)

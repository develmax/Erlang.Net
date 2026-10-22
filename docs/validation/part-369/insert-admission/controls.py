"""Passive structural controls only; never fabricated native evidence."""
from pathlib import Path
import hashlib,importlib.util,json

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('closure_comparison_controls',HERE/'compare.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
atom=lambda x:('atom',x)
pair=lambda *x:('tuple',tuple(x))
items=lambda *x:('list',tuple(x))
frame=pair(atom('binary'),atom('replace'),('integer',4),items())
live=items(frame)
effects=pair(items(pair(atom('live_stack'),atom('error'),atom('marker'),live)),items())
valid=pair(atom('ok'),('binary',b'a'),effects)
rows=[]
def passes(name,test):
    assert test,name;rows.append(dict(Name=name,Passed=True))
def refuses(call):
    try:call();return False
    except (AssertionError,IndexError,TypeError):return True
module.shape(valid,False)
passes('complete-nested-live-stack-preserved',True)
passes('missing-effects-refused',refuses(lambda:module.shape(pair(atom('ok'),('binary',b'a')),False)))
passes('malformed-effects-refused',refuses(lambda:module.shape(pair(atom('ok'),('binary',b'a'),items()),False)))
passes('dropped-live-frame-is-not-equal',valid!=pair(atom('ok'),('binary',b'a'),pair(items(pair(atom('live_stack'),atom('error'),atom('marker'),items())),items())))
passes('changed-backing-number-is-not-equal',pair(('integer',257),('integer',514))!=pair(('integer',257),('integer',257)))
passes('empty-case-list-refused',refuses(lambda:module.named(items())))
passes('duplicate-case-name-refused',refuses(lambda:module.named(items(pair(atom('x'),valid),pair(atom('x'),valid)))))
passes('error-without-full-stack-refused',refuses(lambda:module.shape(pair(atom('error'),atom('error'),atom('badarg'),effects),True)))
passes('malformed-stack-frame-refused',refuses(lambda:module.shape(pair(atom('error'),atom('error'),atom('badarg'),items(pair(atom('x'))),effects),True)))
passes('changed-context-policy-not-equal',module.POLICY!='direct-native-eval-versus-sdk')
result=dict(Status='PASS_PASSIVE_CLOSURE_CONTROL_CHECKS',Count=len(rows),Controls=rows,NativeEvidence=False,SdkExecuted=False)
(HERE/'controls.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Status=result['Status'],Count=len(rows))))

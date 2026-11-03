from pathlib import Path
import json
root=Path.cwd();p=root/'artifacts/library404-io-printable-command-contract-review2';otp=root.parent/'otp'
def lines(path):return path.read_text(encoding='utf-8').splitlines()
def complete_function(ls,needle):
 i=next(i for i,s in enumerate(ls) if needle in s);j=next(j for j in range(i+1,len(ls)) if ls[j]=='}');return '\n'.join(ls[i:j+1])
checks=[]
for file,needle in [('erts/emulator/beam/erl_unicode.c','BIF_RETTYPE io_printable_range_0'),('erts/emulator/sys/common/erl_sys_common_misc.c','void erts_set_printable_characters'),('erts/emulator/sys/common/erl_sys_common_misc.c','int erts_get_printable_characters')]:
 pin=complete_function(lines(p/'authority'/file),needle);live=complete_function(lines(otp/file),needle);checks.append(dict(Section=needle,BodyTextEqualToLive=pin==live,PinnedBody=pin,Authority='PINNED bytes regardless of equality'))
file='erts/emulator/beam/erl_init.c'
def startup(ls):
 i=next(i for i,s in enumerate(ls) if 'if (!sys_strncmp(argv[i],"-pc",3))' in s);j=next(j for j in range(i+1,len(ls)) if 'erts_set_printable_characters(printable_chars)' in ls[j]);return '\n'.join(ls[i:j+2])
checks.append(dict(Section='startup valid range mapping',BodyTextEqualToLive=startup(lines(p/'authority'/file))==startup(lines(otp/file)),PinnedBody=startup(lines(p/'authority'/file)),Authority='PINNED bytes regardless of equality'))
(p/'body-crosscheck.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
assert all(x['BodyTextEqualToLive'] for x in checks)
print('PINNED_GETTER_SETTER_STARTUP_BODIES_CONFIRMED',len(checks))

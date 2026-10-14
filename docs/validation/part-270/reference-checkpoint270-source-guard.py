from pathlib import Path
import json,subprocess,sys,re
head=sys.argv[1];freeze=json.loads(Path('artifacts/pid-order-source-freeze.json').read_text())
for row in freeze['Files']:
 blob=subprocess.check_output(['git','show',head+':'+row['Path']]).decode('utf-8')
 assert blob.replace('\r\n','\n')==row['SourceContent'].replace('\r\n','\n'),row['Path']
cases=subprocess.check_output(['git','show',head+':tools/Erlang.Differential/IdentifierOrderCases.cs']).decode()
assert len(re.findall(r'"identifier-order-[^"]+"',cases))==11
print('Committed270 three source bindings and eleven appended declarations verified')

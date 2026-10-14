from pathlib import Path
import json,hashlib
ns={};exec(Path('artifacts/part276-faithful-etf-parser.py').read_bytes(),ns);decode=ns['decode'];project=ns['project'];r=Path('artifacts/part-278-native269-queue-pinned-distribution-queue-contrasts/distribution-queue-contrasts');facts={}
for scenario in ['small','large_binary','two_binaries','two_buffers','handle_domain','input_counter','input_malformed']:
 folder=r/scenario;facts[scenario]={p.name:decode(p.read_bytes()) for p in sorted(folder.glob('*.etf'))};print(scenario)
 for name,val in facts[scenario].items():
  if any(x in name for x in ['stat','option','wire-shape','outcome','cleanup','profile','reason','delivered']):print(name,repr(project(val)))
p=Path('artifacts/native269-part278-queue-faithful-typed-facts.json');p.write_bytes((json.dumps({'Facts':facts},indent=2)+'\n').encode());print('SHA',hashlib.sha256(p.read_bytes()).hexdigest().upper())

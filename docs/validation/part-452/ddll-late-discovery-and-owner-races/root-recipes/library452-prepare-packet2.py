from pathlib import Path
import json
r=Path.cwd();old=r/'artifacts/library452-probe1';out=r/'artifacts/library452-probe2';assert not out.exists();out.mkdir()
for f in old.iterdir():
 if f.is_file():(out/f.name).write_bytes(f.read_bytes())
p=out/'ddll_tcp452.erl';s=p.read_text();needle='    ok=file:write_file(Ready,integer_to_binary(Port),[exclusive]),';assert needle in s
s=s.replace(needle,'    Publishing=Ready++".publishing",\n    ok=file:write_file(Publishing,integer_to_binary(Port),[exclusive]),\n    ok=file:rename(Publishing,Ready),');p.write_bytes(s.encode())
p=out/'RestartPeer452.cs';s=p.read_text();needle='''            if (File.Exists(ready) && int.TryParse(File.ReadAllText(ready), out readyPort) && readyPort > 0 && readyPort <= ushort.MaxValue)
                break;''';assert needle in s
s=s.replace(needle,'''            try
            {
                if (File.Exists(ready) && int.TryParse(File.ReadAllText(ready), out readyPort) && readyPort > 0 && readyPort <= ushort.MaxValue)
                    break;
            }
            catch (IOException error) when ((error.HResult & ushort.MaxValue) is 32 or 33)
            {
                // Retry only Windows sharing/lock violations within the existing readiness deadline.
            }''');p.write_bytes(s.encode())
p=out/'run-root.py';s=p.read_text();needle='''     if ready.exists():
      value=ready.read_text()
      if value.isdecimal() and 0<int(value)<65536:ready_port=int(value);break''';assert needle in s
s=s.replace(needle,'''     if ready.exists():
      try:
       value=ready.read_text()
       if value.isdecimal() and 0<int(value)<65536:ready_port=int(value);break
      except OSError as error:
       if getattr(error,'winerror',None) not in (32,33):raise''');p.write_bytes(s.encode())
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':str(old),'Reason':'Run3 outgoing old_timer passes real timer/handoff then restart fails on Windows ready-file sharing violation. Publish ready atomically after close using exclusive temp file/rename; readers retry only sharing/lock winerror32/33 within same deadlines. No semantic runtime, common worker, discovery callback, Program/handshake or expected-result change. Preserve failed run3; retain five fully successful groups with their own immutable packet1 bindings and bind readiness-only packet2 supplement explicitly.'},indent=2)+'\n')
same=['Program.cs','DistributionTcp452.cs','DistributionTcp452.Initiator.cs','beam_ddll_distribution452.erl','ddll452_epmd.erl']
assert all((out/n).read_bytes()==(old/n).read_bytes() for n in same)
print('Readiness-only successor2 prepared; all semantic observer/worker/callback/handshake sources unchanged')

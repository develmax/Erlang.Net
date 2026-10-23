from pathlib import Path
import json
r=Path.cwd();old=r/'artifacts/library451-probe1';out=r/'artifacts/library451-probe2';assert not out.exists();out.mkdir()
for f in old.iterdir():
 if f.is_file():(out/f.name).write_bytes(f.read_bytes())
p=out/'run-root.py';s=p.read_text();needle="    while not ready.exists() and process.poll() is None and time.monotonic()<deadline:time.sleep(.05)\n    assert ready.exists() and process.poll() is None,'Original server preparation failed: '+role\n    ports.append(ready.read_text())";assert needle in s
s=s.replace(needle,"""    ready_port=None
    while process.poll() is None and time.monotonic()<deadline:
     if ready.exists():
      value=ready.read_text()
      if value.isdecimal() and 0<int(value)<65536:ready_port=int(value);break
     time.sleep(.05)
    assert ready_port is not None and process.poll() is None,'Original server port preparation failed: '+role
    ports.append(str(ready_port))""");p.write_text(s)
p=out/'RestartPeer451.cs';s=p.read_text();start=s.index('        while (!File.Exists(ready)');end=s.index('\n        }\n        catch',start)
s=s[:start]+'''        int readyPort = 0;
        while (!process.HasExited && deadline.Elapsed < TimeSpan.FromSeconds(20))
        {
            if (File.Exists(ready) && int.TryParse(File.ReadAllText(ready), out readyPort) && readyPort > 0 && readyPort <= ushort.MaxValue)
                break;
            Thread.Sleep(20);
        }

        if (readyPort <= 0 || readyPort > ushort.MaxValue || process.HasExited || process.Id == oldId)
            throw new IOException("Owned restarted peer port preparation failed");

        Port = readyPort;'''+s[end:];p.write_text(s)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':str(old),'Reason':'First incoming child received empty port from a just-created ready file, before guest/runtime fixture startup. Require actual numeric TCP port within the existing deadline in original parent and restart reader; do not accept file existence alone. Preserve failed run1, forced cleanup, and the independently passing outgoing group. No product, scenario expectation, source observer or Erlang BEAM changes.'},indent=2)+'\n')

from pathlib import Path
import shutil,json
out=Path('artifacts/library449-probe5');assert not out.exists();shutil.copytree('artifacts/library449-probe4',out)
p=out/'RestartPeer449.cs';s=p.read_text().replace('if (!old.WaitForExit(10000) || old.ExitCode != 0)','if (!old.WaitForExit(10000) || !old.HasExited)');p.write_text(s)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library449-probe4','Reason':'Process.GetProcessById is not the process creator and .NET refuses ExitCode; retained handle still joins actual OS death before restart. Actual Python owner continues to require old process exit0/unforced in finally; no removal of exit-code acceptance condition.'},indent=2)+'\n')
print('Successor5 prepared')

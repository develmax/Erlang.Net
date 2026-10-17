from pathlib import Path

r = Path.cwd()
script = (r/'artifacts/root-compatibility-display.py').read_text(encoding='utf-8')
script = script.replace("'artifacts/compatibility-display350'", "'artifacts/compatibility-display350-second'")
script = script.replace("r / 'tools/Erlang.Tool/bin/Debug/net10.0'", "r / 'artifacts/additional-type-predicate-candidate/tools/Erlang.Tool/bin/Debug/net10.0'")
exec(compile(script, str(r/'artifacts/root-compatibility-display2.py'), 'exec'))

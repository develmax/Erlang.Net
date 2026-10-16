from pathlib import Path

r = Path.cwd()
p = r / 'artifacts/root-control-connected-spawn-collector2.py'
assert not p.exists()
text = (r / 'artifacts/root-control-connected-spawn-collector.py').read_text()
text = text.replace('connected-spawn-original-collector-proposal', 'connected-spawn-original-collector-proposal2')
text = text.replace('root-connected-spawn-collector-controls', 'root-connected-spawn-collector2-controls')
text = text.replace('0CD7B85FFEAC57AFF2597E953AE3B16286E4AA8FF551C766A6D37A2F9FCF080F', '2080B5FDAE3F93B731B97779AE13BDF33983685CBB0EE85ABDE5AEB05F0997A9')
text = text.replace("m.verify(args)\nchecks.append('exact-seven-file-source-and-external-dependency-closure')", "m.verify(args)\nassert len(m.CASES) == len(set(m.CASES)) == 12\nchecks.append('exact-seven-file-source-and-external-dependency-closure')")
text = text.replace('SourceMirrorOnly=True,', 'SourceMirrorOnly=True, DeclaredScenarios=12,')
compile(text, str(p), 'exec')
p.write_text(text)
print('Prepared collector2 passive controls; old controls remain immutable.')

from pathlib import Path
import json,hashlib
a=Path('artifacts');before=a/'part271-main-capture-helper-initial-await-span.cs';after=a/'part271-main-capture-focus/Program.cs'
old=before.read_text();new=after.read_text()
assert old.replace('if (!before.SequenceEqual(await File.ReadAllBytesAsync(mainPath)))','byte[] after = await File.ReadAllBytesAsync(mainPath);\nif (!before.SequenceEqual(after))')==new
initial=a/'part271-main-capture-helper-initial-await-span.log';assert 'CS4007' in initial.read_text(encoding='utf-8-sig')
final=a/'part271-main-capture-focus-build.log';log=final.read_text(encoding='utf-8-sig');assert ('Build succeeded' in log or 'Сборка успешно завершена' in log)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
imports=json.loads((a/'part271-main-import-guard.json').read_text());assert all(sha(Path(row['Path']))==row['AfterSha256'] for row in imports['Files'])
guard={'Status':'ActualHelperCompileFailureAndMinimalCorrectionPreserved','InitialError':'CS4007 span cannot cross await','InitialSourceSha256':sha(before),'InitialLogSha256':sha(initial),'CorrectedSourceSha256':sha(after),'CorrectedBuildLogSha256':sha(final),'OnlyChange':'Await byte array read before SequenceEqual instead of awaiting inside span expression','CollectorSourcesChanged':False,'All33MainSourceBindingsStillExact':True,'InitialFailurePreserved':True,'NativeExecuted':False}
out=a/'part271-main-capture-helper-repair-guard.json';out.write_bytes((json.dumps(guard,indent=2)+'\n').encode());print(sha(out))

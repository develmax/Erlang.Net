from pathlib import Path
p=Path('artifacts');t=(p/'root-guard301-native.py').read_text().replace('part301','part303').replace("tests['Passed'] == 6621","tests['Passed'] == 6622").replace("'SourcePaths': 14","'SourcePaths': 13").replace("'CSharpPaths': 12","'CSharpPaths': 11")
needle="tool = root / 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0'";assert t.count(needle)==1;t=t.replace(needle,"assert any(row['Name'] == 'runtime/direct-source-load-nif-exact-caller-capability' and row['Status'] == 'Passed' for row in tests['Tests'])\n"+needle)
t=t.replace("'OriginalNewProcessInfoCasesExecuted': False", "'NewSourceLoadNifHookNativeExecuted': False, 'PublicLoadNifActivated': False, 'OriginalNewSourceLoadCasesExecuted': False")
q=p/'root-guard303-native.py';assert not q.exists();q.write_text(t,encoding='utf-8')
t=(p/'root-run301-post-full-native.ps1').read_text().replace('part301','part303').replace('guard301-native','guard303-native');q=p/'root-run303-post-full-native.ps1';assert not q.exists();q.write_text(t,encoding='utf-8')
print('Prepared source303 actual post-full native guard; not executed.')

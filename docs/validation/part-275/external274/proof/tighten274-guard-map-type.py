from pathlib import Path
p=Path('artifacts/guard274-admission-native.py'); s=p.read_text(); anchor="assert get('compiled','nodeup')==('nodeup','epoch@host',[]) and get('compiled','nodedown')==('nodedown','epoch@host',[])"; assert anchor in s
replacement="for name in ['nodeup','nodedown']:\n    info=facts['compiled'][name+'.etf']['Values'][2]; assert info['Type']=='Map' and info['Entries']==[]\n"+anchor
s=s.replace(anchor,replacement); p.write_text(s,encoding='utf-8')

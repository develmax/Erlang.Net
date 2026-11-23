from pathlib import Path
import sys
x,u,y=map(Path,sys.argv[1:])
s=(x/'verify-linked-runtime.py').read_text(encoding='utf-8-sig')
s=s.replace(x.as_posix(),'@STAGE@').replace(u.as_posix(),x.as_posix()).replace('@STAGE@',y.as_posix())
prefix=s[:s.index('rels={')]
(y/'verify-linked-runtime.py').write_text(prefix+(y/'native-witness-body.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
wrapper=(x/'run-linked-runtime.ps1').read_text(encoding='utf-8-sig').replace(x.as_posix(),y.as_posix())
(y/'run-linked-runtime.ps1').write_text(wrapper,encoding='utf-8')


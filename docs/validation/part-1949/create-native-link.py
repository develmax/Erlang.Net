from pathlib import Path
import sys
y,x,z=map(Path,sys.argv[1:])
body=(y/'verify-linked-runtime.py').read_text(encoding='utf-8-sig')
body=body.replace(y.as_posix(),'@STAGE@').replace(x.as_posix(),y.as_posix()).replace('@STAGE@',z.as_posix())
prefix=body[:body.index('rels={')]
(z/'verify-linked-runtime.py').write_text(prefix+(z/'native-witness-body.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
wrapper=(y/'run-linked-runtime.ps1').read_text(encoding='utf-8-sig').replace(y.as_posix(),z.as_posix())
(z/'run-linked-runtime.ps1').write_text(wrapper,encoding='utf-8')


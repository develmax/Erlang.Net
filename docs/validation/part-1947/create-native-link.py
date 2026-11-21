from pathlib import Path
import sys
u,z,x=map(Path,sys.argv[1:])
p=(u/'verify-linked-runtime.py').read_text(encoding='utf-8-sig')
p=p.replace(u.as_posix(),'@stage@').replace(z.as_posix(),u.as_posix()).replace('@stage@',x.as_posix())
prefix=p[:p.index('release_paths=')]
method=p[p.index('def capture_exact('):p.index('release_paths=',p.index('def capture_exact('))] if False else p[p.index('def capture_exact('):p.index('\nif absent:')]
method=method[:method.index('def until_parent():')] if 'def until_parent():' in method else method
parent=p[p.index('def until_parent():'):p.index('\nrelease_paths=',p.index('def until_parent():'))]
(x/'verify-linked-runtime.py').write_text(prefix+method+'\n'+parent+'\n'+(x/'native-witness-body.txt').read_text(encoding='utf-8-sig'),encoding='utf-8')
wrapper=(u/'run-linked-runtime.ps1').read_text(encoding='utf-8-sig').replace(u.as_posix(),x.as_posix())
(x/'run-linked-runtime.ps1').write_text(wrapper,encoding='utf-8')


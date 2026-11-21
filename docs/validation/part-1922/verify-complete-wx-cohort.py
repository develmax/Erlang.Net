from pathlib import Path
import json,sys

stage, parent, scope=map(Path,sys.argv[1:])
def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def key(path):return path.replace(chr(92),'/')
a,b=({key(row['Beam']):row['Status'] for row in read(base/'full-uncached/otp-corpus.json')['Modules']}
     for base in (parent,stage))
assert len(a)==len(b)==1366 and a.keys()==b.keys()
names=sorted({key(n.strip()) for n in scope.read_text(encoding='utf-8-sig').splitlines() if n.strip()})
assert len(names)==65
rows=[{'Beam':name,'Before':a[name],'After':b[name]} for name in names]
assert all(row['After'] in ('Equal','Different') and (row['Before']!='Equal' or row['After']=='Equal')
           for row in rows)
path=stage/'wx-linked-cohort.json'
path.write_text(json.dumps({'Coverage':'Linked uncached complete baseline and candidate', 'Total':len(rows),
    'Equal':sum(row['After']=='Equal' for row in rows),
    'Different':sum(row['After']=='Different' for row in rows),'Rows':rows},indent=2)+'\n',encoding='utf-8')

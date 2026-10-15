from pathlib import Path
r=Path('artifacts')
for kind in ['ordinary','native']:
 t=(r/f'root-dispatch300-{kind}.ps1').read_text()
 t=t.replace('part300','part301').replace('part-300','part-301').replace('-ne 30','-ne 13').replace('-ne 6611','-ne 6621')
 if kind=='native':t=t.replace('588DC646C2E9593FF89F5F496EC6D6BD308DE1F73AD9CC4329E89759C69ED97E','DCDB739375C0CA2C11EB8DC2998A9B790BD6F202D46FAAB136F13F471544D03D')
 p=r/f'root-dispatch301-{kind}.ps1';assert not p.exists();p.write_text(t,encoding='utf-8')
t=(r/'root-run300-post-full-native.ps1').read_text().replace('part300','part301').replace('guard300-native','guard301-native')
p=r/'root-run301-post-full-native.ps1';assert not p.exists();p.write_text(t,encoding='utf-8')
print('Prepared exact-head single-POST dispatch guards and actual post-full native runner; not executed.')

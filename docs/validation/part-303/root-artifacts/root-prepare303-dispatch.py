from pathlib import Path
p=Path('artifacts')
for kind in ['ordinary','native']:
 t=(p/f'root-dispatch301-{kind}.ps1').read_text();t=t.replace('part301','part303').replace('part-301','part-303').replace('-ne 14','-ne 13').replace('-ne 6621','-ne 6622');t=t.replace('DCDB739375C0CA2C11EB8DC2998A9B790BD6F202D46FAAB136F13F471544D03D','C8577299D35B72112F8FFAE3BEBD689A8BC9D7D8318F1601EBD75178D6B937E0');q=p/f'root-dispatch303-{kind}.ps1';assert not q.exists();q.write_text(t,encoding='utf-8')
print('Prepared once-only exact-head303 dispatch guards; not executed.')

import shutil
import stat
import sys
from pathlib import Path, PurePosixPath
import zipfile

archive_path, destination = Path(sys.argv[1]), Path(sys.argv[2]).resolve()
if destination.exists():
    raise RuntimeError('Fresh extraction directory required')
with zipfile.ZipFile(archive_path) as archive:
    members = archive.infolist()
    if len(members) > 5000 or sum(item.file_size for item in members) > 256 * 1024 * 1024:
        raise RuntimeError('Archive member/count byte bounds exceeded')
    names = set()
    for item in members:
        name = item.filename
        path = PurePosixPath(name)
        mode = item.external_attr >> 16
        target = (destination / name).resolve()
        if path.is_absolute() or '..' in path.parts or '\\' in name or name in names or not target.is_relative_to(destination) or stat.S_ISLNK(mode) or item.file_size > 64 * 1024 * 1024:
            raise RuntimeError('Unsafe/duplicate/oversize ZIP member')
        names.add(name)
    destination.mkdir(parents=True)
    for item in members:
        target = destination / item.filename
        if item.is_dir():
            target.mkdir(parents=True, exist_ok=True)
        else:
            target.parent.mkdir(parents=True, exist_ok=True)
            with archive.open(item) as source, target.open('xb') as output:
                shutil.copyfileobj(source, output, 64 * 1024)
            if target.stat().st_size != item.file_size:
                raise RuntimeError('Extracted size differs')

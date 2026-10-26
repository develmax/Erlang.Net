from pathlib import Path
import hashlib
import os

def digest(path):
    h = hashlib.sha256()
    storage = bytearray(65536)
    view = memoryview(storage)
    with Path(path).open('rb', buffering=0) as stream:
        while count := stream.readinto(storage):
            h.update(view[:count])
    return h.hexdigest().upper()

def inventory(root):
    result = {}
    for directory, dirs, files in os.walk(root, followlinks=False):
        for name in [*dirs, *files]:
            path = Path(directory) / name
            if path.is_symlink() or path.is_junction():
                raise RuntimeError('Reparse input refused: ' + str(path))
        for name in files:
            path = Path(directory) / name
            result[path.relative_to(root).as_posix()] = digest(path)
    return result

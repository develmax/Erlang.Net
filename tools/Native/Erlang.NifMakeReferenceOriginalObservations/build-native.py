"""Use the existing source-bound owned native builder without copying it."""
import runpy
from common import BASE
if __name__ == '__main__':
    runpy.run_path(str(BASE/'build-native.py'),run_name='__main__')

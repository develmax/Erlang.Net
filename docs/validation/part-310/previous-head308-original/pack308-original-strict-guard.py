from pathlib import Path
import runpy,sys
sys.dont_write_bytecode=True
sys.argv=[str(Path(__file__).with_name('pack308-original-contract.py')),'strict']
runpy.run_path(sys.argv[0],run_name='__main__')

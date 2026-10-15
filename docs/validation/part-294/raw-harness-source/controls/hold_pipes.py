import os
import sys
import time

os.write(1, bytes([67, 128, 0]))
os.write(2, bytes([68, 129, 255]))
time.sleep(int(sys.argv[1]))

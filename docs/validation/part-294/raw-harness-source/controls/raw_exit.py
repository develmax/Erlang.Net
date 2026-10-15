import os

os.write(1, bytes([0, 128, 255]))
os.write(2, bytes([254, 0, 129]))
raise SystemExit(3)

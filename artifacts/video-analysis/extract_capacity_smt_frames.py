import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent / "pydeps"))

import imageio.v3 as iio
from PIL import Image


video = r"C:\Users\carl0\Videos\capacity smt.mp4"
out = pathlib.Path(__file__).parent
metadata = iio.immeta(video)
duration = float(metadata.get("duration") or 60)
print(metadata)

times = [0, 3, 6, 9, 12, 15, 20, 25, 30, 35, 40, 45, 50, 55]
for seconds in times:
    if seconds >= duration:
        continue
    frame = iio.imread(video, index=int(seconds * 30))
    image = Image.fromarray(frame)
    image.thumbnail((1280, 720))
    path = out / f"capacity-smt-{seconds:03d}.png"
    image.save(path)
    print(path)

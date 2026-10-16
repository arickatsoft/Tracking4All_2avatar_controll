#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p NativePose/converted
NativePose/tools/pnnx-20260704-macos/pnnx Assets/BodyTracking/Resources/RTMPose/Body26.onnx 'inputshape=[1,3,256,192]' fp16=0 ncnnparam=NativePose/converted/Body26.param ncnnbin=NativePose/converted/Body26.bin pnnxparam=NativePose/converted/Body26.pnnx.param pnnxbin=NativePose/converted/Body26.pnnx.bin pnnxpy=NativePose/converted/Body26_pnnx.py pnnxonnx=NativePose/converted/Body26.pnnx.onnx ncnnpy=NativePose/converted/Body26_ncnn.py
python3 - <<'PY'
from pathlib import Path
p=Path('NativePose/converted/Body26.param')
s=p.read_text()
# ncnn Gemm treats the channels of a 3D Mat as matrix rows. Remove this
# spurious singleton batch dimension: [1,26,48] becomes a 2D [26,48] Mat.
# The later attention ExpandDims is intentional and must remain unchanged.
old='0=48 1=26 12=0 13=0 2=1'
assert s.count(old)==1, 'Converter topology changed; inspect and revalidate before using the model.'
p.write_text(s.replace(old,'0=48 1=26'))
PY
rm -f Assets/BodyTracking/Resources/RTMPose/Body26.pnnxsim.onnx

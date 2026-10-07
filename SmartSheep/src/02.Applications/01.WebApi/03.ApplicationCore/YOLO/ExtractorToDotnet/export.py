"""Export the SmartSheep YOLO model (.pt) to ONNX for inference from .NET.

Usage (from this folder):
    python3 -m venv .venv
    .venv/bin/pip install -r requirements.txt
    .venv/bin/python export.py                 # smartsheep.pt -> smartsheep.onnx
    .venv/bin/python export.py --imgsz 640     # override input size

After exporting, the ONNX model is loaded with onnxruntime and run once on a
dummy image to verify it works, and the input/output shapes are printed so the
.NET side knows what to expect.
"""

import argparse
import json
from pathlib import Path

import numpy as np
import onnxruntime as ort
from ultralytics import YOLO

HERE = Path(__file__).resolve().parent


def parse_args():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--weights", default=HERE.parent / "smartsheep.pt", type=Path, help="path to the .pt model")
    p.add_argument("--imgsz", type=int, default=None, help="input size; defaults to the training imgsz")
    p.add_argument("--opset", type=int, default=None, help="ONNX opset; defaults to ultralytics' choice")
    p.add_argument("--half", action="store_true", help="export FP16 (GPU only)")
    return p.parse_args()


def export(args) -> Path:
    model = YOLO(str(args.weights))
    train_args = (model.ckpt or {}).get("train_args", {})
    imgsz = args.imgsz or train_args.get("imgsz", 640)

    print(f"task   : {model.task}")
    print(f"classes: {model.names}")
    print(f"imgsz  : {imgsz}")

    onnx_path = Path(
        model.export(
            format="onnx",
            imgsz=imgsz,
            opset=args.opset,
            half=args.half,
            dynamic=False,  # fixed shape keeps the .NET pre-processing simple
            simplify=True,
        )
    )

    # Class names next to the model so .NET doesn't have to parse ONNX metadata.
    labels_path = onnx_path.with_suffix(".labels.json")
    labels_path.write_text(json.dumps({str(k): v for k, v in model.names.items()}, indent=2))
    print(f"labels : {labels_path.name}")
    return onnx_path


def verify(onnx_path: Path):
    session = ort.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
    inp = session.get_inputs()[0]
    dummy = np.random.rand(*inp.shape).astype(np.float32)
    outputs = session.run(None, {inp.name: dummy})

    print("\n--- ONNX verification ---")
    print(f"input  : {inp.name} {inp.shape} {inp.type}")
    for meta, out in zip(session.get_outputs(), outputs):
        print(f"output : {meta.name} {list(out.shape)} {meta.type}")
    for k, v in session.get_modelmeta().custom_metadata_map.items():
        print(f"meta   : {k} = {v}")


if __name__ == "__main__":
    verify(export(parse_args()))

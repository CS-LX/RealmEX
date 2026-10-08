"""Render the vendored Remix Icon v4.6.0 SVGs. Requires resvg-py.

Run from any directory: python tools/icons/render.py
Original paths are unchanged; the runtime PNGs are white, 3x rasterizations.
"""
from pathlib import Path
import resvg_py

root = Path(__file__).resolve().parents[2]
for source in sorted((root / "tools/icons/remix").glob("*.svg")):
    svg = source.read_text(encoding="utf-8").replace('fill="currentColor"', 'fill="#ffffff"')
    (root / "Assets/RealmEX/Icons" / (source.stem + ".png")).write_bytes(
        resvg_py.svg_to_bytes(svg_string=svg, width=72, height=72)
    )

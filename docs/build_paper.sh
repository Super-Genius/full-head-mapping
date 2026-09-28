#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
python make_figures.py
mkdir -p build
pdflatex -interaction=nonstopmode -output-directory=build full_head_mapping.tex >/dev/null
pdflatex -interaction=nonstopmode -output-directory=build full_head_mapping.tex >/dev/null
cp build/full_head_mapping.pdf full_head_mapping.pdf
echo "Built docs/full_head_mapping.pdf"

#!/usr/bin/env bash
#
# What the shop's own data says about itself — as opposed to whether the app is working.
#
# check-registers.sh asks whether the app agrees with itself. check-negatives.sh asks what it
# refuses. This one reads the figures somebody typed in and says which of them will cause trouble:
# a part with no ceiling price, a GSTIN with a digit wrong, a hole in a number series.
#
# Nothing here is a defect and nothing here fails the run. Read it before a return goes out.
#
# Usage:  ./scripts/check-data.sh
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$HERE/scripts/_db.sh"

echo "Reading the data in $(where_am_i) ..."
python3 "$HERE/scripts/check_data.py"

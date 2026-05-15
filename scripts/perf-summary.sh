#!/usr/bin/env bash
set -euo pipefail

perf_dir="${CAMERAPLUS_PERF_DIR:-$HOME/Library/Application Support/RimWorld/Config/CameraPlusPerf}"
csv_path="${1:-}"
target_draws="${2:-${CAMERAPLUS_TARGET_DRAWS:-}}"

if [[ -z "$csv_path" ]]; then
	csv_path="$(find "$perf_dir" -maxdepth 1 -type f -name 'cameraplus-perf-*.csv' -print | sort | tail -1)"
fi

if [[ -z "$csv_path" || ! -f "$csv_path" ]]; then
	echo "No Camera+ perf CSV found in: $perf_dir" >&2
	exit 1
fi

echo "$csv_path"

selected_frame=""
if [[ -n "$target_draws" ]]; then
	selected_frame="$(awk -F, -v target="$target_draws" '
		NR > 1 && $3 == "section" && $4 == "DotDrawer.DrawDots" && ($5 + 0) >= target {
			print $2
			exit
		}
	' "$csv_path")"

	if [[ -z "$selected_frame" ]]; then
		echo "No DotDrawer.DrawDots row at or beyond ${target_draws} draws in: $csv_path" >&2
		exit 1
	fi
fi

awk \
	-v selectedFrame="$selected_frame" \
	-v targetDraws="$target_draws" \
	-f "$(dirname "$0")/perf-summary.awk" \
	"$csv_path"

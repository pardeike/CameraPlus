BEGIN {
	FS = ","
}

NR == 1 {
	next
}

{
	frame = $2
	if (selectedFrame != "" && frame != selectedFrame)
		next

	kind = $3
	name = $4
	count = $5 + 0
	avgUs = $7 + 0
	maxMs = $8 + 0
	latest = $9
	maxValue = $10

	if (count >= latestCount[name]) {
		latestKind[name] = kind
		latestCount[name] = count
		latestAvgUs[name] = avgUs
		latestMaxMs[name] = maxMs
		latestValue[name] = latest
		latestMaxValue[name] = maxValue
	}
}

END {
	print "Camera+ perf summary"
	if (selectedFrame != "") {
		print "Snapshot: first DotDrawer.DrawDots sample at or beyond " targetDraws " draws, frame " selectedFrame
	}
	print ""

	print "Sections (latest cumulative average)"
	PrintSection("DynamicDrawManager.DrawDynamicThings.Postfix")
	PrintSection("DotDrawer.DrawDots")
	PrintSection("DotDrawer.DrawClipped")
	PrintSection("DotDrawer.DrawMarker")
	PrintSection("DotTools.ShouldShowMarker")
	PrintSection("DotTools.GetMarkerColors")
	PrintSection("MarkerCache.MaterialFor")
	print ""

	print "Samples (latest/max)"
	PrintSample("dotdrawer.all_pawns_spawned")
	PrintSample("dotdrawer.visible_pawns")
	PrintSample("dotdrawer.marker_draws")
	PrintSample("dotdrawer.edge_draws")
	print ""

	print "Counters"
	PrintCounter("dotdrawer.draw_calls")
	PrintCounter("marker_cache.hits")
	PrintCounter("marker_cache.misses")
	PrintCounter("marker_cache.refreshes")
	PrintCounter("quota_cache.DotConfig.requests")
	PrintCounter("quota_cache.DotConfig.refreshes")
	PrintCounter("slow_core.dynamic_draw.calls")
	PrintCounter("slow_core.pawn_render_internal.calls")
	PrintCounter("slow_core.pawn_gui_overlay.calls")
	PrintCounter("slow_core.pawn_label.calls")
}

function PrintSection(name) {
	if (latestKind[name] != "section")
		return
	printf "  %-48s calls=%8d avg=%9.3f us max=%8.3f ms\n", name, latestCount[name], latestAvgUs[name], latestMaxMs[name]
}

function PrintSample(name) {
	if (latestKind[name] != "sample")
		return
	printf "  %-48s latest=%8s max=%8s avg=%9.3f\n", name, latestValue[name], latestMaxValue[name], latestAvgUs[name]
}

function PrintCounter(name) {
	if (latestKind[name] != "counter")
		return
	printf "  %-48s total=%8s\n", name, latestValue[name]
}

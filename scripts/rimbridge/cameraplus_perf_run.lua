rb.assert(params.saveName ~= nil, "params.saveName is required.")

local durationMs = params.durationMs or 30000
local speed = params.speed or "Ultrafast"
local expectedRootSize = params.expectedRootSize or 39.172226
local expectedX = params.expectedX or 128
local expectedZ = params.expectedZ or 122

local loaded = rb.call("rimworld/load_game_ready", {
  saveName = params.saveName,
  timeoutMs = 120000,
  pauseIfNeeded = true,
  waitForScreenFade = true,
  pollIntervalMs = 100
})

local camera = rb.call("rimworld/get_camera_state", nil)
local rootDelta = camera.result.rootSize - expectedRootSize
local xDelta = camera.result.mapPosition.x - expectedX
local zDelta = camera.result.mapPosition.z - expectedZ

rb.assert(rootDelta < 0.01 and rootDelta > -0.01, "Unexpected saved camera root size.")
rb.assert(xDelta <= 1 and xDelta >= -1, "Unexpected saved camera x position.")
rb.assert(zDelta <= 1 and zDelta >= -1, "Unexpected saved camera z position.")

local played = rb.call("rimworld/play_for", {
  durationMs = durationMs,
  speed = speed,
  pollIntervalMs = 50
})

rb.print("save", params.saveName)
rb.print("cameraRootSize", camera.result.rootSize)
rb.print("cameraMapPosition", camera.result.mapPosition)
rb.print("playSpeed", speed)
rb.print("requestedDurationMs", durationMs)
rb.print("advancedTicks", played.result.advancedTicks)

return {
  saveName = params.saveName,
  camera = camera.result,
  play = played.result
}

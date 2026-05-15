rb.assert(params.saveName ~= nil, "params.saveName is required.")

local loaded = rb.call("rimworld/load_game_ready", {
  saveName = params.saveName,
  timeoutMs = 120000,
  pauseIfNeeded = true,
  waitForScreenFade = true,
  pollIntervalMs = 100
})

local camera = rb.call("rimworld/get_camera_state", nil)

rb.print("save", params.saveName)
rb.print("cameraRootSize", camera.result.rootSize)
rb.print("cameraMapPosition", camera.result.mapPosition)

return camera.result

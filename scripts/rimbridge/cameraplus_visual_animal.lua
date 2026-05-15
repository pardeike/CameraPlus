rb.assert(params.saveName ~= nil, "params.saveName is required.")

local x = params.x or 215
local z = params.z or 22
local paddingCells = params.paddingCells or 2
local fileName = params.fileName or "cameraplus-animal-closeup"

local loaded = rb.call("rimworld/load_game_ready", {
  saveName = params.saveName,
  timeoutMs = 120000,
  pauseIfNeeded = true,
  waitForScreenFade = true,
  pollIntervalMs = 100
})

local screenshot = rb.call("rimworld/screenshot_cell_rect", {
  x = x,
  z = z,
  width = 1,
  height = 1,
  paddingCells = paddingCells,
  fileName = fileName,
  includeTargets = true,
  suppressMessage = true,
  doNotResetCamera = true
})

rb.print("cell", { x = x, z = z })
rb.print("paddingCells", paddingCells)
rb.print("screenshotPath", screenshot.result.path)

return screenshot.result

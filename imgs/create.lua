local modId = params.modId or "Camera+"
local topicLabel = params.topicLabel or "Appearance"
local fileName = params.fileName or "cameraplus-settings-appearance"
local timeoutMs = params.timeoutMs or 5000

local opened = rb.call("rimworld/open_mod_settings", {
  modId = modId,
  replaceExisting = true
})

local layout = rb.call("rimworld/get_ui_layout", {
  timeoutMs = timeoutMs
})

local surface = layout.result.surfaces[1]
rb.assert(surface ~= nil, "Camera+ settings surface was not captured.")

-- The Appearance topic is the current README capture target. The left-nav
-- order is fixed in Source/Settings.cs; using the target index keeps this
-- lowered-Lua script below RimBridge's statement budget.
local topicButton = surface.elements[25]
rb.assert(topicButton ~= nil, "Requested Camera+ topic button was not found.")

local clicked = rb.call("rimworld/click_ui_target", {
  targetId = topicButton.targetId,
  timeoutMs = timeoutMs
})

local selectedLayout = rb.call("rimworld/get_ui_layout", {
  timeoutMs = timeoutMs
})

local selectedSurface = selectedLayout.result.surfaces[1]
rb.assert(selectedSurface ~= nil, "Camera+ selected settings surface was not captured.")

local screenshot = rb.call("rimworld/take_screenshot", {
  fileName = fileName,
  includeTargets = true,
  suppressMessage = true,
  clipTargetId = selectedSurface.captureTargetId,
  clipPadding = 0
})

rb.print("modId", modId)
rb.print("topicLabel", topicLabel)
rb.print("topicTargetId", topicButton.targetId)
rb.print("screenshotPath", screenshot.result.path)

return {
  modId = modId,
  topicLabel = topicLabel,
  topicTargetId = topicButton.targetId,
  screenshotPath = screenshot.result.path
}

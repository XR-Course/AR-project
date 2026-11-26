# Android navigation PoC

The Android scene is `Assets/Scenes/AndroidPoc.unity`. It combines the
existing image tracking, PoC map, route display and UI with the existing
navigation service driven by the device pose rather than the editor simulator.

## Build and test

1. Open the project in Unity 6000.3.27f1 and select Android as the build target.
2. Open `Assets/Scenes/AndroidPoc.unity`. If it has not been generated, use
   **Campus Navigation > Build Android PoC Scene**. The command creates or
   overwrites that scene and makes it the only enabled build scene; it does not
   change `SampleScene.unity`.
3. Build and run on an ARCore-compatible Android phone.
4. Select a destination, start navigation and scan the entrance reference
   image. Walk along the route and scan the checkpoint to recalibrate.
   If tracking is lost, scan a known marker after AR tracking recovers.

The PoC map and marker coordinates are examples, not measurements of a real
building. Before relying on the arrows, measure both marker poses and the route
in the same campus coordinate system (including marker height and heading),
update `CampusMarkerMap` in the scene and
`Assets/Pathfinding/Maps/PocCampusMap.asset`, and print the images from the
reference image library at their configured physical sizes. Confirm arrows
are at floor level and the route stays aligned on the actual device.

This is a short controlled demo, not a safety-critical navigation system.
Watch your surroundings. No device test is implied by the presence of this
scene.

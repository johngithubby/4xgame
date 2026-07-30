# SWAT Texture Set

## Purpose

These PNG files are the visible diffuse, normal, bump, opacity, and specular inputs unpacked from the licensed Female SWAT Soldier Blender source. They are capped at 1024 pixels so Unity can import each channel explicitly instead of relying on FBX-embedded texture resolution.

`SwatSurvivorModelImporter.cs` imports normal and bump inputs through Unity's normal-map path and compresses all maps for the mobile target. `PrototypeCharacterFactory.cs` maps the source material-slot names to these textures at scene construction time and creates lit Standard/URP materials with normal maps, metallic response for weapon hardware, and restrained smoothness.

## Regeneration

Open the original licensed `.blend` source in Blender, unpack the visible material images, resize their longest side to at most 1024 pixels, and preserve the original image names. The runtime mapping depends on the `<material>_Diffuse`, `<material>_Normal`, and `<material>_Bump` naming convention.

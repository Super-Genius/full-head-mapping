# Full Head Mapping Revisited

A Unity 6 reconstruction and modernization of Kenneth Hurley's early-2000s **Full Head Mapping** technique for turning multiple photographs of a head into one coherent texture map.

Original article: <https://www.realistic3d.com/Full_Head_Mapping.htm>

The old 3ds Max plug-in did something that still matters: select the best photograph for each surface region, then hide cross-view seams with Burt-Adelson multiresolution blending. This project keeps that idea, removes the dependency on old 3ds Max tooling, and generalizes it to arbitrary Unity projector cameras.

## What is here

- **Unity 6 editor baker** - map front/side/back/3/4 photographs directly onto an existing UV-mapped head.
- **Best-view historical mode** - selects the strongest photograph from the surface normal/view direction.
- **Weighted multi-view mode** - blends all valid observations using view angle and image-border confidence.
- **Multiband mode** - Gaussian weight pyramids + Laplacian color pyramids to reduce seams without washing out fine detail.
- **Confidence map** - shows how strongly each texel was observed.
- **Provenance map** - shows which photograph dominated each texel.
- **Blender helper** - creates photographic projector cameras and exports an interchange manifest; Blender's native UV Project modifier can also be used for preview/alignment.
- **Geometry-provider interface** - leaves room for FLAME/DECA, photogrammetry, Meshy, or another fitting/generation system without coupling the core baker to one model or license.
- **arXiv-style paper** - LaTeX, figures, references, and compiled PDF live under [`docs/`](docs/).

## Unity quick start

1. Open the repository as a Unity 6 project, or copy `Packages/com.supergenius.full-head-mapping` into another project.
2. Import a head mesh with non-overlapping UV0 coordinates.
3. Open **Tools > Super-Genius > Full Head Mapping > Open Baker**.
4. Assign the target head.
5. Add a photograph and a disabled Unity camera for each view.
6. Align each camera until the mesh matches its photograph. The **Align Camera to Scene View** button speeds this up.
7. Select **Multiband** and press **Bake Full Head Texture**.
8. The tool writes:
   - `FullHead_Albedo.png`
   - `FullHead_Confidence.png`
   - `FullHead_Provenance.png`

By default these go under `Assets/FullHeadMappingGenerated/`.

## Weighting model

For surface point `p` observed by source view `i`, the current baker uses

```text
w_i(p) = user_i * max(0, N(p) . V_i(p))^gamma * border_i(p) * visibility_i(p)
```

`visibility_i` is optional in v0.1 because CPU ray tests are expensive and require suitable colliders. Back-facing points are always rejected.

## Blender

Install [`Blender/full_head_mapping_addon.py`](Blender/full_head_mapping_addon.py) as an add-on. It can create front/left/right/back projector cameras, attach photographs as camera backgrounds, and export a capture manifest. Blender's UV Project modifier supports camera projectors, so Blender is useful for visual alignment even when the final multiband bake happens in Unity.

## AI-assisted workflow

AI is treated as an optional geometry/completion stage rather than the source of truth for photographed regions.

A useful modern pipeline is:

```text
photos -> landmarks/camera alignment -> fitted or supplied head mesh
       -> deterministic multi-view projection -> multiband blend
       -> confidence/provenance map -> optional AI fill only where unobserved
```

Meshy currently supports multi-image 3D generation from multiple views, while face-specific models such as FLAME/DECA can provide stronger head priors. The core project does not bundle either dependency.

## Paper

**Full-Head Mapping Revisited: From Orthogonal Photo Projection and Multiresolution Splines to Landmark-Guided Multi-View Texture Synthesis**

- [LaTeX](docs/full_head_mapping.tex)
- [PDF](docs/full_head_mapping.pdf)

## Status

This is a working research/reference implementation, not a finished identity-capture product. Camera calibration is currently manual, color normalization is a scalar overlap correction, and missing regions use local dilation. Landmark-guided calibration and explicit AI inpainting adapters are documented next steps.

## License

MIT for the code in this repository. Third-party systems referenced by the paper have their own licenses; in particular, some face-model/reconstruction research code is non-commercial.

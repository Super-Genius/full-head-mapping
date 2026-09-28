# Original Full Head Mapping Technique

Source: Kenneth Hurley's early-2000s **Full Head Mapping** article:

https://www.realistic3d.com/Full_Head_Mapping.htm

The technique was also published as the **Full Head Mapping** section of *Photorealistic Faces with Vertex and Pixel Shaders* in *Direct3D ShaderX: Vertex and Pixel Shader Tips and Tricks* (2002).

## What the original tool actually did

This was not just cylindrical UV mapping.

1. The destination head used a final cylindrical UV map.
2. Separate photographs were planar-projected from front/side directions using 3ds Max UVW Mapping modifiers.
3. Material IDs identified the view direction: right, left, top, bottom, front, back, plus the final cylindrical map.
4. For each surface region, the tool compared the surface normal with canonical view directions and chose the source view most likely to contribute a good sample.
5. A simple hard view choice produced visible seams.
6. The plug-in therefore used the Burt-Adelson multiresolution spline method: Gaussian and Laplacian image pyramids blended coarse changes over broad regions while preserving fine image detail near the seam.
7. The result was baked into one texture using the destination cylindrical UV coordinates.

The original page reports about 20 seconds to generate and merge the images on a Pentium III 500 MHz system.

## Credits preserved from the original page

The page credits Peter Watje for the original program framework, Chris Hecker for original texture-mapping code later adapted to the tool, and Peter J. Burt and Edward H. Adelson for the multiresolution spline method.

## Modern reconstruction

The Unity reconstruction generalizes the fixed material-ID/view scheme into any number of calibrated projector cameras. A per-texel weight combines surface/view alignment, image-border confidence, optional visibility, and a per-view user weight. The historical hard-selection behavior remains available as **Best View** mode. **Multiband** mode uses Gaussian weight pyramids and Laplacian color pyramids in the same spirit as the original plug-in.

The modern tool also writes confidence and provenance maps. These distinguish directly photographed texels from weak or absent observations, which becomes important when adding AI inpainting or generated geometry later.

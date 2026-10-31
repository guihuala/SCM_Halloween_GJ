# Loading tape scroll

`Materials/PostalLoadingScroll.mat` uses `UI/Postal Loading Scroll` to draw a seamless,
mint-colored diagonal tape pattern. It is assigned to the existing loading screen's
`Loading/Progress/ProgressReveal/Content` Image, with `UIInfiniteScroll` driving it.
SceneLoader already creates this screen for scene transitions and destroys it afterward.

Material controls:

- Tape Color / Stripe Color: the two pattern colors (also multiplied by Image tint).
- Repeat Width: one full pattern repeat in local UI units, independent of sprite UVs.
- Stripe Slant: diagonal direction; zero produces vertical stripes.
- Scroll Speed: repeats per second; negative reverses direction; zero stops movement.

The component advances a bounded phase with unscaled time, so loading from a paused
game still animates. Each enabled instance owns its material and restores the original
on disable. Stencil masks, RectMask2D softness, CanvasGroup fading, sprite alpha and
nine-sliced sprites are supported. No repeat texture or external tween library is needed.

To reuse on another UI Image or RawImage, assign the material and add UIInfiniteScroll.
To preview, enter Play mode and enable the object, or trigger a normal scene transition.
The stripes indicate activity; the existing reveal mask and percentage still show actual
loading progress.

The loading page uses a dark teal night backdrop, existing hand-drawn moon/ghost/parcel
icons and the pickup artwork. The pickup follows displayed loading progress along the
road and gently rocks using unscaled time. `LoadingScreen`'s `Animate Courier` switch
disables the decorative rocking. The percentage and scene transitions use real progress.

`Tools > Night Courier > Install Loading Screen Artwork` reapplies this layout to the
loading prefab. Text and artwork remain editable prefab objects; the installer preserves
the existing progress mask and animated tape material. A rendered preview is saved in
`ArtPreviews/loading-night-courier.png`.

Validation: `PostalLoadingScrollCheck.Run` renders the actual prefab, checks phase wrap,
progress clipping and material lifecycle, and saves a screenshot to
`/private/tmp/postal-loading-scroll-preview.png`. The existing
`UILoadingAnimationSmokeTest.Run` covers loading progress and UI layout regressions.

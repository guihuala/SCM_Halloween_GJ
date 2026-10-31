# Main menu artwork

The original 1920 × 1080 exported layers are preserved here. `Cropped/` contains
derived PNGs trimmed to their nontransparent pixel bounds, without resampling.
The opaque background retains its complete canvas. All derived textures import
as uncompressed UI sprites with no mipmaps.

`Assets/Scenes/MainMenu.unity` assembles the cropped layers in the original
composition. The backdrop covers the window; corner decorations anchor to their
respective edges. The title and courier keep their proportions. The four original
menu buttons retain their controller references and use hand-drawn button sprites.

`MainMenuMotion` on `Canvas/Bg` controls staggered fades, the floating courier,
paper sway and subtle pointer parallax. Layer origins, drift, angle, depth and
delay are editable in the Inspector. Enable `Reduced Motion` to settle all layers
and show them immediately. Hidden entry buttons do not receive input. Motion uses
unscaled time and restores the original positions when disabled.

`Tools > Night Courier > Install Main Menu Artwork` regenerates cropped assets
and reapplies this menu layout. Save scene changes before running the installer;
it opens and saves MainMenu. `MainMenuArtInstaller.InstallAndCheck` is the batch
validation entry point for an isolated project copy, and exits that editor.
It checks the entry/input state, motion, reduced motion, keyboard navigation,
decorative raycasts and menu bounds at 1920 × 1080, 1280 × 800 and 2560 × 1080.
Rendered previews are written to `ArtPreviews/mainmenu-<width>x<height>.png`.

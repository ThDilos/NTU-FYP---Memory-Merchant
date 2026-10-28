# Memory Merchant audio

Original procedural music and synthesized sound effects, without third-party samples.
Regenerate the WAV files with `node Tools/Audio/generate-game-audio.cjs`.
The generator uses a fixed seed, so regenerating produces the same assets.

Files live in `Assets/Marcus/Audio/Resources/MemoryMerchantAudio`:

| File | Purpose |
| --- | --- |
| music_shop.wav | 72 BPM, warm piano arpeggios and music-box melody; shop and start menu |
| music_memory.wav | 60 BPM, lower and sparser variation; Memory World |
| music_puzzle.wav | 88 BPM, brighter variation; photograph puzzle |
| button.wav | Short soft UI click |
| dialogue.wav | Quiet text blip, capped at one every 75 ms |
| purchase.wav | Three bright metallic notes on successful purchases and coin rewards |
| footstep_0.wav / footstep_1.wav | Alternating soft shoe taps |
| interact.wav | Object interaction and picking up draggable words/manual |
| collect.wav | Fragment/Echo collection and manual drop into machine |
| place.wav | Successful reconstruction/puzzle placement |

Music is composed on a 16-bar loop with circular reverb tails. Unity streams music
as stereo Vorbis; effects use preloaded mono PCM. WAV sources are 44.1 kHz/16-bit.
`audio-manifest.json` records duration, peak and RMS levels.

`AudioManager` starts automatically in any scene, survives scene transitions,
crossfades music, and lowers it during dialogue. It attaches click sounds to both
authored and dynamically created Buttons. Existing scene-specific music mappings
and AudioManager API methods remain available. The existing master-volume slider
and `MemoryMerchant.MasterVolume` setting control the entire mix.

Default music level is 0.30; effects level is 0.80 with additional per-effect gains.
Use `AudioManager.Instance.SetMusicVolume` / `SetSFXVolume` to adjust the mix.
Footsteps measure real movement and stop while idle, paused, blocked by a wall,
or when the PlayerController is disabled. Text skips do not produce a burst.

To preview an individual asset, select its WAV in Unity and use the Inspector's
audio preview. Restart Play mode to activate the runtime integration.

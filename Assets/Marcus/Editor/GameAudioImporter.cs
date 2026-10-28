using UnityEditor;
using UnityEngine;

// Stream the long stereo loops; keep the tiny UI sounds ready for immediate play.
public sealed class GameAudioImporter : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Marcus/Audio/Resources/MemoryMerchantAudio/")) return;
        bool music = System.IO.Path.GetFileName(assetPath).StartsWith("music_");
        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
        settings.quality = .75f;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        settings.preloadAudioData = !music;
        importer.defaultSampleSettings = settings;
        // SFX are already authored as mono. Avoid Unity's normalized downmix,
        // which would undo the deliberately quiet dialogue/button levels.
        importer.forceToMono = false;
    }
}

using UnityEditor;
using UnityEngine;

/// <summary>
/// Acerta sozinho o import do som de fundo. Um clipe longo em ciclo não pode
/// entrar como os efeitos curtos: descomprimido na memória, os 27 segundos de
/// chuva ocupavam os 4,8 MB do WAV em RAM, em vez dos poucos centos de kB que
/// o Vorbis pede. Vale para tudo o que se chame "background_*" em Assets/Audio.
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    const string Prefix = PlaceholderAudio.Dir + "/background_";

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Prefix)) return;

        var importer = (AudioImporter)assetImporter;
        var settings = importer.defaultSampleSettings;

        settings.loadType = AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.5f;

        // não descomprime no arranque: a chuva chega uns instantes depois do
        // primeiro ecrã, e entra em fade de qualquer maneira
        settings.preloadAudioData = false;

        importer.defaultSampleSettings = settings;
        importer.loadInBackground = true;
        importer.forceToMono = false;
        importer.ambisonic = false;
    }
}

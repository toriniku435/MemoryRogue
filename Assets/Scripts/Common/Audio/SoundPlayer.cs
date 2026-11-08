using System.Collections.Generic;
using UnityEngine;
using Live2DCardRogue.Common.Repositories;

namespace Live2DCardRogue.Common.Audio
{
    // 音を実際に鳴らす、シーンをまたいで残るシングルトン。直接は呼ばず、AudioUseCase を通すこと。
    // 初めて音を鳴らすときに自分で生成し、Resources/SoundLibrary.asset を読む（MessageToast と同じ作り）。
    //
    // 効果音は数本の AudioSource を順番に使い回し、BGM は2本の AudioSource でクロスフェードする。
    // どれも 2D の音（距離で変わらない）。聞こえるのはシーンの AudioListener（Main Camera）経由
    internal sealed class SoundPlayer : MonoBehaviour
    {
        private const string LibraryResourcePath = "SoundLibrary";
        private const int VoiceCount = 10;

        private static SoundPlayer _instance;
        private static bool _missingLibraryLogged;

        private SoundLibrary _library;
        private AudioSource[] _voices;
        private int _nextVoice;
        private readonly Dictionary<SeId, float> _lastPlayed = new Dictionary<SeId, float>();

        private AudioSource[] _bgmSources;
        private int _activeBgm;
        private BgmId _currentBgm = BgmId.None;
        // 今の曲の音量（設定の BGM 音量を掛ける前）
        private float _currentBgmVolume;
        // オプションで音量を動かしている間の、試しの音量（null なら設定の値）
        private static float? _previewBgmVolume;
        private static float? _previewSeVolume;

        private static float UserBgmVolume => _previewBgmVolume ?? GameSettingsRepository.BgmVolume;
        private static float UserSeVolume => _previewSeVolume ?? GameSettingsRepository.SeVolume;

        /// <summary>オプションの編集中だけ、設定とは別の音量で鳴らす。null を渡すと設定の値に戻る</summary>
        public static void SetVolumePreview(float? bgm, float? se)
        {
            _previewBgmVolume = bgm;
            _previewSeVolume = se;
        }
        private float _fadeProgress = 1f;

        public static SoundPlayer Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var library = Resources.Load<SoundLibrary>(LibraryResourcePath);
                if (library == null)
                {
                    if (!_missingLibraryLogged) Debug.LogWarning("Resources/SoundLibrary が無いため、音を鳴らしません");
                    _missingLibraryLogged = true;
                    return null;
                }

                var go = new GameObject("SoundPlayer");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SoundPlayer>();
                _instance.Setup(library);
                return _instance;
            }
        }

        private void Setup(SoundLibrary library)
        {
            _library = library;
            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++) _voices[i] = NewSource(loop: false);
            _bgmSources = new[] { NewSource(loop: true), NewSource(loop: true) };
        }

        private AudioSource NewSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        public void PlaySe(SeId id)
        {
            if (id == SeId.None) return;
            var entry = _library.FindSe(id);
            if (entry == null) return;

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < _library.SeMinInterval) return;
            _lastPlayed[id] = now;

            float volume = _library.SeVolume * UserSeVolume;
            float pitch = 1f + Random.Range(-entry.PitchJitter, entry.PitchJitter);
            PlayVoice(entry.Clip, entry.Volume * volume, pitch, 0f);
            if (entry.Clip2 != null) PlayVoice(entry.Clip2, entry.Volume2 * volume, pitch, entry.Delay2);
        }

        private void PlayVoice(AudioClip clip, float volume, float pitch, float delay)
        {
            var voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            voice.Stop();
            voice.clip = clip;
            voice.volume = volume;
            voice.pitch = pitch;
            if (delay > 0f) voice.PlayDelayed(delay);
            else voice.Play();
        }

        // その効果音を鳴らしている声をすべて止める（重ねた音、遅延で鳴る前のものも）
        public void StopSe(SeId id)
        {
            var entry = _library.FindSe(id);
            if (entry == null) return;

            foreach (var voice in _voices)
            {
                if (voice.clip == null) continue;
                if (voice.clip == entry.Clip || voice.clip == entry.Clip2) voice.Stop();
            }
        }

        // 同じ曲なら何もしない（シーンを行き来しても頭から鳴り直さない）
        public void PlayBgm(BgmId id)
        {
            if (id == _currentBgm) return;
            _currentBgm = id;

            var entry = _library.FindBgm(id);
            _activeBgm = 1 - _activeBgm;
            var next = _bgmSources[_activeBgm];
            next.Stop();
            if (entry != null)
            {
                next.clip = entry.Clip;
                next.volume = 0f;
                next.Play();
                _currentBgmVolume = entry.Volume * _library.BgmVolume;
            }
            else
            {
                next.clip = null;
                _currentBgmVolume = 0f;
            }
            _fadeProgress = 0f;
        }

        // 設定の BGM 音量は毎フレーム掛け直す（オプションで動かしたら、鳴っている曲にもすぐ効く）
        private void Update()
        {
            var next = _bgmSources[_activeBgm];
            if (_fadeProgress >= 1f)
            {
                next.volume = _currentBgmVolume * UserBgmVolume;
                return;
            }

            float duration = Mathf.Max(0.01f, _library.BgmCrossfade);
            _fadeProgress = Mathf.Min(1f, _fadeProgress + Mathf.Min(Time.unscaledDeltaTime, 1f / 15f) / duration);

            var previous = _bgmSources[1 - _activeBgm];
            next.volume = _currentBgmVolume * UserBgmVolume * _fadeProgress;
            previous.volume *= 1f - _fadeProgress;
            if (_fadeProgress >= 1f) previous.Stop();
        }
    }
}

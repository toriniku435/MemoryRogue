using System;
using System.Collections.Generic;
using UnityEngine;

namespace Live2DCardRogue.Common.Audio
{
    // どの効果音・BGMにどの音を使うか（Resources/SoundLibrary.asset）。
    // 音の差し替えはここの割り当てを変えるか、同じファイル名で上書きする。
    // Inspector は Editor/SoundLibraryEditor.cs（SeId / BgmId の全値を1行ずつ、用途と試聴ボタン付きで並べる）
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        // Id は enum の値の名前で持つ。数値で持つと、enum の途中に値を足したときに割り当てがずれる
        [Serializable]
        public sealed class SeEntry
        {
            public string Id;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume = 1f;
            // 同じ音の連打が単調にならないよう、鳴らすたびに高さを少し揺らす幅（重ねる音にも同じ値を掛ける）
            [Range(0f, 0.2f)] public float PitchJitter;
            // 重ねて鳴らす2つ目の音（空なら Clip だけ）。Delay2 秒遅らせて鳴らせる
            public AudioClip Clip2;
            [Range(0f, 1f)] public float Volume2 = 1f;
            [Range(0f, 2f)] public float Delay2;
        }

        [Serializable]
        public sealed class BgmEntry
        {
            public string Id;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume = 1f;
        }

        [SerializeField][Range(0f, 1f)] private float _seVolume = 0.8f;
        [SerializeField][Range(0f, 1f)] private float _bgmVolume = 0.45f;
        // BGM を切り替えるとき、前の曲と次の曲を重ねる長さ
        [SerializeField][Min(0f)] private float _bgmCrossfade = 0.8f;
        // 同じ効果音をこれより短い間隔では重ねない（ホバーの連続などで音が団子にならないように）
        [SerializeField][Min(0f)] private float _seMinInterval = 0.04f;
        [SerializeField] private SeEntry[] _se;
        [SerializeField] private BgmEntry[] _bgm;

        // 鳴らすたびに名前を比べないよう、最初の検索で作る。Inspector で変えたら作り直す
        private Dictionary<SeId, SeEntry> _seLookup;
        private Dictionary<BgmId, BgmEntry> _bgmLookup;

        public float SeVolume => _seVolume;
        public float BgmVolume => _bgmVolume;
        public float BgmCrossfade => _bgmCrossfade;
        public float SeMinInterval => _seMinInterval;
        public IReadOnlyList<SeEntry> SeEntries => _se ?? Array.Empty<SeEntry>();
        public IReadOnlyList<BgmEntry> BgmEntries => _bgm ?? Array.Empty<BgmEntry>();

        public SeEntry FindSe(SeId id)
        {
            _seLookup ??= BuildLookup<SeId, SeEntry>(_se, e => e.Id, e => e.Clip);
            return _seLookup.TryGetValue(id, out var entry) ? entry : null;
        }

        public BgmEntry FindBgm(BgmId id)
        {
            _bgmLookup ??= BuildLookup<BgmId, BgmEntry>(_bgm, e => e.Id, e => e.Clip);
            return _bgmLookup.TryGetValue(id, out var entry) ? entry : null;
        }

        // 名前が enum に無い行と音の無い行は飛ばす。同じ名前が二つあれば先の行を使う
        private static Dictionary<TId, TEntry> BuildLookup<TId, TEntry>(
            TEntry[] entries, Func<TEntry, string> getId, Func<TEntry, AudioClip> getClip)
            where TId : struct, Enum
            where TEntry : class
        {
            var lookup = new Dictionary<TId, TEntry>();
            if (entries == null) return lookup;
            foreach (var entry in entries)
            {
                if (entry == null || getClip.Invoke(entry) == null) continue;
                if (!Enum.TryParse(getId.Invoke(entry), out TId id)) continue;
                if (!lookup.ContainsKey(id)) lookup.Add(id, entry);
            }
            return lookup;
        }

        private void OnEnable() => ClearLookup();

        private void OnValidate() => ClearLookup();

        private void ClearLookup()
        {
            _seLookup = null;
            _bgmLookup = null;
        }
    }
}

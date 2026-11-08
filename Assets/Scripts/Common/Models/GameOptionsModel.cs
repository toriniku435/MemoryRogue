using System;
using System.Collections.Generic;
using Live2DCardRogue.Common.Repositories;

namespace Live2DCardRogue.Common.Models
{
    // オプション（タイトル・バトル共通）の「編集中の値」。ウインドウを開いたときの値（Original*）から、スライダーなどで変えていく。
    // 反映するまでは設定そのもの（GameSettingsRepository）には触らないので、キャンセルは「この編集を捨てる」だけで済む。
    // 項目を足すときは、ここに値と Set・既定値を足し、IsDirty / IsDefault の比較にも加える
    public sealed class GameOptionsModel
    {
        // 音量は 5% 刻み、戦闘スピードは 0.25 倍刻み。途中の半端な値を作らないよう、値はここで丸める
        public const float VolumeStep = 0.05f;
        public const float BattleSpeedStep = 0.25f;

        private readonly IReadOnlyList<string> _localeCodes;

        public float OriginalBgmVolume { get; }
        public float OriginalSeVolume { get; }
        public float OriginalBattleSpeed { get; }
        public string OriginalLocaleCode { get; }
        public bool OriginalFullscreen { get; }

        public float BgmVolume { get; private set; }
        public float SeVolume { get; private set; }
        public float BattleSpeed { get; private set; }
        public string LocaleCode { get; private set; }
        public bool Fullscreen { get; private set; }

        /// <summary>開いたときから何か変わったか（キャンセルで確認を出すか、反映を押せるか）</summary>
        public bool IsDirty =>
            !Same(BgmVolume, OriginalBgmVolume) || !Same(SeVolume, OriginalSeVolume) || !Same(BattleSpeed, OriginalBattleSpeed)
            || LocaleCode != OriginalLocaleCode || Fullscreen != OriginalFullscreen;

        /// <summary>
        /// 今の値が、すべて既定値か（「デフォルトに戻す」を押せるかどうか）。
        /// 言語は数えない（戻すと読めない言語になりかねないので、「デフォルトに戻す」でも変えない）
        /// </summary>
        public bool IsDefault =>
            Same(BgmVolume, GameSettingsRepository.DefaultVolume) && Same(SeVolume, GameSettingsRepository.DefaultVolume)
            && Same(BattleSpeed, GameSettingsRepository.DefaultBattleSpeed) && Fullscreen == GameSettingsRepository.DefaultFullscreen;

        public event Action OnChanged;

        /// <param name="localeCodes">選べる言語のコード（並びが選ぶ順）</param>
        /// <param name="localeCode">今の言語のコード。選べる言語に無ければ、先頭の言語として扱う</param>
        public GameOptionsModel(float bgmVolume, float seVolume, float battleSpeed, IReadOnlyList<string> localeCodes, string localeCode, bool fullscreen)
        {
            _localeCodes = localeCodes ?? new string[0];
            OriginalBgmVolume = SnapVolume(bgmVolume);
            OriginalSeVolume = SnapVolume(seVolume);
            OriginalBattleSpeed = SnapSpeed(battleSpeed);
            OriginalLocaleCode = IndexOfLocale(localeCode) >= 0 || _localeCodes.Count == 0 ? localeCode : _localeCodes[0];
            OriginalFullscreen = fullscreen;

            BgmVolume = OriginalBgmVolume;
            SeVolume = OriginalSeVolume;
            BattleSpeed = OriginalBattleSpeed;
            LocaleCode = OriginalLocaleCode;
            Fullscreen = OriginalFullscreen;
        }

        public void SetBgmVolume(float volume) => Change(SnapVolume(volume), BgmVolume, v => BgmVolume = v);
        public void SetSeVolume(float volume) => Change(SnapVolume(volume), SeVolume, v => SeVolume = v);
        public void SetBattleSpeed(float speed) => Change(SnapSpeed(speed), BattleSpeed, v => BattleSpeed = v);

        /// <summary>言語を step 個先（負なら前）へ回す。端まで行ったら反対の端へ</summary>
        public void StepLocale(int step)
        {
            if (_localeCodes.Count == 0 || step == 0) return;

            int index = Math.Max(0, IndexOfLocale(LocaleCode));
            int next = ((index + step) % _localeCodes.Count + _localeCodes.Count) % _localeCodes.Count;
            if (_localeCodes[next] == LocaleCode) return;

            LocaleCode = _localeCodes[next];
            OnChanged?.Invoke();
        }

        public void SetFullscreen(bool fullscreen)
        {
            if (Fullscreen == fullscreen) return;
            Fullscreen = fullscreen;
            OnChanged?.Invoke();
        }

        /// <summary>言語以外のすべての項目を既定値にする（まだ反映はしない）</summary>
        public void ResetToDefault()
        {
            bool changed = !IsDefault;
            BgmVolume = SnapVolume(GameSettingsRepository.DefaultVolume);
            SeVolume = SnapVolume(GameSettingsRepository.DefaultVolume);
            BattleSpeed = SnapSpeed(GameSettingsRepository.DefaultBattleSpeed);
            Fullscreen = GameSettingsRepository.DefaultFullscreen;
            if (changed) OnChanged?.Invoke();
        }

        private int IndexOfLocale(string code)
        {
            for (int i = 0; i < _localeCodes.Count; i++)
                if (_localeCodes[i] == code) return i;
            return -1;
        }

        private void Change(float value, float current, Action<float> set)
        {
            if (Same(value, current)) return;
            set.Invoke(value);
            OnChanged?.Invoke();
        }

        private static float SnapVolume(float volume)
        {
            return (float)Math.Round(Math.Clamp(volume, 0f, 1f) / VolumeStep) * VolumeStep;
        }

        private static float SnapSpeed(float speed)
        {
            float clamped = Math.Clamp(speed, GameSettingsRepository.MinBattleSpeed, GameSettingsRepository.MaxBattleSpeed);
            return (float)Math.Round(clamped / BattleSpeedStep) * BattleSpeedStep;
        }

        private static bool Same(float a, float b) => Math.Abs(a - b) < 0.001f;
    }
}

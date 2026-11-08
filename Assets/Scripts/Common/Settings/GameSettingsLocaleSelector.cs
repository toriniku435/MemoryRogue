using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Live2DCardRogue.Common.Repositories;

namespace Live2DCardRogue.Common.Settings
{
    // 起動時の言語を、設定（GameSettingsRepository.LocaleCode）から選ぶ。Localization Settings の Startup Locale Selectors に入れてある
    // （コマンドラインの次。設定が空・知らない言語なら null を返し、次の選び方 = システムの言語 → 既定、に任せる）。
    // 初期化の完了を待ってから SelectedLocale を書き換える形は、初期化をその場で終わらせる呼び出し（GetLocalizedString）の中で
    // 完了の通知が来ると、そこからさらに待つことになり例外になる。選ぶ側に回れば初期化の流れの中で決まるので、その問題が無い
    [Serializable]
    public sealed class GameSettingsLocaleSelector : IStartupLocaleSelector
    {
        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            string code = GameSettingsRepository.LocaleCode;
            return string.IsNullOrEmpty(code) ? null : availableLocales.GetLocale(code);
        }
    }
}

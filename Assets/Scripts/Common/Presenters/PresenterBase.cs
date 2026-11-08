using System;
using Live2DCardRogue.Common.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Live2DCardRogue.Common.Presenters
{
    // 全Presenter共通の基底クラス。ライフサイクル(Initialize/Dispose)と
    // 言語切り替え時の再反映をここで一括して面倒を見る。
    public abstract class PresenterBase : IDisposable
    {
        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;
            OnInitialize();
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _isInitialized = false;

            LocalizationSettings.SelectedLocaleChanged -= HandleSelectedLocaleChanged;
            OnDispose();
        }

        // モデルイベントの購読と初回同期を行う
        protected abstract void OnInitialize();

        // OnInitializeで購読したものを解除する
        protected abstract void OnDispose();

        // 言語切り替え時にローカライズ文字列を反映し直す。
        // 実装漏れ防止のため、abstractにしている。
        // 表示するローカライズ文字列が無いPresenterは空実装でよい。
        protected abstract void OnLocaleChanged();

        // WebGL では切り替え直後は新しい言語のテーブルがまだ読み込まれていないので、読み込みを待ってから反映する。
        // 待つあいだに Dispose されたら何もしない
        private void HandleSelectedLocaleChanged(Locale locale)
        {
            LocalizationHelper.WhenTablesReady(() =>
            {
                if (_isInitialized) OnLocaleChanged();
            });
        }
    }
}

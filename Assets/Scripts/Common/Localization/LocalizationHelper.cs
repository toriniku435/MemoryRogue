using System.Collections.Generic;
using UnityEngine.Localization.Settings;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Localization
{
    // ローカライズテキストの取得を一箇所に集約するヘルパー。
    // テーブルを追加するときは、テーブル名の定数と取得メソッドをここに1組ずつ足す
    // (テーブル名やキーの組み立てをViewやPresenterに散らさないため)。
    //
    // キーにはそのデータのIDをそのまま使う規約。どのフィールドがキーなのかを呼び出し側に選ばせず、
    // 各メソッドはデータそのものを受け取ってここでIDを取り出す。
    // 紐づくデータが無いUIラベルだけは例外で、キーをenum(UiTextKey)で型付けしている。
    public static class LocalizationHelper
    {
        public const string CardNameTable = "CardName";
        public const string CardDescriptionTable = "CardDescription";
        public const string StatusEffectNameTable = "StatusEffectName";
        public const string StatusEffectDescriptionTable = "StatusEffectDescription";
        public const string StatusEffectLogDetailTable = "StatusEffectLogDetail";
        public const string CardPackNameTable = "CardPackName";
        public const string CardPackDescriptionTable = "CardPackDescription";
        public const string EnemyNameTable = "EnemyName";
        public const string EnemyDescriptionTable = "EnemyDescription";
        public const string TutorialTextTable = "TutorialText";
        public const string UiTextTable = "UIText";

        // カード表示名。キーはCardData.Id
        public static string GetCardName(CardData cardData)
        {
            return GetLocalizedString(CardNameTable, cardData.Id);
        }

        // カード説明文。キーはCardData.Id。
        // 文は Smart String で数値の名前（{damage} など）を書いてあり、カードの効果が持つ今の数値を差し込む
        public static string GetCardDescription(CardData cardData)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString(CardDescriptionTable, cardData.Id, new object[] { cardData.CreateDescriptionValues() });
        }

        // カードパックの表示名。キーはCardPackData.Id
        public static string GetCardPackName(CardPackData cardPackData)
        {
            return GetLocalizedString(CardPackNameTable, cardPackData.Id);
        }

        // カードパック（画面上はチップ）の説明文。キーはCardPackData.Id
        public static string GetCardPackDescription(CardPackData cardPackData)
        {
            return GetLocalizedString(CardPackDescriptionTable, cardPackData.Id);
        }

        // 敵の表示名。キーはEnemyData.Id
        public static string GetEnemyName(EnemyData enemyData)
        {
            return GetLocalizedString(EnemyNameTable, enemyData.Id);
        }

        // チュートリアルの吹き出しの文。キーは TutorialStep の文言のキー（手順のアセットに書いてある）
        public static string GetTutorialText(Live2DCardRogue.Common.Tutorial.TutorialStep step)
        {
            return GetLocalizedString(TutorialTextTable, step.TextKey);
        }

        // 敵の特徴（マップで次の相手として見せる）。キーはEnemyData.Id
        public static string GetEnemyDescription(EnemyData enemyData)
        {
            return GetLocalizedString(EnemyDescriptionTable, enemyData.Id);
        }

        // 状態効果の見出し。キーはIStatusEffect.Id
        public static string GetStatusEffectName(IStatusEffect statusEffect)
        {
            return GetLocalizedString(StatusEffectNameTable, statusEffect.Id);
        }

        // 状態効果の説明。キーはIStatusEffect.Id
        public static string GetStatusEffectDescription(IStatusEffect statusEffect)
        {
            return GetLocalizedString(StatusEffectDescriptionTable, statusEffect.Id);
        }

        // バトルログで状態効果に添える、数値入りの詳しい説明の書式（{0}: 値・残りターン、{1}: 時間制の効果の効きの大きさ）。
        // キーはIStatusEffect.Id。説明（StatusEffectDescription）と違って、値を埋めて読む前提の文
        public static string GetStatusEffectLogDetail(IStatusEffect statusEffect)
        {
            return GetLocalizedString(StatusEffectLogDetailTable, statusEffect.Id);
        }

        // UIラベル。キーはUiTextKeyのenum名
        public static string GetUiText(UiTextKey key)
        {
            return GetLocalizedString(UiTextTable, key.ToString());
        }

        /// <summary>チャレンジの名前（ノーマル / ハード / マスター / エンドレス）。UIText の行を引く</summary>
        public static string GetChallengeName(ChallengeType challenge)
        {
            return GetUiText(ToUiTextKey(challenge));
        }

        // 種類を増やしたらここも足す。分岐を1箇所に閉じ込めてあるので、足し忘れれば例外で気付ける
        public static UiTextKey ToUiTextKey(ChallengeType challenge)
        {
            switch (challenge)
            {
                case ChallengeType.Normal: return UiTextKey.ChallengeNormal;
                case ChallengeType.Hard: return UiTextKey.ChallengeHard;
                case ChallengeType.Master: return UiTextKey.ChallengeMaster;
                case ChallengeType.Endless: return UiTextKey.Endless;
                default: throw new System.ArgumentOutOfRangeException(nameof(challenge), challenge, "対応するUIテキストのキーがありません");
            }
        }

        /// <summary>選べる言語（コードと、その言語自身での名前。「日本語」「English」）。設定に並んだ順</summary>
        public static IReadOnlyList<LocaleOption> GetAvailableLocales()
        {
            var options = new List<LocaleOption>();
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (locale == null) continue;
                var culture = locale.Identifier.CultureInfo;
                string name = culture != null ? culture.NativeName : locale.LocaleName;
                options.Add(new LocaleOption(locale.Identifier.Code, name));
            }
            return options;
        }

        /// <summary>今の言語のコード（まだ決まっていなければ空）</summary>
        public static string GetSelectedLocaleCode()
        {
            var locale = LocalizationSettings.SelectedLocale;
            return locale != null ? locale.Identifier.Code : "";
        }

        private static string GetLocalizedString(string tableName, string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key);
        }
    }
}

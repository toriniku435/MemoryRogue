using System;
using System.Linq;
using Live2DCardRogue.Common.Audio;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.SceneTransition
{
    // BattleSceneの開始条件。遷移元(マップなど)が組み立て、BattleSceneInitializerが受け取る。
    //
    // PlayerStatusがintの組ではなくUnitStatusModelそのものなのは、戦闘をまたいでHPや状態効果を
    // 持ち越せるようにするため。1戦ごとに作り直したければ、渡す側で新しく作ればよい
    public sealed class BattleSceneTransitionData : SceneTransitionData
    {
        public UnitStatusModel PlayerStatus { get; }
        // デッキと敵は ID で運ぶ。実物は作るときに GameAssetRepository に預け、戦闘シーンはそこから引き直す
        // （ScriptableObject をそのまま渡すと、前のシーンと一緒に解放されることがある）
        /// <summary>デッキのコマンドの ID（並び順のまま。同じコマンドは同じ ID が並ぶ）</summary>
        public string[] DeckIds { get; }
        /// <summary>敵の ID（手前から順）</summary>
        public string[] EnemyIds { get; }

        /// <summary>
        /// 戦闘が終わったら、結果から戻り先のシーンのデータを作る（マップなら MapSceneTransitionData、
        /// チャレンジで負けたら ChallengeResultTransitionData など）。戦闘はこれを呼んで遷移するだけで、戻り先を知らない。
        /// null なら戦闘画面に留まる（単体で再生したときがこれ）。
        /// シーンをまたいで呼ばれるので、遷移元のシーンのオブジェクトを掴まないこと（値だけを取り込む）
        /// </summary>
        public Func<BattleResult, SceneTransitionData> CreateReturnData { get; }

        /// <summary>敵の強さの上乗せ（エンドレスの深い階層）。上乗せが無ければ EnemyStrength.None</summary>
        public EnemyStrength EnemyStrength { get; }

        /// <summary>戦闘で使えるトークン（トークンを払うコマンド用）。払った分は BattleResult.TokensSpent で返す</summary>
        public int Tokens { get; }

        /// <summary>
        /// 練習の戦闘のチュートリアルを流すか。手順は CampaignTutorialRepository が持っている（ここでは運ばない）
        /// </summary>
        public bool PlayTutorial { get; }

        /// <summary>デッキを切るか。チュートリアルは手順に合わせた並びのまま引かせる（false）</summary>
        public bool ShuffleDeck { get; }

        /// <summary>戦闘中に流す BGM（ボス戦だけ変える、など）</summary>
        public BgmId Bgm { get; }

        public BattleSceneTransitionData(
            UnitStatusModel playerStatus,
            CardData[] deck,
            EnemyData[] enemies,
            Func<BattleResult, SceneTransitionData> createReturnData = null,
            bool playTutorial = false,
            bool shuffleDeck = true,
            EnemyStrength enemyStrength = null,
            int tokens = 0,
            BgmId bgm = BgmId.Battle)
            : base(SceneName.Battle)
        {
            Bgm = bgm;
            Tokens = tokens < 0 ? 0 : tokens;
            PlayerStatus = playerStatus;
            DeckIds = (deck ?? new CardData[0]).Select(GameAssetRepository.Hold).Where(id => id != null).ToArray();
            EnemyIds = (enemies ?? new EnemyData[0]).Select(GameAssetRepository.Hold).Where(id => id != null).ToArray();
            CreateReturnData = createReturnData;
            PlayTutorial = playTutorial;
            ShuffleDeck = shuffleDeck;
            EnemyStrength = enemyStrength ?? EnemyStrength.None;
        }
    }
}

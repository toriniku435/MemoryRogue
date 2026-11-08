using Live2DCardRogue.Common.Tutorial;
using UnityEngine;

namespace Live2DCardRogue.Common.Repositories
{
    public enum CampaignTutorialStage
    {
        // チュートリアル中ではない
        None,
        // マップの説明をする（マップを開いたら流す）
        Map,
        // 練習の戦闘に行っている（戻ってきたら終わり）
        Battle,
    }

    // キャンペーンの最初のチュートリアルがどこまで進んだか。シーンをまたいで残る。
    // 保存はしない: 途中でゲームを終えたら、次はまた最初から（新しいキャンペーンなら、始めるときにもう一度聞く）
    //
    // マップと練習の戦闘の手順も、始めてから終わるまでここが持つ（マップはマップの、戦闘は戦闘の手順を取る）。
    // 手順は ScriptableObject で、マップのシーンからしか参照されていないため、持っている間は「解放しない」印を付ける
    // （印が無いと、ビルドでは戦闘へ切り替えるときに解放され、Android の起動後最初の模擬戦でチュートリアルが流れなかった）。
    // 印は始めるとき（マップが生きているうち）に付け、終えるときに外す
    public static class CampaignTutorialRepository
    {
        public static CampaignTutorialStage Stage { get; private set; } = CampaignTutorialStage.None;

        public static bool IsActive => Stage != CampaignTutorialStage.None;

        /// <summary>
        /// マップに来たら「チュートリアルを見ますか？」と聞くか（新しいキャンペーンを始めたとき、タイトルが立てる）。
        /// 聞くまでマップはファーストビルドを始めない
        /// </summary>
        public static bool IsPromptPending { get; private set; }

        public static void RequestPrompt() => IsPromptPending = true;

        public static void ClearPrompt() => IsPromptPending = false;

        /// <summary>マップで流す手順（チュートリアル中だけ。それ以外は null）</summary>
        public static TutorialSequence MapSequence { get; private set; }

        /// <summary>練習の戦闘で流す手順（チュートリアル中だけ。それ以外は null）</summary>
        public static TutorialSequence BattleSequence { get; private set; }

        public static void StartFromMap(TutorialSequence mapSequence, TutorialSequence battleSequence)
        {
            Release();
            MapSequence = Hold(mapSequence);
            BattleSequence = Hold(battleSequence);
            Stage = CampaignTutorialStage.Map;
        }

        public static void MoveToBattle() => Stage = CampaignTutorialStage.Battle;

        public static void Finish()
        {
            Release();
            Stage = CampaignTutorialStage.None;
        }

        private static TutorialSequence Hold(TutorialSequence sequence)
        {
            if (sequence != null) sequence.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return sequence;
        }

        private static void Release()
        {
            if (MapSequence != null) MapSequence.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
            if (BattleSequence != null) BattleSequence.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
            MapSequence = null;
            BattleSequence = null;
        }
    }
}

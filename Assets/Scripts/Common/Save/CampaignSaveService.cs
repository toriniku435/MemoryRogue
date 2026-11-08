using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Repositories;
using Live2DCardRogue.Common.ScriptableObjects;

namespace Live2DCardRogue.Common.Save
{
    // キャンペーンの進捗（campaign.sav、チャレンジは endless.sav）とクリア記録（clear.sav）の保存と読み込みの入口。
    //
    //   - 進捗: キャンペーンのリポジトリをまとめて1つのファイルにする（形は CampaignSaveSnapshot）。
    //           節目でマップが SaveProgress を呼び、タイトルが TryLoadProgress / CampaignSaveSnapshot.Restore で再開する。
    //           ストーリーとチャレンジは別のファイルなので、1つずつ中断しておける（ファイル名はエンドレスしか無かったころのまま endless.sav）
    //   - クリア記録: ClearRecordStore。進捗を保存するたびに所持コマンドを、戦闘を始めるたびに敵を、一度でも出会ったものとして足す。
    //           チャレンジは負けたか踏破したら FinishChallengeRun で記録を更新し、進捗を消す
    //
    // ファイルの暗号化・改ざん検知・旧形式（平文の .json）の読み込みは SaveFileCodec。
    // 保存先は Application.persistentDataPath/save（Default）。テストでは別のフォルダを渡して作る
    public sealed class CampaignSaveService
    {
        public const string ProgressFileName = "campaign.sav";
        public const string ClearFileName = ClearRecordStore.FileName;
        public const string EndlessProgressFileName = "endless.sav";

        // 暗号化する前の形式（平文の JSON）。読むだけで、書くときは新しい形式へ移して消す
        public const string LegacyProgressFileName = "campaign.json";
        public const string LegacyClearFileName = ClearRecordStore.LegacyFileName;

        private static CampaignSaveService _default;

        private readonly SaveFileCodec _codec;
        private readonly ClearRecordStore _records;

        public CampaignSaveService(SaveFileStorage storage, SaveCipher cipher = null)
        {
            _codec = new SaveFileCodec(storage, cipher ?? SaveCipher.Default);
            _records = new ClearRecordStore(_codec);
        }

        /// <summary>ゲームが使う保存先（Application.persistentDataPath/save）</summary>
        public static CampaignSaveService Default =>
            _default ??= new CampaignSaveService(new SaveFileStorage(Path.Combine(Application.persistentDataPath, "save")));

        public string Directory => _codec.Directory;

        // ---------------- 進捗 ----------------

        /// <summary>今のキャンペーンのリポジトリの中身を、遊んでいるモード（RunModeRepository）のファイルに保存する</summary>
        /// <param name="stageCount">ステージ数（タイトルで「LAYER 04 / 10」と出すために一緒に保存する。終わりが無ければ 0）</param>
        public void SaveProgress(int stageCount)
        {
            var mode = RunModeRepository.Mode;
            _codec.Write(ProgressFileOf(mode), LegacyProgressFileOf(mode), JsonUtility.ToJson(CampaignSaveSnapshot.Create(stageCount)));
            _records.RecordDiscoveredCommands(CampaignDeckRepository.OwnedCards);
        }

        /// <summary>そのモードの中断したキャンペーンがあれば読む。無い・壊れている・新しすぎる版なら false</summary>
        public bool TryLoadProgress(out CampaignSaveData data, RunMode mode = RunMode.Campaign)
        {
            data = null;
            if (!_codec.TryReadJson(ProgressFileOf(mode), LegacyProgressFileOf(mode), out var json)) return false;
            try
            {
                data = JsonUtility.FromJson<CampaignSaveData>(json);
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"進捗のセーブデータが読めません（壊れている）: {e.Message}");
                data = null;
            }
            if (data == null || data.version < 1 || data.version > CampaignSaveData.CurrentVersion) return false;
            // 別のモードのファイルに紛れ込んだ中身は読まない
            return data.isEndless == (mode == RunMode.Challenge);
        }

        public void DeleteProgress(RunMode mode = RunMode.Campaign)
        {
            _codec.Delete(ProgressFileOf(mode));
            var legacy = LegacyProgressFileOf(mode);
            if (legacy != null) _codec.Delete(legacy);
        }

        // ---------------- クリア記録 ----------------

        /// <summary>クリア記録。まだ無ければ空</summary>
        public CampaignClearData LoadClearData() => _records.Load();

        /// <summary>クリアを1件記録する（進捗の削除は呼ぶ側が行う）</summary>
        public void RecordClear(CampaignResult result) => _records.RecordClear(result);

        /// <summary>クリア記録をすべて消す（ユーザーデータの削除）。エピソードの解放も最初に戻る</summary>
        public void DeleteClearData() => _records.Delete();

        public void RecordDiscoveredCommands(IEnumerable<CardData> cards) => _records.RecordDiscoveredCommands(cards);

        public void RecordDiscoveredEnemies(IEnumerable<EnemyData> enemies) => _records.RecordDiscoveredEnemies(enemies);

        /// <summary>
        /// チャレンジを終えた（負けた、または最後の階層まで踏破した）: そのチャレンジの記録を更新し、中断データ（進捗）を消す。
        /// 更新する前の最高到達階層を返す（まだ遊んでいなければ 0）
        /// </summary>
        /// <param name="reachedLayer">到達した階層（1始まり。負けたならその階層、踏破したなら最後の階層）</param>
        public int FinishChallengeRun(ChallengeType challenge, int reachedLayer, bool cleared)
        {
            int previousBest = _records.RecordChallenge(challenge, reachedLayer, cleared);
            DeleteProgress(RunMode.Challenge);
            return previousBest;
        }

        /// <summary>
        /// 一度でも手に入れたコマンドの ID。記録に加えて、中断しているストーリーとチャレンジの所持コマンドも数える
        /// （記録を始める前から遊んでいたデータでも、持っているものは手に入れたことにする）
        /// </summary>
        public HashSet<string> LoadDiscoveredCommandIds()
        {
            var ids = new HashSet<string>(_records.Load().discoveredCommands);
            foreach (var mode in new[] { RunMode.Campaign, RunMode.Challenge })
            {
                if (!TryLoadProgress(out var progress, mode) || progress.ownedCards == null) continue;
                foreach (var id in progress.ownedCards)
                {
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
            }
            return ids;
        }

        private static string ProgressFileOf(RunMode mode)
        {
            return mode == RunMode.Challenge ? EndlessProgressFileName : ProgressFileName;
        }

        // 平文の旧形式があるのはストーリーだけ（エンドレスは暗号化の後にできた）
        private static string LegacyProgressFileOf(RunMode mode)
        {
            return mode == RunMode.Challenge ? null : LegacyProgressFileName;
        }
    }
}

namespace Live2DCardRogue.Common.Audio
{
    // 効果音の種類。SoundLibrary がこの値ごとに音を持つ（値の名前で保存している）。
    // 生成スクリプト（SoundSource/sfx.py）の出力 se_<小文字のスネークケース>.wav と対応している。
    // CommonButton などのコンポーネントは数値で保存しているので、値は途中に挟まず末尾に足すこと
    public enum SeId
    {
        /// <summary>鳴らさない（ボタンごとに音を消したいとき）</summary>
        None,
        [SoundUsage("ボタンにカーソルを乗せた / 使えないカードを調べた")] Hover,
        [SoundUsage("ボタンを押した / 演出のスキップ")] Click,
        [SoundUsage("決定系のボタン（実行・OK・保存・購入）/ ランク確定")] Confirm,
        [SoundUsage("キャンセル系のボタン / 選択の取り消し")] Cancel,
        [SoundUsage("押せないボタン・使えないカードを押した")] Error,
        [SoundUsage("手札やリストのカードを選んだ")] CardSelect,
        [SoundUsage("コマンドをメモリーに書き込んだ / デッキに追加 / 敵が生成された")] MemoryWrite,
        [SoundUsage("コマンドを手札に戻した / デッキから外した")] CardReturn,
        [SoundUsage("プレイヤーのコマンドを実行")] CommandExecute,
        [SoundUsage("ウィンドウが開く")] WindowOpen,
        [SoundUsage("ウィンドウが閉じる")] WindowClose,
        [SoundUsage("シーン切り替えのグリッチ")] Transition,
        [SoundUsage("勝利報酬のファンファーレ / ランクA以上の確定")] Reward,
        [SoundUsage("敗北時のバックアップブースト")] BackupBoost,
        [SoundUsage("トークンのカウントアップ / リザルトの行")] TokenTick,
        [SoundUsage("リザルトのランクのデコード")] RankReveal,
        [SoundUsage("チップのインストールのチャージ（長い音）")] InstallCharge,
        [SoundUsage("チップの色がレアに昇格")] InstallPromote,
        [SoundUsage("チップの色がエピックに昇格")] InstallPromoteEpic,
        [SoundUsage("開封したコマンドがめくれる")] CardFlip,
        [SoundUsage("敵がコマンドを使った")] EnemyAction,
        [SoundUsage("ユニットがダメージを受けた")] Hit,
        [SoundUsage("ユニットが回復した（リジェネ含む）")] Heal,
        [SoundUsage("ウイルス・ワームの継続ダメージ")] VirusTick,
        [SoundUsage("継続効果が付いた・重ねがけされた（ブロックは除く）")] StatusApply,
        [SoundUsage("ブロックを張った・重ねて増えた")] BlockGain,
        [SoundUsage("ブロックが攻撃を防いだ（残りあり）")] BlockAbsorb,
        [SoundUsage("ブロックが割れた（0 になった）")] BlockBreak,
    }
}

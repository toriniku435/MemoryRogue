using Live2DCardRogue.Common.Models;

namespace Live2DCardRogue.Common.Statuses
{
    public interface IStatusEffect
    {
        string Id { get; }

        /// <summary>
        /// ステータス効果の値
        /// </summary>
        int Value { get; }

        /// <summary>役目を終え、StatusEffects リストから取り除んでよいか（ワンショット効果の消費完了判定にも使う）</summary>
        bool IsExpired { get; }

        /// <summary>
        /// HP変動量への割り込みフック。割り込み地点は type で区別する。
        /// value はHPの変動量（マイナスで減少、プラスで回復）なので、
        /// ダメージにだけ関心のある効果はプラスの値を素通しすること。
        /// 関心の無い type もそのまま value を返すこと（StatusEffectBase の既定実装がそうなっている）
        /// </summary>
        int OnHpChange(HpChangeType type, int value);

        /// <summary>
        /// スタミナ変動量への割り込みフック。扱いはOnHpChangeと同じ
        /// </summary>
        int OnStaminaChange(StaminaChangeType type, int value);

        /// <summary>
        /// このユニットが生み出す値への割り込みフック（攻撃力上昇などの強化バフはここ）。
        /// OnHpChange/OnStaminaChangeが「受ける側」なのに対し、こちらは「与える側」。
        /// value は大きさ（プラス）で、符号は持たない
        /// </summary>
        int OnDealtValue(DealtValueType type, int value);

        /// <summary>
        /// 上限・回数への割り込みフック（メモリー容量、引く手札の枚数、行動回数など）。
        /// value は割り込み前の上限で、変えたい上限だけを見て返す。関心の無い type はそのまま返すこと
        /// </summary>
        int OnStatLimit(StatLimitType type, int value);

        /// <summary>
        /// プレイヤーがコマンドを1つ実行し始めるときのフック（効果を実行する前）。
        /// 「次に実行するコマンド」に効く効果は、ここで既に付いていた分だけを対象にする
        /// （そのコマンド自身が付けた効果を、そのコマンドで使い切らないように）
        /// </summary>
        void OnCommandStart();

        /// <summary>
        /// 実行し終えたコマンドの行き先への割り込みフック。関心が無ければ destination をそのまま返すこと
        /// </summary>
        CommandDestination OnCommandDestination(CommandDestination destination);

        /// <summary>プレイヤーがコマンドを1つ実行し終えたときのフック（行き先を決めたあと）。使い切る効果はここで IsExpired を立てる</summary>
        void OnCommandEnd();

        /// <summary>ターン開始時の処理フック</summary>
        void OnTurnStart(UnitStatusModel owner);

        /// <summary>ターン終了時の処理フック</summary>
        void OnTurnEnd(UnitStatusModel owner);

        /// <summary>
        /// 持ち主に新しい状態効果が付こうとしたときのフック。true を返すとその効果は付かない（ウイルス無効など）。
        /// 関心が無ければ false を返すこと（StatusEffectBase の既定実装がそうなっている）
        /// </summary>
        bool BlocksStatusEffect(IStatusEffect incoming);

        /// <summary>既に付いているところへ同じ効果が再度付与されたときの処理。加算・上書き・無視など、重複時の振る舞いは効果ごとに決める</summary>
        /// <param name="value">新たに付与されようとした効果の値</param>
        void OnDuplicated(int value);

        /// <summary>同じ効果かどうかの判定</summary>
        bool IsSameEffect(IStatusEffect other);

        /// <summary>
        /// コマンドで付いたものではなく、そのユニットが生まれつき持つ性質か（敵の無効化・耐性、深い階層の上乗せ）。
        /// 「効果をすべて消す」コマンド（フォーマット）でも消えない
        /// </summary>
        bool IsTrait { get; }
    }
}

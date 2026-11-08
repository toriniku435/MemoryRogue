using Live2DCardRogue.Common.Models;
using Live2DCardRogue.Common.Statuses;
using Live2DCardRogue.Common.Tutorial;
using UnityEngine;

namespace Live2DCardRogue.Common.ScriptableObjects
{
    // キャンペーンの最初のチュートリアル。マップの手順と、練習の戦闘（手順・固定のデッキ・敵・プレイヤーの状態）。
    // 戦闘の手順はデッキの並び（上から引く・切らない）に合わせて書いてあるので、デッキを変えたら手順も見直す
    [CreateAssetMenu(fileName = "CampaignTutorial", menuName = "Tutorial/CampaignTutorialData")]
    public sealed class CampaignTutorialData : ScriptableObject
    {
        [Header("マップ")]
        [SerializeField] private TutorialSequence _mapSequence;

        [Header("練習の戦闘")]
        [SerializeField] private TutorialSequence _battleSequence;
        /// <summary>引く順に並べる（切らない）。最初のターンに上から5個、次のターンに次の5個を引く</summary>
        [SerializeField] private CardData[] _deck = new CardData[0];
        [SerializeField] private EnemyData[] _enemies = new EnemyData[0];
        [SerializeField][Min(1)] private int _playerMaxHp = 100;
        /// <summary>回復やリジェネが目に見えるよう、少し減った状態から始める</summary>
        [SerializeField][Min(1)] private int _playerStartHp = 70;
        [SerializeField][Min(0)] private int _playerMaxMemory = 10;

        public TutorialSequence MapSequence => _mapSequence;
        public TutorialSequence BattleSequence => _battleSequence;
        public CardData[] Deck => _deck ?? new CardData[0];
        public EnemyData[] Enemies => _enemies ?? new EnemyData[0];

        public UnitStatusModel CreatePlayerStatus()
        {
            var status = new UnitStatusModel(_playerMaxHp, _playerMaxMemory);
            int missing = Mathf.Clamp(_playerMaxHp - _playerStartHp, 0, _playerMaxHp - 1);
            if (missing > 0) status.ChangeHp(HpChangeType.TakePiercingDamage, -missing);
            return status;
        }
    }
}

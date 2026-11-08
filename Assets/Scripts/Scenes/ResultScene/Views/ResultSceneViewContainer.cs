using UnityEngine;
using Live2DCardRogue.Common.Views;

namespace Live2DCardRogue.ResultScene.Views
{
    public sealed class ResultSceneViewContainer : MonoBehaviour
    {
        [SerializeField] private ResultView _resultView;

        [SerializeField] private CommonText _titleLabel;
        [SerializeField] private CommonText _tokenRowLabel;
        [SerializeField] private CommonText _backupRowLabel;
        [SerializeField] private CommonText _scoreLabel;
        [SerializeField] private CommonText _rankLabel;

        [SerializeField] private CommonButton _backToTitleButton;
        [SerializeField] private CommonText _backToTitleLabel;

        public ResultView ResultView => _resultView;
        public CommonText TitleLabel => _titleLabel;
        public CommonText TokenRowLabel => _tokenRowLabel;
        public CommonText BackupRowLabel => _backupRowLabel;
        public CommonText ScoreLabel => _scoreLabel;
        public CommonText RankLabel => _rankLabel;
        public CommonButton BackToTitleButton => _backToTitleButton;
        public CommonText BackToTitleLabel => _backToTitleLabel;
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Live2DCardRogue.Common.Views
{
    // 敵詳細ウインドウの、敵が使うコマンド1行: 絵・名前・効果の説明
    public sealed class EnemyCommandRowView : MonoBehaviour
    {
        [SerializeField] private Image _illustration;
        [SerializeField] private CommonText _nameText;
        [SerializeField] private CommonText _descriptionText;

        public void Bind(EnemyDetailCommandInfo info)
        {
            _illustration.sprite = info.Illustration;
            _illustration.enabled = info.Illustration != null;
            _nameText.text = info.Name;
            _descriptionText.text = info.Description;
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }
    }
}

using System.Collections;
using Live2DCardRogue.Common.Localization;
using UnityEngine;

namespace Live2DCardRogue.Common.SceneTransition
{
    // シーンのInitializer（コンポジションルート）の基底。型引数がそのシーンへの入り方を表す。
    //
    // Start で SceneTransitionDataStore から TData を1度だけ取り出して Initialize に渡す。
    // 単体で再生したときは何も渡ってこないので data は null になり、Inspectorのモックへのフォールバックは各シーンが決める。
    //
    // 継承先で Start を定義しないこと。Unity は派生側の Start だけを呼ぶので、この取り出しが黙って飛ばされる
    //
    // WebGL では、ローカライズの文字列テーブルを読み終えるまで Initialize を遅らせる（LocalizationHelper.MustWaitForTables）。
    // 組み立ての途中で文字列を同期で引くため、読み終わる前に始めると例外で途中から先のウインドウが作られない。
    // 待つのは起動直後の最初のシーンだけで、ほかのプラットフォームでは待たない
    public abstract class SceneInitializerBase<TData> : MonoBehaviour where TData : SceneTransitionData
    {
        private IEnumerator Start()
        {
            while (LocalizationHelper.MustWaitForTables) yield return null;

            SceneTransitionDataStore.TryConsume<TData>(out var data);
            Initialize(data);
        }

        /// <param name="data">遷移元から渡されたデータ。シーンを単体で再生したときは null</param>
        protected abstract void Initialize(TData data);
    }
}

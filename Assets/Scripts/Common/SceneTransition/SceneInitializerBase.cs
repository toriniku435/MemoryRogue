using UnityEngine;

namespace Live2DCardRogue.Common.SceneTransition
{
    // シーンのInitializer（コンポジションルート）の基底。型引数がそのシーンへの入り方を表す。
    //
    // Start で SceneTransitionDataStore から TData を1度だけ取り出して Initialize に渡す。
    // 単体で再生したときは何も渡ってこないので data は null になり、Inspectorのモックへのフォールバックは各シーンが決める。
    //
    // 継承先で Start を定義しないこと。Unity は派生側の Start だけを呼ぶので、この取り出しが黙って飛ばされる
    public abstract class SceneInitializerBase<TData> : MonoBehaviour where TData : SceneTransitionData
    {
        private void Start()
        {
            SceneTransitionDataStore.TryConsume<TData>(out var data);
            Initialize(data);
        }

        /// <param name="data">遷移元から渡されたデータ。シーンを単体で再生したときは null</param>
        protected abstract void Initialize(TData data);
    }
}

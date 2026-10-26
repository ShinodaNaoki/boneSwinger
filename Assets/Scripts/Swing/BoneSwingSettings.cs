using System;
using UnityEngine;

namespace Duel.BoneDragger
{
    /// <summary>
    /// BoneSwing の調整値。長さはすべて「チェーン長」で正規化されるので、キャラの大きさ・カメラ設定に依存しない。
    /// </summary>
    [Serializable]
    public class BoneSwingSettings
    {
        [Range(0, 1), Tooltip("慣性。0: 自分の勢いを持たず空気と拘束だけで動く（リボン）, 1: 勢いで振り子のように揺れ返す")]
        public float inertia = 0.5f;

        [Range(0, 1), Tooltip("空気抵抗。高いほど移動方向に向いたまま戻りにくく、揺れもすぐ収まる")]
        public float airDrag = 0.3f;

        [Range(0, 1), Tooltip("復元力。元のポーズ（アニメーションの姿勢）へ戻ろうとする強さ。揺れの周期 ≈ 2π√(慣性 / (復元力×200)) 秒")]
        public float stiffness = 0.3f;

        [Range(0, 1), Tooltip("減衰（減衰比）。曲げの揺れだけを抑え、空気抵抗のように移動方向へは向かない。1 で揺れ返さずに止まる")]
        public float damping = 0.2f;

        [Range(0, 1), Tooltip("はためき。空気との相対速度に応じて、先端ほど横に波打つ（リボン・旗向け）")]
        public float flutter = 0f;

        [Range(1, 180), Tooltip("曲げ限界角（元のポーズからの相対角）。この角度に近づくほど曲がりにくくなる")]
        public float angleLimit = 60f;

        [Range(1, 8), Tooltip("硬化カーブ。2: 早めから徐々に硬くなる（竹）, 6〜: 限界直前まで柔らかい（ゴムのストッパー）")]
        public float hardening = 2f;

        [Range(0, 2), Tooltip("重力の強さ（チェーン長/秒² 相当の相対値）")]
        public float gravity = 0f;

        [Tooltip("重力の向き（ワールド）")]
        public Vector2 gravityDirection = Vector2.down;

        [Tooltip("風（チェーン長/秒）。空気そのものの速度として空気抵抗に効く")]
        public Vector2 wind = Vector2.zero;

        [Tooltip("ON: キャラのルート空間で計算し、キャラ自体の移動・反転を揺れに含めない")]
        public bool simulateInLocalSpace = false;

        [Tooltip("根本(0)→先端(1) の関節の硬さの倍率。根本だけ低くすると棒のように根本で振れ、先端ほど低くするとしなる")]
        public AnimationCurve stiffnessCurve = AnimationCurve.Linear(0, 1, 1, 1);

        [Tooltip("根本(0)→先端(1) の空気抵抗（受風面積）の倍率。既定では面積はボーンの長さに比例し、これを掛けた後で総量 1 に正規化する")]
        public AnimationCurve airDragCurve = AnimationCurve.Linear(0, 1, 1, 1);

        public BoneSwingSettings Clone()
        {
            var c = (BoneSwingSettings)MemberwiseClone();
            c.stiffnessCurve = new AnimationCurve(stiffnessCurve.keys);
            c.airDragCurve = new AnimationCurve(airDragCurve.keys);
            return c;
        }
    }
}

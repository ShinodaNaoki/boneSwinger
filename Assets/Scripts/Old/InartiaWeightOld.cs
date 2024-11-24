using NaughtyAttributes;
using System;
using UnityEngine;

namespace Old.V1
{
    [Serializable]
    public class InartiaWeightOld
    {
        [SerializeField]
        public GameObject target;

        [SerializeField, Range(0, 4)]
        private float moveScale = 1f;

        [SerializeField, Range(0, 1)]
        private float damping = 0.7f;

        [SerializeField, Range(0, 1)]
        private float mass = 1f;

        [SerializeField, Range(1, 4)]
        private float exponent = 2f;

        private float expSquare;

        public bool Freeze = false;

        private Vector3 velocity;

        private Vector3 prevVelocity;

        private float gravity;

        public Vector3 Force
        {
            get
            {
                if (Freeze) return Vector3.zero;
                var vtmp = velocity - prevVelocity;
                var vmag = velocity.magnitude * moveScale;
                var vsum = vmag + gravity;
                // 混合比率としての gravity, ベクトル強度としての gravity で二回掛ける
                return (vtmp * vmag + Vector3.down * gravity * gravity) / vsum;
            }
        }

        private Vector3 accumlatedForce;

        private Vector3 prevPosition;

        // Start is called before the first frame update
        void Awake()
        {
            Reset();
        }

        [Button]
        public void Reset()
        {
            accumlatedForce = Vector3.zero;
            velocity = Vector3.zero;
            prevVelocity = Vector3.zero;
            gravity = Mathf.Pow(mass, 2);
            prevPosition = target.transform.position;
            expSquare = exponent * exponent;
        }

        public void Update()
        {
            // 移動量を力とする
            var curPos = target.transform.position;
            prevVelocity = curPos - prevPosition;

            // 慣性力を考慮した現在の位置
            var move = curPos + velocity - prevPosition;
            prevPosition = curPos;

            if (this.Freeze)
            {
                return;
            }

            accumlatedForce *= (1 - damping);
            var m1 = adjustMagnitude(accumlatedForce.magnitude, 4f);
            var m2 = adjustMagnitude(move.magnitude, 4f);
            var mTotal = m1 + m2;
            if (mTotal <= Mathf.Epsilon)
            {
                return;
            }

            // 力を積算
            accumlatedForce = (m1 * accumlatedForce + m2 * move) / mTotal;

            var mNew = adjustMagnitude(accumlatedForce.magnitude, expSquare);
            var direction = accumlatedForce.normalized;
            // 対数関数を用いて速度を計算
            velocity = direction * mNew;
        }

        private float adjustMagnitude(float mag, float exp)
        {
            return 2f / (1f + Mathf.Pow(exp, -mag)) - 1f;
        }

        private void OnEnable()
        {
            Reset();
        }

        public void OnValidate()
        {
            expSquare = exponent * exponent;
            gravity = Mathf.Pow(mass, 2);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void OnDrawGizmos()
        {
            if (Freeze || target == null) return;
            var pos0 = target.transform.position;
            var size = UnityEditor.HandleUtility.GetHandleSize(pos0);
            var radius = size * 0.1f;

            var pos1 = pos0 + Force;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pos1, radius);
            var pos2 = pos0 + accumlatedForce;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos2, radius * 0.8f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(pos0, pos1);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(prevPosition, pos0);

        }

    }
}
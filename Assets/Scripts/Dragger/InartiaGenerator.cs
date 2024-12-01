using NaughtyAttributes;
using System;
using UnityEngine;

namespace Duel.BoneDragger
{
    [Serializable]
    public class InartiaGenerator
    {
        [SerializeField]
        public Transform target;

        private float expSquare;

        public bool Freeze = false;

        private Vector3 velocity;

        private Vector3 prevVelocity;

        private float gravity;

        public InartiaParameters Parameters { get; set; }

        public Vector3 Force
        {
            get
            {
                if (Freeze || Parameters.moveScale == 0) return Vector3.zero;
                var vtmp = prevVelocity - velocity;
                var vmag = velocity.magnitude * Parameters.moveScale;
                var vsum = vmag + gravity;
                // �����䗦�Ƃ��Ă� gravity, �x�N�g�����x�Ƃ��Ă� gravity �œ��|����
                return (vtmp * vmag + Vector3.down * gravity * gravity) / vsum;
            }
        }

        private Vector3 accumlatedForce;

        private Vector3 prevPosition;

        // Start is called before the first frame update


        internal void Init(Transform baseBone, InartiaParameters ips)
        {
            target = baseBone;
            Parameters = ips;
            Reset();
        }

        [Button]
        public void Reset()
        {
            accumlatedForce = Vector3.zero;
            velocity = Vector3.zero;
            prevVelocity = Vector3.zero;
            gravity = Mathf.Pow(Parameters.mass, 2);
            prevPosition = target.transform.position;
            expSquare = Parameters.exponent * Parameters.exponent;
        }

        public void Update()
        {
            // �ړ��ʂ�͂Ƃ���
            var curPos = target.transform.position;
            prevVelocity = curPos - prevPosition;

            // �����͂��l���������݂̈ʒu
            var move = curPos + velocity - prevPosition;
            prevPosition = curPos;

            if (this.Freeze)
            {
                return;
            }

            accumlatedForce *= (1 - Parameters.damping);
            var m1 = adjustMagnitude(accumlatedForce.magnitude, 4f);
            var m2 = adjustMagnitude(move.magnitude, 4f);
            var mTotal = m1 + m2;
            if (mTotal <= Mathf.Epsilon)
            {
                return;
            }

            // �͂�ώZ
            accumlatedForce = (m1 * accumlatedForce + m2 * move) / mTotal;

            var mNew = adjustMagnitude(accumlatedForce.magnitude, expSquare);
            var direction = accumlatedForce.normalized;
            // �ΐ��֐���p���đ��x���v�Z
            velocity = direction * mNew;
        }

        private float adjustMagnitude(float mag, float exp)
        {
            return 2f / (1f + Mathf.Pow(exp, -mag)) - 1f;
        }


        public void OnValidate(InartiaParameters p)
        {
            Parameters = p;
            expSquare = Parameters.exponent * Parameters.exponent;
            gravity = Mathf.Pow(Parameters.mass, 2);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void OnDrawGizmos()
        {
#if UNITY_EDITOR
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
#endif
        }

    }

    [Serializable]
    public class InartiaParameters
    {
        [SerializeField, Range(0, 4)]
        public float moveScale = 1f;

        [SerializeField, Range(0, 1)]
        public float damping = 0.7f;

        [SerializeField, Range(0, 1)]
        public float mass = 1f;

        [SerializeField, Range(1, 4)]
        public float exponent = 2f;

    }
}

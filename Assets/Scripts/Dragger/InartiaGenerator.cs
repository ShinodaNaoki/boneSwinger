using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private Vector3 force;
        private Queue<Vector3> forceQueue;

        private float gravity;

        public InartiaParameters Parameters { get; set; }

        public Vector3 Force
        {
            get
            {
                if (Freeze || Parameters.moveScale == 0) return Vector3.zero;
                var vtmp = forceQueue.Aggregate(Vector3.zero, (sum,v) => sum+v);
                var vmag = vtmp.magnitude * Parameters.moveScale / forceQueue.Count;
                var vsum = vmag + gravity;
                // 混合比率としての gravity, ベクトル強度としての gravity で二回掛ける
                var f = -vtmp.normalized * vmag + Vector3.down * gravity;
                return f;
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
            forceQueue = new Queue<Vector3>(Enumerable.Range(0,5).Select(_ => Vector3.zero));
            accumlatedForce = Vector3.zero;
            velocity = Vector3.zero;
            prevVelocity = Vector3.zero;
            force = Vector3.zero;
            gravity = Mathf.Pow(Parameters.mass, 2);
            prevPosition = target.transform.position;
            expSquare = Parameters.exponent * Parameters.exponent;
        }

        public void Update()
        {
            // 移動量を力とする
            var curPos = target.transform.position;
            prevVelocity = velocity;
            velocity = curPos - prevPosition;
            prevPosition = curPos;

            if (this.Freeze)
            {
                return;
            }
            force = velocity - prevVelocity;
            force = force.normalized * adjustMagnitude(force.magnitude, expSquare);
            forceQueue.Enqueue(force);
            forceQueue.Dequeue();
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

            var pos1 = pos0 + force;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pos1, radius);
            var pos2 = pos0 + velocity;
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

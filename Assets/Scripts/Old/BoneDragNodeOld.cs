using System;
using UnityEngine;
using Transform = UnityEngine.Transform;

namespace Old.V1
{
    [Serializable]
    public class BoneDragNodeOld
    {
        const float INARTIA_SCALE = 100000f;
        const float MAX_AIR_DRAG_ANGLE = 30f;
        const float AIR_DRAG_MULTIPLIER = 60f;
        const float MAX_RESTORE_ANGLE = 60f;
        const float RESTORE_SPRING_MULTIPLIER = 10000f;
        const float ANGLE_OVER_LIMIT_DAMPER = 2f;

        [SerializeField]
        public Rigidbody rigidbody;
        [SerializeField]
        public Transform target;
        [SerializeField]
        public float airDrag = 1f;
        [SerializeField]
        public float weight = 0.1f;
        [SerializeField]
        public float softness = 1f;
        [SerializeField, Tooltip("initial localRotation")]
        public Quaternion initailRotation;


        [SerializeField, Range(0, 20)]
        private float rotateVelocityMax = 5f;

        public Transform nextBone;
        private Vector3 prevPos;
        private Quaternion prevWorldRot;
        private Quaternion prevLocalRot;
        private Rigidbody targetBody;
        private AngleLimitter limitter = new AngleLimitter();
        public float Length { get; private set; }
        public float FollowingLength { get; private set; }
        public float PrecedingLength { get; private set; }

        public float WeightInLength { get; private set; }

        private Quaternion ParentRotation
        {
            get
            {
                return target.parent != null ? target.parent.rotation : Quaternion.identity;
            }
        }

        public void Init(BoneDragNodeOld next)
        {
            prevPos = target.position;
            prevWorldRot = target.rotation;
            targetBody = target.GetComponent<Rigidbody>();
            limitter = new AngleLimitter();
            FollowingLength = 0;
            if (next == null) return;
            nextBone = next.target;
            Length = (nextBone.transform.position - prevPos).magnitude;
            FollowingLength = next.Length + next.FollowingLength;
        }

        public void LateInit(BoneDraggerManagerOld manager)
        {
            PrecedingLength = Mathf.Max(0, manager.TotalLength - Length - FollowingLength);
            WeightInLength = Length / manager.TotalLength;
        }

        public void Update(BoneDraggerManagerOld manager, BoneDragNodeOld prevNode)
        {
            if (nextBone == null) return;
            var curPos = target.position;
            // ŒÀŠEŠp“xi‚½‚¾‚µ Softness ‚É‰ž‚¶‚Ä’´‚¦‚ç‚ê‚éj‰Šú‰»
            limitter.Init(this, manager.AngularLimit);

            var qAir = RotateByAirDrag(manager);
            var qIna = RotateByInertia(manager);
            var qMix = Quaternion.Slerp(qAir, qIna, manager.AirVsMass);
            var qRes = RotateByRestoreSpring(manager);
            //Debug.Log($"qAir={qAir}, qIna={qIna}, qTrd ={qRes}");

            var qResult = SelectCloserTo(limitter.worldOrigin, qMix, qRes);
            if (qMix != qResult)
            {
                var diffPos = (curPos - prevPos).magnitude;
                var r = manager.RestoreSpring == 0f ? 1f : diffPos == 0f ? 0f : Mathf.Pow(manager.RestoreSpring, 1f) / diffPos;
                qResult = Quaternion.Lerp(qRes, qMix, r);
            }
            Apply(qResult);
            prevPos = target.position;
            prevWorldRot = target.rotation;
            prevLocalRot = target.localRotation;
        }

        #region apply rotation
        private void Apply(Quaternion q)
        {

            var nowRot = target.rotation;
            //q = ConsiderSoftness(q);
            if (targetBody != null)
            {
                Quaternion deltaRot = q * Quaternion.Inverse(prevWorldRot); // rotation‚Ì·•ª‚ð‹‚ß‚é

                targetBody.MoveRotation(ZOnly(deltaRot));
            }
            else
            {
                target.rotation = ZOnly(q);
            }
            prevWorldRot = nowRot;
        }

        private Quaternion WorldToLocal(Quaternion q)
        {
            var inv = Quaternion.Inverse(target.rotation);
            return inv * q;
        }

        private Quaternion ZOnly(Quaternion q)
        {
            return Quaternion.Euler(0, 0, q.eulerAngles.z);
        }

        private float ZRotationRatio(Quaternion a, Quaternion b)
        {
            float bz = Mathf.Abs(b.eulerAngles.z);
            float az = Mathf.Abs(a.eulerAngles.z);
            return az == 0 ? 1f : Mathf.Min(1, bz / az);
        }

        private Quaternion SelectCloserTo(Quaternion q0, Quaternion q1, Quaternion q2)
        {
            float ang1 = Quaternion.Angle(q0, q1);
            float ang2 = Quaternion.Angle(q0, q2);
            Debug.Assert(ang1 >= 0 && ang2 >= 0);
            //Debug.Assert(ang1 < 180f && ang2 < 180);
            return ang1 < ang2 ? q1 : q2;
        }

        #endregion


        private Quaternion RotateByInertia(BoneDraggerManagerOld manager)
        {
            var inartia = manager.InartiaForce;
            var ratio = manager.inartiaTimeScale * Time.fixedDeltaTime;
            var nowPos = target.position;
            var endPos = nextBone.transform.position;
            var boneDir = endPos - nowPos;
            var look = boneDir.normalized + inartia;
            var mag = INARTIA_SCALE * inartia.magnitude;
            mag *= WeightInLength == 0 ? 10 : (WeightInLength + FollowingLength) / WeightInLength;
            //Debug.Log($"inamag={inartia.magnitude}, Wfol={FollowingLength}, Win={WeightInLength}, Wpre={PrecedingLength}");
            //Debug.Log($"inartia={inartia}, look={look}, ratio={ratio}, mag={mag}");

            var rot = Quaternion.FromToRotation(Vector3.right, look);
            var q = Quaternion.RotateTowards(prevWorldRot, rot, ratio * mag);
            return limitter.LimitAsWorld(q, manager.Softness);
        }

        private Quaternion RotateByAirDrag(BoneDraggerManagerOld manager)
        {
            var timeScale = manager.AirDragTimeScale * Time.fixedDeltaTime;
            var nowPos = target.position;
            var endPos = nextBone.transform.position;
            var moveDir = prevPos - nowPos;
            var boneDir = endPos - nowPos;
            var weightRatio = WeightInLength == 0 ? 0 : (WeightInLength + PrecedingLength) / WeightInLength;
            var ratio = AIR_DRAG_MULTIPLIER * timeScale * moveDir.magnitude * weightRatio;
            ratio = Mathf.Min(MAX_AIR_DRAG_ANGLE, ratio);
            //Debug.Log($"r2={r2}, ratio={ratio}, mag={moveDir.magnitude}, tscl={timeScale}");

            var rot = Quaternion.FromToRotation(Vector3.right, moveDir);
            var q = Quaternion.RotateTowards(prevWorldRot, rot, ratio);
            return limitter.LimitAsWorld(q, manager.Softness);
        }

        private Quaternion RotateByRestoreSpring(BoneDraggerManagerOld manager)
        {
            var amove = Mathf.Max((1 - airDrag) * (1 - softness), Mathf.Epsilon);
            var mag = RESTORE_SPRING_MULTIPLIER * Mathf.Pow(manager.RestoreSpring, 2f) * Time.deltaTime;
            var overLimitMultiplier = Mathf.Max(1f, limitter.AngleRatioLocal(prevLocalRot));
            mag = Mathf.Min(MAX_RESTORE_ANGLE, mag * overLimitMultiplier);
            var newRot = Quaternion.RotateTowards(prevLocalRot, initailRotation, mag);
            var saved = target.rotation;
            target.localRotation = newRot;
            newRot = target.rotation;
            target.rotation = saved;
            return newRot;
        }

        // ‰ñ“]”ÍˆÍi‚½‚¾‚µ Softness ‚É‰ž‚¶‚Ä’´‚¦‚ç‚ê‚éj
        internal class AngleLimitter
        {
            public Quaternion localOrigin { get; private set; }

            public Quaternion worldOrigin { get; private set; }
            public Quaternion localMin { get; private set; }
            public Quaternion localMax { get; private set; }
            public Quaternion worldMin { get; private set; }
            public Quaternion worldMax { get; private set; }

            public float freeAngle { get; private set; }
            public AngleLimitter()
            {
            }
            public void Init(BoneDragNodeOld node, float angle)
            {
                this.freeAngle = angle;
                var target = node.target;
                localOrigin = node.initailRotation;
                worldOrigin = target.rotation * Quaternion.Inverse(target.localRotation) * localOrigin;
                localMax = Quaternion.AngleAxis(angle, Vector3.forward);
                localMin = Quaternion.AngleAxis(-angle, Vector3.forward);
                worldMin = worldOrigin * localMin;
                worldMax = worldOrigin * localMax;
            }

            private float AdjustOverLimitAngle(float angle, float softness)
            {
                var newAng = freeAngle;
                if (softness == 0) return freeAngle;

                var angOver = angle - freeAngle;
                var maxAng = 180f * softness;
                var r = Mathf.Min(1f, angOver / maxAng);
                var easing = r - (1f / ANGLE_OVER_LIMIT_DAMPER) * Mathf.Pow(r, ANGLE_OVER_LIMIT_DAMPER);
                newAng += maxAng * easing;

                return newAng;
            }

            public Quaternion LimitAsWorld(Quaternion q, float softness)
            {
                float angle = Quaternion.Angle(q, worldOrigin);
                q = q.normalized;
                // freeAngleˆÈ‰º‚È‚ç•â³‚È‚µ
                if (angle < freeAngle)
                {
                    return q;
                }

                var newAng = AdjustOverLimitAngle(angle, softness);

                // q‚ÉŠp“x‚ª‹ß‚¢•û‚ð‘I‘ð
                float ang1 = Quaternion.Angle(q, worldMin);
                float ang2 = Quaternion.Angle(q, worldMax);
                Quaternion baseAng = ang1 < ang2 ? worldMin : worldMax;
                // •â³‚³‚ê‚½•ª‚¾‚¯‹«ŠEŠp“x‚É‹ß‚Ã‚¯‚é
                Quaternion newRot = Quaternion.RotateTowards(q, baseAng, angle - newAng);
                return newRot;
            }

            public Quaternion LimitAsLocal(Quaternion q, float softness)
            {
                float angle = Quaternion.Angle(q, localOrigin);
                q = q.normalized;
                // freeAngleˆÈ‰º‚È‚ç•â³‚È‚µ
                if (angle < freeAngle)
                {
                    return q;
                }

                var newAng = AdjustOverLimitAngle(angle, softness);

                // q‚ÉŠp“x‚ª‹ß‚¢•û‚ð‘I‘ð
                float ang1 = Quaternion.Angle(q, localMin);
                float ang2 = Quaternion.Angle(q, localMax);
                Quaternion baseAng = ang1 < ang2 ? localMin : localMax;
                // •â³‚³‚ê‚½•ª‚¾‚¯‹«ŠEŠp“x‚É‹ß‚Ã‚¯‚é
                Quaternion newRot = Quaternion.RotateTowards(q, baseAng, angle - newAng);
                return newRot;
            }

            public float AngleRatioLocal(Quaternion q)
            {
                float angle = Quaternion.Angle(q, localOrigin);
                return angle / freeAngle;
            }
        }
    }
}
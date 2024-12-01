using Program.Utils;
using System;
using UnityEngine;
using Transform = UnityEngine.Transform;

namespace Duel.BoneDragger
{
    [Serializable]
    public class BoneDragNode
    {

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
        private Quaternion initialLocalRot;
        private Rigidbody targetBody;
        private bool flipX;
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

        public void Init(BoneDragNode next)
        {
            prevPos = target.position;
            prevWorldRot = target.rotation;
            initialLocalRot = initailRotation.normalized;
            targetBody = target.GetComponent<Rigidbody>();
            limitter = new AngleLimitter();
            FollowingLength = 0;
            flipX = target.lossyScale.x < 0;
            if (next == null) return;
            nextBone = next.target;
            Length = (nextBone.transform.position - prevPos).magnitude;
            FollowingLength = next.Length + next.FollowingLength;
        }

        public void LateInit(BoneDragger manager)
        {
            PrecedingLength = Mathf.Max(0, manager.TotalLength - Length - FollowingLength);
            WeightInLength = Length / manager.TotalLength;
        }

        public void Update(BoneDragger dragger, BoneDragNode prevNode)
        {
            if (nextBone == null) return;
            flipX = target.lossyScale.x < 0;

            var curPos = target.position;
            // ŒÀŠEŠp“xi‚½‚¾‚µ Softness ‚É‰ž‚¶‚Ä’´‚¦‚ç‚ê‚éj‰Šú‰»
            limitter.Init(this, dragger.AngularLimit);

            var qAir = RotateByAirDrag(dragger);
            var qIna = RotateByInertia(dragger);
            var qMix = Quaternion.Slerp(qAir, qIna, dragger.AirVsMass);

            var qRes = RotateByRestoreSpring(dragger);
            // XŽ²”½“]‚¾‚ÆSelectCloserTo‚ªƒoƒO‚é
            //var qResult = SelectCloserTo(limitter.worldOrigin.normalized, qMix, qRes);

            var diffPos = (curPos - prevPos).magnitude;
            var r = dragger.RestoreSpring == 0f ? 1f : diffPos == 0f ? 0f : Mathf.Pow(dragger.RestoreSpring, 1f) / diffPos;
            var qResult = Quaternion.Lerp(qRes, qMix, r);
            Apply(qResult);

            prevPos = target.position;
            prevWorldRot = target.rotation;
            prevLocalRot = target.localRotation.normalized;
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
                var newQ = ZOnly(q);
                target.rotation = newQ;
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
            return new Quaternion(0, 0, q.z, q.w);
        }

        private float ZRotationRatio(Quaternion a, Quaternion b)
        {
            float bz = Mathf.Abs(b.eulerAngles.z);
            float az = Mathf.Abs(a.eulerAngles.z);
            return az == 0 ? 1f : Mathf.Min(1, bz / az);
        }

        // XŽ²”½“]‚¾‚ÆSelectCloserTo‚ªƒoƒO‚é
        [Obsolete]
        private Quaternion SelectCloserTo(Quaternion q0, Quaternion q1, Quaternion q2)
        {
            float ang1 = Quaternion.Angle(q0, q1);
            float ang2 = Quaternion.Angle(q0, q2);
            Debug.Assert(ang1 >= 0 && ang2 >= 0);
            //Debug.Assert(ang1 < 180f && ang2 < 180);
            DebugLog($"ang1={ang1}, ang2={ang2}");

            return ang1 < ang2 ? q1 : q2;
        }

        #endregion

        private void DebugLog(string msg)
        {
            if (target.lossyScale.x < 0)
            {
                Debug.Log(msg.yellow());
            }
            else
            {
                Debug.Log(msg.white());
            }
        }

        private Quaternion RotateByInertia(BoneDragger dragger)
        {
            var inartia = dragger.InartiaForce;
            var ratio = dragger.InartiaTimeScale * dragger.DeltaTime;
            var nowPos = target.position;
            var endPos = nextBone.transform.position;
            var boneDir = endPos - nowPos;
            var look = boneDir.normalized + inartia + dragger.Gravity;
            look.Normalize();
            if (look.magnitude <= Mathf.Epsilon) return prevWorldRot;
            var mag = BoneDraggerSettings.INARTIA_SCALE * inartia.magnitude;
            mag *= WeightInLength == 0 ? 10 : (WeightInLength + FollowingLength) / WeightInLength;
            //Debug.Log($"inamag={inartia.magnitude}, Wfol={FollowingLength}, Win={WeightInLength}, Wpre={PrecedingLength}");
            //Debug.Log($"inartia={inartia}, look={look}, ratio={ratio}, mag={mag}");

            var baseDir = flipX ? Vector3.left : Vector3.right;
            var rot = Quaternion.FromToRotation(baseDir, look.normalized);
            var q = Quaternion.RotateTowards(prevWorldRot, rot, ratio * mag);
            return limitter.LimitAsWorld(q, dragger.Softness, flipX);
        }

        private Quaternion RotateByAirDrag(BoneDragger dragger)
        {
            var timeScale = dragger.AirDragTimeScale * dragger.DeltaTime;
            var nowPos = target.position;
            var moveDir = prevPos - nowPos - dragger.Wind;
            var weightRatio = WeightInLength == 0 ? 0 : (WeightInLength + PrecedingLength) / WeightInLength;
            var ratio = BoneDraggerSettings.AIR_DRAG_MULTIPLIER * timeScale * moveDir.magnitude * weightRatio;
            ratio = Mathf.Min(BoneDraggerSettings.MAX_AIR_DRAG_ANGLE, ratio);
            //Debug.Log($"r2={r2}, ratio={ratio}, mag={moveDir.magnitude}, tscl={timeScale}");
            var baseDir = flipX ? Vector3.left : Vector3.right;
            var rot = Quaternion.FromToRotation(baseDir, moveDir);
            //DebugLog($"movDir={moveDir}, rot={rot}");
            var q = Quaternion.RotateTowards(prevWorldRot, rot, ratio);
            return limitter.LimitAsWorld(q, dragger.Softness, flipX);
        }

        private Quaternion RotateByRestoreSpring(BoneDragger dragger)
        {
            var amove = Mathf.Max((1 - airDrag) * (1 - softness), Mathf.Epsilon);
            var mag = BoneDraggerSettings.RESTORE_SPRING_MULTIPLIER * Mathf.Pow(dragger.RestoreSpring, 2f) * dragger.DeltaTime;
            var overLimitMultiplier = Mathf.Max(1f, limitter.AngleRatioLocal(prevLocalRot));
            mag = Mathf.Min(BoneDraggerSettings.MAX_RESTORE_ANGLE, mag * overLimitMultiplier);
            var newRot = Quaternion.RotateTowards(prevLocalRot, initailRotation, mag);
            //DebugLog($"mag={mag}, prevLocalRot={prevLocalRot}, initailRotation ={initailRotation}");

            var saved = target.rotation;
            target.localRotation = newRot;
            newRot = target.rotation;
            target.rotation = saved;
            return newRot;
        }


        internal class RestoreScale : IDisposable
        {
            private readonly Transform target;
            private readonly Vector3 localScale;

            public RestoreScale(Transform t1)
            {
                // XŽ²”½“]‚µ‚Ä‚éæ‘ctransform‚ðŒ©‚Â‚¯‚é
                target = t1;
                while (t1 != null)
                {
                    if (Mathf.Sign(t1.localScale.x) != Mathf.Sign(t1.lossyScale.x))
                    {
                        target = t1;
                        break;
                    }
                    t1 = t1.parent;
                }
                localScale = target.localScale;
            }

            /**
             * dispose ‚³‚ê‚é‚Ü‚ÅˆêŽž“I‚ÉXŽ²”½“]‚ð‚È‚­‚·
             */
            public void CancelFlipX()
            {
                var flipX = target.lossyScale.x < 0;
                if (flipX)
                {
                    target.localScale = new Vector3(-localScale.x, localScale.y, localScale.z);
                }
            }

            public void Dispose()
            {
                this.target.localScale = this.localScale;
            }
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
            public void Init(BoneDragNode node, float angle)
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
                var damper = BoneDraggerSettings.ANGLE_OVER_LIMIT_DAMPER;
                var easing = r - (1f / damper) * Mathf.Pow(r, damper);
                newAng += maxAng * easing;

                return newAng;
            }

            public Quaternion LimitAsWorld(Quaternion q, float softness, bool flip)
            {
                float angle = Quaternion.Angle(q, worldOrigin);
                q = q.normalized;
                // freeAngleˆÈ‰º‚È‚ç•â³‚È‚µ
                if (angle < freeAngle)
                {
                    return q;
                }

                var newAng = AdjustOverLimitAngle(angle, softness);
                if (flip)
                {
                    angle = -angle;
                    newAng = -newAng;
                }

                // q‚ÉŠp“x‚ª‹ß‚¢•û‚ð‘I‘ð
                float ang1 = Quaternion.Angle(q, worldMin);
                float ang2 = Quaternion.Angle(q, worldMax);
                Quaternion baseAng = ang1 < ang2 ? worldMin : worldMax;
                // •â³‚³‚ê‚½•ª‚¾‚¯‹«ŠEŠp“x‚É‹ß‚Ã‚¯‚é
                Quaternion newRot = Quaternion.RotateTowards(q, baseAng, angle - newAng);
                return newRot;
            }

            public Quaternion LimitAsLocal(Quaternion q, float softness, bool flip)
            {
                float angle = Quaternion.Angle(q, localOrigin);
                q = q.normalized;
                // freeAngleˆÈ‰º‚È‚ç•â³‚È‚µ
                if (angle < freeAngle)
                {
                    return q;
                }

                var newAng = AdjustOverLimitAngle(angle, softness);
                if (flip)
                {
                    angle = -angle;
                    newAng = -newAng;
                }

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
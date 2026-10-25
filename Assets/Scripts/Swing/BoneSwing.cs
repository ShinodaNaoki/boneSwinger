using System.Collections.Generic;
using UnityEngine;

namespace Duel.BoneDragger
{
    /// <summary>
    /// 粒子チェーン（各ボーンの付け根に質点）で髪・布を揺らす。
    ///  - 慣性と空気抵抗:  m·dv/dt = c·(v_air − v) + G  を1ステップ厳密解（指数積分）で解く
    ///  - 長さ拘束:        根本から順に長さを戻す（慣性0なら「移動方向に向く」トラクトリクス挙動になる）
    ///  - 復元力(竹):      曲げ角 θ に対して T(θ) = K·θ / (1 − |θ/θmax|^p) の硬化ばねを陰的に解く
    ///                     関節ごとに下流の慣性モーメント Σm·r² を使い、K と減衰もそれに比例させるので、
    ///                     どの関節も同じ周期 2π√(慣性/K) で揺れる（先端だけ細かく震えない）
    ///  - 重さ・受風面積:  ボーン長に比例（一様な紐）。受風面積だけカーブで分布を変えられ、総量は 1 に正規化する
    ///  - 長さ拘束は「動きにくさ」（慣性＋空気抵抗）で補正を配分する PBD。最後に根本から伸びを取り除く
    /// 長さはチェーン長で正規化し、時間は固定サブステップで刻むのでスケール・fps に依存しない。
    /// リストの最後のボーンは末端の目印として位置だけ使い、回転させない（BoneDragger と同じ）。
    /// </summary>
    // Animator の後、IKManager2D(-2) と SpriteSkin の変形(-1) より前に動かす
    [DefaultExecutionOrder(-3)]
    public class BoneSwing : MonoBehaviour
    {
        // パラメーター(0〜1)を物理量に変換する係数
        private const float DRAG_SCALE = 20f;
        private const float STIFFNESS_SCALE = 200f;
        private const float GRAVITY_SCALE = 50f;
        // はためき: 対気速度 1チェーン長/秒 あたりの周波数(Hz)、上限、粒子ごとの位相ずれ(進行波)
        private const float FLUTTER_HZ = 2f;
        private const float MAX_FLUTTER_HZ = 8f;
        private const float FLUTTER_WAVE = 0.9f;
        // はためき 1 のときの横揺れ速度（対気速度比）と、根本→先端の効き方の指数
        private const float FLUTTER_SCALE = 0.35f;
        private const float FLUTTER_TIP_POWER = 3f;
        // 反転時に広げた限界角のうち、現在の曲げ角が占める割合と、広げた分が閉じる速さ(1/秒)
        private const float SLACK_FILL = 0.5f;
        private const float SLACK_DECAY = 0.7f;
        // 重み付き長さ拘束の反復回数
        private const int CONSTRAINT_ITERATIONS = 6;
        private const float STEP = 1f / 60f;
        private const int MAX_STEPS = 8;
        private const float MAX_DELTA_TIME = 1f / 15f;
        private const float EPS = 1e-4f;

        [SerializeField, Tooltip("根本→末端の順。最後は揺らさない末端の目印ボーン")]
        private List<Transform> bones = new List<Transform>();

        [SerializeField, Tooltip("設定すると下の Settings より優先される")]
        private BoneSwingProfile profile;

        [SerializeField]
        private BoneSwingSettings settings = new BoneSwingSettings();

        [SerializeField, Tooltip("ローカル空間・反転の基準。未設定なら親の Animator（無ければ最上位）")]
        private Transform space;

        [SerializeField]
        private bool drawGizmos = true;

        public BoneSwingSettings Settings
        {
            get => profile != null ? profile.settings : settings;
            set { profile = null; settings = value; }
        }

        public IReadOnlyList<Transform> Bones => bones;

        private int n;
        private Vector3[] pos;
        private Vector3[] vel;
        private Vector3[] stepStart;
        private Vector3[] animPos;
        private float[] animAngle;
        private float[] length;
        private float[] massWeight;
        private float[] limitSlack;
        private float[] invMass;
        private float[] areaWeight;
        private float[] prevBend;
        private float flutterPhase;
        private float flutterSeed;
        private Quaternion[] baseRot;
        private Quaternion[] writtenRot;
        private bool hasWritten;
        private bool initialized;
        private Vector3 prevRootPos;
        private Matrix4x4 prevSpace;
        private float chainLength;

        public void SetBones(IEnumerable<Transform> chain)
        {
            RestoreBasePose();
            bones = new List<Transform>(chain);
            initialized = false;
            Init();
        }

        /// <summary>テレポート時などに呼ぶ。揺れを捨てて現在のポーズから始め直す。</summary>
        public void ResetSimulation()
        {
            initialized = false;
            Init();
        }

        private void OnEnable()
        {
            initialized = false;
        }

        private void OnDisable()
        {
            RestoreBasePose();
        }

        private bool Init()
        {
            if (initialized) return true;
            n = bones.Count;
            if (n < 2 || bones.Exists(b => b == null)) return false;

            pos = new Vector3[n];
            vel = new Vector3[n];
            stepStart = new Vector3[n];
            animPos = new Vector3[n];
            animAngle = new float[n];
            length = new float[n];
            massWeight = new float[n];
            limitSlack = new float[n];
            invMass = new float[n];
            areaWeight = new float[n];
            prevBend = new float[n];
            flutterPhase = 0f;
            flutterSeed = Random.value * 100f;
            baseRot = new Quaternion[n];
            writtenRot = new Quaternion[n];
            for (int i = 0; i < n; i++)
            {
                pos[i] = bones[i].position;
                baseRot[i] = bones[i].localRotation;
            }
            if (space == null)
            {
                var animator = GetComponentInParent<Animator>();
                space = animator != null ? animator.transform : bones[0].root;
            }
            prevSpace = space.localToWorldMatrix;
            prevRootPos = pos[0];
            hasWritten = false;
            initialized = true;
            return true;
        }

        private void LateUpdate()
        {
            if (!Init()) return;
            var s = Settings;

            ReadBasePose();
            if (chainLength <= EPS) return;
            ComputeWeights(s);
            CarryAlongWithSpace(s);

            float dt = Mathf.Min(Time.deltaTime, MAX_DELTA_TIME);
            if (dt > 0f)
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt(dt / STEP - 0.01f), 1, MAX_STEPS);
                float h = dt / steps;
                for (int k = 1; k <= steps; k++)
                {
                    Step(s, h, Vector3.Lerp(prevRootPos, animPos[0], (float)k / steps));
                }
            }
            prevRootPos = animPos[0];
            ApplyToBones();
        }

        /// <summary>
        /// Animator が書いた値ならそれを基準ポーズとし、書いていなければ（揺らしの結果のままなら）前回の基準ポーズに戻す。
        /// Animator の UpdateMode に関係なく、アニメの姿勢を復元の目標にできる。
        /// </summary>
        private void ReadBasePose()
        {
            for (int i = 0; i < n; i++)
            {
                var b = bones[i];
                if (hasWritten && SameExactly(b.localRotation, writtenRot[i]))
                {
                    b.localRotation = baseRot[i];
                }
                else
                {
                    baseRot[i] = b.localRotation;
                }
            }

            chainLength = 0f;
            for (int i = 0; i < n; i++)
            {
                animPos[i] = bones[i].position;
                if (i == 0) continue;
                var d = animPos[i] - animPos[i - 1];
                length[i - 1] = new Vector2(d.x, d.y).magnitude;
                animAngle[i - 1] = Mathf.Atan2(d.y, d.x);
                chainLength += length[i - 1];
            }
        }

        /// <summary>
        /// 粒子の重さと受風面積。隣接ボーン長の半分ずつを受け持つ（一様な紐なら長さに比例）。
        /// 平均 1 になるよう正規化する（＝慣性・空気抵抗スライダーは総量を表す）。受風面積だけはカーブで分布を変えられる。
        /// </summary>
        private void ComputeWeights(BoneSwingSettings s)
        {
            float sumMass = 0f, sumArea = 0f;
            for (int i = 1; i < n; i++)
            {
                float share = 0.5f * (length[i - 1] + (i < n - 1 ? length[i] : 0f));
                float t = (float)i / (n - 1);
                massWeight[i] = share;
                areaWeight[i] = share * Mathf.Max(0f, s.airDragCurve.Evaluate(t));
                sumMass += massWeight[i];
                sumArea += areaWeight[i];
            }
            float massNorm = sumMass > EPS ? (n - 1) / sumMass : 0f;
            float areaNorm = sumArea > EPS ? (n - 1) / sumArea : 0f;
            for (int i = 1; i < n; i++)
            {
                massWeight[i] *= massNorm;
                areaWeight[i] *= areaNorm;
            }
        }

        private void RestoreBasePose()
        {
            if (!initialized || !hasWritten) return;
            for (int i = 0; i < n; i++)
            {
                if (bones[i] != null) bones[i].localRotation = baseRot[i];
            }
            hasWritten = false;
        }

        /// <summary>
        /// ローカル空間モードではキャラの移動・反転ごと粒子を運ぶ。
        /// ワールドモードの反転（向き替え）では粒子をその場に残し、元のポーズ側だけが鏡映される。
        /// 末端は空気抵抗・慣性で置いていかれ、復元力で新しい向きへ回り込む。
        /// </summary>
        private void CarryAlongWithSpace(BoneSwingSettings s)
        {
            var m = space.localToWorldMatrix;
            bool flipped = Det2D(m) * Det2D(prevSpace) < 0f;
            if (s.simulateInLocalSpace)
            {
                var delta = m * prevSpace.inverse;
                for (int i = 0; i < n; i++)
                {
                    pos[i] = delta.MultiplyPoint3x4(pos[i]);
                    vel[i] = delta.MultiplyVector(vel[i]);
                }
                prevRootPos = delta.MultiplyPoint3x4(prevRootPos);
                if (flipped)
                {
                    // 鏡映で曲げ角の符号が逆になる
                    for (int j = 0; j < n; j++) prevBend[j] = -prevBend[j];
                }
            }
            else if (flipped)
            {
                LeaveBehindOnFlip(s);
            }
            prevSpace = m;

            // テレポート等で大きく飛んだらリセット
            if ((animPos[0] - prevRootPos).sqrMagnitude > Mathf.Pow(chainLength * 10f, 2))
            {
                for (int i = 0; i < n; i++)
                {
                    pos[i] = animPos[i];
                    vel[i] = Vector3.zero;
                    prevBend[i] = 0f;
                    limitSlack[i] = 0f;
                }
                prevRootPos = animPos[0];
            }
        }

        /// <summary>
        /// 向き替えの瞬間の処理。
        ///  - 根本は鏡映で反対側へ飛ぶので、チェーンを鏡映せずに根本の移動分だけ平行移動する
        ///    （形は反転前のまま残り、そこから復元力で新しい向きへなびいていく。引きずられて飛ばない）
        ///  - 元のポーズが鏡映されて曲げ角が限界を超えるので、そのままクランプすると一瞬で回転してしまう。
        ///    現在の曲げ角まで限界を一時的に広げ（slack）、関節が戻るにつれて閉じる
        ///  - 減衰が「反転による見かけの曲げ変化」に効かないよう基準を取り直す
        /// </summary>
        private void LeaveBehindOnFlip(BoneSwingSettings s)
        {
            var shift = animPos[0] - prevRootPos;
            shift.z = 0f;
            for (int i = 0; i < n; i++) pos[i] += shift;
            prevRootPos = animPos[0];

            float limit = s.angleLimit * Mathf.Deg2Rad;
            float prevSimAngle = 0f;
            for (int j = 0; j < n - 1; j++)
            {
                float simAngle = SegmentAngle(j);
                float parentOffset = j == 0 ? 0f : prevSimAngle - animAngle[j - 1];
                float theta = WrapAngle(simAngle - animAngle[j] - parentOffset);
                prevBend[j] = theta;
                limitSlack[j] = Mathf.Max(limitSlack[j], Mathf.Abs(theta) / SLACK_FILL - limit);
                prevSimAngle = simAngle;
            }
        }

        private void Step(BoneSwingSettings s, float h, Vector3 rootPos)
        {
            float segLength = chainLength / (n - 1);
            Vector3 gravity = (Vector3)(s.gravityDirection.normalized * (s.gravity * GRAVITY_SCALE * segLength));
            Vector3 airVel = (Vector3)(s.wind * chainLength);
            float drag = DRAG_SCALE * s.airDrag;

            // はためきの位相は先端の対気速度で進める（速いほど細かく波打つ）
            if (s.flutter > 0f)
            {
                var tipRel = airVel - vel[n - 1];
                float hz = FLUTTER_HZ * new Vector2(tipRel.x, tipRel.y).magnitude / chainLength;
                hz *= 0.75f + 0.5f * Mathf.PerlinNoise(flutterSeed, Time.time * 0.7f);
                flutterPhase += Mathf.Min(hz, MAX_FLUTTER_HZ) * Mathf.PI * 2f * h;
            }

            // 1. 慣性・空気抵抗・重力・はためきで予測位置を求める
            for (int i = 0; i < n; i++) stepStart[i] = pos[i];
            pos[0] = rootPos;
            for (int i = 1; i < n; i++)
            {
                float m = s.inertia * massWeight[i];
                float c = drag * areaWeight[i];
                var v = vel[i];
                var force = gravity * massWeight[i];
                if (s.flutter > 0f && c > EPS)
                {
                    // 対気速度に比例した横向きの力。粒子ごとに位相をずらして先端へ進む波にする。根本付近はほぼ効かない
                    var seg = pos[i] - pos[i - 1];
                    var normal = new Vector3(-seg.y, seg.x, 0f).normalized;
                    var rel = airVel - v;
                    float wave = Mathf.Sin(flutterPhase - i * FLUTTER_WAVE);
                    float tipFactor = Mathf.Pow((float)i / (n - 1), FLUTTER_TIP_POWER);
                    force += normal * (FLUTTER_SCALE * s.flutter * c * new Vector2(rel.x, rel.y).magnitude * wave * tipFactor);
                }

                if (m <= EPS)
                {
                    // 質量なし: 空気と一緒に流れるだけ（重力などは終端速度として効く）
                    v = c > EPS ? airVel + force / c : airVel;
                }
                else if (c <= EPS)
                {
                    v += force / m * h;
                }
                else
                {
                    var terminal = airVel + force / c;
                    v = terminal + (v - terminal) * Mathf.Exp(-c / m * h);
                }
                pos[i] += v * h;
                pos[i].z = animPos[i].z;
            }

            // 2. 長さ拘束
            //  まず重さで補正を配分する（PBD: 動きにくい粒子ほど動かさない）。重い先端が軽い中間を引っぱり、まっすぐ張る。
            //  「動きにくさ」は慣性と空気抵抗の両方（陰的オイラーの m/h² + c/h）。根本は固定。
            invMass[0] = 0f;
            for (int i = 1; i < n; i++)
            {
                float resist = s.inertia * massWeight[i] / (h * h) + drag * areaWeight[i] / h;
                invMass[i] = resist > EPS ? 1f / resist : 1f / EPS;
            }
            for (int it = 0; it < CONSTRAINT_ITERATIONS; it++)
            {
                for (int i = 1; i < n; i++)
                {
                    float w0 = invMass[i - 1], w1 = invMass[i];
                    if (w0 + w1 <= 0f) continue;
                    var d = pos[i] - pos[i - 1];
                    d.z = 0f;
                    float dist = d.magnitude;
                    if (dist <= EPS * chainLength) continue;
                    var corr = d * ((dist - length[i - 1]) / dist / (w0 + w1));
                    pos[i - 1] += corr * w0;
                    pos[i] -= corr * w1;
                }
            }
            //  残った伸び縮みを根本から順に取り除く
            for (int i = 1; i < n; i++)
            {
                var d = pos[i] - pos[i - 1];
                d.z = 0f;
                var dir = d.sqrMagnitude > EPS * EPS * chainLength * chainLength
                    ? d.normalized
                    : new Vector3(Mathf.Cos(animAngle[i - 1]), Mathf.Sin(animAngle[i - 1]), 0f);
                var p = pos[i - 1] + dir * length[i - 1];
                p.z = animPos[i].z;
                pos[i] = p;
            }

            // 3. 復元力と減衰（曲げ角を硬化ばねで戻す。下流は剛体として一緒に回す）
            float limit = s.angleLimit * Mathf.Deg2Rad;
            float invSegSq = 1f / (segLength * segLength);
            float prevSimAngle = 0f;
            for (int j = 0; j < n - 1; j++)
            {
                float simAngle = SegmentAngle(j);
                float parentOffset = j == 0 ? 0f : prevSimAngle - animAngle[j - 1];
                float theta = WrapAngle(simAngle - animAngle[j] - parentOffset);

                // 下流の慣性モーメント・空気抵抗モーメント（距離はボーン平均長で正規化）
                // 復元力も慣性モーメントに比例させるので、どの関節も同じ周期で揺れる。
                // 関節ごとの硬さの違い（根本だけで曲がる棒状など）は stiffnessCurve で付ける
                float massMoment = 0f, areaMoment = 0f;
                for (int k = j + 1; k < n; k++)
                {
                    var r = pos[k] - pos[j];
                    float rr = (r.x * r.x + r.y * r.y) * invSegSq;
                    massMoment += massWeight[k] * rr;
                    areaMoment += areaWeight[k] * rr;
                }
                float t = (n > 2) ? (float)j / (n - 2) : 0f;
                float inertia = s.inertia * massMoment;
                float stiffness = STIFFNESS_SCALE * s.stiffness * Mathf.Max(0f, s.stiffnessCurve.Evaluate(t)) * massMoment;
                // 減衰比 ζ = damping になるよう 2√(K·I) を掛ける
                float damper = s.damping * 2f * Mathf.Sqrt(stiffness * inertia) / h;
                float resistance = inertia / (h * h) + drag * areaMoment / h;

                // 反転直後は限界角を一時的に広げておき、戻るにつれて閉じる
                float jointLimit = limit + limitSlack[j];
                float predicted = Mathf.Clamp(theta, -jointLimit * 0.999f, jointLimit * 0.999f);
                float target = resistance + damper > EPS
                    ? (resistance * predicted + damper * prevBend[j]) / (resistance + damper)
                    : predicted;
                float solved = SolveBend(target, jointLimit, s.hardening, stiffness, resistance + damper);
                if (limitSlack[j] > 0f)
                {
                    float needed = Mathf.Abs(solved) / SLACK_FILL - limit;
                    limitSlack[j] = Mathf.Max(0f, Mathf.Min(limitSlack[j] * Mathf.Exp(-SLACK_DECAY * h), needed));
                }
                float correction = solved - theta;
                if (Mathf.Abs(correction) > 0f)
                {
                    RotateDownstream(j, correction);
                }
                prevBend[j] = solved;
                prevSimAngle = simAngle + correction;
            }

            // 4. 位置の変化から速度を求める
            for (int i = 1; i < n; i++)
            {
                var v = (pos[i] - stepStart[i]) / h;
                v.z = 0f;
                vel[i] = v;
            }
            vel[0] = Vector3.zero;
        }

        /// <summary>
        /// 陰的オイラー:  A·(θ' − θt) + T(θ') = 0  を二分法で解く。
        /// A = I/h² + C/h + D/h（慣性・空気抵抗・減衰が補正に抵抗する分）、θt はそれらで重み付けした目標角。
        /// T は限界角で発散する硬化ばね。解は必ず 0 と θt の間にあるので、どんなに硬くても行き過ぎない。
        /// </summary>
        private static float SolveBend(float target, float limit, float hardening, float stiffness, float resistance)
        {
            if (stiffness <= EPS) return target;
            if (resistance <= EPS) return 0f;

            float lo = 0f, hi = target;
            for (int it = 0; it < 20; it++)
            {
                float mid = (lo + hi) * 0.5f;
                float r = Mathf.Pow(Mathf.Abs(mid) / limit, hardening);
                float torque = stiffness * mid / Mathf.Max(1f - r, EPS);
                float f = resistance * (mid - target) + torque;
                // f は mid に対して単調増加。符号が θt と同じなら戻しすぎ
                if (f * Mathf.Sign(target) > 0f) hi = mid; else lo = mid;
            }
            return (lo + hi) * 0.5f;
        }

        private void RotateDownstream(int joint, float angle)
        {
            var pivot = pos[joint];
            float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            for (int k = joint + 1; k < n; k++)
            {
                var d = pos[k] - pivot;
                pos[k] = new Vector3(pivot.x + d.x * cos - d.y * sin, pivot.y + d.x * sin + d.y * cos, pos[k].z);
            }
        }

        private float SegmentAngle(int j)
        {
            var d = pos[j + 1] - pos[j];
            return Mathf.Atan2(d.y, d.x);
        }

        /// <summary>
        /// 粒子の向きを、基準ポーズからの差分回転としてボーンに乗せる。
        /// 親空間に変換してから角度を測るので、X反転（負のスケール）でも符号を気にしなくてよい。
        /// </summary>
        private void ApplyToBones()
        {
            for (int i = 0; i < n - 1; i++)
            {
                var b = bones[i];
                var cur = bones[i + 1].position - b.position;
                var want = pos[i + 1] - b.position;
                var parent = b.parent;
                if (parent != null)
                {
                    cur = parent.InverseTransformVector(cur);
                    want = parent.InverseTransformVector(want);
                }
                float delta = Mathf.DeltaAngle(Mathf.Atan2(cur.y, cur.x) * Mathf.Rad2Deg, Mathf.Atan2(want.y, want.x) * Mathf.Rad2Deg);
                b.localRotation = Quaternion.AngleAxis(delta, Vector3.forward) * b.localRotation;
            }
            for (int i = 0; i < n; i++)
            {
                writtenRot[i] = bones[i].localRotation;
            }
            hasWritten = true;
        }

        private static float WrapAngle(float rad)
        {
            return Mathf.Repeat(rad + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;
        }

        private static float Det2D(Matrix4x4 m)
        {
            return m.m00 * m.m11 - m.m01 * m.m10;
        }

        private static bool SameExactly(Quaternion a, Quaternion b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z && a.w == b.w;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || !initialized || pos == null) return;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < n - 1; i++)
            {
                Gizmos.DrawLine(pos[i], pos[i + 1]);
            }
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            for (int i = 0; i < n - 1; i++)
            {
                Gizmos.DrawLine(animPos[i], animPos[i + 1]);
            }
        }
    }
}

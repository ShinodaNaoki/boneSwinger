using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Duel.BoneDragger.Sample
{
    /// <summary>
    /// 旧 BoneDragger と新 BoneSwing を左右に並べ、同じ動きをさせて比べるサンプル。
    /// 画面左上の GUI で動きとパラメーターを切り替えられる（キー 1〜8 でも動きを切替）。
    /// </summary>
    public class BoneSwingComparison : MonoBehaviour
    {
        public enum Motion
        {
            Still, ShakeX, ShakeY, Dash, Circle, Flip, Jump, Teleport
        }

        [SerializeField, Tooltip("BoneDragger が設定済みのキャラ（Assets/prefab/Chara.prefab）")]
        private GameObject characterPrefab;

        [SerializeField, Tooltip("設定すると新方式の値をこのアセットに直接書く（プレイ後も残る）")]
        private BoneSwingProfile profile;

        [SerializeField]
        private float spacing = 5f;

        [SerializeField]
        private Motion motion = Motion.ShakeX;

        [SerializeField, Range(0.1f, 3f), Tooltip("動きの大きさ（キャラの身長比）")]
        private float amplitude = 0.5f;

        [SerializeField, Range(0.1f, 4f)]
        private float speed = 1f;

        [SerializeField, Range(0.25f, 3f), Tooltip("両キャラのスケール。新方式は見た目が変わらないはず")]
        private float characterScale = 1f;

        private readonly List<Rig> rigs = new List<Rig>();
        private BoneSwingSettings settings;
        private Camera cam;
        private float motionTime;
        private float characterHeight = 1f;
        private Vector2 scroll;

        private class Rig
        {
            public string label;
            public Transform mover;
            public Vector3 home;
            public Animator animator;
            public Bounds bounds;
        }

        private void Start()
        {
            if (characterPrefab == null)
            {
                Debug.LogError("BoneSwingComparison: characterPrefab が未設定です。");
                enabled = false;
                return;
            }
            settings = profile != null ? profile.settings : new BoneSwingSettings();

            rigs.Add(Spawn("旧: BoneDragger", -spacing * 0.5f, false));
            rigs.Add(Spawn("新: BoneSwing", spacing * 0.5f, true));

            var bounds = rigs[0].bounds;
            bounds.Encapsulate(rigs[1].bounds);
            characterHeight = Mathf.Max(0.1f, rigs[0].bounds.size.y);
            SetupCamera(bounds);
        }

        private Rig Spawn(string label, float x, bool useNew)
        {
            var mover = new GameObject(label).transform;
            mover.SetParent(transform, false);
            mover.localPosition = new Vector3(x, 0f, 0f);
            var chara = Instantiate(characterPrefab, mover);
            chara.transform.localPosition = Vector3.zero;

            // プレハブ内のカメラは使わない
            foreach (var c in chara.GetComponentsInChildren<Camera>(true)) c.gameObject.SetActive(false);

            // キャラの見た目の中心を mover の原点に合わせる
            var renderers = chara.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers.Aggregate(renderers[0].bounds, (acc, r) => { acc.Encapsulate(r.bounds); return acc; });
                var offset = b.center - mover.position;
                offset.z = 0f;
                chara.transform.position -= offset;
            }

            if (useNew)
            {
                foreach (var dragger in chara.GetComponentsInChildren<BoneDragger>())
                {
                    var bones = dragger.Bones.ToList();
                    dragger.enabled = false;
                    var swing = dragger.gameObject.AddComponent<BoneSwing>();
                    swing.Settings = settings;
                    swing.SetBones(bones);
                }
            }

            var all = chara.GetComponentsInChildren<Renderer>();
            var bounds = all.Length > 0
                ? all.Aggregate(all[0].bounds, (acc, r) => { acc.Encapsulate(r.bounds); return acc; })
                : new Bounds(mover.position, Vector3.one);

            return new Rig
            {
                label = label,
                mover = mover,
                home = mover.localPosition,
                animator = chara.GetComponentInChildren<Animator>(),
                bounds = bounds,
            };
        }

        private void SetupCamera(Bounds bounds)
        {
            cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.3f, 0.38f);
            var size = bounds.size;
            float margin = 1.0f + amplitude * 1.5f;
            cam.orthographicSize = Mathf.Max(size.y * 0.5f * margin, size.x * 0.5f * margin / Mathf.Max(cam.aspect, 0.1f));
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
        }

        private void Update()
        {
            for (int i = 0; i < 8; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SetMotion((Motion)i);
            }

            motionTime += Time.deltaTime * speed;
            float a = amplitude * characterHeight;
            foreach (var rig in rigs)
            {
                var offset = Vector3.zero;
                float sx = 1f;
                switch (motion)
                {
                    case Motion.ShakeX:
                        offset.x = Mathf.Sin(motionTime * Mathf.PI * 2f) * a;
                        break;
                    case Motion.ShakeY:
                        offset.y = Mathf.Sin(motionTime * Mathf.PI * 2f) * a;
                        break;
                    case Motion.Dash:
                        offset.x = DashCurve(motionTime * 0.5f) * a * 2f;
                        break;
                    case Motion.Circle:
                        offset.x = Mathf.Cos(motionTime * Mathf.PI * 2f) * a;
                        offset.y = Mathf.Sin(motionTime * Mathf.PI * 2f) * a;
                        break;
                    case Motion.Flip:
                        sx = Mathf.Repeat(motionTime * 0.5f, 1f) < 0.5f ? 1f : -1f;
                        break;
                    case Motion.Jump:
                        float t = Mathf.Repeat(motionTime, 1.5f);
                        offset.y = t < 1f ? 4f * t * (1f - t) * a * 1.5f : 0f;
                        break;
                    case Motion.Teleport:
                        offset.x = Mathf.Repeat(motionTime * 0.5f, 1f) < 0.5f ? -a : a;
                        break;
                }
                rig.mover.localPosition = rig.home + offset;
                rig.mover.localScale = new Vector3(sx * characterScale, characterScale, 1f);
            }
        }

        // 0→1 で: 加速して走る → 急停止 → 止まる → 戻る
        private static float DashCurve(float t)
        {
            t = Mathf.Repeat(t, 1f);
            if (t < 0.35f) return Mathf.SmoothStep(-0.5f, 0.5f, t / 0.35f);
            if (t < 0.5f) return 0.5f;
            if (t < 0.85f) return Mathf.SmoothStep(0.5f, -0.5f, (t - 0.5f) / 0.35f);
            return -0.5f;
        }

        private void SetMotion(Motion m)
        {
            motion = m;
            motionTime = 0f;
        }

        private void Trigger(AvatarMotion m)
        {
            foreach (var rig in rigs)
            {
                if (rig.animator != null) rig.animator.SetTrigger(m.ToString());
            }
        }

        private void OnGUI()
        {
            if (settings == null) return;
            if (cam != null)
            {
                foreach (var rig in rigs)
                {
                    var top = cam.WorldToScreenPoint(rig.mover.position + Vector3.up * characterHeight * characterScale * 0.6f);
                    GUI.Label(new Rect(top.x - 60f, Screen.height - top.y - 20f, 160f, 24f), rig.label);
                }
            }

            GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label("動き (キー 1〜8)");
            var names = Enum.GetNames(typeof(Motion));
            int selected = GUILayout.SelectionGrid((int)motion, names, 4);
            if (selected != (int)motion) SetMotion((Motion)selected);
            amplitude = Slider("大きさ", amplitude, 0.1f, 3f);
            speed = Slider("速さ", speed, 0.1f, 4f);
            characterScale = Slider("キャラのスケール", characterScale, 0.25f, 3f);

            GUILayout.Label("アニメーション");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("idle")) Trigger(AvatarMotion.idle);
            if (GUILayout.Button("walk")) Trigger(AvatarMotion.walk);
            if (GUILayout.Button("run")) Trigger(AvatarMotion.run);
            if (GUILayout.Button("attack")) Trigger(AvatarMotion.attack);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("新方式のパラメーター（全房共通）");
            settings.inertia = Slider("慣性", settings.inertia, 0f, 1f);
            settings.airDrag = Slider("空気抵抗", settings.airDrag, 0f, 1f);
            settings.stiffness = Slider("復元力", settings.stiffness, 0f, 1f);
            GUILayout.Label(settings.inertia > 0f && settings.stiffness > 0f
                ? $"　揺れの周期 ≈ {2f * Mathf.PI * Mathf.Sqrt(settings.inertia / (200f * settings.stiffness)):0.00} 秒"
                : "　揺れの周期: 慣性0か復元力0なので揺れ返さない");
            settings.damping = Slider("減衰", settings.damping, 0f, 1f);
            settings.flutter = Slider("はためき", settings.flutter, 0f, 1f);
            settings.angleLimit = Slider("曲げ限界角", settings.angleLimit, 1f, 180f);
            settings.hardening = Slider("硬化カーブ", settings.hardening, 1f, 8f);
            settings.gravity = Slider("重力", settings.gravity, 0f, 2f);
            settings.wind.x = Slider("風 X", settings.wind.x, -3f, 3f);
            settings.simulateInLocalSpace = GUILayout.Toggle(settings.simulateInLocalSpace, "ローカル空間で計算");

            GUILayout.Space(4);
            GUILayout.Label("プリセット");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("リボン")) Preset(0f, 0.6f, 0.15f, 0f, 0.5f, 120f, 2f, 0.3f);
            if (GUILayout.Button("髪")) Preset(0.5f, 0.3f, 0.2f, 0.3f, 0f, 60f, 2f, 0.2f);
            if (GUILayout.Button("竹")) Preset(0.8f, 0.1f, 0.6f, 0.15f, 0f, 45f, 2f, 0f);
            if (GUILayout.Button("ゴム止め")) Preset(0.7f, 0.15f, 0.15f, 0.2f, 0f, 40f, 8f, 0f);
            GUILayout.EndHorizontal();

            GUILayout.Label("重さの分布（根本→先端）");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("均一")) settings.massCurve = AnimationCurve.Linear(0, 1, 1, 1);
            if (GUILayout.Button("先端が重い")) settings.massCurve = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.7f, 0.5f), new Keyframe(1, 4f));
            if (GUILayout.Button("根本が重い")) settings.massCurve = AnimationCurve.Linear(0, 2f, 1, 0.3f);
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Preset(float inertia, float airDrag, float stiffness, float damping, float flutter, float angleLimit, float hardening, float gravity)
        {
            settings.inertia = inertia;
            settings.airDrag = airDrag;
            settings.stiffness = stiffness;
            settings.damping = damping;
            settings.flutter = flutter;
            settings.angleLimit = angleLimit;
            settings.hardening = hardening;
            settings.gravity = gravity;
        }

        private static float Slider(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}", GUILayout.Width(110));
            value = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.Label(value.ToString("0.00"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            return value;
        }
    }
}

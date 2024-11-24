using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.U2D.Animation;


namespace Old.V1
{
    [ExecuteInEditMode]
    public class BoneDraggerManagerOld : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool executeInEditMode = false;

        public bool Execute { get => executeInEditMode || Application.isPlaying; }
#else
    public bool Execute { get => true: }
#endif
        [SerializeField, Range(0, 2), Tooltip("Multiplier according to delta time")]
        private float airDragTimeScale = 1f;

        [SerializeField, Range(0, 1), Tooltip("Multiplier according to delta time")]
        public float inartiaTimeScale = 1f;

        [SerializeField, Tooltip("Module to calc inartia force")]
        private InartiaWeightOld inartia;

        [SerializeField, Range(0, 1), Tooltip("Mixing ratio of air-dragg vs inartia")]
        public float AirVsMass = 1f;

        [SerializeField, Range(0, 1), Tooltip("Decay ratio when its over angular limit")]
        public float Softness = 0.5f;

        [SerializeField, Range(0, 180), Tooltip("Angular limit from original angle")]
        public float AngularLimit = 90f;

        [SerializeField, Range(0, 1), Tooltip("Force to restore original pose")]
        private float restoreSpring = 0.1f;


        public float RestoreSpring { get => restoreSpring; }

        public float AirDragTimeScale { get => airDragTimeScale / 0.02f; }

        [SerializeField]
        List<BoneDragNodeOld> nodes = new List<BoneDragNodeOld>();

        public Transform baseBone { get => nodes.First()?.target; }

        private Vector3 prevOriginPos = Vector3.zero;

        private bool initialized = false;

        public float TotalLength { get; private set; }

        public Vector3 InartiaForce { get => inartia.Force; }


        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        private void Init()
        {
            if (initialized) return;
            initialized = true;

            if (inartia != null) inartia.Reset();

            var pos0 = baseBone.position;

            BoneDragNodeOld prevNode = null;
            foreach (var node in nodes.Reverse<BoneDragNodeOld>())
            {
                node.Init(prevNode);
                prevNode = node;
            }
            TotalLength = nodes.Sum(x => x.Length);
            foreach (var node in nodes)
            {
                node.LateInit(this);
            }
        }


        // Update is called once per frame
        void FixedUpdate()
        {
            if (!Execute) return;
            inartia.Update();
            BoneDragNodeOld prev = null;
            foreach (var node in nodes.Reverse<BoneDragNodeOld>())
            {
                node.Update(this, prev);
            }
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Application.isPlaying) return;
            // エディタで作業中に更新されるように
            FixedUpdate();
        }
#endif

        #region Editor Helpers
        [Button]
        private void SetupInartiaModule()
        {
            if (inartia == null)
            {
                inartia = new InartiaWeightOld();
            }
            inartia.target = baseBone.gameObject;
            inartia.Reset();
        }

        public void AutoSetDescendants()
        {
            var avaRoot = FindAvatarRoot();
            var bones = avaRoot.GetComponentsInChildren<SpriteSkin>()
                .SelectMany(ss => ss.boneTransforms).Distinct().ToArray();
            var curBone = baseBone;
            Transform nextBone;
            while ((nextBone = FindNextBone(baseBone, bones)) != null)
            {
                AddNext(nextBone);
            }

        }

        private Transform FindNextBone(Transform parent, Transform[] allBones)
        {
            foreach (Transform ch in parent)
            {
                if (allBones.Contains(ch))
                {
                    return ch;
                }
            }
            return null;
        }

        private GameObject FindAvatarRoot()
        {
            var parent = transform;
            while (parent != null)
            {
                var children = parent.GetComponentsInChildren<SpriteSkin>();
                if (children != null && children.Length > 0)
                {
                    return parent.gameObject;
                }
                parent = parent.parent;
            }
            return null;
        }


        internal void AddNext(Transform bone)
        {
            var node = new BoneDragNodeOld();
            node.target = bone;
            node.rigidbody = bone.GetComponent<Rigidbody>();
            // TODO create if not found.
            nodes.Add(node);
        }

        [Button]
        public void HomigenizeNode()
        {
            float airDrag = 1f / nodes.Count;
            float weight = 1f / nodes.Count;
            foreach (var node in nodes)
            {
                node.airDrag = airDrag;
                node.weight = weight;
            }
        }

        [Button]
        public void UpdateDescendants()
        {
            if (nodes == null || nodes.Count == 0) return;

            var newList = new List<BoneDragNodeOld>();
            var parent = nodes[0].target;
            if (parent == null) return;
            newList.Add(nodes[0]);

            foreach (var item in nodes.Skip(1))
            {
                if (item == null) return;
                if (item.target.parent == parent)
                {
                    newList.Add(item);
                    parent = item.target;
                    continue;
                }
                if (item.target.childCount == 0) break;
                foreach (Transform child in item.target)
                {
                    var rb = child.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        var newItem = item.target.GetChild(0);
                        var newNode = new BoneDragNodeOld();
                        newNode.target = newItem;
                        newNode.airDrag = item.airDrag;
                        newNode.weight = item.weight;
                        newList.Add(newNode);
                        parent = item.target;
                        break;
                    }
                }
                break;
            }
        }

        [Button]
        private void SaveCurrentBonesRotation()
        {
            foreach (var item in nodes)
            {
                if (item == null || item.target == null) break;
                item.initailRotation = Quaternion.Euler(0, 0, item.target.localRotation.eulerAngles.z);
            }
        }

        [Button]
        private void RestoreBonesRotation()
        {
            foreach (var item in nodes)
            {
                if (item == null || item.target == null) break;
                item.target.localRotation = item.initailRotation;
            }
        }

        [Button]
        private void SetRigidBodiesForNodes()
        {
            Transform prev = null;
            foreach (var item in nodes)
            {
                if (item == null || item.target == null) break;
                var rb = item.target.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = item.target.AddComponent<Rigidbody>();
                }
                rb.constraints = RigidbodyConstraints.FreezePositionZ
                    | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
                rb.useGravity = false;
                rb.isKinematic = true;
                if (prev != null)
                {
                    var joint = item.target.GetComponent<HingeJoint>();
                    if (joint == null)
                    {
                        joint = item.target.AddComponent<HingeJoint>();
                        joint.connectedBody = prev.GetComponent<Rigidbody>();
                        joint.axis = Vector3.forward;
                    }

                }
                prev = item.target;
            }
        }

        [Button]
        private void RemoveRigidBodiesForNodes()
        {
            foreach (var item in nodes)
            {
                if (item == null || item.target == null) break;
                var joint = item.target.GetComponent<HingeJoint>();
                if (joint != null)
                {
                    DestroyImmediate(joint);
                }
                var rb = item.target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    DestroyImmediate(rb);
                }

            }
        }
        #endregion

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnDrawGizmos()
        {
            if (!initialized || inartia == null) return;
            inartia.OnDrawGizmos();
        }

        private void OnDestroy()
        {
        }

        private void OnEnable()
        {
            Init();
        }

        private void OnValidate()
        {
            if (inartia != null) inartia.OnValidate();
        }

    }
}
using NaughtyAttributes;
using Program.Util;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Duel.BoneDragger
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(BoneDraggerParameters))]
    public class BoneDragger : MonoBehaviour
    {
        [SerializeField]
        private BoneDraggerParameters parameters;

        public float RestoreSpring { get => parameters.RestoreSpring; }

        public float AirDragTimeScale { get => parameters.AirDragTimeScale / 0.02f; }

        public float AngularLimit { get => parameters.AngularLimit; }

        public float AirVsMass { get => parameters.AirVsMass; }

        public float InartiaTimeScale { get => parameters.InartiaTimeScale; }

        public float Softness {  get => parameters.Softness; }

        [SerializeField]
        List<BoneDragNode> nodes = new List<BoneDragNode>();

        public IEnumerable<Transform> Bones { get => nodes.Select(n => n.target); }

        public float DeltaTime { get => parameters.ApplyPhysics ? Time.fixedDeltaTime : Time.deltaTime; }

        public Transform baseBone { get => nodes.First()?.target; }

        private Vector3 prevOriginPos = Vector3.zero;

        [SerializeField]
        private InartiaGenerator InartiaGenerator;

        private bool initialized = false;

        public float TotalLength { get; private set; }

        public Vector3 InartiaForce { get => InartiaGenerator.Force; }

        private BoneDraggerManager manager;

        public Vector3 Gravity {  get => transform.TransformVector (manager.Gravity); }
        public Vector3 Wind { get => transform.TransformVector (manager.Wind); }

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        private void Init()
        {
            if (nodes.Count ==0 || initialized) return;
            initialized = true;

            if (InartiaGenerator == null)
            {
                InartiaGenerator = new InartiaGenerator();
            }

            if (parameters == null)
            {
                parameters = GetComponent<BoneDraggerParameters>();
            }

            if (parameters.Inartia != null && baseBone != null) {
                InartiaGenerator.Init(baseBone, parameters.Inartia);
            }

            BoneDragNode prevNode = null;
            foreach (var node in nodes.Reverse<BoneDragNode>())
            {
                node.Init(prevNode);
                prevNode = node;
            }
            TotalLength = nodes.Sum(x => x.Length);
            foreach (var node in nodes)
            {
                node.LateInit(this);
            }

            manager = gameObject.FindClosest<BoneDraggerManager>();
        }


        void UpdateCore()
        {
#if UNITY_EDITOR
            // エディタで作業中に更新するかどうか
            if (!parameters.Execute) return;
#endif

            InartiaGenerator.Update();
            BoneDragNode prev = null;
            foreach (var node in nodes.Reverse<BoneDragNode>())
            {
                node.Update(this, prev);
            }
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (parameters.ApplyPhysics)
            {
                UpdateCore();
            }
        }

        private void Update()
        {
            if (!parameters.ApplyPhysics)
            {
                UpdateCore();
            }
        }

        #region Editor Helpers

        [Button("Auto add descendant nodes")]
        public void UpdateDescendants()
        {
            if (nodes == null || nodes.Count == 0) return;

            var first = nodes[0].target;

            var branch = SpriteSkinUtil.FindSequentialBoneBranches(first.gameObject)
                .Where(br => br.Contains(first)).FirstOrDefault();
            if (branch == null)
            {
                Debug.LogError("繋がるボーンが見つかりませんでした。");
                return;
            }
            InitWithBones(branch.SkipWhile(b => b != first).ToList());
        }

        [Button("Homogenize weight and air drag for nodes")]
        public void HomogenizeNode()
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
        public void SaveCurrentBonesRotation()
        {
            foreach (var item in nodes)
            {
                if (item == null || item.target == null) break;
                item.initailRotation = Quaternion.Euler(0, 0, item.target.localRotation.eulerAngles.z);
            }
        }

        [Button]
        public void RestoreBonesRotation()
        {
            foreach (var item in nodes)
            {
                if (item == null || item.target == null) break;
                item.target.localRotation = item.initailRotation;
            }
        }

        //[Button]
        private void SetRigidBodiesForNodes()
        {
            Transform prev = null;
            foreach (var item in nodes)
            {
                if (item == null || item.target == null) break;
                var rb = item.target.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = item.target.gameObject.AddComponent<Rigidbody>();
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
                        joint = item.target.gameObject.AddComponent<HingeJoint>();
                        joint.connectedBody = prev.GetComponent<Rigidbody>();
                        joint.axis = Vector3.forward;
                    }

                }
                prev = item.target;
            }
        }

        //[Button]
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

        public void InitWithBones(List<Transform> bones)
        {
            nodes = bones.Select(b => {
                var newNode = new BoneDragNode();
                newNode.target = b;
                return newNode;
            }).ToList();
            SaveCurrentBonesRotation();
            
            Init();
        }
        #endregion

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnDrawGizmos()
        {
            if (!initialized || parameters == null || parameters.Inartia == null) return;
            InartiaGenerator.OnDrawGizmos();
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
            if (InartiaGenerator != null) InartiaGenerator.OnValidate(parameters.Inartia);
        }

    }
}
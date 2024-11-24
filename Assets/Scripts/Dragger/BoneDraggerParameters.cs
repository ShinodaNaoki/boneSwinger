using UnityEngine;

namespace Duel.BoneDragger
{
    public class BoneDraggerParameters : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField]
        private bool executeInEditMode = false;

        public bool Execute { get => executeInEditMode || Application.isPlaying; }
#else
        public bool Execute { get => true; }
#endif
        [SerializeField, Range(0, 2), Tooltip("Multiplier according to delta time")]
        public float AirDragTimeScale = 1f;

        [SerializeField, Range(0, 1), Tooltip("Multiplier according to delta time")]
        public float InartiaTimeScale = 1f;

        [SerializeField, Tooltip("Module to calc inartia force")]
        public InartiaParameters Inartia = new InartiaParameters();

        [SerializeField, Range(0, 1), Tooltip("Mixing ratio of air-dragg vs inartia")]
        public float AirVsMass = 1f;

        [SerializeField, Range(0, 1), Tooltip("Decay ratio when its over angular limit")]
        public float Softness = 0.5f;

        [SerializeField, Range(0, 180), Tooltip("Angular limit from original angle")]
        public float AngularLimit = 90f;

        [SerializeField, Range(0, 1), Tooltip("Force to restore original pose")]
        public float RestoreSpring = 0.1f;

        [SerializeField, Tooltip("Use FixedUpdate")]
        private bool applyPhysics = true;
#if UNITY_EDITOR
        public bool ApplyPhysics { get => Application.isPlaying ? applyPhysics : false; }
#else
        public bool ApplyPhysics { get => applyPhysics; }
#endif
    }
}
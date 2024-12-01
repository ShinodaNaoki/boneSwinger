using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "BoneDragger/Create Settings")]
public class BoneDraggerSettings : ScriptableObject
{
    private static BoneDraggerSettings _instance;

    private void OnEnable()
    {
        _instance = this;
    }

    [SerializeField, Range(1f, 15f)]
    private int InartiaAmplitude = 5;

    private float InartiaScale;

    [SerializeField, Range(0, 180)]
    private float MaxArirDragAngle = 30f;

    [SerializeField]
    private float AirDragAmplitude = 3;

    private float AirDragScale;

    [SerializeField, Range(0, 180)]
    private float MaxRestoreAngle = 60f;

    [SerializeField, Range(1f, 15f)]
    private int RestoreSpringAmplitude = 1;

    private float RestoreSpringMultiplier;

    [SerializeField, Range(0, 180)]
    private float AngleOverLimitDumper = 2f;


    void OnValidate()
    {
        InartiaScale = Mathf.Pow(10, InartiaAmplitude);
        AirDragScale = Mathf.Pow(4, AirDragAmplitude);
        RestoreSpringMultiplier = Mathf.Pow(10, RestoreSpringAmplitude);
    }

    static public float INARTIA_SCALE { get => _instance != null ? _instance.InartiaScale : 100000f; }
    static public float MAX_AIR_DRAG_ANGLE { get => _instance != null ? _instance.MaxArirDragAngle : 30f; }
    static public float AIR_DRAG_MULTIPLIER { get => _instance != null ? _instance.AirDragScale : 60f; }
    static public float MAX_RESTORE_ANGLE { get => _instance != null ? _instance.MaxRestoreAngle : 60f; }
    static public float RESTORE_SPRING_MULTIPLIER { get => _instance != null ? _instance.RestoreSpringMultiplier : 10000f; }
    static public float ANGLE_OVER_LIMIT_DAMPER { get => _instance != null ? _instance.AngleOverLimitDumper : 2f; }
}

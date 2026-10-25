using UnityEngine;

namespace Duel.BoneDragger
{
    /// <summary>
    /// 複数の房・キャラで BoneSwing の設定を共有するためのアセット。
    /// </summary>
    [CreateAssetMenu(menuName = "BoneDragger/Create BoneSwing Profile")]
    public class BoneSwingProfile : ScriptableObject
    {
        public BoneSwingSettings settings = new BoneSwingSettings();
    }
}

using Program.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace Duel.BoneDragger
{
    public class BoneDraggerManager : MonoBehaviour
    {
        public Vector2 Gravity = Vector2.zero;
        public Vector2 Wind = Vector2.zero;

        public void SaveCurrentRotations()
        {
            foreach (var dragger in gameObject.FindRecursively<BoneDragger>())
            {
                dragger.SaveCurrentBonesRotation();
            }
        }

        public void RestoreSavedRotations()
        {
            foreach (var dragger in gameObject.FindRecursively<BoneDragger>())
            {
                dragger.RestoreBonesRotation();
            }
        }     

        public void CheckDuplicatedNodes()
        {
            var result = false;
            var draggers = gameObject.FindRecursively<BoneDragger>().ToList();
            foreach (var dragger in draggers)
            {
                
                foreach(var other in gameObject.FindRecursively<BoneDragger>())
                {
                    if (dragger == other) continue;
                    result |= CheckDupulication(dragger, other);
                }
            }
            if (!result)
            {
                UnityEngine.Debug.Log("èdï°ìoò^ÇÕå©ìñÇΩÇËÇ‹ÇπÇÒÇ≈ÇµÇΩ".lime());
            }
        }

        private bool CheckDupulication(BoneDragger d1, BoneDragger d2)
        {
            var result = false;
            var b1 = d1.Bones; var b2 = d2.Bones;
            foreach(var bone in b1)
            {
                if(b2.Contains(bone))
                {
                    UnityEngine.Debug.LogError($"'{d1.name.orange()}' Ç∆ '{d2.name.yellow()}' ÇÕìØÇ∂É{Å[Éì '{bone.name.lime()}' Çä‹ÇÒÇ≈Ç¢Ç‹Ç∑");
                    result = true;
                }
            }
            return result;
        }
    }
}
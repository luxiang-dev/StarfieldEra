using UnityEngine;
namespace StarfieldEra
{
    public sealed class LocalChestVisual:MonoBehaviour
    {
        Transform lid;Quaternion closed;bool initialized;
        void Initialize(){if(initialized)return;foreach(var node in GetComponentsInChildren<Transform>())if(node.name=="ChestLid"){lid=node;closed=node.localRotation;break;}initialized=true;}
        public void SetOpened(bool opened){Initialize();if(lid)lid.localRotation=closed*Quaternion.Euler(opened?-68:0,0,0);}
    }
}

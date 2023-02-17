using System;

using UnityEngine;

namespace CWJ
{
    public static class ObjActivateCallback_System_Utility
    {
        public static ObjActivateCallback_System GetObjActivateListener_System(this Transform transform)
        {
            return transform.GetOrAddComponent<ObjActivateCallback_System>();
        }
    }

    public class ObjActivateCallback_System : MonoBehaviour
    {
        public event Action<GameObject> enabledEvent;

        public event Action<GameObject> disabledEvent;

        private void OnEnable() => enabledEvent?.Invoke(gameObject);

        private void OnDisable() => disabledEvent?.Invoke(gameObject);
    }
}
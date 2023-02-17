using UnityEngine;

namespace CWJ
{
    public static class ObjActivateCallback_Unity_Utility
    {
        public static ObjActivateCallback_Unity GetObjActivateListener_Unity(this Transform transform)
        {
            return transform.GetOrAddComponent<ObjActivateCallback_Unity>();
        }
    }

    public class ObjActivateCallback_Unity : MonoBehaviour
    {
        public UnityEvent_GameObject enabledEvent = new UnityEvent_GameObject();
        public UnityEvent_GameObject disabledEvent = new UnityEvent_GameObject();

        private void OnEnable() => enabledEvent.Invoke(gameObject);

        private void OnDisable() => disabledEvent.Invoke(gameObject);
    }
}
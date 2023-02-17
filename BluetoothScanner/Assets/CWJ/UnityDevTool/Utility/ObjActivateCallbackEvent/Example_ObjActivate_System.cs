using System.Collections.Generic;

using UnityEngine;

namespace CWJ
{
    public class Example_ObjActivate_System : MonoBehaviour
    {
        public List<ObjActivateCallback_System> objectSystemListeners = new List<ObjActivateCallback_System>();

        private void Start()
        {
            foreach (Transform child in transform)
            {
                ObjActivateCallback_System objSystemListener = child.GetObjActivateListener_System();

                objSystemListener.enabledEvent += (go) => Debug.LogError(go.name + " is Enabled");
                objSystemListener.disabledEvent += (go) => Debug.LogError(go.name + " is Disabled");

                objectSystemListeners.Add(objSystemListener);
            }
        }
    }
}
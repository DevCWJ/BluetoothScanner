using System.Collections.Generic;

using UnityEngine;

namespace CWJ
{
    public class Example_ObjActivate_Unity : MonoBehaviour
    {
        public List<ObjActivateCallback_Unity> objectUnityListeners = new List<ObjActivateCallback_Unity>();

        private void Start()
        {
            foreach (Transform child in transform)
            {
                ObjActivateCallback_Unity objUnityListener = child.GetObjActivateListener_Unity();

                objUnityListener.enabledEvent.AddListener_New(PrintEnabled, false);
                objUnityListener.disabledEvent.AddListener_New(PrintDisabled, false);

                objectUnityListeners.Add(objUnityListener);
            }
        }

        private void PrintEnabled(GameObject gameObject)
        {
            Debug.LogError(gameObject.name + " is Enabled");
        }

        private void PrintDisabled(GameObject gameObject)
        {
            Debug.LogError(gameObject.name + " is Disabled");
        }
    }
}
using System;

using CWJ.Singleton.SwapSingleton;

using UnityEngine;
using UnityEngine.Events;

namespace CWJ
{
    [AddComponentMenu("Scripts/" + nameof(CWJ) + "/CWJ_" + nameof(TouchManager))]
    public class TouchManager : SingletonBehaviourDontDestroy_Swap<TouchManager>
    {
#if UNITY_EDITOR

        [UnityEditor.MenuItem("GameObject/" + nameof(CWJ) + "/" + nameof(TouchManager), false, 10)]
        public static void CreateTouchManager()
        {
            Transform backupParent = UnityEditor.Selection.activeTransform;

            if (IsExists)
            {
                UnityEditor.Selection.activeTransform = FindUtil.FindObjectOfType_New<TouchManager>(false, false).transform;
                return;
            }

            GameObject newObj = new GameObject(nameof(TouchManager), typeof(TouchManager));
            newObj.transform.SetParent(backupParent);
            newObj.transform.Reset();
            UnityEditor.Selection.activeObject = newObj;
        }

#endif

        /// <summary>
        /// Editor 디버깅용
        /// </summary>
        [Readonly] [SerializeField] protected bool isMobile;

        public TouchListener[] touchListeners = new TouchListener[0];

        protected UnityEvent[] touchEvents = new UnityEvent[5]
        {
            new UnityEvent(), //Began
            new UnityEvent(), //Moved
            new UnityEvent(), //Stationary
            new UnityEvent(), //Ended
            new UnityEvent() //Canceled
        };

        protected UnityEvent onUpdateEnded = new UnityEvent();

        [Foldout("Variable For Check")]
        [Readonly, SerializeField] private bool _isHoldDown;//클릭중인지

        public bool isHoldDown { get => _isHoldDown; protected set => _isHoldDown = value; }

        [Foldout("Variable For Check")]
        [Readonly, SerializeField] private bool _isMoving; //클릭후 포인터를 이동중인지

        public bool isMoving { get => _isMoving; protected set => _isMoving = value; }

        public virtual bool AddTouchListener(TouchListener listener)
        {
            if (ArrayUtil.IsExists(touchListeners, listener)) //중복 추가 방지
            {
                return false;
            }
            ArrayUtil.Add(ref touchListeners, listener);
            return true;
        }

        public virtual bool RemoveTouchListener(TouchListener listener)
        {
            if (!ArrayUtil.IsExists(touchListeners, listener)) //제거 반복 방지
            {
                return false;
            }
            ArrayUtil.Remove(ref touchListeners, listener);
            return true;
        }

        public virtual void UpdateInputSystem()
        {
        }

        protected void Update()
        {
            if (touchListeners.Length == 0)
            {
                return;
            }

            UpdateInputSystem();
        }

        protected override Type GetSwapType()
        {
#if !UNITY_EDITOR
#if UNITY_ANDROID
            isMobile = true;
#else
            isMobile = false;
#endif
#endif
            return isMobile ? typeof(TouchManager_Mobile) : typeof(TouchManager_PC);
        }

        protected override void SwapSetting(TouchManager newComp)
        {
            newComp.isMobile = isMobile;

            for (int i = 0; i < touchListeners.Length; i++)
            {
                newComp.AddTouchListener(touchListeners[i]);
            }
        }
    }
}
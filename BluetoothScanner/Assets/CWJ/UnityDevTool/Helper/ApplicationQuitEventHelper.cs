using CWJ.Singleton;

using UnityEngine;
using UnityEngine.Events;

namespace CWJ
{
    [UnityEngine.DefaultExecutionOrder(32000)]
    public class ApplicationQuitEventHelper : SingletonBehaviourDontDestroy<ApplicationQuitEventHelper>, IDontPrecreatedInScene
    {
        //#if UNITY_EDITOR
        //        [UnityEditor.InitializeOnLoadMethod]
        //        public static void InitializeOnLoad()
        //        {
        //            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.timeSinceStartup < 15)//(play시키지않은상태이며 에디터를 켠지 15초가 안된 때)
        //            {
        //                CWJ.EditorScript.SetScriptExecutionOrder.SetOrder(typeof(ApplicationQuitEvent), 32000);
        //            }
        //        }
        //#endif

        [UnityEngine.SerializeField] private UnityEvent quitEvent = new UnityEvent();

        [UnityEngine.SerializeField] private UnityEvent lastQuitEvent = new UnityEvent();

        /// <summary>
        /// RuntimeInitializeOnLoadMethod 에서 넣어줘야함
        /// </summary>
        /// <param name="action"></param>
        public void AddQuitEvent(UnityAction action)
        {
            quitEvent.AddListener_New(action, false);
        }

        /// <summary>
        /// RuntimeInitializeOnLoadMethod 에서 넣어줘야함
        /// </summary>
        /// <param name="action"></param>
        public void AddAtLastQuitEvent(UnityAction action)
        {
            lastQuitEvent.AddListener_New(action, false);
        }

        protected override void _OnApplicationQuit()
        {
            quitEvent?.Invoke();

            if (Application.isEditor)
            {
                lastQuitEvent?.Invoke();
            }
        }

        //OnApplicationQuit보다 OnDestroy가 더 늦게 실행됨 즉, DontDestroyOnLoad 싱글톤의 OnDestroy가 프로그램종료 시 가장 늦게 실행될거임
        //에디터에서는 실행종료된다해도 실행되지않음.
        protected override void _OnDestroy()
        {
            if (!Application.isEditor)
            {
                lastQuitEvent?.Invoke();
            }
        }

        protected override void _Awake()
        {
            HideGameObject();
            transform.SetAsLastSibling();
        }
    }
}
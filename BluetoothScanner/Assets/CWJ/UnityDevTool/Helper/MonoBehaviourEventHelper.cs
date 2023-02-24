using CWJ.Singleton;

using UnityEditor;

using UnityEngine;
using UnityEngine.Events;

namespace CWJ
{
    [UnityEngine.DefaultExecutionOrder(-32000)]
    public class MonoBehaviourEventHelper : SingletonBehaviourDontDestroy<MonoBehaviourEventHelper>, IDontPrecreatedInScene
    {
#if UNITY_EDITOR
        public static bool Editor_IsManagedByEditorScript = false;
        public static bool Editor_IsSilentlyCreateInstance = false;

        [UnityEditor.InitializeOnLoadMethod]
        public static void InitializeOnLoad()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
            CWJ.AccessibleEditor.CWJ_EditorEventHelper.ProjectOpenEvent += EditorEventSystem_ProjectOpenEvent;
        }

        private static void EditorEventSystem_ProjectOpenEvent()
        {
            try
            {
                CWJ.AccessibleEditor.ScriptExecutionOrder.SetMonoBehaviourExecutionOrder<MonoBehaviourEventHelper>(-32000);
                CWJ.AccessibleEditor.ScriptExecutionOrder.SetMonoBehaviourExecutionOrder<LastCloseObject>(32000);
            }
            finally
            {
            }
        }

        protected static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                IS_QUIT = true;
                IS_PLAYING = false;
            }
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                IS_QUIT = false;
                IS_PLAYING = false;
            }
        }
#endif


        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnAfterAssembliesLoaded()
        {
            IS_PLAYING = true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            _UpdateInstance(false);
        }

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

        LastCloseObject lastCloseObject = null;

        protected override void OnInstanceAssigned()
        {
            if (lastCloseObject == null)
            {
                lastCloseObject = new GameObject(nameof(LastCloseObject)).AddComponent<LastCloseObject>();
                lastCloseObject.transform.SetParent(transform);
                lastCloseObject.quitEvent.AddListener(InvokeStaticQuitEvent);
                lastCloseObject.lastQuitEvent.AddListener(InvokeStaticLastQuitEventEvent);
            }
        }

        public static event System.Action AwakeEvent;
        public static event System.Action StartEvent;

        public static event System.Action QuitEvent;
        public static event System.Action LastQuitEvent;
        void InvokeStaticQuitEvent() => QuitEvent?.Invoke();
        void InvokeStaticLastQuitEventEvent() => LastQuitEvent?.Invoke();

        public static bool IS_EDITOR =>
#if UNITY_EDITOR
                true;
#else
                false;
#endif

        public static bool IS_PLAYING { get; private set; } = false;

        //        public static bool ApplicationIsPlaying
        //        {
        //#if UNITY_EDITOR
        //            get => UnityEditor.EditorApplication.isPlaying;
        //#else
        //            get => Application.isPlaying;
        //#endif
        //        }

        /// <summary>
        /// OnDisabled, OnDestroy와 같은 곳에선 종료시에도 불릴수있기때문에 IsQuit으로 return처리 해주어야함
        /// </summary>
        public static bool IS_QUIT { get; private set; }


        /// <summary>
        /// 실행중 ~ 종료되기전까지 true
        /// DontDestroyOnLoad는 이게 true일때만 가능
        /// </summary>
        public static bool GetIsPlayingBeforeQuit() => IS_PLAYING && !IS_QUIT;

        /// <summary>
        /// 생성혹은 제거가 가능 할 때
        /// </summary>
        public static bool GetIsValidCreateObject() =>
#if UNITY_EDITOR
            (!IS_PLAYING || !IS_QUIT);
#else
        GetIsPlayingBeforeQuit();
#endif
        //

        protected override void _OnApplicationQuit()
        {
            IS_QUIT = true;
        }

        protected override void _Awake()
        {
            HideGameObject();
            transform.SetAsLastSibling();
            AwakeEvent?.Invoke();
        }

        protected override void _Start()
        {
            StartEvent?.Invoke();
        }
    }
}
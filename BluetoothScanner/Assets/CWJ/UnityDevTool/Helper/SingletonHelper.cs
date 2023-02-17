using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using System.Linq;
using CWJ.Singleton;

#if UNITY_EDITOR
using UnityEditor;

#if UNITY_2021_3_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif
#endif
namespace CWJ
{
    public class SingletonHelper : SingletonBehaviourDontDestroy<SingletonHelper>, IDontPrecreatedInScene
    {
        private static List<MonoBehaviourCWJ_AwakableInInactive> awakableInInactives = new List<MonoBehaviourCWJ_AwakableInInactive>();
        private static SingletonHelper instanceForNonMono = null;
        public static void AddAwakableInInactive(MonoBehaviourCWJ_AwakableInInactive awakableInInactive)
        {
            awakableInInactives.Add(awakableInInactive);

            if (instanceForNonMono != null && CO_CallAwakeForInactiveObj == null)
            {
                CO_CallAwakeForInactiveObj = instanceForNonMono.StartCoroutine(DO_CallAwakeForInactiveObj());
            }
        }
        public static void RemoveAwakableInInactive(MonoBehaviourCWJ_AwakableInInactive awakableInInactive)
        {
            awakableInInactives.Remove(awakableInInactive);
        }

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
                CWJ.AccessibleEditor.ScriptExecutionOrder.SetMonoBehaviourExecutionOrder<SingletonHelper>(-32000);
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


        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnAfterAssembliesLoaded()
        {
            IS_PLAYING = true;
        }

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

        private static List<Component> _AllSingletonComps = new List<Component>();

        [VisualizeField, Readonly] private List<Component> _allSingletonComps = new List<Component>();
        public Component[] allSingletonComps
        {
            get => _allSingletonComps.ToArray();
        }

        public static void AddSingletonAllElem(Component singletonComp)
        {
            if (HasInstance) Instance._allSingletonComps.Add(singletonComp);
            else _AllSingletonComps.Add(singletonComp);
        }
        public static void RemoveSingletonAllElem(Component singletonComp)
        {
            if (HasInstance) Instance._allSingletonComps.Remove(singletonComp);
            else _AllSingletonComps.Remove(singletonComp);
        }

        private static List<Component> _SingletonInstanceComps { get; set; } = new List<Component>();

        [VisualizeProperty, Readonly] private List<Component> _singletonInstanceComps { get; set; } = new List<Component>();
        public Component[] singletonInstanceComps
        {
            get => _singletonInstanceComps.ToArray();
        }

        public static void AddSingletonInstanceElem(Component singletonComp)
        {
            if (HasInstance) Instance._singletonInstanceComps.Add(singletonComp);
            else _SingletonInstanceComps.Add(singletonComp);
        }
        public static void RemoveSingletonInstanceElem(Component singletonComp)
        {
            if (HasInstance) Instance._singletonInstanceComps.Remove(singletonComp);
            else _SingletonInstanceComps.Remove(singletonComp);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            _UpdateInstance(false);
        }

        protected override sealed void OnApplicationQuit()
        {
            IS_QUIT = true;
            base.OnApplicationQuit();
        }

        protected override void _Awake()
        {
            HideGameObject();
            transform.SetAsFirstSibling();
        }

        static Coroutine CO_CallAwakeForInactiveObj = null;
        static IEnumerator DO_CallAwakeForInactiveObj()
        {
            yield return null;
            awakableInInactives = awakableInInactives.Where(x => x != null).ToList();
            if (awakableInInactives.Count > 0)
            {
                for (int i = awakableInInactives.Count - 1; i > 0; --i)
                {
                    var comp = awakableInInactives[i];
                    awakableInInactives.RemoveAt(i);

                    if (comp == null || comp.gameObject == null || !comp.gameObject.scene.IsValid()) 
                        continue;
#if UNITY_EDITOR
                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                    bool isPrefabObj = prefabStage != null ? (PrefabUtility.GetOutermostPrefabInstanceRoot(comp.gameObject).Equals(prefabStage.prefabContentsRoot)) : PrefabUtility.IsPartOfAnyPrefab(comp.gameObject);

                    if (isPrefabObj && comp.gameObject.scene.IsNull())
                    {
                        continue;
                    }
#endif

                    comp.AwakeInInactive();
                }
            }

            yield return null;


            if (awakableInInactives.Count == 0)
            {
                CO_CallAwakeForInactiveObj = null;
            }
            else
            {
                CO_CallAwakeForInactiveObj = (instanceForNonMono != null) ? instanceForNonMono.StartCoroutine(DO_CallAwakeForInactiveObj()) : null;
            }
        }

        protected override void OnInstanceAssigned()
        {
            instanceForNonMono = _Instance;

            if (CO_CallAwakeForInactiveObj != null) StopCoroutine(CO_CallAwakeForInactiveObj);
            CO_CallAwakeForInactiveObj = StartCoroutine(DO_CallAwakeForInactiveObj());

            _singletonInstanceComps.AddRange(_SingletonInstanceComps.ToArray());
            _SingletonInstanceComps.Clear();
            _allSingletonComps.AddRange(_AllSingletonComps.ToArray());
            _AllSingletonComps.Clear();
        }
    }
}
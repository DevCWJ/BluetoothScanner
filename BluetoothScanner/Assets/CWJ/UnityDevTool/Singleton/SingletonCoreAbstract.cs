using System;
using System.Linq;

using UnityEngine;

using CWJ.Singleton.OnlyUseNew;
using static CWJ.MonoBehaviourEventHelper;

namespace CWJ.Singleton.Core
{
    using static CWJ.FindUtil;

    public abstract class SingletonCoreAbstract<T> : SingletonCore where T : MonoBehaviour
    {

        #region Use these methods instead of original unity's magic-methods //I had to break the naming convention to declare it with a similar name. ('_')

        protected abstract override void _Reset();
        protected abstract override void _OnValidate();
        protected abstract override void _Awake();
        protected abstract override void _OnEnable();
        protected abstract override void _OnDisable();
        protected abstract override void _Start();
        protected abstract override void OnDispose();

        protected abstract override void _OnDestroy();
        protected abstract override void _OnApplicationQuit();
        #endregion Use these methods instead of original unity's magic-methods //I had to break the naming convention to declare it with a similar name. ('_')

        /// <summary>
        /// When just before assigning an Instance
        /// <para>CWJ.SingletonCore</para>
        /// </summary>
        protected virtual void OnJustBeforeInstanceAssigned() { }

        /// <summary>
        /// When after assigning an Instance
        /// <para>CWJ.SingletonCore</para>
        /// </summary>
        protected virtual void OnInstanceAssigned() { }

        #region SingletonBehav Interface
        protected static readonly System.Type TargetType = typeof(T);
        protected static readonly string TargetTypeName = TargetType.Name;
        protected static readonly Type[] TargetInterfaces = typeof(T).GetInterfaces();

        private static readonly bool _IsDontAutoCreatedWhenNull = TargetInterfaces.IsExists(typeof(IDontAutoCreatedWhenNull));
        private static readonly string DontAutoCreateWhenNullErrorMsg = $"{TargetTypeName} 스크립트는 \n자동생성이 불가능한 Singleton입니다.\n('{nameof(Singleton.IDontAutoCreatedWhenNull)}' 인터페이스가 상속됨)";
        public static bool IsDontAutoCreatedWhenNull => _IsDontAutoCreatedWhenNull;
        public override sealed bool isDontAutoCreatedWhenNull => _IsDontAutoCreatedWhenNull;


        private static readonly bool _IsDontPreCreatedInScene = TargetInterfaces.IsExists(typeof(IDontPrecreatedInScene));
        private static readonly string DontPreCreatedErrorMsg = $"{TargetTypeName} 스크립트는 \n씬에 미리 만들어둘 수 없는 Singleton입니다.\n실행중에만 호출 및 생성이가능합니다\n('{nameof(IDontPrecreatedInScene)}' 인터페이스가 상속됨)";
        public static bool IsDontPreCreatedInScene => _IsDontPreCreatedInScene;
        public override sealed bool isDontPreCreatedInScene => _IsDontPreCreatedInScene;

        private static readonly bool _IsDontGetInstanceInEditorMode = TargetInterfaces.IsExists(typeof(IDontGetInstanceInEditorMode));
        public static bool IsDontGetInstanceInEditorMode => _IsDontGetInstanceInEditorMode;
        public override sealed bool isDontGetInstanceInEditorMode => _IsDontGetInstanceInEditorMode;

        private static readonly bool _IsDontSaveInBuild = TargetInterfaces.IsExists(typeof(IDontSaveInBuild));
        public static bool IsDontSaveInBuild => _IsDontSaveInBuild;
        public override sealed bool isDontSaveInBuild => _IsDontSaveInBuild;

        #endregion

        /// <summary>
        /// DontDestroyOnLoad Singleton
        /// </summary>
        public override abstract bool isDontDestroyOnLoad { get; }

        /// <summary>
        /// OnlyUseNew Singleton
        /// </summary>
        public override abstract bool isOnlyUseNew { get; }


        private static bool IsDialogPopupEnabled = true;

        public static void UpdateInstance(bool isPrintLogOrPopup = true)
        {
            _UpdateInstance(isPrintLogOrPopup);
        }

        /// <summary>
        /// Call MySelf
        /// </summary>
        /// <param name="isPrintLogOrPopup"></param>
        protected static void _UpdateInstance(bool isPrintLogOrPopup = true)
        {
            if (HasInstance) return;

            IsDialogPopupEnabled = isPrintLogOrPopup;
            _Instance = GetInstance();
        }

        protected void UpdateInstanceForcibly()
        {
            if (!isOnlyUseNew) return;

            _Instance = GetInstance();
        }

        public static bool HasInstance => _instance != null;

        public static bool IsExists => (HasInstance || FindObjectsOfType_New<T>(includeInactive: true, includeDontDestroyOnLoadObjs: true).Length > 0);

        private bool _isInstance = false;
        public sealed override bool isInstance => _isInstance;
        public static string GameObjectName = null;

        private bool _isAutoCreated = false;
        public sealed override bool isAutoCreated => _isAutoCreated;
        private const string AutoCreatedNameTag = " (Created)";

        private static object lockObj = new object();

        /// <summary>
        /// _instance는 SingletonCoreGeneric 외에는 사용금지
        /// </summary>
        private static T _instance;

        protected static T _Instance
        {
            get => _instance;
            set
            {
                if (_instance != null)
                {
                    (_instance as SingletonCoreAbstract<T>)._isInstance = false;
                    SingletonHelper.RemoveSingletonInstanceElem(_instance);
                }

                if (value != null)
                {
                    (value as SingletonCoreAbstract<T>).OnJustBeforeInstanceAssigned();
                }

                _instance = value;

                if (value != null)
                {
                    var insCore = (value as SingletonCoreAbstract<T>);
                    insCore._isInstance = true;
                    GameObjectName = insCore.gameObject.name;
                    if (insCore.isDontDestroyOnLoad)
                    {
                        insCore.SetDontDestroyOnLoad();
                    }
                    insCore.OnInstanceAssigned();
                    SingletonHelper.AddSingletonInstanceElem(value);
                }
            }
        }

        /// <summary>
        /// <para>Instance 호출 시 null이면 자동으로 생성해서 할당시킴</para>
        /// <para>Instance를 호출하지않고 씬에서 <see cref="T"/> 존재유무만 알고싶으면 <see cref="IsExists"/> 사용할것</para>
        /// <para>Instance를 호출하지않고 Instance 할당여부 확인은 <see cref="HasInstance"/> </para>
        /// </summary>
        public static T Instance
        {
            get
            {
                CheckValidateForGetInstance();

                if (_instance == null)
                {
                    lock (lockObj)
                    {
                        _UpdateInstance();
                    }
                }

                return _instance;
            }
        }

        public static readonly System.Type[] IgnoreLogTypes = new Type[]
        {
            typeof(SingletonHelper),
            typeof(MonoBehaviourEventHelper),
            typeof(AccessibleEditor.DebugSetting.UnityDevConsoleVisible)
        };

        public static readonly bool IsIgnoreLogType = IgnoreLogTypes.IsExists(TargetType);
        protected static void CheckValidateForGetInstance()
        {
            if (MonoBehaviourEventHelper.IS_PLAYING)
            {
                if (IS_QUIT)
                {
                    throw new ObjectDisposedException(objectName: GameObjectName, message: $"{TargetTypeName} was called when the application is quitted or Scene is disabled or object is destroyed\nTo avoid error, add 'if ({nameof(SingletonHelper)}.{nameof(IS_QUIT)}) return;' codes in first line");
                    //^이게 불렸다면 OnDisable이나 OnDestroy내에서 Instance 가 불리는 곳이 있는거임 if(!Application.isPlaying) return; 처리해주기
                }
            }
#if UNITY_EDITOR
            else
            {
                if (_IsDontGetInstanceInEditorMode && !Editor_IsManagedByEditorScript)
                    throw new ObjectDisposedException(objectName: GameObjectName, message: $"{TargetTypeName} was called when editor mode\nTo avoid error, cancel the instance call or remove '{nameof(IDontGetInstanceInEditorMode)}' interface");
                //^이게 불렸다면 실행중이 아닐때 Instance를 호출했다는거임 (해결방법은 Editor모드에서 Instance 호출하는 코드를 제거하거나 ICannotCreateInEditorMode 인터페이스를 제거하거나)
            }
#endif
        }

        private static T GetInstance()
        {
            T[] findArray = FindObjectsOfType_New<T>(includeInactive: true, includeDontDestroyOnLoadObjs: true);

            if (findArray.Length == 0)
            {
                if (_IsDontAutoCreatedWhenNull
#if UNITY_EDITOR
                    && !Editor_IsManagedByEditorScript
#endif
                    )
                {
#if UNITY_EDITOR
                    if (!Editor_IsSilentlyCreateInstance)
#endif
                        typeof(SingletonCore).PrintLogWithClassName(DontAutoCreateWhenNullErrorMsg, LogType.Error, isBigFont: false, isPreventStackTrace: false);
                    return null;
                }

                if (!GetIsValidCreateObject())
                {
                    return null;
                }

                string newObjName = TargetTypeName;
                if (!IsIgnoreLogType)
                {
                    newObjName += AutoCreatedNameTag;
#if UNITY_EDITOR
                    if (!Editor_IsSilentlyCreateInstance)
#endif
                        typeof(SingletonCore).PrintLogWithClassName($"{(IS_PLAYING ? "플레이 중에" : "Editor 작업중에")} '{TargetTypeName}' 가 씬에 존재하지 않아서 생성시켰음", isPreventStackTrace: false);
                }
                GameObject instanceObj = new GameObject(newObjName, TargetType);
                T newIns = instanceObj.GetComponent<T>();
                var singleton = (newIns as SingletonCoreAbstract<T>);
                singleton._isAutoCreated = true;

                return newIns;
            }
            else if (findArray.Length == 1)
            {
                return findArray[0];
            }
            else //findArray.Length > 1
            {

                T returnIns = null;
                SingletonCoreAbstract<T> tmpElem = (findArray[0] as SingletonCoreAbstract<T>);

                System.Action<T> afterAssigned = null;
                if (!IsIgnoreLogType)
                {
                    string nameList = string.Join("\n", System.Array.ConvertAll(findArray, (f) => f.gameObject.scene.name + "/" + f.gameObject.name));
#if UNITY_EDITOR
                    if (!Editor_IsSilentlyCreateInstance)
#endif
                        afterAssigned = (ins) =>
                        {
                            typeof(SingletonCore).PrintLogWithClassName(
                            $"Singleton인 {TargetTypeName} 가 중복되게 존재함 " +
                            $"\n(총{findArray.Length}개 이며 {(tmpElem.isOnlyUseNew ? "새로 생성된" : "기존에 있던")} {ins.gameObject.scene.name}/{ins.gameObject.name}으로 Instance 할당됩니다" +
                            $"\n전체 오브젝트 리스트:\n{nameList}" +
                            $"\nInstance 외에는 모두 제거되었음.", isBigFont: false, isPreventStackTrace: false);
                        };
                }

                System.Action<T> othersDestroyCallback = null;

                if (tmpElem.isOnlyUseNew && 
                    (!tmpElem.isDontDestroyOnLoad || !(findArray[0] as SingletonBehaviourDontDestroy_OnlyUseNew<T>).isConfirmedInstance))
                {
                    othersDestroyCallback = DestroySingletonObj;
                }
                else
                {
                    othersDestroyCallback = (e) => DestroySingletonObj((e as SingletonCoreAbstract<T>).GetRootObj());
                }

                if (tmpElem.isOnlyUseNew)
                {
                    returnIns = findArray.Min((f) => f.GetInstanceID(), othersDestroyCallback);
                }
                else
                {
                    if (_instance == null)
                    {
                        returnIns = findArray.Max((f) => f.GetInstanceID(), othersDestroyCallback);
                    }
                    else
                    {
                        returnIns = _instance;

                        for (int i = 0; i < findArray.Length; ++i)
                        {
                            if (returnIns != findArray[i])
                                DestroySingletonObj(findArray[i].gameObject);
                        }
                    }
                }

                afterAssigned?.Invoke(returnIns);

                return returnIns;
            }
        }

        public void Ping(GameObject pingObj = null)
        {
#if UNITY_EDITOR
            if (pingObj == null) pingObj = this.gameObject;
            AccessibleEditor.AccessibleEditorUtil.PingObj(pingObj);
#endif
        }


#if UNITY_EDITOR
        [NonSerialized] bool editor_isChecked = false;
        protected void OnValidate()
        {
            if (!editor_isChecked
                && !Application.isPlaying && !Editor_IsManagedByEditorScript)
            {
                WillDestroyInterface(isOnValidate: true);
                editor_isChecked = true;
            }

            _OnValidate();
        }

        bool WillDestroyInterface(bool isOnValidate = false)
        {
            if (_IsDontPreCreatedInScene)
            {
                if(!Editor_IsManagedByEditorScript 
                    && (IsIgnoreLogType || CWJ.AccessibleEditor.DisplayDialogUtil.DisplayDialog<T>
                    (DontPreCreatedErrorMsg, ok: "Destroy now", cancel: "Wait! I'll destroy myself", logObj: gameObject)))
                {
                    if (isOnValidate)
                        CWJ.AccessibleEditor.EditorCallback.AddWaitForFrameCallback(() => DestroySingletonObj(GetRootObj()));
                    else
                    {
                        Debug.LogError(DontPreCreatedErrorMsg);
                        DestroyImmediate(this);
                    }
                    return true;
                }
            }
            else if (_IsDontSaveInBuild)
            {
                SetHideFlag(HideFlags.DontSaveInBuild);
            }
            return false;
        }

        protected void Reset() //에디터에서 && Component추가 시 실행됨
        {
            if (WillDestroyInterface())
            {
                return;
            }

            string message = $"싱글톤 {TargetTypeName}\n 컴포넌트 추가 시도 감지. [결과 :";
            bool isPopupEnabled = IsDialogPopupEnabled;
            IsDialogPopupEnabled = true;

            SingletonCoreAbstract<T>[] singletonRootArray = FindObjectsOfType_New<SingletonCoreAbstract<T>>(includeInactive: true, includeDontDestroyOnLoadObjs: true);

            if (singletonRootArray.Length > 1)
            {
                Predicate<SingletonCoreAbstract<T>> findOther = (i) => i != this;
                UnityEditor.Selection.objects = (from item in singletonRootArray
                                                 where findOther(item)
                                                 select item.gameObject).ToArray();
                UnityEditor.EditorGUIUtility.PingObject(UnityEditor.Selection.activeInstanceID);
                UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");

                message = "[ERROR] " + message + $" 추가 실패]\n\n현재 씬에 이미 존재하는 싱글톤입니다.\n컴포넌트 추가를 취소합니다\n(base:{nameof(SingletonCoreAbstract<T>)})";
                if (!Editor_IsSilentlyCreateInstance) CWJ.AccessibleEditor.DisplayDialogUtil.DisplayDialog<T>(message);
                DestroyImmediate(this);
                return;
            }

            message += " 추가 성공]\n\n(에디터에서 컴포넌트를 AddComponent 했거나, 런타임중이 아닐때 컴포넌트가 없는데 Instance를 호출한 경우 나오는 메세지입니다)";

            //if(GetComponents<Component>().Length <= 2 && transform.childCount==0)
            //{
            //    gameObject.name = TargetTypeName;
            //}

            if (isPopupEnabled)
            {
                if (!Editor_IsSilentlyCreateInstance) CWJ.AccessibleEditor.DisplayDialogUtil.DisplayDialog<T>(
                    message + $"\n\n({nameof(isDontDestroyOnLoad)}:{isDontDestroyOnLoad}\n{nameof(isOnlyUseNew)}:{isOnlyUseNew}\n{nameof(isDontAutoCreatedWhenNull)}:{_IsDontAutoCreatedWhenNull}\n{nameof(isDontPreCreatedInScene)}:{_IsDontPreCreatedInScene})");
            }


            string prevGameObjName = gameObject.name;

            GameObject go = gameObject;

            _Reset();

            if (this == null && go != null)
            {
                go.name = prevGameObjName;
            }
        }

        private void SetHideFlag(HideFlags setHideFlag)
        {
            if (this.hideFlags.HasFlag(setHideFlag))
            {
                return;
            }
            //if (gameObject.GetComponents<Component>().Length <= 2 && transform.childCount == 0)
            //{
            //    gameObject.hideFlags |= setHideFlag;
            //}
            this.hideFlags |= setHideFlag;
        }
#endif

        protected virtual void Awake()
        {
            SingletonHelper.AddSingletonAllElem(this);
        }

        protected void OnDisable()
        {
            _OnDisable();
            if (IS_QUIT) return;
        }

        protected override sealed void OnDestroy()
        {
            base.OnDestroy();

            if (IS_QUIT) return;

            SingletonHelper.RemoveSingletonAllElem(this);

            if (HasInstance && _instance == this)
                _Instance = null;
        }

        protected override sealed void OnApplicationQuit()
        {
            if (GameObjectName == null)
                GameObjectName = gameObject.name;
            base.OnApplicationQuit();
        }

        /// <summary>
        /// 새 instance생성을 막아야하는경우에 true를 return
        /// </summary>
        /// <returns></returns>
        protected bool IsPreventNewInstance()
        {
            if (isOnlyUseNew)
            {
                return false;
            }

            if (HasInstance && _instance != this)
            {
                DestroySingletonObj(GetRootObj());
                return true;
            }
            else
            {
                return false;
            }
        }

        protected UnityEngine.Object GetRootObj()
        {
            if (_isAutoCreated)
                return gameObject;
            else
            {
                if (ComponentUtil.IsGoHasOnlyThisCompWithRequireComps(this))
                    return gameObject;
                else
                    return this;
            }
        }

        protected static void DestroySingletonObj(UnityEngine.Object obj)
        {
            if (obj == null || obj is Transform) return;
            if (!GetIsValidCreateObject()) return;
            if (IS_PLAYING)
            {
                Destroy(obj);
            }
            else
            {
                //OnValidate에선 한프레임 대기후 실행해야함.
                DestroyImmediate(obj);
            }
        }

        /// <summary>
        /// DontDestroyOnLoad 스크립트에서만 씀
        /// </summary>
        protected void SetDontDestroyOnLoad()
        {
            if (!isDontDestroyOnLoad || !GetIsPlayingBeforeQuit()) return;
            if (!TargetTypeName.Equals(nameof(SingletonHelper)) && gameObject.IsDontDestroyOnLoad()) return;
            DontDestroyOnLoad(transform.root.gameObject);
            if (transform.root != transform)
            {
                typeof(SingletonCore).PrintLogWithClassName($"{TargetTypeName}때문에 '{transform.root.name}'(오브젝트이름)에 DontDestroyOnLoad를 실행했습니다", LogType.Log, false, obj: gameObject, isPreventStackTrace: false);
            }
        }

        protected void HideGameObject()
        {
            bool isEditorDebugMode =
#if CWJ_EDITOR_DEBUG_ENABLED
            true;
#else
            false;
#endif

            var addHideFlag = isEditorDebugMode ? UnityEngine.HideFlags.NotEditable : UnityEngine.HideFlags.HideInHierarchy;

            if (!gameObject.hideFlags.HasFlagOrEquals(addHideFlag))
            {
                gameObject.hideFlags |= addHideFlag;
            }
        }
    }
}
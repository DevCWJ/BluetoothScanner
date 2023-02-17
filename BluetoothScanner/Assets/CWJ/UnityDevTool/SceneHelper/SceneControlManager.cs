#if (CWJ_SCENEENUM_ENABLED || CWJ_SCENEENUM_DISABLED)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using CWJ.Singleton;
using CWJ.Serializable;
using CWJ.AccessibleEditor;
using UnityEngine.Events;

namespace CWJ.SceneHelper
{
    public interface ISceneEvent
    {
        void SceneEvent_Enable();
        void SceneEvent_Start();
        IEnumerator SceneEvent_LoadingWait();
        /// <summary>
        /// SceneEvent_LoadingWait 가 끝나면 isCompleteLoadingWait을 true로 만들어줄것
        /// </summary>
        bool isCompleteLoadingWait { get; }
    }
    [System.Serializable] public class SI_SceneEvent : InterfaceSerializable<ISceneEvent> { }

    public class SceneControlManager : SingletonBehaviourDontDestroy<SceneControlManager>
    {
        public string AwakeSceneName = 0.ToEnum<SceneEnum>().ToSceneName();

        [VisualizeField] public static bool IsTest;

        private static bool isLoading;

        /// <summary>
        /// 씬전환 완료 대기
        /// User리스트가 최신화 되기전까지 true
        /// </summary>
        [VisualizeProperty]
        public static bool IsLoading
        {
            get => isLoading;
            private set
            {
                if (isLoading == value) return;
                if (SingletonHelper.IS_QUIT) return;
                //로딩이미지 표시
                SceneLoadingManager.Instance.enabled = isLoading = value;
            }
        }

        /// <summary>
        /// 씬전환이 시작되기직전부터 ~ 씬전환 완료직후
        /// </summary>
        public bool isWhileSceneChanging;

        [Readonly] public bool isNeedSceneChangedSignExchange;
        public bool isLoadingSkip = false;

        [Readonly, SerializeField] private float sceneLoadingTime;

        [Readonly] public string CurSceneName = "";

        [Header("Scene Change")]

        [SerializeField, Readonly] private int sceneTypeEnumLength = 0;
        public int SceneTypeEnumLength
        {
            get
            {
                if (sceneTypeEnumLength == 0)
                {
                    sceneTypeEnumLength = Enum.GetNames(typeof(SceneEnum)).Length;
                }
                return sceneTypeEnumLength;
            }
        }
        [Readonly] public SceneEnum curSceneType;
        [Readonly] public SceneEnum nextSceneType;

        [SerializeField, Readonly] private int _sceneLengthInBuildSetting = 0;
        public int sceneLengthInBuildSetting
        {
            get
            {
                if (_sceneLengthInBuildSetting == 0)
                {
                    _sceneLengthInBuildSetting = SceneManager.sceneCountInBuildSettings;
                }

                return _sceneLengthInBuildSetting;
            }
        }

        [SerializeField, Readonly] private string[] _sceneNamesInBuildSetting;
        public string[] sceneNamesInBuildSetting
        {
            get
            {
                if (_sceneNamesInBuildSetting.LengthSafe() != sceneLengthInBuildSetting)
                {
                    _sceneNamesInBuildSetting = new string[sceneLengthInBuildSetting];
                    for (int i = 0; i < sceneLengthInBuildSetting; i++)
                    {
                        _sceneNamesInBuildSetting[i] = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
                    }
                }
                return _sceneNamesInBuildSetting;
            }
        }

        public SI_SceneEvent[] si_sceneEvents;

        public UnityEvent onEnableAfterSceneChangeEvent = new UnityEvent();
        public UnityEvent onStartAfterSceneChangeEvent = new UnityEvent();


        protected override void _Awake()
        {
            if (sceneLengthInBuildSetting != SceneTypeEnumLength)
            {
                //DisplayDialogUtil.DisplayDialog<SceneControlManager>("SceneType Enum수와 Build Settings의 Scene 수가 다름", isError: sceneLengthInBuildSetting < SceneTypeEnumLength);
            }

            List<string> wrongNames = new List<string>();
            for (int i = 0; i < SceneTypeEnumLength; i++)
            {
                string typeName = i.ToEnum<SceneEnum>().ToSceneName();
                for (int j = 0; j < sceneLengthInBuildSetting; j++)
                {
                    if (typeName.Equals(sceneNamesInBuildSetting[j]))
                    {
                        break;
                    }
                    else if (j == sceneLengthInBuildSetting - 1)
                    {
                        wrongNames.Add(typeName);
                    }
                }

            }

            if (wrongNames.Count > 0)
            {
                DisplayDialogUtil.DisplayDialog<SceneManager>($"SceneType Enum 이름중 Build Setting에 등록되어있지 않은 이름이 있습니다.\n({string.Join(",", wrongNames.ToArray())})", isError: true);
            }
        }

        #region 씬 전환 실행

        private bool CheckAccessableNextScene(string nextSceneName, bool isSceneChangeFinished = false)
        {
            bool isExistInBuildSetting = false;
            for (int i = 0; i < sceneNamesInBuildSetting.Length; i++)
            {
                if (string.Equals(nextSceneName, sceneNamesInBuildSetting[i]))
                {
                    isExistInBuildSetting = true;
                    break; //존재하는 씬. 씬전환가능
                }
            }
            if (!isExistInBuildSetting)
            {
                DisplayDialogUtil.DisplayDialog<SceneManager>($"Scene name({nextSceneName}) does not exist in 'Build Settings'", isError: true);
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExecuteMenuItem("File/Build Settings...");
#endif
            }

            bool isExistInEnum = false;
            for (int i = 0; i < SceneTypeEnumLength; i++)
            {
                if (string.Equals(((SceneEnum)i).ToString(), nextSceneName))
                {
                    if (isSceneChangeFinished)
                    {
                        curSceneType = nextSceneName.ToEnum<SceneEnum>();
                    }
                    isExistInEnum = true;
                    break;
                }
            }
            if (!isExistInEnum)
            {
                DisplayDialogUtil.DisplayDialog<SceneManager>($"Scene name({nextSceneName}) does not exist in Enum, {nameof(SceneEnum)}", isError: true);
            }

            return isExistInBuildSetting && isExistInEnum;
        }

        public void ChangeSceneDangerous(string _nextSceneName, bool isLoadingUI, bool isFakeLoading = false, bool isOverlapLoad = false, bool isForciblyChange = false)
        {
            if (!ArrayUtil.IsExists(sceneNamesInBuildSetting, _nextSceneName))
            {
                DisplayDialogUtil.DisplayDialog<SceneManager>($"Scene name({_nextSceneName}) does not exist in 'Build Settings'", isError: true);
                return;
            }

            if (CO_ChangeSceneWithLoading != null)
            {
                StopCoroutine(CO_ChangeSceneWithLoading);
            }

            SceneLoadingManager.Instance.gameObject.SetActive(isLoadingUI);

            CO_ChangeSceneWithLoading = StartCoroutine(DO_ChangeSceneWithLoading(_nextSceneName, isAsyncLoading: true, isFake: isFakeLoading, isForciblyChange: isForciblyChange));
        }

        public void ChangeScene(SceneEnum nextScene, bool isLoadingUI, bool isFakeLoading = false, bool onOverlapLoad = false, bool isForciblyChange = false)
        {
            string nextSceneName = nextScene.ToString();
            if (!onOverlapLoad && string.Equals(nextSceneName, curSceneType.ToString()))
            {
                Debug.LogError(string.Format("현재씬과 동일한 이름의 씬을 Load 시도하는중입니다 : {0}", nextSceneName));

                return; //onOverlapLoad이 false일땐 현재씬과 이름이 같으면 씬전환 불가능
            }

#if !CWJ_SCENEENUM_ENABLED
            if (!CheckAccessableNextScene(nextSceneName))
            {
                return;
            }
#endif
            if (CO_ChangeSceneWithLoading != null)
            {
                StopCoroutine(CO_ChangeSceneWithLoading);
            }

            this.nextSceneType = nextScene;

            SceneLoadingManager.Instance.gameObject.SetActive(isLoadingUI);

            CO_ChangeSceneWithLoading = StartCoroutine(DO_ChangeSceneWithLoading(nextSceneName, isAsyncLoading: true, isFake: isFakeLoading, isForciblyChange: isForciblyChange));
        }
#endregion 씬 전환 실행

#region 씬전환 작업(비동기)

        [Readonly] [SerializeField] private bool isFakeProgress;

        private const float LOADED_PERCENTAGE = 0.9f;
        private float[] step;
        private int stepLength;
        private int curStep;

        private float realProgress;

        private float fakeProgress;

        public float CurProgressValue
        {
            get
            {
                if (isFakeProgress)
                {
                    return fakeProgress;
                }
                else //realProgresss값 바뀌는때
                {
                    realProgress = (asyncOper != null) ? asyncOper.progress : 1;
                    SceneLoadingManager.Instance.SetProgressBar(realProgress);
                    return realProgress;
                }
            }

            private set
            {
                if (isFakeProgress) //fakeProgress값 바뀌는때
                {
                    fakeProgress = value;
                    SceneLoadingManager.Instance.SetProgressBar(fakeProgress);
                }
                else
                {
                    return;
                }
            }
        }

        private void BeforeSceneChange()
        {
        }

        private const int TOCK_Internal = 3;
        private AsyncOperation asyncOper = null;
        private Coroutine CO_ChangeSceneWithLoading;

        private IEnumerator DO_ChangeSceneWithLoading(string sceneName, bool isAsyncLoading = true, bool isFake = false, bool isForciblyChange = false)
        {
            if (!isForciblyChange && CO_SceneStartRoutine != null)
            {
                yield return new WaitWhile(() => CO_SceneStartRoutine != null);
            }

            BeforeSceneChange();
            isWhileSceneChanging = true;

            if (!isAsyncLoading)
            {
                SceneManager.LoadScene(sceneName);
                asyncOper = null;
                CO_ChangeSceneWithLoading = null;

                yield break;
            }

            isLoadingSkip = false;
            IsLoading = true;
            isFakeProgress = isFake;


            Debug.Log($"Start to scene change! ({sceneName})");
            asyncOper = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            asyncOper.allowSceneActivation = false;
            float lastTime = Time.realtimeSinceStartup;

            if (isFakeProgress)
            {
#region fake init

                CurProgressValue = 0;
                stepLength = UnityEngine.Random.Range(4, 7 + 1);
                step = new float[stepLength];
                float offset = 1.0f / stepLength;

                for (int i = 0; i < stepLength; i++)
                {
                    step[i] = UnityEngine.Random.Range(offset * i, offset * (i + 1));
                }
                curStep = 0;

#endregion fake init

#region fake progress loop

                do
                {
                    //if (!CurSceneName.Equals(AwakeSceneName))
                    //{
                    //    if (frameCount >= TOCK_Internal)
                    //    {
                    //        //tic-tock 보내는 타이밍
                    //        frameCount = 0;
                    //    }
                    //    else
                    //    {
                    //        frameCount++;
                    //    }
                    //}

                    if (curStep < stepLength - 1)
                    {
                        CurProgressValue = asyncOper.progress;

                        if (CurProgressValue > step[curStep])
                        {
                            CurProgressValue = step[curStep];
                        }
                        curStep++;
                    }
                    else
                    {
                        WaitUntil asyncProgressUpper = new WaitUntil(() => asyncOper.progress >= LOADED_PERCENTAGE);
                        if (!asyncOper.allowSceneActivation)
                        {
                            yield return asyncProgressUpper;
                            CurProgressValue = step[stepLength - 1] > LOADED_PERCENTAGE ? step[stepLength - 1] : asyncOper.progress;
                            CurProgressValue = 1;
                        }
                    }
                    Debug.Log(curStep + " (fake)loading..." + CurProgressValue);
                    Debug.Log("(real)loading..." + asyncOper.progress);
                    if (CurProgressValue == 1)
                    {
                        yield return null;
                        asyncOper.allowSceneActivation = true;
                    }
                    yield return null;
                } while (!asyncOper.isDone);

#endregion fake progress loop
            }
            else
            {
#region real progress loop

                do
                {
                    asyncOper.allowSceneActivation = (CurProgressValue >= LOADED_PERCENTAGE);
                    yield return null;
                }
                while (!asyncOper.isDone);

#endregion real progress loop
            }

            sceneLoadingTime = Time.realtimeSinceStartup - lastTime;
            Debug.Log("Scene change completed. " + sceneName + "\nloading time : " + sceneLoadingTime);
            asyncOper = null;
            CO_ChangeSceneWithLoading = null;
        }
#endregion

#region 씬전환 직후 프로세스

        protected override void _OnEnable()
        {
            SceneManager.sceneLoaded += SceneFinishedLoading;
        }

        protected override void _OnDisable()
        {
            SceneManager.sceneLoaded -= SceneFinishedLoading;
        }

        private void SceneFinishedLoading(Scene scene, LoadSceneMode mode)
        {
            CurSceneName = scene.name;

            Debug.Log($"Scene change completed! ({CurSceneName})");

            //if (!CheckAccessableNextScene(CurSceneName, true))
            //{
            //    return;
            //} //->ChangeScene에서 함

            if (CO_ChangeSceneWithLoading != null)
            {
                StopCoroutine(CO_ChangeSceneWithLoading);
            }

            si_sceneEvents = SerializableInterfaceUtil.FindSerializableInterfaces<ISceneEvent, SI_SceneEvent>(includeInactive: true, includeDontDestroyOnLoadObjs: true);

            if (si_sceneEvents.Length > 0)
            {
                //브금바꾸는타이밍

                foreach (var item in si_sceneEvents)
                {
                    item.Interface.SceneEvent_Enable();
                }
            }
            onEnableAfterSceneChangeEvent?.Invoke();

            CO_SceneStartRoutine = StartCoroutine(DO_SceneStartRoutine());

            isWhileSceneChanging = false;
        }

        public Coroutine CO_SceneStartRoutine = null;

        private IEnumerator DO_SceneStartRoutine()
        {
            yield return null;

            onStartAfterSceneChangeEvent?.Invoke();

            if (si_sceneEvents.Length > 0)
            {
                foreach (var item in si_sceneEvents)
                {
                    item.Interface.SceneEvent_Start();
                }

                yield return null;

                foreach (var item in si_sceneEvents)
                {
                    StartCoroutine(item.Interface.SceneEvent_LoadingWait());
                }

                foreach (var item in si_sceneEvents)
                {
                    yield return new WaitUntil(() => isLoadingSkip || item.Interface.isCompleteLoadingWait);
                }
            }

            isLoadingSkip = false;
            IsLoading = false;

            CO_SceneStartRoutine = null;
        }

#endregion 씬전환 직후 프로세스
    }
}

#endif
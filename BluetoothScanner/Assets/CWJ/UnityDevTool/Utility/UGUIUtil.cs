using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CWJ
{
    public static class UGUIUtil
    {
        /// <summary>
        /// 간혹가다가 사용하는게 아니라면 직접 코드 작성하는게 성능상에서 유리 (ped와 results를 만들어놓는 차이)
        /// </summary>
        /// <param name="mousePosition"></param>
        /// <returns></returns>
        public static RaycastResult[] GetPointerOverUIRayResults(this Vector2 mousePosition)
        {
            PointerEventData ped = new PointerEventData(EventSystem.current);

            ped.position = mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();

            EventSystem.current.RaycastAll(ped, results);

            return results.ToArray();
        }

        public static bool FindLastRaycast(this List<RaycastResult> candidates, out int lastIndex, GameObject ignoreObj = null)
        {
            int cnt = candidates.Count;
            for (var i = cnt - 1; i >= 0; --i)
            {
                if (candidates[i].gameObject.IsNullOrMissing() || (ignoreObj != null && candidates[i].gameObject == ignoreObj))
                    continue;
                lastIndex = i;
                return true;
            }
            lastIndex = -1;
            return false;
        }


        /// <summary>
        /// availableTime : longPressEvent 시작 시간
        /// </summary>
        /// <param name="button"></param>
        /// <param name="unityAction"></param>
        /// <param name="availableTime"></param>
        public static void AddLongPressEvent(this Button button, UnityEngine.Events.UnityAction unityAction, float availableTime = 0.5f)
        {
            var longPressEventSystem = button.targetGraphic.gameObject.GetOrAddComponent<CWJ.UI.LongPressEventSystem>();
            longPressEventSystem.Constructor(button, false, unityAction, availableTime);
        }

        public static void RemoveLongPressEvent(this Button button, UnityEngine.Events.UnityAction unityAction)
        {
            var longPressEventSystem = button.targetGraphic.GetComponent<CWJ.UI.LongPressEventSystem>();
            if (longPressEventSystem == null) return;
            longPressEventSystem.longPressEvent.RemoveListener_New(unityAction);
        }

        public static void AddLongTogglePressEvent(this Toggle toggle, UnityEngine.Events.UnityAction unityAction, float availableTime = 0.5f)
        {
            var longPressEventSystem = toggle.targetGraphic.gameObject.GetOrAddComponent<CWJ.UI.LongPressEventSystem>();
            longPressEventSystem.Constructor(toggle, false, unityAction, availableTime);
        }

        public static void RemoveLongTogglePressEvent(this Toggle toggle, UnityEngine.Events.UnityAction unityAction)
        {
            var longPressEventSystem = toggle.targetGraphic.GetComponent<CWJ.UI.LongPressEventSystem>();
            if (longPressEventSystem == null) return;
            longPressEventSystem.longPressEvent.RemoveListener_New(unityAction);
        }

        /// <summary>
        /// loopInterval : longPressLoopEvent 반복실행 간격
        /// </summary>
        /// <param name="button"></param>
        /// <param name="unityAction"></param>
        /// <param name="availableTime"></param>
        public static void AddLongPressLoopEvent(this Button button, UnityEngine.Events.UnityAction unityAction, float availableTime = 0.5f, float loopInterval = 0, bool isOnClickEventSame = false)
        {
            var longPressEventSystem = button.targetGraphic.gameObject.GetOrAddComponent<CWJ.UI.LongPressEventSystem>();
            longPressEventSystem.Constructor(button, unityAction, availableTime: availableTime, loopInterval: loopInterval);

            if (isOnClickEventSame)
            {
                button.onClick.AddListener_New(unityAction);
            }
        }

        public static void RemoveLongPressLoopEvent(this Button button, UnityEngine.Events.UnityAction unityAction)
        {
            var longPressEventSystem = button.targetGraphic.GetComponent<CWJ.UI.LongPressEventSystem>();
            if (longPressEventSystem == null) return;
            longPressEventSystem.longPressLoopEvent.RemoveListener_New(unityAction);
        }

        /// <summary>
        /// 꾹 눌렀다가 떼면 Invoke
        /// </summary>
        /// <param name="button"></param>
        /// <param name="unityAction"></param>
        /// <param name="availableTime"></param>
        public static void AddLongPressUpEvent(this Button button, UnityEngine.Events.UnityAction unityAction, float availableTime = 0, bool isOnClickEventSame = false)
        {
            var longPressEventSystem = button.targetGraphic.gameObject.GetOrAddComponent<CWJ.UI.LongPressEventSystem>();
            longPressEventSystem.Constructor(button, true, unityAction, availableTime);

            if (isOnClickEventSame)
            {
                button.onClick.AddListener_New(unityAction);
            }
        }

        public static void RemoveLongPressUpEvent(this Button button, UnityEngine.Events.UnityAction unityAction)
        {
            var longPressEventSystem = button.targetGraphic.GetComponent<CWJ.UI.LongPressEventSystem>();
            if (longPressEventSystem == null) return;
            longPressEventSystem.longPressUpEvent.RemoveListener_New(unityAction);
        }

        /// <summary>
        /// availableTime : doubleClickEvent 유효 시간
        /// </summary>
        /// <param name="button"></param>
        /// <param name="unityAction"></param>
        /// <param name="availableTime"></param>
        public static void AddDoubleClickEvent(this Button button, UnityEngine.Events.UnityAction unityAction, float availableTime = 0)
        {
            var doubleClickEventSystem = button.targetGraphic.gameObject.GetOrAddComponent<CWJ.UI.DoubleClickEventSystem>();
            doubleClickEventSystem.Constructor(button, unityAction, availableTime);
        }

        /// <summary>
        /// availableTime : doubleClickEvent 유효 시간
        /// </summary>
        /// <param name="button"></param>
        /// <param name="unityAction"></param>
        /// <param name="availableTime"></param>
        public static void RemoveDoubleClickEvent(this Button button, UnityEngine.Events.UnityAction unityAction)
        {
            var doubleClickEventSystem = button.targetGraphic.GetComponent<CWJ.UI.DoubleClickEventSystem>();
            if (doubleClickEventSystem == null) return;
            doubleClickEventSystem.doubleClickEvent.RemoveListener_New(unityAction);
        }

        public enum ElementAlign
        {
            Top,
            Middle,
            Bottom
        }
        /// <summary>
        /// 타겟이 보이도록 스크롤을 타겟 위치까지 이동시키는 기능
        /// </summary>
        /// <param name="canvasRectTrf"></param>
        /// <param name="scrollRect"></param>
        /// <param name="targetRectTrf"></param>
        /// <param name="elementAlign"></param>
        public static void ScrollMoveToElement(RectTransform canvasRectTrf, ScrollRect scrollRect, RectTransform targetRectTrf, ElementAlign elementAlign)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.velocity = Vector2.zero;

            Vector2 contentRectPos = canvasRectTrf.InverseTransformPoint(scrollRect.content.position);
            Vector2 targetRectPos = canvasRectTrf.InverseTransformPoint(targetRectTrf.position);

            float contentHeight = scrollRect.content.rect.height;
            float targetHeight = targetRectTrf.rect.height * 0.6f; //0.5 + 여백 0.1
            float viewHeight = scrollRect.viewport.rect.height;

            float offset = contentRectPos.y - (targetRectPos.y + targetHeight);

            float integral = contentHeight;

            if (elementAlign.Equals(ElementAlign.Top))
            {
                integral -= viewHeight;
            }
            else if (elementAlign.Equals(ElementAlign.Bottom))
            {
                integral -= viewHeight;
                offset -= viewHeight - (targetHeight * 2);
            }

            scrollRect.verticalNormalizedPosition = 1 - Mathf.Clamp01(offset / integral);

            Canvas.ForceUpdateCanvases();
        }
    }
}

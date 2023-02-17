using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace CWJ.UI
{
    [DisallowMultipleComponent]
    public class LongPressEventSystem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// loop
        /// </summary>
        /// <param name="selectable"></param>
        /// <param name="longPressLoopAction"></param>
        /// <param name="availableTime"></param>
        /// <param name="loopInterval"></param>
        public void Constructor(UnityEngine.UI.Selectable selectable, UnityEngine.Events.UnityAction longPressLoopAction, float availableTime, float loopInterval)
        {
            this.selectable = selectable;
            longPressLoopEvent.AddListener_New(longPressLoopAction);
            this.availableTime = availableTime;
            this.loopInterval = loopInterval;
        }

        /// <summary>
        /// up or down
        /// </summary>
        /// <param name="selectable"></param>
        /// <param name="isUp"></param>
        /// <param name="longPressAction"></param>
        /// <param name="availableTime"></param>
        public void Constructor(UnityEngine.UI.Selectable selectable, bool isUp, UnityEngine.Events.UnityAction longPressAction, float availableTime)
        {
            this.selectable = selectable;
            if (isUp) longPressUpEvent.AddListener_New(longPressAction);
            else longPressEvent.AddListener_New(longPressAction);
            this.availableTime = availableTime;
        }

        public UnityEngine.UI.Selectable selectable;

        public bool Interactable => selectable.interactable;

        public float availableTime = .0f;
        public UnityEvent longPressEvent = new UnityEvent();

        public float loopInterval = .0f;
        public UnityEvent longPressLoopEvent = new UnityEvent();

        public bool isLongPressed { get; private set; }
        public UnityEvent longPressUpEvent = new UnityEvent();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Interactable)
                StartCheckLongPress();
        }

        private void StartCheckLongPress()
        {
            StopCheckLongPress();

            CO_CheckLongPress = StartCoroutine(DO_CheckLongPress());
        }

        public void StopCheckLongPress()
        {
            if (CO_CheckLongPress != null)
            {
                StopCoroutine(CO_CheckLongPress);
                CO_CheckLongPress = null;
            }
            if (isLongPressed)
            {
                longPressUpEvent?.Invoke();
                isLongPressed = false;
            }
        }

        private Coroutine CO_CheckLongPress = null;

        private IEnumerator DO_CheckLongPress()
        {
            yield return null;
            yield return new WaitForSeconds(availableTime);

            if (!Interactable)
            {
                StopCheckLongPress();
                yield break;
            }

            longPressEvent?.Invoke();
            isLongPressed = true;

            WaitForSeconds waitForInterval = new WaitForSeconds(loopInterval);

            do
            {
                if (loopInterval == 0)
                {
                    yield return null;
                }
                else
                {
                    yield return waitForInterval;
                }

                if (!Interactable) break;

                longPressLoopEvent?.Invoke();

            } while (CO_CheckLongPress != null);

            StopCheckLongPress();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            StopCheckLongPress();
        }
    }
}
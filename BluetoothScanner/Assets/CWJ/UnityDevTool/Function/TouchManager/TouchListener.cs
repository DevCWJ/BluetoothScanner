using UnityEngine;
using UnityEngine.Events;

namespace CWJ
{
    [AddComponentMenu("Scripts/" + nameof(CWJ) + "/CWJ_" + nameof(TouchListener))]
    public class TouchListener : MonoBehaviour
    {
        [Tooltip("mobile only\n멀티 터치 전용 이벤트일 시 체크")]
        [ReadonlyConditional(EPlayMode.PlayMode)] public bool isMultiTouchOnly;

        public UnityEvent[] touchEvents { get; private set; } = null;

        [Space]
        public UnityEvent onTouchBegan = new UnityEvent();//터치 시작 이벤트

        public UnityEvent onTouchMoved = new UnityEvent(); //홀드 터치 움직임 이벤트
        public UnityEvent onTouchStationary = new UnityEvent(); //홀드 터치 고정 이벤트
        public UnityEvent onTouchEnded = new UnityEvent(); //터치 종료 이벤트

        [Header("터치인식 수가 너무 많을때 실행됨")]
        public UnityEvent onTouchCanceled = new UnityEvent(); //터치 취소됨 이벤트, 모바일전용

        [Space]
        public UnityEvent onUpdateEnded = new UnityEvent(); //업데이트 마지막 실행 이벤트

        private void Awake()
        {
            touchEvents = new UnityEvent[5];
            touchEvents[0] = onTouchBegan;
            touchEvents[1] = onTouchMoved;
            touchEvents[2] = onTouchStationary;
            touchEvents[3] = onTouchEnded;
            touchEvents[4] = onTouchCanceled;
        }

        private void OnEnable()
        {
            TouchManager.Instance?.AddTouchListener(this);
            Debug.LogWarning($"{ nameof(TouchManager) }에 { nameof(TouchListener) } 추가");
        }

        private void OnDisable()
        {
            if (MonoBehaviourEventHelper.IS_QUIT) return;
            TouchManager.Instance?.RemoveTouchListener(this);
            Debug.LogWarning($"{ nameof(TouchManager) }에 { nameof(TouchListener) } 제거");
        }
    }
}
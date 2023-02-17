using System;

namespace CWJ
{
    /// <summary>
    /// Inspector에 함수를 실행하는 버튼을 만들어줌
    /// <para>void 함수, Coroutine, 리턴타입(ValueType, Tuple, class, Struct) 함수, 선택적 매개변수를 가진 함수 등 테스트완료. (안되는 경우가 있을시 연락바람)</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field)]
    public class InvokeButtonAttribute : Attribute
    {
        public readonly string aboveFieldName;
        public readonly string onMarkedBoolName;
        public readonly bool isNeedUndo;
        public readonly bool isOnlyButton;
        public readonly string displayName;
        public readonly string tooltip;

        /// <summary>
        /// <param name="onMarkedBoolName"></param>
        /// </summary>
        /// <param name="onMarkedBoolName">이 이름을 가진 bool변수(field/property)를 true로 만듬 (코드를 통해 실행되었을때와 InvokeButtonAttribute를 통해 실행되었을때에 대한 차이점을 만들기위해)</param>
        /// <param name="isOnlyButton"><see langword="true"/>: foldout으로 숨겨져있지않고 버튼만 표시함.(매개변수가 없어야함)</param>
        public InvokeButtonAttribute(string aboveFieldName = null, string onMarkedBoolName = null, bool isNeedUndo = false, bool isOnlyButton = true, string displayName = null, string tooltip = null)
        {
            this.aboveFieldName = aboveFieldName;
            this.onMarkedBoolName = onMarkedBoolName;
            this.isNeedUndo = isNeedUndo;
            this.isOnlyButton = isOnlyButton;
            this.displayName = displayName;
            this.tooltip = tooltip;
        }
    }
}
#if UNITY_2020_1_OR_NEWER && UNITY_EDITOR // #if UNITY_2020_1_OR_NEWER && UNITY_EDITOR 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEditor;

using CWJ.AccessibleEditor;

namespace CWJ.Serializable
{
    using static DictionaryPropertyDrawerCore;

    /// <summary>
    /// for unity 2020
    /// </summary>
    [CustomPropertyDrawer(typeof(DictionaryVisualized<,>))]
    public class VisualizedDictionaryPropertyDrawer : PropertyDrawer
    {
        static readonly float combinedPadding = lineHeight + standardVertSpace;

        public const string FieldName_keyCollision = "keyCollision";

        bool isVisible = true;
        private Action<int> buttonCallback = null;
        private int buttonActionIndex;
        EditorGUI_CWJ.MemberAttributeCache attributeCache;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent headerLabel)
        {
            if(!attributeCache.isInit)
            {
                attributeCache = new EditorGUI_CWJ.MemberAttributeCache(fieldInfo, fieldInfo.FieldType, property.GetTargetObject(), null, false);
            }
            var visualizeInfo = attributeCache.GetVisualizeState(false);
            isVisible = visualizeInfo.isVisible;
            if (!isVisible) return;

            headerLabel = EditorGUI.BeginProperty(position, headerLabel, property);
            var headerPos = position;
            headerPos.height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
                headerPos.xMax -= s_buttonStyle.CalcSize(s_iconPlus).x;

            EditorGUI.PropertyField(headerPos, property, headerLabel, false);

            if (property.isExpanded)
            {
                bool isReadonly = visualizeInfo.isReadonly || fieldInfo.IsDefined(typeof(CWJ.ReadonlyAttribute), true);
                SerializedProperty keyValueListProp = property.FindPropertyRelative(FieldName_keyValues); 

                int keyValueCnt = keyValueListProp.GetSafelyLength();

                int[] nullKeyIndexes = ConvertIntArray(property.FindPropertyRelative(FieldName_NullKeyIndexes));
                int nullKeyLength = nullKeyIndexes.LengthSafe();

                int[] conflictOriginIndexes = ConvertIntArray(property.FindPropertyRelative(FieldName_ConflictOriginKeyIndexes));
                int conflictOriginLength = conflictOriginIndexes.LengthSafe();

                int[] conflictWarningIndexes = ConvertIntArray(property.FindPropertyRelative(FieldName_ConflictWarningKeyIndexes));
                int conflictWarningLength = conflictWarningIndexes.LengthSafe();
                Action<int, Rect> drawConflictInfo = null;

                if (conflictOriginLength > 0 || nullKeyLength > 0)
                {
                    drawConflictInfo = (itemIndex, lineRect) =>
                    {
                        Rect iconRect = lineRect;
                        GUIContent icon = null;

                        if (conflictOriginIndexes.IsExists(itemIndex))
                        {
                            icon = s_warningIcon_Origin;
                        }
                        else if (conflictWarningIndexes.IsExists(itemIndex))
                        {
                            icon = s_warningIcon_Conflict;
                        }
                        else if (nullKeyIndexes.IsExists(itemIndex))
                        {
                            icon = s_warningIcon_Null;
                        }

                        if (icon != null)
                        {
                            iconRect.size = s_buttonStyle.CalcSize(icon);
                            GUI.Label(iconRect, icon);
                        }
                    };
                }

                var buttonPosition = position;
                buttonPosition.xMin = buttonPosition.xMax - ButtonWidth;
                buttonPosition.height = EditorGUIUtility.singleLineHeight;

                using (new EditorGUI.DisabledScope(conflictWarningLength > 0 || nullKeyLength > 0 || isReadonly))
                {
                    if (GUI.Button(buttonPosition, s_iconPlus, s_buttonStyle))
                    {
                        buttonCallback += keyValueListProp.InsertArrayElementAtIndex;
                        buttonActionIndex = keyValueCnt;
                    }
                }

                EditorGUI.indentLevel++;
                var linePosition = position;
                linePosition.y += EditorGUIUtility.singleLineHeight;
                linePosition.xMax -= ButtonWidth;
                 
                foreach (var keyValue in GetKeyValueEnumerable(keyValueListProp)) //GetKeyValueEnumerable함수를 통해 배열의 모든 KeyValuePropStruct 값을 가져옴
                { 
                    var keyProperty = keyValue.keyProperty;
                    var valueProperty = keyValue.valueProperty;
                    int i = keyValue.index;

                    float lineHeight = DrawKeyValueLine(keyProperty, valueProperty, linePosition, i);

                    buttonPosition = linePosition;
                    buttonPosition.xMin = buttonPosition.xMax - ButtonWidth;
                    buttonPosition.width = ButtonWidth;
                    buttonPosition.x += ButtonWidth;
                    buttonPosition.height = EditorGUIUtility.singleLineHeight;

                    using (new EditorGUI.DisabledScope(isReadonly))
                    {
                        if (GUI.Button(buttonPosition, s_iconMinus, s_buttonStyle))
                        {
                            buttonCallback += (actionIndex) => DeleteArrayElementAtIndex(keyValueListProp, actionIndex);
                            buttonActionIndex = i;
                        }
                    }

                    drawConflictInfo?.Invoke(i, linePosition);

                    linePosition.y += lineHeight;
                }

                if (buttonCallback != null)
                {
                    EditorGUI_CWJ.RemoveFocusFromText();
                    buttonCallback.Invoke(buttonActionIndex);
                    buttonCallback = null;
                }

                EditorGUI.indentLevel--;
            }



            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float propertyHeight = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                var keyValueListProperty = property.FindPropertyRelative(FieldName_keyValues);

                foreach (var keyValue in GetKeyValueEnumerable(keyValueListProperty))
                {
                    var keyProperty = keyValue.keyProperty;
                    var valueProperty = keyValue.valueProperty;
                    float keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
                    float valuePropertyHeight = valueProperty != null ? EditorGUI.GetPropertyHeight(valueProperty) : 0f;
                    float lineHeight = Mathf.Max(keyPropertyHeight, valuePropertyHeight);
                    propertyHeight += lineHeight;
                }

                var conflictState = GetConflictState(property);

                if (conflictState.conflictIndex != -1)
                {
                    propertyHeight += conflictState.conflictLineHeight;
                }
            }

            return propertyHeight;
        }
    }
}
#endif
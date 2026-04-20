using UnityEngine;
    public static class FsmGizmoHelper
    {
        public static void DrawStateLabel(Transform transform, string text, Color color, float height = 2f, FontStyle fontStyle = FontStyle.Bold)
        {
            #if UNITY_EDITOR
            if (transform == null || string.IsNullOrEmpty(text))
                return;

            var style = new GUIStyle(UnityEditor.EditorStyles.boldLabel)
            {
                normal = { textColor = color },
                fontStyle = fontStyle
            };

            UnityEditor.Handles.Label(transform.position + Vector3.up * height, text, style);
            #endif
        }
}

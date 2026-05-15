#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PivotToBottomTool : EditorWindow
{
    [MenuItem("Tools/Set Pivot to Bottom")]
    static void SetPivotToBottom()
    {
        var targets = Selection.gameObjects;

        Selection.activeGameObject = null;

        foreach (var go in targets)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            // 월드 bounds 계산
            var bounds = renderers[0].bounds;
            foreach (var r in renderers)
                bounds.Encapsulate(r.bounds);

            // 상위 오브젝트 생성
            var pivot = new GameObject(go.name);
            pivot.transform.SetParent(go.transform.parent);
            pivot.transform.position = go.transform.position;
            pivot.transform.rotation = go.transform.rotation;
            pivot.transform.localScale = Vector3.one;

            // mesh 오브젝트를 pivot 아래로 이동
            Undo.SetTransformParent(go.transform, pivot.transform, "Set Pivot to Bottom");
            go.name = go.name + "_mesh";

            // mesh 오브젝트를 offset만큼 올려서 pivot이 최하단에 오도록
            go.transform.position += new Vector3(0, bounds.size.y / 2, 0);

            Undo.RegisterCreatedObjectUndo(pivot, "Set Pivot to Bottom");
        }
    }
}
#endif
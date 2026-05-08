// 저장 위치: Assets/Editor/TextNodeChainEditor.cs

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextNodeChainEditor : EditorWindow
{
    // ─── 상태 ────────────────────────────────────────────────────────────────
    private string _savePath = "Assets/Scripts/Domain/World/SOs/TextChain";
    private List<NodeEntry> _nodes = new List<NodeEntry>();
    private Vector2 _scroll;
    private TextNode _loadedHead = null;

    // ─── NodeEntry ────────────────────────────────────────────────────────────
    private class NodeEntry
    {
        public string fileName = "TextNode";
        public string text = "";
        public TextNode asset = null;

        // override 여부 토글
        public bool usePrevOverride = false;
        public bool useNextOverride = false;

        // 실제 오버라이드 대상 (ObjectField)
        public TextNode overridePrev = null;
        public TextNode overrideNext = null;
    }

    // ─── 열기 ────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TextNode Chain Editor")]
    public static void Open()
    {
        var win = GetWindow<TextNodeChainEditor>("TextNode Chain Editor");
        win.minSize = new Vector2(520, 420);
    }

    // ─── GUI 루트 ─────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        DrawToolbar();
        DrawPathField();
        EditorGUILayout.Space(6);
        DrawNodeList();
        EditorGUILayout.Space(6);
        DrawBottomButtons();
    }

    // ── 툴바 ──────────────────────────────────────────────────────────────────
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("TextNode Chain Editor", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60))) SaveChain();
        if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(60))) LoadChain();
        if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(60))) NewChain();

        EditorGUILayout.EndHorizontal();
    }

    // ── 경로 필드 ─────────────────────────────────────────────────────────────
    private void DrawPathField()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Save Path");
        _savePath = EditorGUILayout.TextField(_savePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string chosen = EditorUtility.OpenFolderPanel("저장 폴더 선택", Application.dataPath, "");
            if (!string.IsNullOrEmpty(chosen))
            {
                _savePath = chosen.StartsWith(Application.dataPath)
                    ? "Assets" + chosen.Substring(Application.dataPath.Length)
                    : chosen;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    // ── 노드 리스트 ───────────────────────────────────────────────────────────
    private void DrawNodeList()
    {
        EditorGUILayout.LabelField("Nodes  (index 0 = Head)", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        for (int i = 0; i < _nodes.Count; i++)
        {
            if (DrawNodeEntry(i)) break; // 삭제/이동 시 루프 탈출
        }

        EditorGUILayout.EndScrollView();
    }

    // 반환값 true = 리스트 변경됨 (루프 중단 필요)
    private bool DrawNodeEntry(int i)
    {
        var node = _nodes[i];
        bool isHead = (i == 0);

        var bgColor = isHead
            ? new Color(0.3f, 0.55f, 0.3f, 0.28f)
            : new Color(0.25f, 0.35f, 0.5f, 0.18f);

        var rect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(20));
        EditorGUI.DrawRect(rect, bgColor);

        // ── 헤더 행 ──────────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();

        string label = isHead ? $"[{i}] HEAD" : $"[{i}]";
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField("FileName:", GUILayout.Width(60));
        node.fileName = EditorGUILayout.TextField(node.fileName, GUILayout.Width(150));

        GUILayout.FlexibleSpace();

        // ▲ ▼
        GUI.enabled = i > 0;
        if (GUILayout.Button("▲", GUILayout.Width(24)))
        {
            (_nodes[i], _nodes[i - 1]) = (_nodes[i - 1], _nodes[i]);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        GUI.enabled = i < _nodes.Count - 1;
        if (GUILayout.Button("▼", GUILayout.Width(24)))
        {
            (_nodes[i], _nodes[i + 1]) = (_nodes[i + 1], _nodes[i]);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        GUI.enabled = true;

        // ✕
        var oldCol = GUI.color;
        GUI.color = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("✕", GUILayout.Width(24)))
        {
            _nodes.RemoveAt(i);
            GUI.color = oldCol;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        GUI.color = oldCol;

        EditorGUILayout.EndHorizontal();

        // ── 텍스트 ───────────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Text:", GUILayout.Width(40));
        node.text = EditorGUILayout.TextArea(node.text, GUILayout.MinHeight(36), GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        // ── prev / next 행 ───────────────────────────────────────────────────
        DrawLinkRow(node, isPrev: true, autoIndex: i - 1);
        DrawLinkRow(node, isPrev: false, autoIndex: i + 1);

        EditorGUILayout.Space(4);
        EditorGUILayout.EndVertical();
        return false;
    }

    // ── prev / next 한 행 ────────────────────────────────────────────────────
    private void DrawLinkRow(NodeEntry node, bool isPrev, int autoIndex)
    {
        bool valid = autoIndex >= 0 && autoIndex < _nodes.Count;
        string autoLabel = valid ? _nodes[autoIndex].fileName : "null";
        string linkLabel = isPrev ? "prev" : "next";

        // ref 대신 로컬 변수로 읽고 다시 기록 (C# ref-to-field 제약 우회)
        bool useOverride = isPrev ? node.usePrevOverride : node.useNextOverride;
        TextNode overrideVal = isPrev ? node.overridePrev : node.overrideNext;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(linkLabel + ":", GUILayout.Width(34));

        // override 토글
        bool newUse = EditorGUILayout.ToggleLeft("override", useOverride, GUILayout.Width(78));
        if (newUse != useOverride)
        {
            useOverride = newUse;
            if (!useOverride) overrideVal = null; // override 해제 시 초기화
        }

        if (useOverride)
        {
            // ObjectField로 직접 지정
            overrideVal = (TextNode)EditorGUILayout.ObjectField(
                overrideVal, typeof(TextNode), false, GUILayout.ExpandWidth(true));
        }
        else
        {
            // 자동 값 표시 (읽기 전용)
            GUI.enabled = false;
            EditorGUILayout.TextField($"auto  →  {autoLabel}", GUILayout.ExpandWidth(true));
            GUI.enabled = true;
        }

        EditorGUILayout.EndHorizontal();

        // 값 기록
        if (isPrev) { node.usePrevOverride = useOverride; node.overridePrev = overrideVal; }
        else { node.useNextOverride = useOverride; node.overrideNext = overrideVal; }
    }

    // ── 하단 버튼 ─────────────────────────────────────────────────────────────
    private void DrawBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Node", GUILayout.Height(28)))
            _nodes.Add(new NodeEntry { fileName = $"TextNode_{_nodes.Count}" });
        EditorGUILayout.EndHorizontal();
    }

    // ─── 저장 ────────────────────────────────────────────────────────────────
    private void SaveChain()
    {
        if (_nodes.Count == 0)
        {
            EditorUtility.DisplayDialog("저장 실패", "노드가 하나도 없습니다.", "OK");
            return;
        }

        // 폴더 생성
        if (!AssetDatabase.IsValidFolder(_savePath))
        {
            string parent = Path.GetDirectoryName(_savePath).Replace('\\', '/');
            string folderName = Path.GetFileName(_savePath);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        string headPath = $"{_savePath}/{_nodes[0].fileName}.asset";

        // ── 1단계: Head SO 생성 또는 재사용 (main asset) ─────────────────────
        TextNode headSO = AssetDatabase.LoadAssetAtPath<TextNode>(headPath);
        if (headSO == null)
        {
            headSO = ScriptableObject.CreateInstance<TextNode>();
            headSO.name = _nodes[0].fileName;
            AssetDatabase.CreateAsset(headSO, headPath);
        }
        headSO.text = _nodes[0].text;
        headSO.prevNode = null;
        headSO.nextNode = null;
        _nodes[0].asset = headSO;

        // ── 2단계: 기존 sub-asset 전부 제거 후 새로 추가 ─────────────────────
        // (순서·이름 변경에 안전하게 대응하기 위해 전부 교체)
        var existingSubs = AssetDatabase.LoadAllAssetsAtPath(headPath);
        foreach (var obj in existingSubs)
        {
            if (obj != headSO && obj is TextNode sub)
            {
                // 참조를 끊고 제거
                DestroyImmediate(sub, true);
            }
        }

        // ── 3단계: 나머지 노드를 sub-asset으로 생성 ──────────────────────────
        var assets = new List<TextNode> { headSO };
        for (int i = 1; i < _nodes.Count; i++)
        {
            var entry = _nodes[i];
            var so = ScriptableObject.CreateInstance<TextNode>();
            so.name = entry.fileName;
            so.text = entry.text;
            so.prevNode = null;
            so.nextNode = null;
            entry.asset = so;
            assets.Add(so);
            AssetDatabase.AddObjectToAsset(so, headPath);
        }

        // ── 4단계: prev / next 연결 (override 우선, 없으면 자동) ─────────────
        for (int i = 0; i < assets.Count; i++)
        {
            var entry = _nodes[i];

            assets[i].prevNode = entry.usePrevOverride
                ? entry.overridePrev
                : (i > 0 ? assets[i - 1] : null);

            assets[i].nextNode = entry.useNextOverride
                ? entry.overrideNext
                : (i < assets.Count - 1 ? assets[i + 1] : null);

            EditorUtility.SetDirty(assets[i]);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        _loadedHead = headSO;
        Selection.activeObject = headSO;
        EditorGUIUtility.PingObject(headSO);

        EditorUtility.DisplayDialog("저장 완료",
            $"Head: {headSO.name}\nSub-assets: {assets.Count - 1}개", "OK");
    }

    // ─── 불러오기 ─────────────────────────────────────────────────────────────
    private void LoadChain()
    {
        string picked = EditorUtility.OpenFilePanel("Head TextNode 선택", Application.dataPath, "asset");
        if (string.IsNullOrEmpty(picked)) return;

        if (!picked.StartsWith(Application.dataPath))
        {
            EditorUtility.DisplayDialog("오류", "Assets 폴더 안의 파일만 불러올 수 있습니다.", "OK");
            return;
        }

        string relPath = "Assets" + picked.Substring(Application.dataPath.Length);
        TextNode head = AssetDatabase.LoadAssetAtPath<TextNode>(relPath);
        if (head == null)
        {
            EditorUtility.DisplayDialog("오류", "TextNode SO를 불러오지 못했습니다.", "OK");
            return;
        }

        // main asset이 Head여야 함 (sub-asset을 선택했을 경우 main으로 교정)
        if (!AssetDatabase.IsMainAsset(head))
        {
            var mainObj = AssetDatabase.LoadMainAssetAtPath(relPath);
            if (mainObj is TextNode mainNode) head = mainNode;
            else
            {
                EditorUtility.DisplayDialog("오류", "선택한 파일의 Main Asset이 TextNode가 아닙니다.", "OK");
                return;
            }
        }

        // 체인의 맨 앞으로 이동 (prev 거슬러 올라가기)
        var visited = new HashSet<TextNode>();
        var current = head;
        while (current.prevNode != null && !visited.Contains(current.prevNode))
        {
            visited.Add(current);
            current = current.prevNode;
        }
        visited.Clear();

        // 앞에서 끝까지 순회
        _nodes.Clear();
        var orderedAssets = new List<TextNode>();
        while (current != null && !visited.Contains(current))
        {
            visited.Add(current);
            orderedAssets.Add(current);
            _nodes.Add(new NodeEntry
            {
                fileName = current.name,
                text = current.text,
                asset = current,
            });
            current = current.nextNode;
        }

        // 자동 연결과 실제 값이 다르면 override로 표시
        for (int i = 0; i < _nodes.Count; i++)
        {
            var so = orderedAssets[i];
            var entry = _nodes[i];

            TextNode autoPrev = (i > 0) ? orderedAssets[i - 1] : null;
            TextNode autoNext = (i < orderedAssets.Count - 1) ? orderedAssets[i + 1] : null;

            if (so.prevNode != autoPrev)
            {
                entry.usePrevOverride = true;
                entry.overridePrev = so.prevNode;
            }
            if (so.nextNode != autoNext)
            {
                entry.useNextOverride = true;
                entry.overrideNext = so.nextNode;
            }
        }

        _loadedHead = _nodes.Count > 0 ? _nodes[0].asset : null;
        _savePath = Path.GetDirectoryName(relPath).Replace('\\', '/');

        EditorUtility.DisplayDialog("불러오기 완료",
            $"{_nodes.Count}개의 노드를 불러왔습니다.\nHead: {_nodes[0].fileName}", "OK");
    }

    // ─── 새 체인 ──────────────────────────────────────────────────────────────
    private void NewChain()
    {
        if (_nodes.Count > 0 &&
            !EditorUtility.DisplayDialog("새 체인", "현재 작업이 초기화됩니다. 계속할까요?", "Yes", "No"))
            return;

        _nodes.Clear();
        _loadedHead = null;
        _nodes.Add(new NodeEntry { fileName = "TextNode_Head" });
    }
}
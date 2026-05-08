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

    // ─── 에디터 전용 데이터 ───────────────────────────────────────────────────
    // TextNode 하나에 대응
    private class NodeEntry
    {
        public string fileName = "TextNode";
        public ETextViewType viewType = ETextViewType.Dialogue;
        public TextNode asset = null;

        // viewType == Selection : singles 전체 사용
        // 그 외                 : singles[0]만 사용
        public List<SingleEntry> singles = new List<SingleEntry> { new SingleEntry() };

        // prevNode: -1 = auto(이전 인덱스), 그 외 = _nodes 인덱스
        public bool usePrevOverride = false;
        public int overridePrevIndex = -1;

        public bool IsSelection => viewType == ETextViewType.Selection;
    }

    // SingleTextNode 하나에 대응
    private class SingleEntry
    {
        public string text = "";

        // nextNode: -1 = auto(다음 인덱스), 그 외 = _nodes 인덱스
        public bool useNextOverride = false;
        public int overrideNextIndex = -1;
    }

    // ─── 열기 ────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TextNode Chain Editor")]
    public static void Open()
    {
        var win = GetWindow<TextNodeChainEditor>("TextNode Chain Editor");
        win.minSize = new Vector2(560, 440);
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
                _savePath = chosen.StartsWith(Application.dataPath)
                    ? "Assets" + chosen.Substring(Application.dataPath.Length)
                    : chosen;
        }
        EditorGUILayout.EndHorizontal();
    }

    // ── 노드 리스트 ───────────────────────────────────────────────────────────
    private void DrawNodeList()
    {
        EditorGUILayout.LabelField("Nodes  (index 0 = Head)", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        for (int i = 0; i < _nodes.Count; i++)
            if (DrawNodeEntry(i)) break;

        EditorGUILayout.EndScrollView();
    }

    // 반환값 true = 리스트 변경됨
    private bool DrawNodeEntry(int i)
    {
        var node = _nodes[i];
        bool isHead = (i == 0);

        var bgColor = isHead
            ? new Color(0.3f, 0.55f, 0.3f, 0.28f)
            : node.viewType == ETextViewType.Selection
                ? new Color(0.55f, 0.4f, 0.1f, 0.28f)
                : node.viewType == ETextViewType.Balloon
                    ? new Color(0.35f, 0.25f, 0.55f, 0.28f)
                    : new Color(0.25f, 0.35f, 0.5f, 0.18f);

        var rect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(20));
        EditorGUI.DrawRect(rect, bgColor);

        // ── 헤더 행 ──────────────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();

        string label = isHead ? $"[{i}] HEAD" : $"[{i}]";
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(60));

        EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
        node.fileName = EditorGUILayout.TextField(node.fileName, GUILayout.Width(130));

        // viewType Enum (Selection이면 singles 복수 허용, 단일→복수 전환 시 데이터 유지)
        var newViewType = (ETextViewType)EditorGUILayout.EnumPopup(node.viewType, GUILayout.Width(80));
        if (newViewType != node.viewType)
        {
            bool wasSelection = node.IsSelection;
            node.viewType = newViewType;
            // Selection → 단일 전환 시 singles 1개로 줄임
            if (wasSelection && !node.IsSelection && node.singles.Count > 1)
                node.singles.RemoveRange(1, node.singles.Count - 1);
        }

        GUILayout.FlexibleSpace();

        // ▲ ▼
        GUI.enabled = i > 0;
        if (GUILayout.Button("▲", GUILayout.Width(24)))
        {
            (_nodes[i], _nodes[i - 1]) = (_nodes[i - 1], _nodes[i]);
            EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical();
            return true;
        }
        GUI.enabled = i < _nodes.Count - 1;
        if (GUILayout.Button("▼", GUILayout.Width(24)))
        {
            (_nodes[i], _nodes[i + 1]) = (_nodes[i + 1], _nodes[i]);
            EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical();
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
            EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical();
            return true;
        }
        GUI.color = oldCol;

        EditorGUILayout.EndHorizontal();

        // ── prev 행 ───────────────────────────────────────────────────────────
        DrawPrevRow(node, i);

        // ── SingleTextNode 행들 ───────────────────────────────────────────────
        if (node.IsSelection)
        {
            // 선택지 모드: 여러 SingleEntry
            for (int s = 0; s < node.singles.Count; s++)
                DrawSingleEntry(node, s, i, isSelection: true);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            if (GUILayout.Button("+ 선택지 추가", GUILayout.Width(100)))
                node.singles.Add(new SingleEntry());
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            // 단일 모드: singles[0]만
            DrawSingleEntry(node, 0, i, isSelection: false);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.EndVertical();
        return false;
    }

    // ── prevNode 행 ───────────────────────────────────────────────────────────
    private void DrawPrevRow(NodeEntry node, int nodeIndex)
    {
        bool use = node.usePrevOverride;
        int ovIndex = node.overridePrevIndex;
        string autoLabel = nodeIndex > 0 ? _nodes[nodeIndex - 1].fileName : "null";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("prev:", GUILayout.Width(34));

        bool newUse = EditorGUILayout.ToggleLeft("override", use, GUILayout.Width(78));
        if (newUse != use) { use = newUse; if (!use) ovIndex = -1; }

        if (use)
            ovIndex = DrawNodeIndexPopup(ovIndex, nodeIndex);
        else
        {
            GUI.enabled = false;
            EditorGUILayout.TextField($"auto  →  {autoLabel}", GUILayout.ExpandWidth(true));
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();

        node.usePrevOverride = use;
        node.overridePrevIndex = ovIndex;
    }

    // ── SingleEntry 한 행 ─────────────────────────────────────────────────────
    private void DrawSingleEntry(NodeEntry node, int s, int nodeIndex, bool isSelection)
    {
        var single = node.singles[s];

        // 선택지 모드일 때 헤더 + 삭제
        if (isSelection)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  [{s}]", EditorStyles.boldLabel, GUILayout.Width(30));

            // 삭제 (최소 1개 유지)
            GUI.enabled = node.singles.Count > 1;
            var oldCol = GUI.color;
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("✕", GUILayout.Width(20)))
            {
                node.singles.RemoveAt(s);
                GUI.color = oldCol; GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                return;
            }
            GUI.color = oldCol; GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        // text
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(isSelection ? "   text:" : "text:", GUILayout.Width(isSelection ? 50 : 40));
        single.text = EditorGUILayout.TextArea(single.text, GUILayout.MinHeight(34), GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        // nextNode
        bool use = single.useNextOverride;
        int ovIndex = single.overrideNextIndex;
        string autoNextLabel = (nodeIndex + 1 < _nodes.Count) ? _nodes[nodeIndex + 1].fileName : "null";

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(isSelection ? "   next:" : "next:", GUILayout.Width(isSelection ? 50 : 40));

        bool newUse = EditorGUILayout.ToggleLeft("override", use, GUILayout.Width(78));
        if (newUse != use) { use = newUse; if (!use) ovIndex = -1; }

        if (use)
            ovIndex = DrawNodeIndexPopup(ovIndex, nodeIndex);
        else
        {
            GUI.enabled = false;
            EditorGUILayout.TextField($"auto  →  {autoNextLabel}", GUILayout.ExpandWidth(true));
            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();

        single.useNextOverride = use;
        single.overrideNextIndex = ovIndex;
    }

    // ── 노드 인덱스 드롭다운 ──────────────────────────────────────────────────
    // 현재 노드 자신(selfIndex)을 제외한 전체 노드 목록을 드롭다운으로 표시
    // 반환값: 선택된 _nodes 인덱스 (-1 = 미선택)
    private int DrawNodeIndexPopup(int selectedIndex, int selfIndex)
    {
        // 항목 구성: "(none)" + 각 노드
        var labels = new System.Collections.Generic.List<string> { "(none)" };
        var indices = new System.Collections.Generic.List<int> { -1 };
        for (int i = 0; i < _nodes.Count; i++)
        {
            if (i == selfIndex) continue;
            labels.Add($"[{i}] {_nodes[i].fileName}");
            indices.Add(i);
        }

        int popupSel = indices.IndexOf(selectedIndex);
        if (popupSel < 0) popupSel = 0;

        int newPopupSel = EditorGUILayout.Popup(popupSel, labels.ToArray(), GUILayout.ExpandWidth(true));
        return indices[newPopupSel];
    }

    // ── 하단 버튼 ─────────────────────────────────────────────────────────────
    private void DrawBottomButtons()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Node", GUILayout.Height(28)))
        {
            var prevViewType = _nodes.Count > 0
                ? _nodes[_nodes.Count - 1].viewType
                : ETextViewType.Dialogue;
            _nodes.Add(new NodeEntry
            {
                fileName = $"TextNode_{_nodes.Count}",
                viewType = prevViewType,
            });
        }
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

        // ── 1단계: Head SO (main asset) ───────────────────────────────────────
        TextNode headSO = AssetDatabase.LoadAssetAtPath<TextNode>(headPath);
        if (headSO == null)
        {
            headSO = ScriptableObject.CreateInstance<TextNode>();
            headSO.name = _nodes[0].fileName;
            AssetDatabase.CreateAsset(headSO, headPath);
        }
        _nodes[0].asset = headSO;

        // ── 2단계: 기존 sub-asset 제거 ────────────────────────────────────────
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(headPath))
        {
            if (obj != headSO && obj is TextNode sub)
                DestroyImmediate(sub, true);
        }

        // ── 3단계: 나머지 노드를 sub-asset으로 생성 ──────────────────────────
        var assets = new List<TextNode> { headSO };
        for (int i = 1; i < _nodes.Count; i++)
        {
            var entry = _nodes[i];
            var so = ScriptableObject.CreateInstance<TextNode>();
            so.name = entry.fileName;
            entry.asset = so;
            assets.Add(so);
            AssetDatabase.AddObjectToAsset(so, headPath);
        }

        // ── 4단계: 각 SO 데이터 + 링크 설정 ──────────────────────────────────
        for (int i = 0; i < assets.Count; i++)
        {
            var entry = _nodes[i];
            var so = assets[i];

            so.viewType = entry.viewType;
            so.isSelection = entry.IsSelection;

            // prevNode
            so.prevNode = entry.usePrevOverride
                ? (entry.overridePrevIndex >= 0 ? assets[entry.overridePrevIndex] : null)
                : (i > 0 ? assets[i - 1] : null);

            // node (List<SingleTextNode>)
            so.node = new List<SingleTextNode>();
            foreach (var s in entry.singles)
            {
                so.node.Add(new SingleTextNode
                {
                    text = s.text,
                    nextNode = s.useNextOverride
                        ? (s.overrideNextIndex >= 0 ? assets[s.overrideNextIndex] : null)
                        : (i < assets.Count - 1 ? assets[i + 1] : null),
                });
            }

            EditorUtility.SetDirty(so);
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

        // sub-asset 선택 시 main으로 교정
        if (!AssetDatabase.IsMainAsset(head))
        {
            var mainObj = AssetDatabase.LoadMainAssetAtPath(relPath);
            if (mainObj is TextNode mn) head = mn;
            else { EditorUtility.DisplayDialog("오류", "Main Asset이 TextNode가 아닙니다.", "OK"); return; }
        }

        // 체인 맨 앞으로
        var visited = new HashSet<TextNode>();
        var current = head;
        while (current.prevNode != null && !visited.Contains(current.prevNode))
        {
            visited.Add(current); current = current.prevNode;
        }
        visited.Clear();

        // 앞→끝 순회 (첫 번째 singles[0].nextNode 경로만 따라감)
        _nodes.Clear();
        var orderedAssets = new List<TextNode>();
        while (current != null && !visited.Contains(current))
        {
            visited.Add(current);
            orderedAssets.Add(current);

            var entry = new NodeEntry
            {
                fileName = current.name,
                viewType = current.viewType,
                asset = current,
                singles = new List<SingleEntry>(),
            };

            foreach (var sn in current.node)
                entry.singles.Add(new SingleEntry { text = sn.text });

            _nodes.Add(entry);
            current = (current.node != null && current.node.Count > 0) ? current.node[0].nextNode : null;
        }

        // override 판정
        for (int i = 0; i < _nodes.Count; i++)
        {
            var so = orderedAssets[i];
            var entry = _nodes[i];

            // prev override
            TextNode autoPrev = i > 0 ? orderedAssets[i - 1] : null;
            if (so.prevNode != autoPrev)
            {
                entry.usePrevOverride = true;
                entry.overridePrevIndex = orderedAssets.IndexOf(so.prevNode); // -1이면 체인 외부
            }

            // next override (각 single)
            TextNode autoNext = i < orderedAssets.Count - 1 ? orderedAssets[i + 1] : null;
            for (int s = 0; s < entry.singles.Count && s < so.node.Count; s++)
            {
                if (so.node[s].nextNode != autoNext)
                {
                    entry.singles[s].useNextOverride = true;
                    entry.singles[s].overrideNextIndex = orderedAssets.IndexOf(so.node[s].nextNode);
                }
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
        _nodes.Add(new NodeEntry { fileName = "TextNode_Head", viewType = ETextViewType.Dialogue });
    }
}
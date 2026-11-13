using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// メニュー「Tools > 迷路を広げる (1.5倍)」で、迷路の広さだけを拡大する。
/// ・壁と床：位置と長さを横方向 (X/Z) に拡大。厚さ (1 未満の軸) と高さはそのまま
/// ・Player / 回復アイテム / トラップ：位置だけ移動（大きさは変えない）
/// ・原点 (0,0,0) を中心に広げる
/// Ctrl+Z で元に戻せる。何度でも実行できる。
/// </summary>
public static class MazeScaler
{
    const float Factor = 1.5f;

    [MenuItem("Tools/迷路を広げる (1.5倍)")]
    static void Expand()
    {
        Undo.SetCurrentGroupName("Expand Maze");
        int undoGroup = Undo.GetCurrentGroup();
        var scene = EditorSceneManager.GetActiveScene();
        int structures = 0, moved = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // カメラ・ライト・Volume などの見た目を持たないものは触らない
            bool isPlayer = root.GetComponent<PlayerController>() != null;
            bool isPickup = root.GetComponent<HealItem>() != null || root.GetComponent<DamageTrap>() != null;
            bool hasMesh = root.GetComponent<MeshRenderer>() != null;
            if (!isPlayer && !hasMesh) continue;

            Transform t = root.transform;
            Undo.RecordObject(t, "Expand Maze");

            Vector3 p = t.position;
            t.position = new Vector3(p.x * Factor, p.y, p.z * Factor);

            if (isPlayer || isPickup)
            {
                moved++;
                continue;
            }

            // 壁・床：長い軸だけ伸ばす（厚さ 0.1 などの薄い軸は変えない）
            Vector3 s = t.localScale;
            if (Mathf.Abs(s.x) >= 1f) s.x *= Factor;
            if (Mathf.Abs(s.z) >= 1f) s.z *= Factor;
            t.localScale = s;
            structures++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"迷路を {Factor} 倍に広げました（壁・床 {structures} 個、Player・アイテム {moved} 個）。" +
                  "おかしければ Ctrl+Z で戻せます。よければ Ctrl+S で保存してください。");
    }
}

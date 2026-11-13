using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 迷路の「内側の壁」をランダム生成する。外周の壁は既存のものをそのまま使う。
/// 穴掘り法（再帰的バックトラック）で作るので、どのマスからでも全マスへ必ず行ける。
///
/// このオブジェクトの位置 = 迷路エリアの中心（Y は床の高さ）。
/// 「Tools > 迷路ジェネレーターをセットアップ」で外周の壁に自動で合わせられる。
/// Scene ビューでは黄色の枠でエリアが表示される。
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Header("迷路のマス目")]
    [Min(2)] public int columns = 8;   // X 方向のマス数
    [Min(2)] public int rows = 8;      // Z 方向のマス数
    [Tooltip("1マスの大きさ (X, Z) [m]")]
    public Vector2 cellSize = Vector2.one;

    [Header("壁")]
    public float wallHeight = 2.5f;
    public float wallThickness = 0.1f;
    public Material wallMaterial;

    [Header("ランダム")]
    [Tooltip("0 なら毎回違う迷路。0 以外なら毎回同じ迷路になる")]
    public int seed = 0;
    [Tooltip("行き止まりを減らすため、余分に壁を消す割合 (0〜1)。0 なら道は一本道の木構造")]
    [Range(0f, 0.5f)] public float extraOpenings = 0.1f;

    [Header("いつ生成するか")]
    public bool generateOnStart = true;
    [Tooltip("生成時に Player・回復アイテム・トラップをマスの中央に寄せる（壁にめり込まないように）")]
    public bool snapObjectsToCells = true;

    const string WallContainerName = "GeneratedWalls";

    // 壁の有無。hWalls[c, r] = マス(c,r) と (c,r+1) の間、vWalls[c, r] = マス(c,r) と (c+1,r) の間
    private bool[,] hWalls;
    private bool[,] vWalls;

    // 他のスクリプトの Start（回復アイテムの位置記録など）より先に生成する
    void Awake()
    {
        if (generateOnStart) Generate();
    }

    [ContextMenu("今すぐ生成")]
    public void Generate()
    {
        var rng = seed == 0 ? new System.Random() : new System.Random(seed);
        CarveMaze(rng);
        BuildWalls();
        if (snapObjectsToCells && Application.isPlaying) SnapObjectsToCells();
    }

    // ---- Player・アイテム・トラップをマスの中央へ ----
    void SnapObjectsToCells()
    {
        var targets = new List<Transform>();
        foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None)) targets.Add(p.transform);
        foreach (var h in FindObjectsByType<HealItem>(FindObjectsSortMode.None)) targets.Add(h.transform);
        foreach (var d in FindObjectsByType<DamageTrap>(FindObjectsSortMode.None)) targets.Add(d.transform);

        Vector3 min = AreaMin();
        foreach (Transform t in targets)
        {
            Vector3 p = t.position;
            float fx = (p.x - min.x) / cellSize.x;
            float fz = (p.z - min.z) / cellSize.y;
            // エリアの外にあるものは触らない
            if (fx < 0f || fx > columns || fz < 0f || fz > rows) continue;

            int c = Mathf.Clamp(Mathf.FloorToInt(fx), 0, columns - 1);
            int r = Mathf.Clamp(Mathf.FloorToInt(fz), 0, rows - 1);
            t.position = new Vector3(min.x + (c + 0.5f) * cellSize.x, p.y, min.z + (r + 0.5f) * cellSize.y);
        }
        Physics.SyncTransforms();
    }

    [ContextMenu("生成した壁を消す")]
    public void Clear()
    {
        Transform old = transform.Find(WallContainerName);
        if (old == null) return;
        if (Application.isPlaying) Destroy(old.gameObject);
        else DestroyImmediate(old.gameObject);
    }

    // ---- 迷路データを作る（穴掘り法） ----
    void CarveMaze(System.Random rng)
    {
        hWalls = new bool[columns, rows - 1];
        vWalls = new bool[columns - 1, rows];
        for (int c = 0; c < columns; c++) for (int r = 0; r < rows - 1; r++) hWalls[c, r] = true;
        for (int c = 0; c < columns - 1; c++) for (int r = 0; r < rows; r++) vWalls[c, r] = true;

        var visited = new bool[columns, rows];
        var stack = new Stack<Vector2Int>();
        var start = new Vector2Int(rng.Next(columns), rng.Next(rows));
        visited[start.x, start.y] = true;
        stack.Push(start);

        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        var candidates = new List<Vector2Int>(4);

        while (stack.Count > 0)
        {
            Vector2Int cur = stack.Peek();
            candidates.Clear();
            foreach (var d in dirs)
            {
                Vector2Int n = cur + d;
                if (n.x >= 0 && n.x < columns && n.y >= 0 && n.y < rows && !visited[n.x, n.y])
                    candidates.Add(n);
            }

            if (candidates.Count == 0)
            {
                stack.Pop();
                continue;
            }

            Vector2Int next = candidates[rng.Next(candidates.Count)];
            RemoveWallBetween(cur, next);
            visited[next.x, next.y] = true;
            stack.Push(next);
        }

        // 余分に壁を消して、ループ（別ルート）を作る
        for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows - 1; r++)
                if (hWalls[c, r] && rng.NextDouble() < extraOpenings) hWalls[c, r] = false;
        for (int c = 0; c < columns - 1; c++)
            for (int r = 0; r < rows; r++)
                if (vWalls[c, r] && rng.NextDouble() < extraOpenings) vWalls[c, r] = false;
    }

    void RemoveWallBetween(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x) hWalls[a.x, Mathf.Min(a.y, b.y)] = false;
        else vWalls[Mathf.Min(a.x, b.x), a.y] = false;
    }

    // ---- 壁オブジェクトを作る ----
    void BuildWalls()
    {
        Clear();
        var container = new GameObject(WallContainerName).transform;
        container.SetParent(transform, false);

        Vector3 origin = AreaMin();
        float y = transform.position.y + wallHeight / 2f;

        // 横向きの壁（Z 方向に隣り合うマスの間）
        for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows - 1; r++)
                if (hWalls[c, r])
                {
                    Vector3 pos = new Vector3(origin.x + (c + 0.5f) * cellSize.x, y, origin.z + (r + 1) * cellSize.y);
                    CreateWall(container, pos, new Vector3(cellSize.x + wallThickness, wallHeight, wallThickness));
                }

        // 縦向きの壁（X 方向に隣り合うマスの間）
        for (int c = 0; c < columns - 1; c++)
            for (int r = 0; r < rows; r++)
                if (vWalls[c, r])
                {
                    Vector3 pos = new Vector3(origin.x + (c + 1) * cellSize.x, y, origin.z + (r + 0.5f) * cellSize.y);
                    CreateWall(container, pos, new Vector3(wallThickness, wallHeight, cellSize.y + wallThickness));
                }
    }

    void CreateWall(Transform parent, Vector3 worldPos, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent, true);
        wall.transform.position = worldPos;
        wall.transform.rotation = Quaternion.identity;
        wall.transform.localScale = size;
        if (wallMaterial != null) wall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
    }

    /// <summary>迷路エリアの左下（X・Z が最小の角）。</summary>
    Vector3 AreaMin()
    {
        Vector3 p = transform.position;
        return new Vector3(p.x - columns * cellSize.x / 2f, p.y, p.z - rows * cellSize.y / 2f);
    }

    // ---- Scene ビューにエリアを表示 ----
    void OnDrawGizmosSelected()
    {
        Vector3 min = AreaMin();
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.9f);
        for (int c = 0; c <= columns; c++)
        {
            float x = min.x + c * cellSize.x;
            Gizmos.DrawLine(new Vector3(x, min.y, min.z), new Vector3(x, min.y, min.z + rows * cellSize.y));
        }
        for (int r = 0; r <= rows; r++)
        {
            float z = min.z + r * cellSize.y;
            Gizmos.DrawLine(new Vector3(min.x, min.y, z), new Vector3(min.x + columns * cellSize.x, min.y, z));
        }
    }
}

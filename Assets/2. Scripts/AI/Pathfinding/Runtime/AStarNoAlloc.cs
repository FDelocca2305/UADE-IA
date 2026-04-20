using UnityEngine;

public static class AStarNoAlloc
{
    private struct Heap
    {
        public int Count;
        public int[] idx;
        public float[] fscore;
        public void Init(int[] iBuf, float[] fBuf) { idx = iBuf; fscore = fBuf; Count = 0; }
        public void Push(int node, float f) { int i = Count++; idx[i] = node; fscore[i] = f; Up(i); }
        public int Pop() { int t = idx[0]; Count--; idx[0] = idx[Count]; fscore[0] = fscore[Count]; Down(0); return t; }
        void Up(int i) { while (i > 0) { int p = (i - 1) >> 1; if (fscore[p] <= fscore[i]) break; Swap(p, i); i = p; } }
        void Down(int i) { int n = Count; for (; ; ) { int l = (i << 1) + 1, r = l + 1, m = i; if (l < n && fscore[l] < fscore[m]) m = l; if (r < n && fscore[r] < fscore[m]) m = r; if (m == i) break; Swap(m, i); i = m; } }
        void Swap(int a, int b) { (idx[a], idx[b]) = (idx[b], idx[a]); (fscore[a], fscore[b]) = (fscore[b], fscore[a]); }
    }

    public static int FindPath(
        GraphAsset g, int start, int goal,
        float[] G, float[] F, int[] from, bool[] closed,
        int[] heapIdx, float[] heapF,
        int[] outPath)
    {
        int n = g.NodeCount; if (n == 0 || start < 0 || start >= n || goal < 0 || goal >= n) return 0;

        for (int i = 0; i < n; i++) { G[i] = float.PositiveInfinity; F[i] = float.PositiveInfinity; from[i] = -1; closed[i] = false; }
        var heap = new Heap(); heap.Init(heapIdx, heapF);

        G[start] = 0f; F[start] = Vector3.Distance(g.nodePositions[start], g.nodePositions[goal]); heap.Push(start, F[start]);

        while (heap.Count > 0)
        {
            int cur = heap.Pop();
            if (cur == goal) return Reconstruct(from, cur, outPath);

            closed[cur] = true;
            var curPos = g.nodePositions[cur];
            var neigh = g.neighbors[cur].data;
            for (int i = 0; i < neigh.Length; i++)
            {
                int nb = neigh[i]; if (closed[nb]) continue;
                float tentative = G[cur] + Vector3.Distance(curPos, g.nodePositions[nb]);
                if (tentative < G[nb])
                {
                    from[nb] = cur; G[nb] = tentative;
                    F[nb] = tentative + Vector3.Distance(g.nodePositions[nb], g.nodePositions[goal]);
                    heap.Push(nb, F[nb]);
                }
            }
        }
        return 0;
    }

    static int Reconstruct(int[] from, int cur, int[] outPath)
    {
        int len = 0; int w = cur;
        while (w != -1 && len < outPath.Length) { outPath[len++] = w; w = from[w]; }
        for (int i = 0, j = len - 1; i < j; i++, j--) (outPath[i], outPath[j]) = (outPath[j], outPath[i]);
        return len;
    }
}
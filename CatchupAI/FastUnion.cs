using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatchupAI
{
    class FastUnion
    {
        private int[] parent;
        private int[] size;

        public FastUnion(int slots)
        {
            parent = new int[slots];
            size = new int[slots];
            for (int i = 0; i < slots; ++i)
            {
                parent[i] = i;
                size[i] = 1;
            }
        }

        public int find(int i)
        {
            if (parent[i] == i) return i;
            return parent[i] = find(parent[i]);
        }

        public int join(int i, int j)
        {
            i = find(i);
            j = find(j);
            if (i == j) return i;

            if (size[i] < size[j])
            {
                int t = i;
                i = j;
                j = t;
            }

            parent[j] = i;
            size[i] += size[j];
            return i;
        }

        public int querySize(int i)
        {
            i = find(i);
            return size[i];
        }

        public int representativeSize(int i)
        {
            if (parent[i] == i) return size[i];
            return 0;
        }

        public void CopyTo(FastUnion fu)
        {
            Debug.Assert(parent.Length == fu.parent.Length);
            parent.CopyTo(fu.parent, 0);
            size.CopyTo(fu.size, 0);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatchupAI
{
    class FastUnion
    {
        private int[] setParent;
        private int[] setSize;

        public FastUnion(int slots)
        {
            setParent = new int[slots];
            setSize = new int[slots];
            for (int i = 0; i < slots; ++i)
            {
                setParent[i] = i;
                setSize[i] = 1;
            }
        }

        public int find(int i)
        {
            if (setParent[i] == i) return i;
            return setParent[i] = find(setParent[i]);
        }

        public int join(int i, int j)
        {
            i = find(i);
            j = find(j);
            if (i == j) return i;

            if (setSize[i] < setSize[j])
            {
                int t = i;
                i = j;
                j = t;
            }

            setParent[j] = i;
            setSize[i] += setSize[j];
            return i;
        }

        public int querySize(int i)
        {
            i = find(i);
            return setSize[i];
        }
    }
}

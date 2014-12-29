using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatchupAI
{
    class Game
    {
        public const int S = 5;
        public const int maxX = 2 * S - 1, maxY = maxX;

        public enum Stone { Empty, Black, White };

        private Stone[,] stones;
        public int locLen;

        private IPlayer[] players;
        private int currentPlayer;
        private bool mayPass;
        private int remainingPlays;
        private bool triggerCatchup;
        private int catchupThreshold;
        private int emptyHexes;

        // E, N, NW, W, S, SE
        private const int nd = 6;
        private int[] dx = { 1,  0, -1, -1, 0, 1 };
        private int[] dy = { 0, -1, -1,  0, 1, 1 };

        FastUnion fu;

        public Game()
        {
            stones = new Stone[maxX, maxY];
            players = new IPlayer[2];

            // TODO: Replace this with configuration.
            players[0] = new RandomAI();

            currentPlayer = 0;
            remainingPlays = 1;
            mayPass = false;
            triggerCatchup = false;
            catchupThreshold = 1;
            emptyHexes = 3 * S * (S - 1) + 1;

            locLen = toLoc(0, maxY);
            fu = new FastUnion(locLen);

            maybeRequestPlay();
        }

        public Stone getStone(int x, int y)
        {
            return stones[x, y];
        }

        public bool getMayPass()
        {
            return !isGameOver() && mayPass;
        }

        public int toLoc(int x, int y)
        {
            return y * maxX + x;
        }

        public void fromLoc(int loc, out int x, out int y)
        {
            Debug.Assert(0 <= loc && loc < locLen);
            x = loc % (2 * Game.S - 1);
            y = loc / (2 * Game.S - 1);
        }

        public void userPlay(int x, int y)
        {
            if (players[currentPlayer] != null) return;
            if (stones[x, y] != Stone.Empty) return;

            play(x, y);
        }

        public void userPass()
        {
            if (players[currentPlayer] != null) return;
            if (!getMayPass()) return;
            pass();
        }

        public void pass()
        {
            Debug.Assert(getMayPass());
            endTurn();
            maybeRequestPlay();
        }

        public void play(int loc)
        {
            int x, y;
            fromLoc(loc, out x, out y);
            play(x, y);
        }

        public void play(int x, int y)
        {
            Debug.Assert(stones[x, y] == Stone.Empty);

            Stone stone = currentPlayer == 0 ? Stone.Black : Stone.White;
            stones[x, y] = stone;

            int i = toLoc(x, y);
            for (int k = 0; k < nd; ++k)
            {
                int x2 = x + dx[k];
                int y2 = y + dy[k];
                if (!inBounds(x2, y2)) continue;

                if (stones[x, y] != stones[x2, y2]) continue;

                int j = toLoc(x2, y2);
                fu.join(i, j);
            }

            int joinedSize = fu.querySize(i);
            if (joinedSize > catchupThreshold)
            {
                triggerCatchup = true;
                catchupThreshold = joinedSize;
            }

            mayPass = true;
            --remainingPlays;
            --emptyHexes;

            if (emptyHexes == 0)
            {
                return;
            }

            if (remainingPlays == 0)
            {
                endTurn();
            }

            maybeRequestPlay();
        }

        private void endTurn()
        {
            currentPlayer = 1 - currentPlayer;
            mayPass = false;
            remainingPlays = 2;
            if (triggerCatchup)
            {
                remainingPlays = 3;
            }
            triggerCatchup = false;
        }

        private void maybeRequestPlay()
        {
            if (players[currentPlayer] == null) return;

            players[currentPlayer].play(this);
        }

        public static bool inBounds(int x, int y)
        {
            return x >= 0 &&
                y - x <= S - 1 &&
                y < maxY &&
                x < maxX &&
                x - y <= S - 1 &&
                y >= 0;
        }

        public int getCurrentPlayer()
        {
            return currentPlayer;
        }

        public bool isGameOver()
        {
            return emptyHexes == 0;
        }

        // Returns array indexed by player.
        public List<int>[] getScore()
        {
            var ret = new List<int>[2];
            for (int p = 0; p < 2; ++p)
            {
                ret[p] = new List<int>();
            }

            for (int x = 0; x < maxX; ++x)
            {
                for (int y = 0; y < maxY; ++y)
                {
                    if (!inBounds(x, y)) continue;
                    if (stones[x, y] == Stone.Empty) continue;
                    int rp = fu.representativeSize(toLoc(x,y));
                    if (rp == 0) continue;

                    int p = stones[x, y] == Stone.Black ? 0 : 1;
                    ret[p].Add(rp);
                }
            }

            for (int p = 0; p < 2; ++p)
            {
                ret[p].Sort();
                ret[p].Reverse();
            }

            return ret;
        }

        public int getRemainingPlays()
        {
            return remainingPlays;
        }
    }
}

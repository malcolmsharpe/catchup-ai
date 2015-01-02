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
        public const int locLen = maxX * maxY;

        public enum Stone { Empty, Black, White };

        private Stone[,] stones;

        private IPlayer[] players;
        private int currentPlayer;
        private bool mayPass;
        private int remainingPlays;
        private bool triggerCatchup;
        private int catchupThreshold;
        private int emptyHexes;
        private List<int> freshMoves = new List<int>();

        // E, N, NW, W, S, SE
        private const int nd = 6;
        private int[] dx = { 1,  0, -1, -1, 0, 1 };
        private int[] dy = { 0, -1, -1,  0, 1, 1 };

        FastUnion fu;

        public Game()
            : this(new IPlayer[2])
        {
        }

        public Game(IPlayer[] players)
        {
            stones = new Stone[maxX, maxY];
            this.players = players;

            currentPlayer = 0;
            remainingPlays = 1;
            mayPass = false;
            triggerCatchup = false;
            catchupThreshold = 1;
            emptyHexes = 3 * S * (S - 1) + 1;

            fu = new FastUnion(locLen);

            maybeRequestPlay();
        }

        // Copy state to the other game, but not players.
        public void CopyTo(Game game)
        {
            for (int x = 0; x < maxX; ++x)
            {
                for (int y = 0; y < maxY; ++y)
                {
                    game.stones[x, y] = stones[x, y];
                }
            }
            game.currentPlayer = currentPlayer;
            game.remainingPlays = remainingPlays;
            game.triggerCatchup = triggerCatchup;
            game.catchupThreshold = catchupThreshold;
            game.emptyHexes = emptyHexes;
            fu.CopyTo(game.fu);
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

        public void ApplyMove(int loc)
        {
            Debug.Assert(0 <= loc && loc <= locLen);
            if (loc == locLen)
            {
                pass();
            }
            else
            {
                if (players[currentPlayer] != null)
                {
                    if (!mayPass)
                    {
                        freshMoves.Clear();
                    }
                    freshMoves.Add(loc);
                }

                play(loc);
            }
        }

        // This should really return an immutable list
        public List<int> getFreshMoves()
        {
            return freshMoves.ToList();
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

        private void pass()
        {
            Debug.Assert(getMayPass());
            endTurn();
            maybeRequestPlay();
        }

        private void play(int loc)
        {
            int x, y;
            fromLoc(loc, out x, out y);
            play(x, y);
        }

        private void play(int x, int y)
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

                if (stone != stones[x2, y2]) continue;

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

        public int GetWinner()
        {
            Debug.Assert(isGameOver());

            List<int>[] score = getScore();

            for (int i = 0; ; ++i)
            {
                Debug.Assert(i < score[0].Count || i < score[1].Count);
                if (i == score[0].Count) return 1;
                if (i == score[1].Count) return 0;

                if (score[0][i] < score[1][i]) return 1;
                if (score[0][i] > score[1][i]) return 0;
            }
        }

        public int getRemainingPlays()
        {
            return remainingPlays;
        }

        // 'locLen' means pass
        public List<int> getLegalMoves(bool includePass)
        {
            List<int> legals = new List<int>(emptyHexes + 1);

            for (int x = 0; x < maxX; ++x)
            {
                for (int y = 0; y < maxY; ++y)
                {
                    if (!inBounds(x, y)) continue;

                    if (getStone(x, y) != Stone.Empty) continue;
                    legals.Add(toLoc(x, y));
                }
            }

            if (includePass && getMayPass()) legals.Add(locLen);

            return legals;
        }
    }
}

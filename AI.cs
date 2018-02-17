/* Copyright (C) 2018 by John Cronin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nmm
{
    class AIPlayer : Player
    {
        static int DEPTH = 6;

        Dictionary<ulong, int>[] cache_v;
        Dictionary<ulong, ulong>[] cache_move;

        public AIPlayer(bool white)
        {
            IsWhite = white;
            cache_v = new Dictionary<ulong, int>[DEPTH + 1];
            cache_move = new Dictionary<ulong, ulong>[DEPTH + 1];
            for (int i = 0; i < DEPTH + 1; i++)
            {
                cache_v[i] = new Dictionary<ulong, int>();
                cache_move[i] = new Dictionary<ulong, ulong>();
            }
        }

        public override ulong GetMove(ulong board)
        {
            //minimax(board, DEPTH, true, out var move);
            alphabeta(board, DEPTH, int.MinValue, int.MaxValue, true, out var move);
            if (move == 0)
                throw new Exception();
            return move;
        }

        public override void IllegalMove(ulong move)
        {
            throw new NotImplementedException();
        }

        int minimax(ulong board, int depth, bool maximizingPlayer, out ulong move)
        {
            if (cache_v[depth].TryGetValue(board, out var cached)) { move = cache_move[depth][board]; return cached; }

            var children = Board.GetMoves(board);
            if (depth == 0 || Board.CurrentPlayerHasLessThanThree(board) || children.Count == 0)
            {
                move = 0UL;
                return Heuristic(board, IsWhite, children);
            }

            if(maximizingPlayer)
            {
                var bestValue = int.MinValue;
                ulong best_move = 0UL;
                foreach(var child in children)
                {
                    var v = minimax(child, depth - 1, false, out var unused);
                    if(v > bestValue)
                    {
                        bestValue = v;
                        best_move = child;
                    }
                }
                move = best_move;
                cache_v[depth][board] = bestValue;
                cache_move[depth][board] = move;
                return bestValue;
            }
            else
            {
                // minimizing player
                var bestValue = int.MaxValue;
                ulong best_move = 0UL;
                foreach (var child in children)
                {
                    var v = minimax(child, depth - 1, true, out var unused);
                    if (v < bestValue)
                    {
                        bestValue = v;
                        best_move = child;
                    }
                }
                move = best_move;
                cache_v[depth][board] = bestValue;
                cache_move[depth][board] = move;
                return bestValue;
            }
        }

        int alphabeta(ulong board, int depth, int alpha, int beta, bool maximizingPlayer, out ulong move)
        {
            if (cache_v[depth].TryGetValue(board, out var cached)) { move = cache_move[depth][board]; return cached; }

            var children = Board.GetMoves(board);
            if (depth == 0 || Board.CurrentPlayerHasLessThanThree(board) || children.Count == 0)
            {
                move = 0UL;
                return Heuristic(board, IsWhite, children);
            }

            if (maximizingPlayer)
            {
                var bestValue = int.MinValue;
                ulong best_move = 0UL;
                foreach (var child in children)
                {
                    var v = alphabeta(child, depth - 1, alpha, beta, false, out var unused);
                    if (v > bestValue)
                    {
                        if (v > alpha)
                            alpha = v;
                        bestValue = v;
                        best_move = child;
                    }
                    if (beta <= alpha)
                        break;
                }
                move = best_move;
                cache_v[depth][board] = bestValue;
                cache_move[depth][board] = move;
                return bestValue;
            }
            else
            {
                // minimizing player
                var bestValue = int.MaxValue;
                ulong best_move = 0UL;
                foreach (var child in children)
                {
                    var v = alphabeta(child, depth - 1, alpha, beta, true, out var unused);
                    if (v < bestValue)
                    {
                        if (v < beta)
                            beta = v;
                        bestValue = v;
                        best_move = child;
                    }
                    if (beta <= alpha)
                        break;
                }
                move = best_move;
                cache_v[depth][board] = bestValue;
                cache_move[depth][board] = move;
                return bestValue;
            }
        }

        public static int Heuristic(ulong board, bool max_for_white, List<ulong> possible_moves = null)
        {
            if (!Board.IsPlacementPhase(board))
            {
                if (Board.IsWhiteTurn(board) && Board.WhitePiecesOnBoard(board) < 3)
                {
                    if (max_for_white)
                        return -1000;
                    else
                        return 1000;
                }
                else if(!Board.IsWhiteTurn(board) && Board.BlackPiecesOnBoard(board) < 3)
                {
                    if (max_for_white)
                        return 1000;
                    else
                        return -1000;
                }
            }

            if (possible_moves == null)
                possible_moves = Board.GetMoves(board);

            if(possible_moves.Count == 0)
            {
                if(Board.IsWhiteTurn(board))
                {
                    if (max_for_white) return -1000; else return 1000;
                }
                else
                {
                    if (max_for_white) return 1000; else return -1000;
                }
            }

            /* We score +1 for a move, +2 if it forms a mill */
            int score = 0;
            var old_runs = Board.GetRunPieces(board);
            foreach (var move in possible_moves)
            {
                var new_runs = Board.GetRunPieces(move);
                if (new_runs != old_runs && Board.CountBits(new_runs) >= Board.CountBits(old_runs))
                {
                    score += 5;
                }
                else
                    score++;
            }

            if(Board.IsWhiteTurn(board))
            {
                if (max_for_white) return score; else return -score;
            }
            else
            {
                if (max_for_white) return -score; else return score;
            }
        }

        public static int Heuristic(ulong board, List<ulong> possible_moves = null)
        {
            if (Board.CurrentPlayerHasLessThanThree(board))
                return -1000;       // this is a losing position

            if (possible_moves == null)
                possible_moves = Board.GetMoves(board);

            if (possible_moves.Count == 0)
                return -1000;       // this is a losing position

            /* We score +1 for a move, +2 if it forms a mill */
            int score = 0;
            var old_runs = Board.GetRunPieces(board);
            foreach(var move in possible_moves)
            {
                var new_runs = Board.GetRunPieces(move);
                if (new_runs != old_runs && Board.CountBits(new_runs) >= Board.CountBits(old_runs))
                {
                    score += 5;
                }
                else
                    score++;
            }

            return score;
        }
    }
}

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
    class Game
    {
        public Player White;
        public Player Black;

        bool next_player = true;
        ulong board = Board.Starting;
        int turn = 0;

        Dictionary<int, string> spot_map = new Dictionary<int, string>();
        public Game()
        {
            // use HumanPlayer to populate spot_map
            var p = new HumanPlayer(true);
            foreach (var kvp in p.spot_map)
                spot_map[kvp.Value] = kvp.Key;
        }

        public bool MakeTurn()
        {
            if (next_player)
                Console.WriteLine("Turn: " + (turn++).ToString());

            Board.PrintBoard(board);

            List<ulong> AllowedMoves = null;
            if(Board.CurrentPlayerHasLessThanThree(board) || (AllowedMoves = Board.GetMoves(board)).Count == 0)
            {
                // Current player has lost
                if (next_player)
                    Console.Write("Black");
                else
                    Console.Write("White");
                Console.WriteLine(" Wins!");
                return false;
            }
            

            while (true)
            {
                ulong move;
                if (next_player)
                    move = White.GetMove(board);
                else
                    move = Black.GetMove(board);

                // ensure move is valid
                foreach (var allowed_move in AllowedMoves)
                {
                    if (allowed_move == move)
                    {
                        next_player = !next_player;
                        DumpMove(board, move);
                        board = move;
                        return true;
                    }
                }

                // move is invalid
                if (next_player)
                    White.IllegalMove(move);
                else
                    Black.IllegalMove(move);

                // request again
            }
        }

        private void DumpMove(ulong board, ulong move)
        {
            if (Board.IsWhiteTurn(board))
                Console.Write("White");
            else
                Console.Write("Black");

            // get bits set in original that are no longer set
            var unset_bits = board & ~move;
            var set_bits = move & ~board;

            ulong player_set;
            ulong player_unset;
            ulong opp_unset;

            if(Board.IsWhiteTurn(board))
            {
                player_set = Board.WhiteBoard(set_bits);
                player_unset = Board.WhiteBoard(unset_bits);
                opp_unset = Board.BlackBoard(unset_bits);
            }
            else
            {
                player_set = Board.BlackBoard(set_bits);
                player_unset = Board.BlackBoard(unset_bits);
                opp_unset = Board.WhiteBoard(unset_bits);
            }

            Console.Write(": ");
            if (player_unset != 0)
            {
                DumpPlace(player_unset);
                Console.Write(" -> ");
            }
            DumpPlace(player_set);
            if(opp_unset != 0)
            {
                Console.Write(" (");
                DumpPlace(opp_unset);
                Console.Write(")");
            }

            Console.WriteLine();
        }

        // dump first bit set
        private void DumpPlace(ulong board)
        {
            for (int i = 0; i < 24; i++)
            {
                if ((board & 0x1) == 0x1)
                {
                    Console.Write(spot_map[i]);
                    return;
                }
                board >>= 1;
            }
            Console.Write("{null}");
        }
    }
}

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
    abstract class Player
    {
        public bool IsWhite;

        abstract public ulong GetMove(ulong board);
        abstract public void IllegalMove(ulong move);
    }

    class HumanPlayer : Player
    {
        internal Dictionary<string, int> spot_map = new Dictionary<string, int>();

        public HumanPlayer(bool white)
        {
            IsWhite = white;

            spot_map["a7"] = 0;
            spot_map["d7"] = 1;
            spot_map["g7"] = 2;
            spot_map["b6"] = 3;
            spot_map["d6"] = 4;
            spot_map["f6"] = 5;
            spot_map["c5"] = 6;
            spot_map["d5"] = 7;
            spot_map["e5"] = 8;
            spot_map["a4"] = 9;
            spot_map["b4"] = 10;
            spot_map["c4"] = 11;
            spot_map["e4"] = 12;
            spot_map["f4"] = 13;
            spot_map["g4"] = 14;
            spot_map["c3"] = 15;
            spot_map["d3"] = 16;
            spot_map["e3"] = 17;
            spot_map["b2"] = 18;
            spot_map["d2"] = 19;
            spot_map["f2"] = 20;
            spot_map["a1"] = 21;
            spot_map["d1"] = 22;
            spot_map["g1"] = 23;
        }

        public override ulong GetMove(ulong board)
        {
            ulong player_board, opp_board;
            int player_piecestoplace, opp_piecestoplace;
            if (Board.IsWhiteTurn(board))
            {
                player_board = Board.WhiteBoard(board);
                opp_board = Board.BlackBoard(board);
                player_piecestoplace = Board.WhitePiecesLeftToPlace(board);
                opp_piecestoplace = Board.BlackPiecesLeftToPlace(board);
            }
            else
            {
                player_board = Board.BlackBoard(board);
                opp_board = Board.WhiteBoard(board);
                player_piecestoplace = Board.BlackPiecesLeftToPlace(board);
                opp_piecestoplace = Board.WhitePiecesLeftToPlace(board);
            }

            ulong new_player_board;
            if (Board.IsPlacementPhase(board))
            {
                var new_piece = InterpretInput("Place piece at: ");

                new_player_board = player_board | new_piece;
                player_piecestoplace--;
            }
            else
            {
                var old_piece = InterpretInput("Take piece from: ");
                var new_piece = InterpretInput("And place at: ");
                new_player_board = player_board & ~old_piece | new_piece;
            }
                throw new NotImplementedException();

            // see if we made a run
            var old_runs = Board.GetRunPieces(player_board);
            var new_runs = Board.GetRunPieces(new_player_board);
            ulong new_opp_board = opp_board;
            if (new_runs != old_runs && Board.CountBits(new_runs) >= Board.CountBits(old_runs))
            {
                var remove_piece = InterpretInput("Remove piece at: ");

                new_opp_board &= ~remove_piece;
            }

            return Board.CreateBoardForNextTurn(IsWhite, new_player_board, new_opp_board, player_piecestoplace, opp_piecestoplace);
        }

        private ulong InterpretInput(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                try
                {
                    var str = Console.ReadLine().ToLower().Substring(0, 2);

                    return (1UL << spot_map[str]);
                }
                catch (Exception)
                {

                }
            }
        }

        public override void IllegalMove(ulong move)
        {
            Console.WriteLine("You made an illegal move!");
        }
    }

    class DumbCpu : Player
    {
        public DumbCpu(bool is_white) { IsWhite = is_white; rnd = new Random(); }

        Random rnd;

        public override ulong GetMove(ulong board)
        {
            // just return a random possible move
            var possible_moves = Board.GetMoves(board);

            return possible_moves[rnd.Next(0, possible_moves.Count - 1)];
        }

        public override void IllegalMove(ulong move)
        {
            throw new NotImplementedException();
        }
    }

    class SemiDumbCpu : Player
    {
        public override ulong GetMove(ulong board)
        {
            // minimise the heuristic for the opponent
            int min_h = int.MaxValue;
            ulong best_move = 0UL;
            foreach(var move in Board.GetMoves(board))
            {
                var h = AIPlayer.Heuristic(move);
                if(h < min_h)
                {
                    min_h = h;
                    best_move = move;
                }
            }
            return best_move;
        }

        public override void IllegalMove(ulong move)
        {
            throw new NotImplementedException();
        }
    }
}

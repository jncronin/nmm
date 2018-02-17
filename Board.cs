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

/* Board is represented as ulong, therefore all functions here are static
 * 
 * Lowest 24 bits are spaces occupied by white.
 * Next 7 are integer count of white pieces remaining to be placed (placement phase) - allows up to 127 pieces.
 *  When zero the movement phase begins.
 * Next 1 bit is reserved
 * Next 24 bits are spaces occupied by black.
 * Next 7 are count of black pieces to be placed.
 * Last bit is 0 for white to play, 1 for black to play.
 *
 * Heuristic function is -1000 if player has lost (i.e. less than 2 pieces on board or cannot move)
 */

namespace nmm
{
    public class Board
    {
        public static ulong Starting = 0x900000009000000UL;        // 9 pieces in each hand

        // there are 16 potential runs.  we define them here
        static ulong[] PossibleRuns = new ulong[]
        {
            0x7,
            0x38,
            0x1c0,
            0xe00,
            0x7000,
            0x38000,
            0x1c0000,
            0xe00000,
            0x200201,
            0x40408,
            0x8840,
            0x92,
            0x490000,
            0x21100,
            0x102020,
            0x804004,
        };

        // potential along-line moves from each location
        static ulong[] PossibleLineMoves = new ulong[]
        {
            0x202,
            0x15,
            0x4002,
            0x410,
            0x55,
            0x2010,
            0x880,
            0x150,
            0x1080,
            0x200401,
            0x40A04,
            0x8440,
            0x22100,
            0x105020,
            0x802004,
            0x10800,
            0xA8000,
            0x11000,
            0x80400,
            0x550000,
            0x82000,
            0x400200,
            0xA80000,
            0x404000
        };

        public static bool IsWhiteTurn(ulong board) { return ((board >> 63) & 0x1) == 0x0; }
        public static int WhitePiecesLeftToPlace(ulong board) { return (int)((board >> 24) & 0x7f); }
        public static int BlackPiecesLeftToPlace(ulong board) { return (int)((board >> 56) & 0x7f); }
        public static int PiecesLeftToPlace(ulong board) { if (IsWhiteTurn(board)) return WhitePiecesLeftToPlace(board); else return BlackPiecesLeftToPlace(board); }
        public static bool IsPlacementPhase(ulong board) { return PiecesLeftToPlace(board) > 0; }

        internal static int CountBits(ulong v)
        {
            // can be optimised with bit twiddling
            int count = 0;
            for(int i = 0; i < 64; i++)
            {
                if ((v & 0x1) == 0x1)
                    count++;
                v >>= 1;
            }
            return count;
        }
        public static ulong WhiteBoard(ulong board) { return board & 0xffffffUL; }
        public static ulong BlackBoard(ulong board) { return (board >> 32) & 0xffffffUL; }
        public static int WhitePiecesOnBoard(ulong board) { return (CountBits(WhiteBoard(board))); }
        public static int BlackPiecesOnBoard(ulong board) { return (CountBits(BlackBoard(board))); }
        public static bool IsMovementPhase(ulong board) { if (IsPlacementPhase(board)) return false; if (IsWhiteTurn(board)) return WhitePiecesOnBoard(board) > 3; else return BlackPiecesOnBoard(board) > 3; }
        public static bool CurrentPlayerHasLessThanThree(ulong board) { if (IsPlacementPhase(board)) return false; if (IsWhiteTurn(board)) return WhitePiecesOnBoard(board) < 3; else return BlackPiecesOnBoard(board) < 3; }
        public static bool IsFlyingPhase(ulong board) { if (IsPlacementPhase(board)) return false; if (IsWhiteTurn(board)) return WhitePiecesOnBoard(board) == 3; else return BlackPiecesOnBoard(board) == 3; }


        public static List<ulong> GetMoves(ulong board)
        {
            ulong player_board, opp_board;
            int player_piecestoplace, opp_piecestoplace;
            if(IsWhiteTurn(board))
            {
                player_board = WhiteBoard(board);
                opp_board = BlackBoard(board);
                player_piecestoplace = WhitePiecesLeftToPlace(board);
                opp_piecestoplace = BlackPiecesLeftToPlace(board);
            }
            else
            {
                player_board = BlackBoard(board);
                opp_board = WhiteBoard(board);
                player_piecestoplace = BlackPiecesLeftToPlace(board);
                opp_piecestoplace = WhitePiecesLeftToPlace(board);
            }

            /* First, build a list of new 24-bit player boards.  We then compare these with the previous to see
             *  if a mill has been created */
            List<ulong> new_player_boards = new List<ulong>();
            if(player_piecestoplace > 0)
            {
                // placement phase
                ulong possible_positions = ~player_board & ~opp_board;
                for(int i = 0; i < 24; i++)
                {
                    ulong new_position = possible_positions & (1UL << i);
                    if (new_position != 0)
                        new_player_boards.Add(player_board | new_position);
                }
                player_piecestoplace--;
            }
            else
            {
                // movement/flying phase
                bool is_movement = IsMovementPhase(board);
                // first iterate starting point for pieces
                for(int i = 0; i < 24; i++)
                {
                    if((player_board & (1UL << i)) != 0)
                    {
                        // there is a valid piece at this stating location
                        ulong possible_moves;
                        if (is_movement)
                            possible_moves = PossibleLineMoves[i];
                        else
                            possible_moves = 0xffffffUL;    // in flying phase can move to any location

                        // mask out those locations which already have a piece
                        possible_moves = possible_moves & ~player_board & ~opp_board;
                        if(possible_moves != 0)
                        {
                            // iterate destination locations
                            for(int j = 0; j < 24; j++)
                            {
                                if((possible_moves & (1UL << j)) != 0)
                                {
                                    // remove starting location, add destination location
                                    new_player_boards.Add(player_board & ~(1UL << i) | (1UL << j));
                                }
                            }
                        }
                    }
                }
            }

            /* Determine which pieces we could potentially remove from the opponent */
            var opp_runs = GetRunPieces(opp_board);
            var opp_removable_pieces = opp_board & opp_runs;
            // unless all pieces are part of a run in which case we can
            if (opp_removable_pieces == 0)
                opp_removable_pieces = opp_board;

            /* Now determine new vs old runs to decide if this is a move that can remove an opponent piece */
            var old_runs = GetRunPieces(player_board);

            var ret = new List<ulong>();
            foreach(var new_player_board in new_player_boards)
            {
                var new_runs = GetRunPieces(new_player_board);
                if(new_runs != old_runs && CountBits(new_runs) >= CountBits(old_runs))
                {
                    /* We have formed a new run.  Iterate opp_removable pieces to generate all possible moves */
                    for(int i = 0; i < 24; i++)
                    {
                        var opp_piece_to_remove = opp_removable_pieces & (1UL << i);
                        if(opp_piece_to_remove != 0)
                        {
                            var new_opp_board = opp_board & ~opp_piece_to_remove;
                            var new_board = CreateBoardForNextTurn(IsWhiteTurn(board), new_player_board, new_opp_board, player_piecestoplace, opp_piecestoplace);
                            ret.Add(new_board);
                        }
                    }
                }
                else
                {
                    /* We have not formed a new run - do nothing to the opponent pieces */
                    var new_board = CreateBoardForNextTurn(IsWhiteTurn(board), new_player_board, opp_board, player_piecestoplace, opp_piecestoplace);
                    ret.Add(new_board);
                }
            }

            return ret;
        }

        public static ulong CreateBoardForNextTurn(bool current_player_is_white, ulong new_player_board, ulong new_opp_board, int player_piecestoplace, int opp_piecestoplace)
        {
            ulong ret = 0x0UL;

            if(current_player_is_white)
            {
                ret |= new_player_board;
                ret |= (ulong)player_piecestoplace << 24;
                ret |= new_opp_board << 32;
                ret |= (ulong)opp_piecestoplace << 56;
                ret |= 0x8000000000000000UL;
            }
            else
            {
                ret |= new_opp_board;
                ret |= (ulong)opp_piecestoplace << 24;
                ret |= new_player_board << 32;
                ret |= (ulong)player_piecestoplace << 56;
            }
            return ret;
        }

        /* Return those pices which comprise a run */
        public static ulong GetRunPieces(ulong player_board)
        {
            ulong ret = 0UL;
            foreach(var run in PossibleRuns)
            {
                if ((run & player_board) == run)
                    ret |= run;
            }
            return ret;
        }

        public static void PrintBoard(ulong board)
        {
            if (IsWhiteTurn(board))
                Console.Write("White");
            else
                Console.Write("Black");
            Console.Write(" to play: ");
            if (IsPlacementPhase(board))
                Console.WriteLine("Placement Phase");
            else if (IsMovementPhase(board))
                Console.WriteLine("Movement Phase");
            else
                Console.WriteLine("Flying Phase");
            Console.WriteLine("Pieces to play: White: " + WhitePiecesLeftToPlace(board) + ", Black: " + BlackPiecesLeftToPlace(board));

            /* O-----O-----O
             * |     |     |
             * | O---O---O |
             * | |   |   | |
             * | | O-O-O | |
             * | | |   | | |
             * O-O-O   O-O-O
             * | | |   | | |
             * | | O-O-O | |
             * | |   |   | |
             * | O---O---O |
             * |     |     |
             * O-----O-----O */

            Console.WriteLine("7 " + PrintPiece(board, 0) + "-----" + PrintPiece(board, 1) + "-----" + PrintPiece(board, 2));
            Console.WriteLine("  " + "|     |     |");
            Console.WriteLine("6 " + "| " + PrintPiece(board, 3) + "---" + PrintPiece(board, 4) + "---" + PrintPiece(board, 5) + " |");
            Console.WriteLine("  " + "| |   |   | |");
            Console.WriteLine("5 " + "| | " + PrintPiece(board, 6) + "-" + PrintPiece(board, 7) + "-" + PrintPiece(board, 8) + " | |");
            Console.WriteLine("  " + "| | |   | | |");
            Console.WriteLine("4 " + PrintPiece(board, 9) + "-" + PrintPiece(board, 10) + "-" + PrintPiece(board, 11) + "   " + PrintPiece(board, 12) + "-" + PrintPiece(board, 13) + "-" + PrintPiece(board, 14));
            Console.WriteLine("  " + "| | |   | | |");
            Console.WriteLine("3 " + "| | " + PrintPiece(board, 15) + "-" + PrintPiece(board, 16) + "-" + PrintPiece(board, 17) + " | |");
            Console.WriteLine("  " + "| |   |   | |");
            Console.WriteLine("2 " + "| " + PrintPiece(board, 18) + "---" + PrintPiece(board, 19) + "---" + PrintPiece(board, 20) + " |");
            Console.WriteLine("  " + "|     |     |");
            Console.WriteLine("1 " + PrintPiece(board, 21) + "-----" + PrintPiece(board, 22) + "-----" + PrintPiece(board, 23));
            Console.WriteLine();
            Console.WriteLine("  a b c d e f g");
            Console.WriteLine();
        }

        private static string PrintPiece(ulong board, int v)
        {
            ulong w = WhiteBoard(board);
            ulong b = BlackBoard(board);
            if ((w & (1UL << v)) != 0x0)
                return "W";
            else if ((b & (1UL << v)) != 0x0)
                return "B";
            else
                return "O";
        }
    }
}

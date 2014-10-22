/*
 * Program:         Scrabble
 * Module:          PlotTiles.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     Structure that obtains the row number, column number, and letter for location on a grid  
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;

namespace TilesLibrary
{  
    //Structure that hold the row number, column number, and tile letter of the tiles to be plotted onto the grid
    public struct plotTileStruct
    {
        public int rowNum, colNum;
        public char tileLetter;
        public plotTileStruct(int r, int c, char tl)
        {
            rowNum = r;
            colNum = c;
            tileLetter = tl;
        }
    }
}

/*
 * Program:         Scrabble
 * Module:          BoardData.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     Class that holds all the data based on a grid.
 *                  Including row numbers, column numbers, tile letters, button number, and background image for the grid.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Drawing;
using System.Windows.Controls;

using System.Runtime.Serialization;


namespace TilesLibrary
{
    //Class BoardData that holds every row number, column number, tile letter, button number, and background image per grid
    [DataContract]
    public class BoardData
    {
        [DataMember]
        public int rowNum { get; set; }
        [DataMember]
        public int colNum { get; set; }
        [DataMember]
        public char tileLetter { get; set; }
        [DataMember]
        public Button buttonNum { get; set; }
        [DataMember]
        public Image gridBackground { get; set; }

        public BoardData(int r, int c, char tl, Button bn, Image gbg)
        {
            rowNum = r;
            colNum = c;
            tileLetter = tl;
            buttonNum = bn;
            gridBackground = gbg;
        }
    }
}

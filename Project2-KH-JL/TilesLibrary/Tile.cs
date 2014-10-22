/*
 * Program:         Scrabble
 * Module:          Tile.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     Tile class holds all the letters for each tile.
 *                  It also gets the letter, value, and returns the name
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;

namespace TilesLibrary
{
    [DataContract]
    public class Tile
    {
        public enum LetterID
        {
            A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z //, Blank
        };

        // Member variables and accessor methods
        [DataMember] 
        public LetterID Letter { get; private set; }
        [DataMember] 
        public int Value { get; private set; }
        [DataMember]
        public string Name { get { return Letter.ToString() + " of " + Value.ToString(); } private set { } }

        // C'tor
        public Tile(LetterID s, int r)
        {
            Letter = s;
            Value = r;
        }
    }
}

/*
 * Program:         Scrabble
 * Module:          PlayerHand.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     Defines the data contract and datamembers for a PlayerHand and draws a specific number of tiles for 
 *                  each player. Can draw Tiles from the TileBag to contain 7 tiles per hand.
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
    public class PlayerHand
    {
        //List of tiles form each player hand
        [DataMember]
        public List<Tile> Hand { get; private set; }

        public PlayerHand() { 
            Console.WriteLine("Constructing a PlayerHand object");
            Hand = new List<Tile>();
        }

        //Draw tiles for the player hand to fill it with 7 Tiles
        public void DrawHand(TileBag b)
        {
            while (this.Hand == null || this.Hand.Count < 7)
            {
                Tile tile = b.Draw();
                this.Hand.Add(tile);
            }
        }
    }
}

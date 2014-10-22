/*
 * Program:         Scrabble
 * Module:          CallbackInfo.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     declares the data contract and data members to send the numbers of tiles, endgame state, playerhand, playerid, 
 *                  whose turn it is, board information, words played and total store to the clients.
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
    public class CallbackInfo
    {
        [DataMember]
        public int NumTiles { get; private set; }
        [DataMember]
        public bool EndGame { get; private set; }
        [DataMember]
        public PlayerHand PlayerHand { get; private set; }
        [DataMember]
        public int PlayerID { get; private set; }
        [DataMember]
        public bool MyTurn { get; private set; }
        [DataMember]
        public int PlayerEndTurn { get; private set; }
        [DataMember]
        public List<plotTileStruct> BoardInformation { get; set; }
        [DataMember]
        public Dictionary<int, int> TotalPlayerScore { get; private set; }
        [DataMember]
        public bool UpdateBoard { get; private set; }
        [DataMember]
        public int LastTurnScore { get; private set; }
        [DataMember]
        public List<string> WordsPlayed { get; private set; }

        public CallbackInfo(int t, bool e, PlayerHand tiles, int playerNumber, bool flg, int pet, List<plotTileStruct> bInfo, Dictionary<int, int> totalPlayerScore, bool updateBoard, int scoreOne, List<string> wordsPlayed)
        {
            NumTiles = t;
            EndGame = e;
            PlayerHand = tiles;
            PlayerID = playerNumber;
            MyTurn = flg;
            PlayerEndTurn = pet;
            BoardInformation = bInfo;
            TotalPlayerScore = totalPlayerScore;
            UpdateBoard = updateBoard;
            LastTurnScore = scoreOne;
            WordsPlayed = wordsPlayed;
        }
    }
}

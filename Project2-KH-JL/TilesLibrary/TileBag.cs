/*
 * Program:         Scrabble
 * Module:          TileBag.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     TileBag holds all the functionality of the Scrabble game, as well as connects to the service.
 *                  TileBag contains logic to start the game, draw the player hands, randomize the tiles, validate the word against the grid,
 *                  validate word against the dictionary, score the words, update the PlayerLobby and MainWindow, and end the game.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Drawing;
using System.Windows.Controls;
using System.Data;
using System.Data.OleDb;

namespace TilesLibrary
{
    // callback contract for twoway communication
    [ServiceContract]
    public interface ICallback
    {
        [OperationContract(IsOneWay = true)] // so it won't wait on this causing a deadlock
        void UpdateGui(CallbackInfo info);
        [OperationContract(IsOneWay = true)]
        void UpdateLobbyGui(int clientCount, bool startGame);
    }
    //ITileBag
    [ServiceContract(CallbackContract = typeof(ICallback))]
    public interface ITileBag
    {
        [OperationContract]
        Tile Draw();
        [OperationContract(IsOneWay = true)]
        void Shuffle();
        int NumTiles { [OperationContract] get; }
        [OperationContract(IsOneWay = true)]
        void RegisterForCallbacks();  // so we can get callbacks
        [OperationContract(IsOneWay = true)]
        void UnregisterForCallbacks();
        [OperationContract(IsOneWay = true)]
        void StartGame();
        [OperationContract(IsOneWay = true)]
        void UpdateLobby(bool gameStart, bool playerLeftLWin);
        int ClientCount { [OperationContract] get; }
        [OperationContract(IsOneWay = true)]
        void PlotAllTiles(List<int> row, List<int> col, List<char> btnLetter);
        [OperationContract(IsOneWay = true)]
        void ExitGame();
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TileBag : ITileBag
    {
        //Struct
        public struct tileCoord
        {
            public int rowNum, colNum;
            public tileCoord(int r, int c)
            {
                rowNum = r;
                colNum = c;
            }
        }

        // Arrays
        private string[] tileLetters = new string[] {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" //, Blank
        };
        private int[] tileScores = new int[] {
             1,   3,   3,   2,   1,   4,   2,   4,   1,   8,   5,   1,   3,   1,   1,   3,   10,  1,   1,   1,   1,   4,   4,   8,   4,   10
        };
        private int[] tileQuantities = new int[] {
             9,   2,   2,   4,   12,  2,   3,   2,   9,   1,   1,   4,   2,   6,   8,   2,   1,   6,   4,   6,   4,   2,   2,   1,   2,   1
        };

        //Dictionaries
        private Dictionary<tileCoord, char> currentBoardStateDict = new Dictionary<tileCoord, char>();// tracks occupied positions and values on the board from previous turns
        // keys for totalPlayerScoreDict, clientCallbacks, playerHands corespond with eachother
        private Dictionary<int, int> totalPlayerScoreDict = new Dictionary<int, int>();// current players scores
        private Dictionary<int, ICallback> clientCallbacks = new Dictionary<int, ICallback>(); // current player client callbacks
        private Dictionary<int, PlayerHand> playerHands = new Dictionary<int, PlayerHand>(); // current player hand tile sets
        // dictionary contained board letter and word bonuses based on grid coordinates
        private Dictionary<tileCoord, string> bonusCoordDataDict = new Dictionary<tileCoord, string>()
        {
            {new tileCoord {rowNum= 0, colNum = 3}, "TW"},{new tileCoord {rowNum= 0, colNum = 6}, "TL"},{new tileCoord {rowNum= 0, colNum = 8}, "TL"},{new tileCoord {rowNum= 0, colNum = 11}, "TW"},
            {new tileCoord {rowNum= 1, colNum = 2}, "DL"},{new tileCoord {rowNum= 1, colNum = 5}, "DW"},{new tileCoord {rowNum= 1, colNum = 9}, "DW"},{new tileCoord {rowNum= 1, colNum = 12}, "DL"},
            {new tileCoord {rowNum= 2, colNum = 1}, "DL"},{new tileCoord {rowNum= 2, colNum = 4}, "DL"},{new tileCoord {rowNum= 2, colNum = 10}, "DL"},{new tileCoord {rowNum= 2, colNum = 13}, "DL"},
            {new tileCoord {rowNum= 3, colNum = 0}, "TW"},{new tileCoord {rowNum= 3, colNum = 3}, "TL"},{new tileCoord {rowNum= 3, colNum = 7}, "DW"},{new tileCoord {rowNum= 3, colNum = 11}, "TL"}, 
            {new tileCoord {rowNum= 3, colNum = 14}, "TW"},{new tileCoord {rowNum= 4, colNum = 2}, "DL"},{new tileCoord {rowNum= 4, colNum = 6}, "DL"},{new tileCoord {rowNum= 4, colNum = 8}, "DL"}, 
            {new tileCoord {rowNum= 4, colNum = 12}, "DL"},{new tileCoord {rowNum= 5, colNum = 1}, "DW"},{new tileCoord {rowNum= 5, colNum = 5}, "TL"},{new tileCoord {rowNum= 5, colNum = 9}, "TL"},
            {new tileCoord {rowNum= 5, colNum = 13}, "DW"},{new tileCoord {rowNum= 6, colNum = 0}, "TL"},{new tileCoord {rowNum= 6, colNum = 4}, "DL"},{new tileCoord {rowNum= 6, colNum = 10}, "DL"},
            {new tileCoord {rowNum= 6, colNum = 14}, "TL"},{new tileCoord {rowNum= 7, colNum = 3}, "DW"},{new tileCoord {rowNum= 7, colNum = 11}, "DW"}, {new tileCoord {rowNum= 8, colNum = 0}, "TL"},
            {new tileCoord {rowNum= 8, colNum = 4}, "DL"},{new tileCoord {rowNum= 8, colNum = 10}, "DL"},{new tileCoord {rowNum= 8, colNum = 14}, "TL"},{new tileCoord {rowNum= 11, colNum = 14}, "TW"},
            {new tileCoord {rowNum= 9, colNum = 1}, "DW"},{new tileCoord {rowNum= 9, colNum = 5}, "TL"},{new tileCoord {rowNum= 9, colNum = 9}, "TL"},{new tileCoord {rowNum= 9, colNum = 13}, "DW"},
            {new tileCoord {rowNum= 10, colNum = 2}, "DL"},{new tileCoord {rowNum= 10, colNum = 6}, "DL"},{new tileCoord {rowNum= 10, colNum = 8}, "DL"},{new tileCoord {rowNum= 10, colNum = 12}, "DL"},
            {new tileCoord {rowNum= 11, colNum = 0}, "TW"},{new tileCoord {rowNum= 11, colNum = 3}, "TL"},{new tileCoord {rowNum= 11, colNum = 7}, "DW"},{new tileCoord {rowNum= 11, colNum = 11}, "TL"},
            {new tileCoord {rowNum= 12, colNum = 1}, "DL"},{new tileCoord {rowNum= 12, colNum = 4}, "DL"},{new tileCoord {rowNum= 12, colNum = 10}, "DL"},{new tileCoord {rowNum= 12, colNum = 13}, "DL"},
            {new tileCoord {rowNum= 13, colNum = 2}, "DL"},{new tileCoord {rowNum= 13, colNum = 5}, "DW"},{new tileCoord {rowNum= 13, colNum = 9}, "DW"},{new tileCoord {rowNum= 13, colNum = 12}, "DL"},
            {new tileCoord {rowNum= 14, colNum = 3}, "TW"},{new tileCoord {rowNum= 14, colNum = 6}, "TL"},{new tileCoord {rowNum= 14, colNum = 8}, "TL"},{new tileCoord {rowNum= 14, colNum = 11}, "TW"}
        };

        // Lists
        // a list of the tiles in the bag
        private List<Tile> tiles = new List<Tile>();
        // a list to track the tiles played in the current turn so they can be sent to update the clients
        private List<plotTileStruct> plotTilesList = new List<plotTileStruct>();
        // a list of the words scored on the current turn to be sent back to the clients
        private List<string> wordsPlayed;
        
        // Member Variables
        private int tileIdx;
        private int nextCallbackId = 1;
        private int totalTurnScore = 0;
        // track the current players turn and their key 
        private int playerTurn = 1;
        private int playerTurnIdx = 1;
        private bool firstTurn = true;
        private bool advanceTurn = false;
        private bool updateBoard = false;
        private int tilesCount = 0;

        // C'tor
        public TileBag()
        {
            try
            {
                Console.WriteLine("Constructing a TileBag object");

                tileIdx = 0;
                repopulate();
                tilesCount = tiles.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Constructing the TileBag: " + ex.Message);
            }
        }

        // Public methods

        // Return: the next Tile from the bag
        public Tile Draw()
        {
            if (tileIdx == tiles.Count)
                throw new System.IndexOutOfRangeException("The Bag is empty. Please reset.");

            --tilesCount;
            Tile tile = tiles[tileIdx++];
            return tile;
        }

        // Randomize all the tiles in the tilebag
        public void Shuffle()
        {
            try
            {
                Console.WriteLine("Shaking the Bag");
                randomizeTiles();

                updateAllClients(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Shuffling the Bag: " + ex.Message);
            }
        }

        //Returns: the number of tiles left
        public int NumTiles
        {
            get { return tiles.Count - tileIdx; }
        }

        // Register for callbacks
        public void RegisterForCallbacks()
        {
            // Store ICallback interface (client object) reference for the client, which is currently calling RegisterForCallbacks()
            try
            {
                ICallback cb = OperationContext.Current.GetCallbackChannel<ICallback>();
                clientCallbacks.Add(nextCallbackId, cb);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Registering For Callbacks: " + ex.Message);
            }
            nextCallbackId++;
        }

        // Unregister the callback the called this
        public void UnregisterForCallbacks()
        {
            try
            {
                clientCallbacks.Remove(clientCallbacks.First(kvp => kvp.Value == OperationContext.Current.GetCallbackChannel<ICallback>()).Key);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Unregistering For Callbacks: " + ex.Message);
            }
        }

        //Start the game by drawing 7 tiles for each player
        public void StartGame()
        {
            try
            {
                // set the first player
                playerTurnIdx = clientCallbacks.ToList()[playerTurn - 1].Key;
                foreach (int i in clientCallbacks.Keys)
                {
                    playerHands.Add(i, new PlayerHand());
                    playerHands[i].DrawHand(this); //draw tiles 
                    totalPlayerScoreDict.Add(i, 0);
                }
                updateAllClients(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Populating Player Hand! " + ex.Message);
            }
        }

        // Update all the clients
        // parameter: bool of whether the game is over
        private void updateAllClients(bool endGame)
        {
            try
            {
                // if the turn needs to be passed to the next player
                if (advanceTurn)
                {
                    playerTurn++;
                    if (playerTurn > clientCallbacks.Count())
                    {
                        playerTurn = 1;
                    }
                    // set the new current player turn
                    playerTurnIdx = clientCallbacks.ToList()[playerTurn-1].Key;
                }

                // loop through all clients turning the tile count, playerhand, player turn, word scores
                foreach (int i in clientCallbacks.Keys)
                {
                    CallbackInfo info = new CallbackInfo(tiles.Count - tileIdx, endGame, playerHands[i], i, i == playerTurnIdx, playerTurnIdx, plotTilesList, totalPlayerScoreDict, updateBoard, totalTurnScore, wordsPlayed);
                    clientCallbacks[i].UpdateGui(info);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Updating clients! " + ex.Message);
            }
        }

        // Update the Lobby client checking if the game has been started or if a player has left the game
        // parameters: bool of whether the game is starting, bool of whether to remove this player from the lobby when they leave the game
        private void updateLobbyClients(bool gameStart, bool removeCall)
        {
            try
            {
                //Check if the game started
                if (gameStart)
                {
                    StartGame();
                }
                //Check if a player left the game
                else if (removeCall)
                {
                    UnregisterForCallbacks();
                }
                // loop through all clients and update them
                foreach (int i in clientCallbacks.Keys)
                {
                    clientCallbacks[i].UpdateLobbyGui(ClientCount, gameStart);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating Lobby! " + ex.Message);
            }
        }

        // Repopulate the tilebag
        private void repopulate()
        {
            try
            {
                Console.WriteLine("Repopulating the Tilebag");

                // Remove "old" tiles
                tiles.Clear();

                // Populate with new tiles
                for (int i = 0; i < tileLetters.Length; i++)
                {
                    for (int j = 0; j < tileQuantities[i]; j++)
                    {
                        // Add a tile
                        tiles.Add(new Tile((Tile.LetterID)Enum.Parse(typeof(Tile.LetterID), tileLetters[i]), tileScores[i]));
                    }
                }
                // Shuffle the tiles
                randomizeTiles();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Repopulating the TileBag: " + ex.Message);
            }
        }

        // Randomize the tiles in the tilebag
        private void randomizeTiles()
        {
            try
            {
                Random rand = new Random();
                Tile temp;

                for (int i = 0; i < tiles.Count; i++)
                {
                    // Choose a random index
                    int randIdx = rand.Next(tiles.Count);

                    if (randIdx != i)
                    {
                        // Swap
                        temp = tiles[i];
                        tiles[i] = tiles[randIdx];
                        tiles[randIdx] = temp;
                    }

                    // Start dealing off the top of the bag
                    tileIdx = 0;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Randomizing Tiles: " + ex.Message);
            }
        }

        // return: the number of current players
        public int ClientCount
        {
            get {
                return clientCallbacks.Count;
            }
        }

        // Update the player lobby 
        // parameters: bool of whether the game is starting, bool of whether to remove this player from the lobby when they leave the game
        public void UpdateLobby(bool gameStart, bool removePlayer)
        {
            try
            {
                updateLobbyClients(gameStart, removePlayer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Updating New Player to Lobby! " + ex.Message);
            } 
        }

        // Update to board for every client
        public void endTurn()
        {
            try
            {
                updateAllClients(false);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error Updating End Turn! " + ex.Message);  
            }
        }

        // ensure the correct player is playing then validates the players turn, updating current game state and scoring thrie turn
        // parameters: lists of the coordinates and the letters placed by the player
        public void PlotAllTiles(List<int> row, List<int> col, List<char> btnLetter)
        {
            try
            {
                plotTilesList.Clear();
                // ensure the correct player is trying to score
                if (clientCallbacks.FirstOrDefault(x => x.Value == OperationContext.Current.GetCallbackChannel<ICallback>()).Key == playerTurnIdx)
                {
                    // if the validate and score function passes, update the game state
                    if (validateAndScore(row, col, btnLetter))
                    {
                        // set the flag to pass play to the next player
                        advanceTurn = true;
                        updateBoard = true;
                        //Place the tiles on the board
                        for (int t = 0; t < btnLetter.Count(); t++)
                        {
                            currentBoardStateDict[new tileCoord(row[t], col[t])] = btnLetter[t];

                            //Clear player hand with tiles on the board and repopulate hand
                            plotTileStruct dataGrid = new plotTileStruct(row[t], col[t], btnLetter[t]);
                            plotTilesList.Add(dataGrid);
                            //Remove the button letter from the player hand and then call draw
                            for (int k = 0; 0 < playerHands[playerTurnIdx].Hand.Count(); ++k)
                            {
                                if (Convert.ToChar(playerHands[playerTurnIdx].Hand[k].Letter.ToString()) == btnLetter[t])
                                {
                                    //remove from playerHands dictionary
                                    playerHands[playerTurnIdx].Hand.Remove(playerHands[playerTurnIdx].Hand[k]);
                                    break;
                                }

                                //call the redraw until it hits 7 tiles and read to the dictionary
                                if (tilesCount <= 0)
                                {
                                    //End the game!
                                    updateAllClients(true);
                                    break;
                                }
                            }
                            playerHands[playerTurnIdx].DrawHand(this);
                        }
                        updateAllClients(false);
                    }
                    else
                    {
                        //If false clear those tiles from the board  
                        for (int i = 0; i < btnLetter.Count(); i++)
                        {
                            plotTileStruct dataGrid = new plotTileStruct(row[i], col[i], btnLetter[i]);
                            plotTilesList.Add(dataGrid);
                        }
                        advanceTurn = false;
                        updateBoard = false;
                        updateAllClients(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Updating Datagrid Dictionary! " + ex.Message);
            }
        }

        // Score the words
        // parameters: Lists of grid coordinates and a List of the characters played
        public void scoreWords(List<int> row, List<int> col, List<char> btnLetter)
        {
            int dwScore = 0;
            int twScore = 0;
            int thisWordScore = 0;

            try
            {
                //check the the points scored by the letter
                for (int i = 0; i < tileLetters.Count(); i++)
                {
                    for (int k = 0; k < btnLetter.Count(); k++)
                    {
                        if (Convert.ToChar(tileLetters[i]) == btnLetter[k])
                        {

                            int letterScore = tileScores[i];
                            // check for a bonus type under this tile and apply the bonus
                            tileCoord letter = new tileCoord(row[k], col[k]);
                            if (bonusCoordDataDict.ContainsKey(letter))
                            {
                                //get the key with the letter
                                string tileBonusValue = bonusCoordDataDict[letter];
                                bonusCoordDataDict.Remove(letter);

                                //check to see what to multiply the number or letter by
                                if (tileBonusValue == "DL")
                                {
                                    letterScore = letterScore * 2;
                                }
                                else if (tileBonusValue == "TL")
                                {
                                    letterScore = letterScore * 3;
                                }
                                else if (tileBonusValue == "TW")
                                {
                                    twScore++;

                                }
                                else if (tileBonusValue == "DW")
                                {
                                    dwScore++;
                                }
                            }
                            thisWordScore += letterScore;
                        }
                    }

                }
                thisWordScore = thisWordScore * Convert.ToInt32(Math.Pow(2, dwScore)) * Convert.ToInt32(Math.Pow(3, twScore));
                totalPlayerScoreDict[playerTurnIdx] += thisWordScore;
                totalTurnScore += thisWordScore;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Updating Player Score: " + ex.Message);
            }
        }

        // Validate each word on the board
        // parameters: Lists of grid coordinates and a List of the characters played
        // returns: bool of whether or not the validation passed
        public bool validateAndScore(List<int> lRow, List<int> lCol, List<char> lLetter)
        {
            try
            {
                totalTurnScore = 0;
                wordsPlayed = new List<string>();
                string wordToValidate;
                List<List<int>> scoreRowCoord = new List<List<int>>();
                List<List<int>> scoreColCoord = new List<List<int>>();
                List<List<char>> scoreLetter = new List<List<char>>();
                // set the first word to the List of all the played tiles, for validation and scoring
                scoreRowCoord.Add(new List<int>(lRow));
                scoreColCoord.Add(new List<int>(lCol));
                scoreLetter.Add(new List<char>(lLetter));

                bool isHorizontal = false;
                bool isConnected = false;
                int tilesPlayed = lRow.Count;
                // ensure sure there isn't already a letter in the new tile locations
                for (int i = 0; i < tilesPlayed; ++i)
                {
                    if (currentBoardStateDict.ContainsKey(new tileCoord(lRow[i], lCol[i])))
                    {
                        return false;
                    }
                }
                // no tiles were placed, score 0
                if (tilesPlayed == 0)
                {
                    wordsPlayed.Add("no word played");
                    return true;
                }
                // if this is the first turn, validate the word being on the center and having consecutive locations
                else if (firstTurn)
                {
                    bool foundMore = false;
                    // check if the first tile is on the center
                    bool onCenter = false;
                    if (lRow[0] == 7 && lCol[0] == 7)
                    {
                        onCenter = true;
                    }
                    wordToValidate = lLetter[0].ToString();
                    int rowCount = lRow[0];
                    int colCount = lCol[0];
                    // ensure there are 2 or more letters
                    if (tilesPlayed < 2)
                    {
                        return false;
                    }
                    if (lRow[0] == lRow[1])
                    {
                        // check left of first element
                        do
                        {
                            --colCount;
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed; ++i)
                            {
                                // if a played tile is at the row and column to the left of the previous tile
                                if (lCol[i] == colCount && lRow[i] == lRow[0])
                                {
                                    foundMore = true;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = thisLetter + wordToValidate;
                                    if (lCol[i] == 7 && lRow[i] == 7) onCenter = true;
                                }
                            }
                        } while (foundMore);
                        // check right of first element
                        colCount = lCol[0];
                        do
                        {
                            ++colCount;
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed; ++i)
                            {
                                // if a played tile is at the row and column to the right of the previous tile
                                if (lCol[i] == colCount && lRow[i] == lRow[0])
                                {
                                    foundMore = true;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = wordToValidate + thisLetter;
                                    if (lCol[i] == 7 && lRow[i] == 7) onCenter = true;
                                }
                            }
                        } while (foundMore);
                    }
                    else if (lCol[0] == lCol[1])
                    {
                        // check left of first element
                        do
                        {
                            --rowCount;
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed; ++i)
                            {
                                // if a played tile is at the row and column to the left of the previous tile
                                if (lRow[i] == rowCount && lCol[i] == lCol[0])
                                {
                                    foundMore = true;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = thisLetter + wordToValidate;
                                    if (lRow[i] == 7 && lCol[i] == 7) onCenter = true;
                                }
                            }
                        } while (foundMore);
                        // check right of first element
                        rowCount = lRow[0];
                        do
                        {
                            ++rowCount;
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed; ++i)
                            {
                                // if a played tile is at the row and column to the right of the previous tile
                                if (lRow[i] == rowCount && lCol[i] == lCol[0])
                                {
                                    foundMore = true;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = wordToValidate + thisLetter;
                                    if (lRow[i] == 7 && lCol[i] == 7) onCenter = true;
                                }
                            }
                        } while (foundMore);
                    }
                    else
                    {
                        return false;
                    }
                    // check that all the tiles are part of this word then validate
                    if (validateWord(wordToValidate) && onCenter && wordToValidate.Length == lRow.Count)
                    {
                        firstTurn = false;
                        scoreWords(scoreRowCoord[0], scoreColCoord[0], scoreLetter[0]);
                        wordsPlayed.Add(wordToValidate);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                // if only one tile was played
                else if (tilesPlayed == 1)
                {
                    // current row
                    wordToValidate = lLetter[0].ToString();
                    int rowCount = lRow[0];
                    int colCount = lCol[0];
                    // if there is a tile on the board above or below this tile
                    if (currentBoardStateDict.ContainsKey(new tileCoord(rowCount - 1, lCol[0])) || currentBoardStateDict.ContainsKey(new tileCoord(rowCount + 1, lCol[0])))
                    {
                        isHorizontal = true;
                        // while there are tiles above this
                        while (currentBoardStateDict.ContainsKey(new tileCoord(--rowCount, lCol[0])))
                        {
                            // checking for a vertical word
                            char thisLetter = currentBoardStateDict[new tileCoord(rowCount, lCol[0])];
                            wordToValidate = thisLetter + wordToValidate;
                            scoreRowCoord[0].Add(rowCount);
                            scoreColCoord[0].Add(lCol[0]);
                            scoreLetter[0].Add(thisLetter);
                            isConnected = true;
                        }
                        // reset rowCounter to the original letter
                        rowCount = lRow[0];
                        // while there are tiles below this
                        while (currentBoardStateDict.ContainsKey(new tileCoord(++rowCount, lCol[0])))
                        {
                            char thisLetter = currentBoardStateDict[new tileCoord(rowCount, lCol[0])];
                            wordToValidate = wordToValidate + thisLetter;
                            scoreRowCoord[0].Add(rowCount);
                            scoreColCoord[0].Add(lCol[0]);
                            scoreLetter[0].Add(thisLetter);
                            isConnected = true;
                        }
                        if (!validateWord(wordToValidate))
                        {
                            return false;
                        }
                        wordsPlayed.Add(wordToValidate);
                        // validate wordToValidate
                    }
                    // else if there is a tile to the left or right of this tile
                    if (currentBoardStateDict.ContainsKey(new tileCoord(lRow[0], colCount - 1)) || currentBoardStateDict.ContainsKey(new tileCoord(lRow[0], colCount + 1)))
                    {
                        // reset word builder variables
                        wordToValidate = lLetter[0].ToString();
                        rowCount = lRow[0];
                        colCount = lCol[0];
                        int wordCounter = 0;
                        // if there was a horizontal word then add another element to our list of word to score
                        if (isHorizontal)
                        {
                            scoreRowCoord.Add(new List<int>(lRow));
                            scoreColCoord.Add(new List<int>(lCol));
                            scoreLetter.Add(new List<char>(lLetter));
                            wordCounter = 1;
                        }
                        // while there are tiles to the left of this
                        while (currentBoardStateDict.ContainsKey(new tileCoord(lRow[0], --colCount)))
                        {
                            // checking for a horizontal adjacent tile
                            char thisLetter = currentBoardStateDict[new tileCoord(lRow[0], colCount)];
                            wordToValidate = thisLetter + wordToValidate;
                            scoreRowCoord[wordCounter].Add(lRow[0]);
                            scoreColCoord[wordCounter].Add(colCount);
                            scoreLetter[wordCounter].Add(thisLetter);
                            isConnected = true;
                        }
                        // reset col counter to placed tile location
                        colCount = lCol[0];
                        while (currentBoardStateDict.ContainsKey(new tileCoord(lRow[0], ++colCount)))
                        {
                            char thisLetter = currentBoardStateDict[new tileCoord(lRow[0], colCount)];
                            wordToValidate = wordToValidate + thisLetter;
                            scoreRowCoord[wordCounter].Add(lRow[0]);
                            scoreColCoord[wordCounter].Add(colCount);
                            scoreLetter[wordCounter].Add(thisLetter);
                            isConnected = true;
                        }
                        if (!validateWord(wordToValidate))
                        {
                            return false;
                        }
                        wordsPlayed.Add(wordToValidate);

                    }
                    if (isConnected)
                    {
                        for (int i = 0; i < scoreRowCoord.Count; ++i)
                        {
                            scoreWords(scoreRowCoord[i], scoreColCoord[i], scoreLetter[i]);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                // if more than one tile was played
                else
                {
                    wordToValidate = lLetter[0].ToString();
                    int rowCount = lRow[0];
                    int colCount = lCol[0];
                    // counter for how many placed tiles are part of this word, if not all tiles are accounted for it is somewhere else on the board and the word is invalid
                    int usedTileCount = 1;
                    // if the word appears to be on a row
                    if (lCol[0] == lCol[1])
                    {
                        isHorizontal = false;
                        // check left of tile
                        bool foundMore = true;
                        while (foundMore == true)
                        {
                            // check left for a tile on the board
                            while (currentBoardStateDict.ContainsKey(new tileCoord(--rowCount, lCol[0])))
                            {
                                char thisLetter = currentBoardStateDict[new tileCoord(rowCount, lCol[0])];
                                wordToValidate = thisLetter + wordToValidate;
                                scoreRowCoord[0].Add(rowCount);
                                scoreColCoord[0].Add(lCol[0]);
                                scoreLetter[0].Add(thisLetter);
                                isConnected = true;
                            }
                            // no more on the board so check the played tiles
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed && !foundMore; ++i)
                            {
                                // if a played tile is at the row and column to the left of the previous tile
                                if (lRow[i] == rowCount && lCol[i] == lCol[0])
                                {
                                    foundMore = true;
                                    ++usedTileCount;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = thisLetter + wordToValidate;
                                }
                            }
                        }
                        foundMore = true;
                        rowCount = lRow[0];
                        while (foundMore == true)
                        {
                            // check right for a tile on the board
                            while (currentBoardStateDict.ContainsKey(new tileCoord(++rowCount, lCol[0])))
                            {
                                char thisLetter = currentBoardStateDict[new tileCoord(rowCount, lCol[0])];
                                wordToValidate = wordToValidate + thisLetter;
                                scoreRowCoord[0].Add(rowCount);
                                scoreColCoord[0].Add(lCol[0]);
                                scoreLetter[0].Add(thisLetter);
                                isConnected = true;
                            }
                            // no more on the board so check the played tiles
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed && !foundMore; ++i)
                            {
                                // if a played tile is at the row and column to the right of the previous tile
                                if (lRow[i] == rowCount && lCol[i] == lCol[0])
                                {
                                    foundMore = true;
                                    ++usedTileCount;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = wordToValidate + thisLetter;
                                }
                            }
                        }
                        // if the played tiles aren't all in the word then validation fails
                        if (!validateWord(wordToValidate) || usedTileCount != tilesPlayed)
                        {
                            return false;
                        }
                        else
                        {
                            wordsPlayed.Add(wordToValidate);
                        }
                    }
                    // if the word appears to be on a row
                    else if (lRow[0] == lRow[1])
                    {
                        isHorizontal = true;
                        // check above the tile
                        bool foundMore = true;
                        while (foundMore == true)
                        {
                            // check left for a tile on the board
                            while (currentBoardStateDict.ContainsKey(new tileCoord(lRow[0], --colCount)))
                            {
                                char thisLetter = currentBoardStateDict[new tileCoord(lRow[0], colCount)];
                                wordToValidate = thisLetter + wordToValidate;
                                scoreRowCoord[0].Add(lRow[0]);
                                scoreColCoord[0].Add(colCount);
                                scoreLetter[0].Add(thisLetter);
                                isConnected = true;
                            }
                            // no more on the board so check the played tiles
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed && !foundMore; ++i)
                            {
                                // if a played tile is at the row and column to the left of the previous tile
                                if (lCol[i] == colCount && lRow[i] == lRow[0])
                                {
                                    foundMore = true;
                                    ++usedTileCount;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = thisLetter + wordToValidate;
                                }
                            }
                        }
                        foundMore = true;
                        colCount = lCol[0];
                        while (foundMore == true)
                        {
                            // check right for a tile on the board
                            while (currentBoardStateDict.ContainsKey(new tileCoord(lRow[0], ++colCount)))
                            {
                                char thisLetter = currentBoardStateDict[new tileCoord(lRow[0], colCount)];
                                wordToValidate = wordToValidate + thisLetter;
                                scoreRowCoord[0].Add(lRow[0]);
                                scoreColCoord[0].Add(colCount);
                                scoreLetter[0].Add(thisLetter);
                                isConnected = true;
                            }
                            // no more on the board so check the played tiles
                            foundMore = false;
                            for (int i = 1; i < tilesPlayed && !foundMore; ++i)
                            {
                                // if a played tile is at the row and column to the right of the previous tile
                                if (lCol[i] == colCount && lRow[i] == lRow[0])
                                {
                                    foundMore = true;
                                    ++usedTileCount;
                                    char thisLetter = lLetter[i];
                                    wordToValidate = wordToValidate + thisLetter;
                                }
                            }
                        }
                        // if the played tiles aren't all in the word then validation fails
                        if (usedTileCount != tilesPlayed)
                        {
                            return false;
                        }
                        // otherwise add it to the list of words to be validated and scored
                        else
                        {
                            if (!validateWord(wordToValidate))
                            {
                                return false;
                            }
                            wordsPlayed.Add(wordToValidate);
                        }

                    }
                    // first two tiles are neither horizontal or vertical to eachother
                    else
                    {
                        return false;
                    }

                    // if the played word was horizontal, check for vertical words off of the placed tiles
                    if (isHorizontal)
                    {
                        int wordCounter = 0;
                        for (int i = 0; i < lRow.Count; ++i)
                        {
                            rowCount = lRow[i];
                            colCount = lCol[i];
                            // if there is a tile on the board above or below this tile
                            if (currentBoardStateDict.ContainsKey(new tileCoord(rowCount - 1, lCol[i])) || currentBoardStateDict.ContainsKey(new tileCoord(rowCount + 1, lCol[i])))
                            {
                                isConnected = true;
                                // current row
                                wordToValidate = lLetter[i].ToString();
                                ++wordCounter;

                                scoreRowCoord.Add(new List<int>(lRow[i]));
                                scoreColCoord.Add(new List<int>(lCol[i]));
                                scoreLetter.Add(new List<char>(lLetter[i]));
                                scoreRowCoord[wordCounter].Add(lRow[i]);
                                scoreColCoord[wordCounter].Add(lCol[i]);
                                scoreLetter[wordCounter].Add(lLetter[i]);

                                // while there are tiles above this
                                while (currentBoardStateDict.ContainsKey(new tileCoord(--rowCount, lCol[i])))
                                {
                                    // checking for a vertical word
                                    char thisLetter = currentBoardStateDict[new tileCoord(rowCount, lCol[i])];
                                    wordToValidate = thisLetter + wordToValidate;
                                    scoreRowCoord[wordCounter].Add(rowCount);
                                    scoreColCoord[wordCounter].Add(lCol[i]);
                                    scoreLetter[wordCounter].Add(thisLetter);
                                }
                                // reset rowCounter to the original letter
                                rowCount = lRow[i];
                                // while there are tiles below this
                                while (currentBoardStateDict.ContainsKey(new tileCoord(++rowCount, lCol[i])))
                                {
                                    char thisLetter = currentBoardStateDict[new tileCoord(rowCount, lCol[i])];
                                    wordToValidate = wordToValidate + thisLetter;
                                    scoreRowCoord[wordCounter].Add(rowCount);
                                    scoreColCoord[wordCounter].Add(lCol[i]);
                                    scoreLetter[wordCounter].Add(thisLetter);
                                }
                                if (!validateWord(wordToValidate))
                                {
                                    return false;
                                }
                                wordsPlayed.Add(wordToValidate);
                            }
                        }
                    }
                    // otherwise it was vertical, so check for horizontal words off the placed tiles
                    else
                    {
                        int wordCounter = 0;
                        for (int i = 0; i < lRow.Count; ++i)
                        {
                            rowCount = lRow[i];
                            colCount = lCol[i];
                            if (currentBoardStateDict.ContainsKey(new tileCoord(lRow[i], colCount - 1)) || currentBoardStateDict.ContainsKey(new tileCoord(lRow[i], colCount + 1)))
                            {
                                isConnected = true;
                                // build the next word
                                ++wordCounter;
                                // reset word builder variables
                                wordToValidate = lLetter[i].ToString();
                                // add another element to our list of word to score
                                scoreRowCoord.Add(new List<int>(lRow[i]));
                                scoreColCoord.Add(new List<int>(lCol[i]));
                                scoreLetter.Add(new List<char>(lLetter[i]));

                                // while there are tiles to the left of this
                                while (currentBoardStateDict.ContainsKey(new tileCoord(lRow[i], --colCount)))
                                {
                                    // checking for a horizontal adjacent tile
                                    char thisLetter = currentBoardStateDict[new tileCoord(lRow[i], colCount)];
                                    wordToValidate = thisLetter + wordToValidate;
                                    scoreRowCoord[wordCounter].Add(lRow[i]);
                                    scoreColCoord[wordCounter].Add(colCount);
                                    scoreLetter[wordCounter].Add(thisLetter);
                                }
                                // reset col counter to placed tile location
                                colCount = lCol[i];
                                while (currentBoardStateDict.ContainsKey(new tileCoord(lRow[i], ++colCount)))
                                {
                                    char thisLetter = currentBoardStateDict[new tileCoord(lRow[i], colCount)];
                                    wordToValidate = wordToValidate + thisLetter;
                                    scoreRowCoord[wordCounter].Add(lRow[i]);
                                    scoreColCoord[wordCounter].Add(colCount);
                                    scoreLetter[wordCounter].Add(thisLetter);
                                }
                                if (!validateWord(wordToValidate))
                                {
                                    return false;
                                }
                                wordsPlayed.Add(wordToValidate);
                            }
                        }
                    }
                    //Check if the word is connected to other words 
                    if (isConnected)
                    {
                        for (int i = 0; i < scoreRowCoord.Count; ++i)
                        {
                            scoreWords(scoreRowCoord[i], scoreColCoord[i], scoreLetter[i]);
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Validating Word: " + ex.Message);
                return false;
            }
        }

        // Validate the word against the database
        // parameters: string of the word to be validated
        // returns : bool of whether or not the validation passed
        public bool validateWord(string wordToValidate)
        {
            try
            {
                //Connect to the database
                OleDbConnection conn = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=../../Scrabble.accdb");
                OleDbDataAdapter odba = new OleDbDataAdapter();
                odba.SelectCommand = new OleDbCommand("SELECT * FROM Dictionary WHERE Word = '" + wordToValidate + "'", conn);
                DataSet ds = new DataSet();
                conn.Open();
                odba.Fill(ds, "Dictionary");
                conn.Close();

                foreach (DataRow row in ds.Tables["Dictionary"].Rows)
                {
                    // if it returned a row then it is valid, but testing it agains our word just in case
                    if (row.Field<String>("Word").ToUpper() == wordToValidate)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (OleDbException ex)
            {
                Console.WriteLine("Connection to Database Failed: " + ex.Message);
                return false;
            }
        }

        //Exit the game
        public void ExitGame()
        {
            updateAllClients(true);
        }
    }
}
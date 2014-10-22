/*
 * Program:         Scrabble
 * Module:          MainWindow.xaml.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     The main Scrabble windows that displays the grid to all players.
 *                  Drag and drop functions used to place tiles from the player hand onto the grid.
 *                  Tile bag and scores are placed within labels and listboxs dynamically.
 *                  Access to PlayerLobby and ExitGame window are generated from MainWindows Channel.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;

using System.ServiceModel;
using TilesLibrary;

namespace TilesClient
{
    //Allows threads to run stuff on the UI
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public partial class MainWindow : Window, ICallback
    {
        //Global Variables
        List<BoardData> playedGridInfo = new List<BoardData>();
        List<plotTileStruct> allGridInfo = new List<plotTileStruct>();
        PlayerLobby pWin;
        //Drag and drop global variables
        Button flagBtnNum;
        private Point startpoint;
        private ITileBag bag = null;
        bool playerLeaveGWin = false;

        public MainWindow()
        {
            try
            {
                //Center the window on startup
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                InitializeComponent();
                createGrid();
                // Configure the Endpoint details
                DuplexChannelFactory<ITileBag> channel = new DuplexChannelFactory<ITileBag>(this, "TileBag");

                // Activate a remote Bag object
                bag = channel.CreateChannel();
                // Register this client for the callback service
                bag.RegisterForCallbacks();

                lblPlayerScore.Content = 0;
                pWin = new PlayerLobby(this, bag.ClientCount);
                pWin.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Initializing Window " + ex.Message);
            }
        }

        //Dynamically create the grid
        public void createGrid()
        {
            try
            {
                ScrabbleGrid.Width = 600;
                //Add the row and columns to the grid
                for (int i = 0; i < 15; i++)
                {
                    RowDefinition row = new RowDefinition();
                    row.Height = new GridLength(37);
                    ScrabbleGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    ScrabbleGrid.RowDefinitions.Add(row);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error Initializng Scrabble Grid" + ex.Message);
            }
        }

        //End the player turn and go to the next player in the list, if the entered word is valid
        private void btnEndTurn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<int> rowValues = new List<int>();
                List<int> colValues = new List<int>();
                List<char> letterValues = new List<char>();
                foreach (BoardData match in playedGridInfo)
                {
                    rowValues.Add(match.rowNum);
                    colValues.Add(match.colNum);
                    letterValues.Add(match.tileLetter);
                }

                bag.PlotAllTiles(rowValues, colValues, letterValues);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Ending Turn: " + ex.Message);
            }
        }

        // Helper methods
        private void updateTileCount()
        {
            lblBagCount.Content = bag.NumTiles.ToString();
        }

        // Implementation of the ICallback callback contract
        // delegate to allow passing of update to other thread
        private delegate void ClientUpdateDelegate(CallbackInfo info);

        // function to update the user interface with data from the callback
        public void UpdateGui(CallbackInfo info)
        {
            try
            {
                if (System.Threading.Thread.CurrentThread == this.Dispatcher.Thread)
                {
                    lblBagCount.Content = bag.NumTiles.ToString();
                    ScoreListBox.Items.Clear();
                    lblPlayerTurn.Content = info.PlayerEndTurn;
                    lblLastWord.Content = "";

                    foreach (KeyValuePair<int, int> match in info.TotalPlayerScore)
                    {
                        ScoreListBox.Items.Add("Player " + match.Key + "\t\t Score: " + match.Value.ToString());
                    }

                    //Set the players number label
                    PlayerNumber.Content = "Player " + info.PlayerID;
                    //Set the player scores  label
                    lblPlayerScore.Content = info.TotalPlayerScore[info.PlayerID];
                    //Set the player turn!
                    if (info.MyTurn)
                    {
                        // if the game is over, disable the player tiles
                        if (info.EndGame)
                        {
                            btnEndTurn.IsEnabled = false;
                            btnOne.IsEnabled = false;
                            btnTwo.IsEnabled = false;
                            btnThree.IsEnabled = false;
                            btnFour.IsEnabled = false;
                            btnFive.IsEnabled = false;
                            btnSix.IsEnabled = false;
                            btnSeven.IsEnabled = false;
                        }
                        else
                        {
                            btnEndTurn.IsEnabled = true;
                            btnOne.IsEnabled = true;
                            btnTwo.IsEnabled = true;
                            btnThree.IsEnabled = true;
                            btnFour.IsEnabled = true;
                            btnFive.IsEnabled = true;
                            btnSix.IsEnabled = true;
                            btnSeven.IsEnabled = true;
                        }
                    }
                    else
                    {
                        btnEndTurn.IsEnabled = false;
                        btnOne.IsEnabled = false;
                        btnTwo.IsEnabled = false;
                        btnThree.IsEnabled = false;
                        btnFour.IsEnabled = false;
                        btnFive.IsEnabled = false;
                        btnSix.IsEnabled = false;
                        btnSeven.IsEnabled = false;
                    }
                    // update the tiles to have the image of their tile
                    imageTiles(info.PlayerHand.Hand); 

                    //Update the player board - forloop to go through boardInformation
                    if (info.UpdateBoard)
                    {
                        for (int i = 0; i < info.BoardInformation.Count(); i++)
                        {
                            Image gridBG = new Image { Width = 35, Height = 35 };
                            var bitmapImage = new BitmapImage(new Uri(@"..\..\images\tiles\" + info.BoardInformation[i].tileLetter + ".png", UriKind.Relative));

                            gridBG.Source = bitmapImage;
                            gridBG.SetValue(Grid.ColumnProperty, info.BoardInformation[i].colNum);
                            gridBG.SetValue(Grid.RowProperty, info.BoardInformation[i].rowNum);
                            ScrabbleGrid.Children.Add(gridBG);
                            // update board locations so tiles can't be placed on eachother
                            allGridInfo.Add(new plotTileStruct(info.BoardInformation[i].rowNum, info.BoardInformation[i].colNum, info.BoardInformation[i].tileLetter));
                        }
                        // list all the words scored from the last players turn
                        foreach (string word in info.WordsPlayed)
                        {
                            lblLastWord.Content += word + " ";
                        }
                        lblLastWordScore.Content = info.LastTurnScore;
                        playedGridInfo.Clear();
                    }
                    else
                    {
                        //else remove the tiles off the board!
                        for (int i = 0; i < info.BoardInformation.Count(); i++)
                        {
                            foreach (BoardData match in playedGridInfo)
                            {
                                if (match.rowNum == info.BoardInformation[i].rowNum && match.colNum == info.BoardInformation[i].colNum)
                                {
                                    //Remove the image from the grid
                                    ScrabbleGrid.Children.Remove(match.gridBackground);
                                    playedGridInfo.Remove(match);
                                    break;
                                }
                            }
                        }
                    }
                    // if the callback calls for the end of the game
                    if (info.EndGame)
                    {
                        string endGame = "";
                        if(playerLeaveGWin)
                        {
                            endGame = "Player left the game!";
                        }
                        else
                        {
                            endGame = "Tilebag Empty! Game Over!";
                        }
                        EndGame eWin = new EndGame(info.TotalPlayerScore, endGame);
                        eWin.Show();
                    }
                }
                else
                {
                    // Only the main (dispatcher) thread can change the GUI so..
                    this.Dispatcher.BeginInvoke(new ClientUpdateDelegate(UpdateGui), info);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Updating Clients: " + ex.Message);
            }
        }

        //Update each tile in the hand with its specific image
        public void imageTiles(List<Tile> hand)
        {
            try
            {
                string s = hand[0].Letter.ToString();
                btnOne.Background = new ImageBrush(new BitmapImage(new Uri(@"..\..\images\tiles\" + s + ".png", UriKind.Relative)));
                btnOne.Content = s;
                btnOne.Visibility = Visibility.Visible;
                s = hand[1].Letter.ToString();
                btnTwo.Background = new ImageBrush(new BitmapImage(new Uri(@"..\..\images\tiles\" + s + ".png", UriKind.Relative)));
                btnTwo.Content = s;
                btnTwo.Visibility = Visibility.Visible;
                s = hand[2].Letter.ToString();
                btnThree.Background = new ImageBrush(new BitmapImage(new Uri(@"..\..\images\tiles\" + s + ".png", UriKind.Relative)));
                btnThree.Content = s;
                btnThree.Visibility = Visibility.Visible;
                s = hand[3].Letter.ToString();
                btnFour.Background = new ImageBrush(new BitmapImage(new Uri(@"..\..\images\tiles\" + s + ".png", UriKind.Relative)));
                btnFour.Content = s;
                btnFour.Visibility = Visibility.Visible;
                s = hand[4].Letter.ToString();
                btnFive.Background = new ImageBrush(new BitmapImage(new Uri(@"..\..\images\tiles\" + s + ".png", UriKind.Relative)));
                btnFive.Content = s;
                btnFive.Visibility = Visibility.Visible;
                s = hand[5].Letter.ToString();
                btnSix.Background = new ImageBrush(new BitmapImage(new Uri(@"..\..\images\tiles\" + s + ".png", UriKind.Relative)));
                btnSix.Content = s;
                btnSix.Visibility = Visibility.Visible;
                s = hand[6].Letter.ToString();
                btnSeven.Background = new ImageBrush(new BitmapImage(new Uri(@"..\..\images\tiles\" + s + ".png", UriKind.Relative)));
                btnSeven.Content = s;
                btnSeven.Visibility = Visibility.Visible;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error Updating Playerhand Tile Images: " + ex.Message);
            }
        }

        //Drag and Drop Tiles from the player hand to the scrabble board (step 1 - 5)
        //STEP 1 - detect a drag operation
        private void btnOne_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(null); //absolute position
        }

        private void btnTwo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(null); //absolute position
        }

        private void btnThree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(null); //absolute position
        }

        private void btnFour_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(null); //absolute position
        }

        private void btnFive_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(null); //absolute position
        }

        private void btnSix_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(null); //absolute position
        }

        private void btnSeven_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startpoint = e.GetPosition(null); //absolute position
        }

        //STEP 2.0 - Calculate mouse movements
        private void btnOne_MouseMove(object sender, MouseEventArgs e)
        {
            flagBtnNum = btnOne;
            mouseMove(sender, e);
        }

        private void btnTwo_MouseMove(object sender, MouseEventArgs e)
        {

            flagBtnNum = btnTwo;
            mouseMove(sender, e);
        }

        private void btnThree_MouseMove(object sender, MouseEventArgs e)
        {
            flagBtnNum = btnThree;
            mouseMove(sender, e);
        }

        private void btnFour_MouseMove(object sender, MouseEventArgs e)
        {
            flagBtnNum = btnFour;
            mouseMove(sender, e);
        }

        private void btnFive_MouseMove(object sender, MouseEventArgs e)
        {
            flagBtnNum = btnFive;
            mouseMove(sender, e);
        }

        private void btnSix_MouseMove(object sender, MouseEventArgs e)
        {
            flagBtnNum = btnSix;
            mouseMove(sender, e);
        }

        private void btnSeven_MouseMove(object sender, MouseEventArgs e)
        {
            flagBtnNum = btnSeven;
            mouseMove(sender, e);
        }

        //STEP 2.1 - Calculate the mouse movements and initiate the dragging
        public void mouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                //Get the mouse position and difference since the start dragging
                Point mousePos = e.GetPosition(null);
                Vector diff = startpoint - mousePos;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    //Get the dragged button item
                    Button b = (Button)sender;

                    //step2 create a DataObject containing the "stuff" to be "dragged"
                    DataObject dragData = new DataObject(typeof(char), flagBtnNum.Content);

                    //Step 3- Initiate the dragging
                    DragDrop.DoDragDrop(flagBtnNum, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Detecting Mouse Movement: " + ex.Message);
            }
        }

        //Step 3 - Drop even handler on the board grid
        private void ScrabbleGrid_Drop(object sender, DragEventArgs e)
        {
            try
            {
                int row = 0;
                int col = 0;
                if (e.Data.GetDataPresent(typeof(char)))
                {
                    bool tileOverlayFlag = false;
                    var point = e.GetPosition(ScrabbleGrid);
                    double accumulatedHeight = 0.0;
                    double accumulatedWidth = 0.0;

                    //calc row mouse was over
                    foreach (var rowDefinition in ScrabbleGrid.RowDefinitions)
                    {
                        accumulatedHeight += rowDefinition.ActualHeight;
                        if (accumulatedHeight >= point.Y)
                            break;
                        row++;
                    }

                    //calc col mouse was over
                    foreach (var columnDefinition in ScrabbleGrid.ColumnDefinitions)
                    {
                        accumulatedWidth += columnDefinition.ActualWidth;
                        if (accumulatedWidth >= point.X)
                            break;
                        col++;
                    }
                    //Check the grid location for a played tile from the current turn
                    foreach (BoardData match in playedGridInfo)
                    {
                        if (match.rowNum == row && match.colNum == col)
                        {
                            //TODO - make boing error
                            MessageBox.Show("Error: Tile already there!", "BOINK", MessageBoxButton.OK);
                            tileOverlayFlag = true;
                        }
                    }
                    //Check if grid location for a played tile from previous turns
                    foreach (plotTileStruct rowColSet in allGridInfo)
                    {
                        if (rowColSet.rowNum == row && rowColSet.colNum == col)
                        {
                            //TODO - make boing error
                            MessageBox.Show("Error: Tile already there!", "BOINK", MessageBoxButton.OK);
                            tileOverlayFlag = true;
                        }
                    }
                    if (!tileOverlayFlag)
                    {
                        gridImage(row, col, Convert.ToChar(e.Data.GetData(typeof(char))));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Dropping Tile On ScrabbleGrid: " + ex.Message);
            }
        }

        //Step 4 - use dragEnter event to detect and dragging over the drop location
        private void ScrabbleGrid_DragEnter(object sender, DragEventArgs e)
        {
            try
            {

                if (!e.Data.GetDataPresent(typeof(char)) || sender == e.Source)
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on ScrabbleGrid Drag Enter: " + ex.Message);
            }
        }

        //Step 5 - Remove tiles from the board and place back into playerhand
        private void ScrabbleGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                int row = 0;
                int col = 0;
                //On double click of the grid return the tile back to its position
                var point = e.GetPosition(ScrabbleGrid);
                double accumulatedHeight = 0.0;
                double accumulatedWidth = 0.0;

                //calc row mouse was over
                foreach (var rowDefinition in ScrabbleGrid.RowDefinitions)
                {
                    accumulatedHeight += rowDefinition.ActualHeight;
                    if (accumulatedHeight >= point.Y)
                        break;
                    row++;
                }

                //calc col mouse was over
                foreach (var columnDefinition in ScrabbleGrid.ColumnDefinitions)
                {
                    accumulatedWidth += columnDefinition.ActualWidth;
                    if (accumulatedWidth >= point.X)
                        break;
                    col++;
                }

                //Check to see if row and col is in the dictionary
                foreach (BoardData match in playedGridInfo)
                {
                    if (match.rowNum == row && match.colNum == col)
                    {
                        //remove it from the dictionary and how the visibility of the button
                        match.buttonNum.Visibility = Visibility.Visible;

                        //Remove the image from the grid
                        ScrabbleGrid.Children.Remove(match.gridBackground);

                        //Delete the key from the dictionary 
                        playedGridInfo.Remove(match);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Finding Mouse Location: " + ex.Message);
            }
        }

        //Generate the image of the tile to be placed onto the grid
        public void gridImage(int row, int col, char c)
        {
            try
            {
                //Change the grids background image depending on its row and column to the button image
                Image gridBG = new Image { Width = 35, Height = 35 };
                var bitmapImage = new BitmapImage(new Uri(@"..\..\images\tiles\" + c + ".png", UriKind.Relative));

                gridBG.Source = bitmapImage;
                gridBG.SetValue(Grid.ColumnProperty, col);
                gridBG.SetValue(Grid.RowProperty, row);
                ScrabbleGrid.Children.Add(gridBG);
                //Hide the button that is placed onto the grid
                flagBtnNum.Visibility = Visibility.Hidden;
                //Add the data to the struct and dictionary
                BoardData gridData = new BoardData(row, col, c, flagBtnNum, gridBG);
                playedGridInfo.Add(gridData);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error Adding Image to the ScrabbleGrid: " + ex.Message);
            }
        }

        //Update the player lobby is a player left the game
        public void updatePlayerLobby(bool update, bool playerLeftLWin)
        {
            bag.UpdateLobby(update, playerLeftLWin);
        }

        //Update the player lobby listboxes if a player left the game or start the game
        private delegate void ClientUpdateDel(int clientCount, bool startGame);
        public void UpdateLobbyGui(int clientCount, bool startGame)
        {
            if (System.Threading.Thread.CurrentThread == this.Dispatcher.Thread)
            {
                try
                {
                    if (startGame)  // then start the game for all clients
                    {                      
                        this.Show();
                        pWin.Hide();
                    }
                    else
                    {
                        // update the GUI with the 
                        pWin.LbLobby.Items.Clear();
                        for (int i = 1; i <= clientCount; i++)
                        {
                            pWin.LbLobby.Items.Add("Player " + i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Updating Lobby GUI: " + ex.Message);
                }
            }
            else
            {
                // Only the main (dispatcher) thread can change the GUI so..
                this.Dispatcher.BeginInvoke(new ClientUpdateDel(UpdateLobbyGui), clientCount, startGame);
            }
        }

        //If the window is cosed end the game
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                playerLeaveGWin = true;
                bag.ExitGame();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error Closing MainWindow: " + ex.Message);
            }
        }
    }
}
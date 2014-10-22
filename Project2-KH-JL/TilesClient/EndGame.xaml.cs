/*
 * Program:         Scrabble
 * Module:          EndGame.xmal.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     EndGame window that opens when a player leaves the Scrabble game or when the tile bag is empty ending the game.
 *                  The window displays the total scores of each player in descending order.
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
using System.Windows.Shapes;
using System.ServiceModel;

namespace TilesClient
{
    public partial class EndGame : Window
    {
        public EndGame(Dictionary<int,int> playerScores, string playerLeft)
        {
            try
            {
                //Center window on startup
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                InitializeComponent();

                //For each player and score order in descending order and print to the listbox
                foreach (KeyValuePair<int, int> match in playerScores.OrderByDescending(i => i.Value))
                {
                    lboxEndLeaderboard.Items.Add("Player " + match.Key + "\t\t Score: " + match.Value);
                }
                //Update the label explaining to the user the tilebag is empty, or a player left
                endGameReason.Content = playerLeft;
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error Initializing EndGame Window!" + ex.Message);
            }
        }

        private void btnExitGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //Shutdown the application
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                //Shutdown the application
                Application.Current.Shutdown();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}

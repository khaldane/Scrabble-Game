/*
 * Program:         Scrabble
 * Module:          PlayerLobby.xaml.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     First window displayed to the user that shows all users who have joined the service.
 *                  The clients update the lobby listbox when a player exits the lobby or joins the lobby.
 *                  The Start Game button starts the Scrabble client for all players within the lobby.
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
using TilesLibrary; 

namespace TilesClient
{
    public partial class PlayerLobby : Window
    {
        //Global Variables
        private MainWindow mWindow;

        public PlayerLobby(MainWindow mWin, int clientCount)
        {
            try
            {
                //Center window on startup
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                InitializeComponent();
                //Add the users to the lobby listbox
                LbLobby.Items.Add("Player " + clientCount);
                WelcomePlayer.Content = "Welcome to Scrabble!";
                mWindow = mWin;
                mWin.updatePlayerLobby(false, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Initializing Lobby Window! " + ex.Message);
            }
        }

        //Start the game closing the playerlobby window and opening the mainwindow
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mWindow.updatePlayerLobby(true, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Starting The Game! " + ex.Message);
            }
        }

        //When players leaves the game updatePlayerLobby updates the listbox to handle user exit
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                mWindow.updatePlayerLobby(false, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Updating Clients on Player Leaving the Lobby! " + ex.Message);
            }
        }
    }
}

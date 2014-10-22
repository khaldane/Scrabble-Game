/*
 * Program:         Scrabble
 * Module:          Program.cs
 * Author:          Katherine Haldane & Jared Lerner
 * Date:            April 11, 2014
 * Description:     Service that creates the endpoint address, starts the service, and shuts down the service    
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;
using TilesLibrary;

namespace TilesService
{
    class TileService
    {
        static void Main(string[] args)
        {
            try
            {
                // Endpoint Address
                ServiceHost servHost = new ServiceHost(typeof(TileBag));

                // Start the service
                servHost.Open();

                // Keep the service running until <Enter> is pressed
                Console.WriteLine("TileBag service is activated, Press <Enter> to quit.");
                Console.ReadKey();

                // Shut down the service
                servHost.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }
    }
}

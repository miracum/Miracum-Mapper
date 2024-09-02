//    MIRACUM Mapper, a program for mapping local medical codes to a standard teminology.
//    Copyright (C) 2019-2020 The MIRACUM Project, Universitätsklinikum Erlangen.
//    Implemented by: Sebastian Mate (Sebastian.Mate(at)uk-erlangen.de)
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.


using System;
using System.Windows.Forms;

namespace UKER_Mapper
{
    static class Program 
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        { 
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SplashScreen.ShowSplashScreen();
            System.Threading.Thread.Sleep(1000);
            mapperForm mainForm = new mapperForm(); //this takes ages
            SplashScreen.CloseForm();
            Application.Run(mainForm); 
        }
    }
}

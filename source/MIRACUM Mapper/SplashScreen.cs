using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UKER_Mapper
{
    public partial class SplashScreen : Form
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        // https://stackoverflow.com/a/15836105

        //Delegate for cross thread call to close
        private delegate void CloseDelegate();

        //The type of form to be displayed as the splash screen.
        private static SplashScreen splashForm;

        static public void ShowSplashScreen()
        {
            // Make sure it is only launched once.

            if (splashForm != null)
                return;
            Thread thread = new Thread(new ThreadStart(SplashScreen.ShowForm));
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        static private void ShowForm()
        {
            splashForm = new SplashScreen();
            Application.Run(splashForm);
        }

        static public void CloseForm()
        {
            splashForm.Invoke(new CloseDelegate(SplashScreen.CloseFormInternal));
        }

        static private void CloseFormInternal()
        {
            splashForm.Close();
            splashForm = null;
        }
    }
}

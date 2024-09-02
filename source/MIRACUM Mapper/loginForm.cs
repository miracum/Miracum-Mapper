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
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            user.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            password.Text = "";
            password.UseSystemPasswordChar = true;
            password.PasswordChar = '*';
            this.loginButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.CenterToScreen();
            this.Focus();
            password.Select();
        }

        private void loginButton_Click(object sender, EventArgs e)
        {

        }

        private void user_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                password.Focus();
            }
        }

        private void password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //loginButton_Click(sender, e);
                loginButton.Focus();
                SendKeys.SendWait("{ENTER}");
            }
        }

        private void user_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

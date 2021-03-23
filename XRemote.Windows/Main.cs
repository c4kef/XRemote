using System;
using System.Windows.Forms;
using XRemote.Windows.Server;

namespace XRemote.Windows
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            var server = new ServerObject();
            server.Listen();
        }

        private void Server_subExceptions(Exception ex)
        {
            MessageBox.Show($"Ex: {ex.Message}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace communication
{
    class GameTable
    {
        public Player[] gamePlayer;
        Service service;
        public GameTable(ListBox listbox)
        {
            gamePlayer = new Player[2];
            service = new Service(listbox);
        }
    }
}

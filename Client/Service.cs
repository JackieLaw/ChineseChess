using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    class Service
    {
        ListBox listbox;
        StreamWriter sw;
        public Service(ListBox listbox, StreamWriter sw)
        {
            this.listbox = listbox;
            this.sw = sw;
        }
        //向服务器发送数据
        public void SendToServer(string str)
        {
            try
            {
                sw.WriteLine(str);
                sw.Flush();
            }
            catch
            {
                AddItemToListBox("发送数据失败");
            }
        }
        delegate void ListBoxDelegate(string str);
        //往listBox里写信息
        public void AddItemToListBox(string str)
        {
            if (listbox.InvokeRequired)
            {
                ListBoxDelegate d = AddItemToListBox;
                listbox.Invoke(d, str);
            }
            else
            {
                listbox.Items.Add(str);
                listbox.SelectedIndex = listbox.Items.Count - 1;
                listbox.ClearSelected();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//在listBox中添加信息
//向各方发送信息
namespace communication
{
    class Service
    {
        private ListBox listbox;
        private delegate void AddItemDelegate(string str);
        private AddItemDelegate addItemDelegate;
        public Service(ListBox listbox)
        {
            this.listbox = listbox;
            addItemDelegate = new AddItemDelegate(AddItem);
        }
        //在listBox中追加的信息,C#中禁止跨线程直接访问控件
        public void AddItem(string str)
        {
            if(listbox.InvokeRequired)
            {
                listbox.Invoke(addItemDelegate, str);
            }
            else
            {
                listbox.Items.Add(str);
                listbox.SelectedIndex = listbox.Items.Count - 1;
                listbox.ClearSelected();
            }
        }
        //向客户端发送消息
        public void SendToOne(User user, string str)
        {
            try
            {
                user.sw.WriteLine(str);
                user.sw.Flush();
                AddItem(string.Format("向{0}发送{1}", user.userName, str));
            }
            catch
            {
                AddItem(string.Format("向{0}发送失败", user.userName));
            }
        }
        //向同桌发送消息
        public void SendToBoth(GameTable gameTable, string str)
        {
            for(int i = 0; i < 2; i++)
            {
                if(gameTable.gamePlayer[i].someone == true)
                {
                    SendToOne(gameTable.gamePlayer[i].user, str);
                }
            }
        }
        //向所有客户端发送消息
        public void SendToAll(System.Collections.Generic.List<User> userList, string str)
        {
            for(int i = 0; i < userList.Count; i++)
            {
                SendToOne(userList[i], str);
            }
        }
    }
}

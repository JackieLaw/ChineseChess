using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace communication
{
    public partial class FromServer : Form
    {
        //允许进入的最大人数
        private int maxUsers;
        //连接的用户
        System.Collections.Generic.List<User> userList = new List<User>();
        //游戏桌数
        private int maxTables;
        private GameTable[] gameTable;
        //本机IP
        IPAddress localAddress;
        //监听端口
        private int port = 51888;
        private TcpListener myListener;
        private Service service;
        public FromServer()
        {
            InitializeComponent();
            service = new Service(listBox1);
        }
        //加载窗体时
        private void FromServer_Load(object sender, EventArgs e)
        {
            listBox1.HorizontalScrollbar = true;
            IPAddress[] addrIP = Dns.GetHostAddresses(Dns.GetHostName());
            localAddress = addrIP[0];
            buttonStop.Enabled = false;
        }
        //“启动服务”按钮
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if(int.TryParse(textBoxMaxTables.Text, out maxTables) == false
                || int.TryParse(textBoxMaxUsers.Text, out maxUsers) == false)
            {
                MessageBox.Show("请输入范围内的正整数！！！");
                return;
            }
            if(maxUsers < 1 || maxUsers > 300)
            {
                MessageBox.Show("允许人数范围为 1-300 ！！！");
                return;
            }
            if(maxTables < 1 || maxTables > 100)
            {
                MessageBox.Show("允许桌数范围为 1-100 ！！！");
                return;
            }
            textBoxMaxTables.Enabled = false;
            textBoxMaxUsers.Enabled = false;
            //创建游戏桌数组
            gameTable = new GameTable[maxTables];
            for(int i = 0; i < maxTables; i++)
            {
                gameTable[i] = new GameTable(listBox1);
            }
            //监听
            myListener = new TcpListener(localAddress, port);
            myListener.Start();
            service.AddItem(string.Format("开始在{0}:{1}监听客户连接", localAddress, port));
            //创建一个线程监听客户端连接请求
            ThreadStart ts = new ThreadStart(ListenClientConnect);
            Thread myThread = new Thread(ts);
            myThread.Start();
            buttonStart.Enabled = false;
            buttonStop.Enabled = true;
        }
        //“停止服务”按钮
        private void buttonStop_Click(object sender, EventArgs e)
        {
            service.AddItem(string.Format("目前连接用户数:{0}", userList.Count));
            service.AddItem(string.Format("马上停止服务，用户依次退出"));
            for(int i = 0; i < userList.Count; i++)
            {
                userList[i].client.Close();
            }
            //退出监听线程
            myListener.Stop();
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
            textBoxMaxUsers.Enabled = true;
            textBoxMaxTables.Enabled = true;
        }
        //接收客户端连接
        private void ListenClientConnect()
        {
            while (true)
            {
                TcpClient newClient = null;
                try
                {
                    newClient = myListener.AcceptTcpClient();
                }
                catch
                {
                    break;
                }
                //为每个客户端建立一个线程
                ParameterizedThreadStart pts = new ParameterizedThreadStart(ReceiveData);
                Thread threadReceive = new Thread(pts);
                User user = new User(newClient);
                threadReceive.Start(user);
                userList.Add(user);
                service.AddItem(string.Format("{0}进入", newClient.Client.RemoteEndPoint));
                service.AddItem(string.Format("当前连接用户数: {0}", userList.Count));
            }
        }
        //接收客户端信息
        private void ReceiveData(object obj)
        {
            User user = (User)obj;
            TcpClient client = user.client;
            //是否正常退出接收线程
            bool normalExit = false;
            //用于控制是否退出循环
            bool exitWhile = false;
            while(exitWhile == false)
            {
                string receiveString = null;
                try
                {
                    receiveString = user.sr.ReadLine();
                }
                catch
                {
                    service.AddItem("接收数据失败");
                }
                //如果TcpClient对象关闭而底层套接字未关闭，不产生异常，但是读取结果为null
                if (receiveString == null)
                {
                    if(normalExit == false)
                    {
                        if(client.Connected == true)
                        {
                            service.AddItem(string.Format("与{0}失去联系，已终止接收该用户信息", client.Client.RemoteEndPoint));
                        }
                        RemoveClientfromPlayer(user);
                    }
                    break;
                }
                service.AddItem(string.Format("来自{0}:{1}", user.userName, receiveString));
                string[] splitString = receiveString.Split(',');
                int tableIndex = -1; //桌号
                int side = -1;//座位号
                int anotherSide = -1; //对方座位号
                string sendString = "";
                string command = splitString[0].ToLower();
                switch (command)
                {
                    //登陆，格式：Login,昵称
                    case "login":
                        if(userList.Count > maxUsers)
                        {
                            sendString = "Sorry";
                            service.SendToOne(user, sendString);
                            service.AddItem("人数已满，拒绝" + splitString[1] + "进入游戏室");
                            exitWhile = true;
                        }
                        else
                        {
                            //将用户昵称保存到用户列表中
                            user.userName = string.Format("[{0}]", splitString[1]);
                            //将各桌是否有人的情况发送给该用户
                            sendString = "Tables," + this.GetOnlineString();
                            service.SendToOne(user, sendString);

                        }
                        break;
                    //退出，格式：Logout
                    case "logout":
                        service.AddItem(string.Format("{0}退出游戏室", user.userName));
                        normalExit = true;
                        exitWhile = true;
                        break;
                    //坐下,格式：SitDown，桌号，座号
                    case "sitdown":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        gameTable[tableIndex].gamePlayer[side].user = user;
                        gameTable[tableIndex].gamePlayer[side].someone = true;
                        service.AddItem(string.Format("{0}在第{1}桌第{2}座入座", user.userName,
                                                        tableIndex + 1, side + 1));
                        //得到对方座位号
                        anotherSide = (side + 1) % 2;
                        //判断对方是否有人
                        if(gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            //告诉用户对方已入座
                            //格式：SitDown，座位号，用户名
                            sendString = string.Format("SitDown,{0},{1}", anotherSide,
                                            gameTable[tableIndex].gamePlayer[anotherSide].user.userName);
                            service.SendToOne(user, sendString);
                        }
                        //告诉两个用户该用户入座
                        //格式：SitDown，座位号，用户名
                        sendString = string.Format("SitDown,{0},{1}", side, user.userName);
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        //将游戏室各桌情况发送给所有用户
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    //离座,格式：GetUp，桌号，座位号
                    case "getup":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        service.AddItem(string.Format("{0}离座，返回游戏室", user.userName));
                        //将离座信息发给两个用户，格式：GetUp，座位号，用户名
                        service.SendToBoth(gameTable[tableIndex], string.Format("GetUp,{0},{1}", side, user.userName));
                        gameTable[tableIndex].gamePlayer[side].someone = false;
                        gameTable[tableIndex].gamePlayer[side].started = false;
                        anotherSide = (side + 1) % 2;
                        if(gameTable[tableIndex].gamePlayer[anotherSide].someone == true)
                        {
                            gameTable[tableIndex].gamePlayer[anotherSide].started = false;
                        }
                        //将游戏室各桌情况发给所有用户
                        service.SendToAll(userList, "Tables," + this.GetOnlineString());
                        break;
                    //聊天，格式：Talk,用户名，对话内容
                    case "talk":
                        tableIndex = int.Parse(splitString[1]);
                        //对逗号特殊处理
                        sendString = string.Format("Talk,{0},{1}", user.userName,
                                    receiveString.Substring(splitString[0].Length + splitString[1].Length + 2));
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        break;
                    //准备，格式：Start，桌号，座位号
                    case "start":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        gameTable[tableIndex].gamePlayer[side].started = true;
                        if(side == 0)
                        {
                            anotherSide = 1;
                            sendString = "Message,黑方已准备";
                        }
                        else
                        {
                            anotherSide = 0;
                            sendString = "Message,红方已准备";
                        }
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        if(gameTable[tableIndex].gamePlayer[anotherSide].started == true)
                        {
                            sendString = "AllReady";
                            service.SendToBoth(gameTable[tableIndex], sendString);
                        }
                        break;
                    //棋子移动信息，格式：ChessInfo,桌号，座号，棋子编号，原始x，原始y，目的x，目的y
                    case "chessinfo":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        anotherSide = (side + 1) % 2;
                        int cno;//棋子编号
                        int x0, y0;//原始坐标
                        int x1, y1;//目的坐标
                        cno = int.Parse(splitString[3]);
                        x0 = ChangeX(int.Parse(splitString[4]));
                        y0 = ChangeY(int.Parse(splitString[5]));
                        x1 = ChangeX(int.Parse(splitString[6]));
                        y1 = ChangeY(int.Parse(splitString[7]));
                        sendString = string.Format("ChessInfo,{0},{1},{2},{3},{4},{5}", side, int.Parse(splitString[3]),
                             int.Parse(splitString[4]), int.Parse(splitString[5]), int.Parse(splitString[6]), int.Parse(splitString[7]));
                        service.SendToOne(gameTable[tableIndex].gamePlayer[side].user, sendString);
                        service.AddItem(string.Format("{0}：{1}:从({2},{3}) -> ({4},{5})", gameTable[tableIndex].gamePlayer[side].user.userName,
                            int.Parse(splitString[3]), int.Parse(splitString[4]), int.Parse(splitString[5]), 
                            int.Parse(splitString[6]), int.Parse(splitString[7])));
                        sendString = string.Format("ChessInfo,{0},{1},{2},{3},{4},{5}", side, cno, x0, y0, x1, y1);
                        service.SendToOne(gameTable[tableIndex].gamePlayer[anotherSide].user, sendString);
                        service.AddItem(string.Format("{0}：{1}:从({2},{3}) -> ({4},{5})", gameTable[tableIndex].gamePlayer[anotherSide].user.userName,
                            cno, x0, y0, x1, y1));
                        break;
                    //胜利,格式：Win,桌号，座号
                    case "win":
                        tableIndex = int.Parse(splitString[1]);
                        side = int.Parse(splitString[2]);
                        anotherSide = (side + 1) % 2;
                        sendString = string.Format("win,{0}",side);
                        service.SendToBoth(gameTable[tableIndex], sendString);
                        gameTable[tableIndex].gamePlayer[side].started = false;
                        gameTable[tableIndex].gamePlayer[anotherSide].started = false;
                        break;
                }
            }
            userList.Remove(user);
            client.Close();
            service.AddItem(string.Format("有一个退出，剩余连接用户数：{0}", userList.Count));
        }
        //变换棋子横坐标
        private int ChangeY(int x)
        {
            return x + 2 * (4 - x);
        }
        //变换棋子纵坐标
        private int ChangeX(int y)
        {
            return y + 2 * (4 - y) + 1;
        }
        //检测该用户是否坐到游戏桌上，如果是，将其移除，并终止该桌游戏
        private void RemoveClientfromPlayer(User user)
        {
            for(int i = 0; i < gameTable.Length; i++)
            {
                for(int j = 0; j < 2; j++)
                {
                    if(gameTable[i].gamePlayer[j].user != null)
                    {
                        if(gameTable[i].gamePlayer[j].user == user)
                        {
                            StopPlayer(i, j);
                            return;
                        }
                    }
                }
            }
        }
        //停止第i桌的游戏
        private void StopPlayer(int i, int j)
        {
            gameTable[i].gamePlayer[j].someone = false;
            gameTable[i].gamePlayer[j].started = false;
            int otherSide = (j + 1) % 2;
            if(gameTable[i].gamePlayer[otherSide].started == true)
            {
                gameTable[i].gamePlayer[otherSide].started = false;
                if(gameTable[i].gamePlayer[otherSide].user.client.Connected == true)
                {
                    service.SendToOne(gameTable[i].gamePlayer[otherSide].user,
                                        string.Format("Lost,{0},{1}",
                                        j, gameTable[i].gamePlayer[j].user.userName));
                }
            }
        }
        //获取每桌是否有人的字符串，0表示有人，1表示无人
        private string GetOnlineString()
        {
            string str = "";
            for(int i = 0; i < gameTable.Length; i++)
            {
                for(int j = 0; j < 2; j++)
                {
                    str += gameTable[i].gamePlayer[j].someone == true ? "1" : "0";
                }
            }
            return str;
        }
        //关闭窗体前触发的事件
        private void FromDDServer_FromClosing(object sender, FormClosingEventArgs e)
        {
            if(myListener != null)
            {
                buttonStop_Click(null, null);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class FormRoom : Form
    {
        private int maxPlayingTables;
        private CheckBox[,] checkBoxGameTables;
        private TcpClient client = null;
        private StreamWriter sw;
        private StreamReader sr;
        private Service service;
        private FormPlaying formPlaying;
        //是否正常退出接收线程
        private bool normalExit = false;
        //命令是否来自服务器
        private bool isReceiveCommand = false;
        //所坐的游戏桌座位号，-1表示未入座，0表示黑方，1表示红方
        private int side = -1;
        public FormRoom()
        {
            InitializeComponent();
        }
        private void FormRoom_Load(object sender, EventArgs e)
        {
            Random r = new Random((int)DateTime.Now.Ticks);
            textBoxName.Text = "Player" + r.Next(1, 100);
            maxPlayingTables = 0;
            textBoxLocal.ReadOnly = true;
            textBoxServer.ReadOnly = true;
        }
        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                client = new TcpClient(Dns.GetHostName(), 51888);
            }
            catch
            {
                MessageBox.Show("与服务器连接失败", "",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            textBoxLocal.Text = client.Client.LocalEndPoint.ToString();
            textBoxServer.Text = client.Client.RemoteEndPoint.ToString();
            buttonConnect.Enabled = false;
            //获取网络流
            NetworkStream netStream = client.GetStream();
            sr = new StreamReader(netStream, System.Text.Encoding.UTF8);
            sw = new StreamWriter(netStream, System.Text.Encoding.UTF8);
            service = new Service(listBox1, sw);
            //获取服务器各桌信息
            //格式：Login,昵称
            service.SendToServer("Login," + textBoxName.Text.Trim());
            Thread threadReceive = new Thread(new ThreadStart(ReceiveData));
            threadReceive.Start();
        }
        //处理接收的数据
        private void ReceiveData()
        {
            bool exitWhile = false;
            while(exitWhile == false)
            {
                string receiveString = null;
                try
                {
                    receiveString = sr.ReadLine();
                }
                catch
                {
                    service.AddItemToListBox("接收数据失败");
                }
                if(receiveString == null)
                {
                    if(normalExit == false)
                    {
                        MessageBox.Show("与服务器失去联系，游戏无法继续！");
                    }
                    if(side != 1)
                    {
                        ExitFormPlaying();
                    }
                    side = -1;
                    normalExit = true;
                    break;
                }
                service.AddItemToListBox("收到：" + receiveString);
                string[] splitString = receiveString.Split(',');
                string command = splitString[0].ToLower();
                switch (command)
                {
                    //大厅已满
                    case "sorry":
                        MessageBox.Show("连接成功，但大厅已满");
                        exitWhile = true;
                        break;
                    //游戏桌情况
                    //格式：Tables，各桌是否有人的字符串
                    //1表示有人，0表示没人
                    case "tables":
                        string s = splitString[1];
                        //若maxPlayingTables为0，说明未创建checkBoxGameTables
                        if(maxPlayingTables == 0)
                        {
                            //计算桌数
                            maxPlayingTables = s.Length / 2;
                            checkBoxGameTables = new CheckBox[maxPlayingTables, 2];
                            isReceiveCommand = true;
                            //将CheckBox对象添加到数组
                            for(int i = 0; i < maxPlayingTables; i++)
                            {
                                AddCheckBoxToPanel(s, i);
                            }
                            isReceiveCommand = false;
                        }
                        else{
                            isReceiveCommand = true;
                            for(int i = 0; i < maxPlayingTables; i++)
                            {
                                for(int j = 0; j < 2; j++)
                                {
                                    if(s[2 * i + j] == '0')
                                    {
                                        UpdateCheckBox(checkBoxGameTables[i, j], false);
                                    }
                                    else
                                    {
                                        UpdateCheckBox(checkBoxGameTables[i, j], true);
                                    }
                                }
                                isReceiveCommand = false;
                            }
                        }
                        break;
                    //入座，格式：SitDown，座位号，用户名
                    case "sitdown":
                        formPlaying.SetTableSideText(splitString[1], splitString[2],
                                                string.Format("{0}进入", splitString[2]));
                        break;
                    //离座
                    case "getup":
                        if(side == int.Parse(splitString[1]))
                        {
                            side = -1;
                        }
                        else
                        {
                            formPlaying.SetTableSideText(splitString[1], "",
                                string.Format("{0}退出", splitString[2]));
                            formPlaying.Restart("敌人逃跑，我方胜利");
                        }
                        break;
                    //对方与服务器断开连接
                    case "lost":
                        formPlaying.SetTableSideText(splitString[1], "",
                            string.Format("[{0}]与服务器失去联系", splitString[2]));
                        formPlaying.Restart("对家与服务器失去联系，游戏无法继续");
                        break;
                    //聊天
                    //格式：Talk，说话者，内容
                    case "talk":
                        if(formPlaying != null)
                        {
                            formPlaying.ShowTalk(splitString[1],
                                receiveString.Substring(splitString[0].Length +
                                splitString[1].Length + 2));
                        }
                        break;
                    //服务器发送的信息
                    //格式：Message，信息
                    case "message":
                        formPlaying.ShowMessage(splitString[1]);
                        break;
                    //棋子变动信息,格式：ChessInfo, side, 棋子编号，原始x，原始y，目的x，目的y
                    case "chessinfo":
                        int tside, cno, oriX, oriY, endX, endY;
                        tside = int.Parse(splitString[1]);
                        cno = int.Parse(splitString[2]);
                        oriX = int.Parse(splitString[3]);
                        oriY = int.Parse(splitString[4]);
                        endX = int.Parse(splitString[5]);
                        endY = int.Parse(splitString[6]);
                        formPlaying.ChangeChess(-1, oriX, oriY);
                        formPlaying.ChangeChess(cno, endX, endY);
                        formPlaying.RePaint();
                        formPlaying.drawFrame("blue", endX, endY);
                        formPlaying.ChangeOrder(tside);
                        formPlaying.CheckWin();
                        break;
                    //胜利,格式：Win，side
                    case "win":
                        int winner = int.Parse(splitString[1]);
                        if(winner == side)
                        {
                            formPlaying.Restart("我方胜利！！！");
                        }
                        else
                        {
                            formPlaying.Restart("我方失败。。。");
                        }

                        break;
                    //双方都准备好了
                    case "allready":
                        formPlaying.ShowMessage("双方都已准备，游戏开始！");
                        formPlaying.Ready(side);
                        break;
                }
            }
            Application.Exit();
        }
        delegate void ExitFormPlayingDelegate();
        //退出游戏
        private void ExitFormPlaying()
        {
                if (formPlaying.InvokeRequired == true)
                {
                    ExitFormPlayingDelegate d = new ExitFormPlayingDelegate(ExitFormPlaying);
                    this.Invoke(d);
                }
                else
                {
                    formPlaying.Close();
                }
        }
        delegate void Paneldelegate(string s, int i);
        //添加一个游戏桌
        private void AddCheckBoxToPanel(string s, int i)
        {
            if(panel1.InvokeRequired == true)
            {
                Paneldelegate d = AddCheckBoxToPanel;
                this.Invoke(d, s, i);
            }
            else
            {
                Label label = new Label();
                label.Location = new Point(10, 15 + i * 30);
                label.Text = string.Format("第{0}桌: ", i + 1);
                label.Width = 70;
                this.panel1.Controls.Add(label);
                CreateCheckBox(i, 0, s, "黑方");
                CreateCheckBox(i, 1, s, "红方");
            }
        }
        delegate void CheckBoxDelegate(CheckBox checkbox, bool isChecked);
        //修改选择状态
        private void UpdateCheckBox(CheckBox checkbox, bool isChecked)
        {
            if(checkbox.InvokeRequired == true)
            {
                CheckBoxDelegate d = UpdateCheckBox;
                this.Invoke(d, checkbox, isChecked);
            }
            else
            {
                if(side == -1)
                {
                    checkbox.Enabled = !isChecked;
                }
                else
                {
                    //已入座，禁止再选其他桌
                    checkbox.Enabled = false;
                }
                checkbox.Checked = isChecked;
            }
        }
        //添加游戏桌座位的选项
        private void CreateCheckBox(int i, int j, string s, string text)
        {
            int x = j == 0 ? 100 : 200;
            checkBoxGameTables[i, j] = new CheckBox();
            checkBoxGameTables[i, j].Name = string.Format("check{0:0000}{1:0000}", i, j);
            checkBoxGameTables[i, j].Width = 60;
            checkBoxGameTables[i, j].Location = new Point(x, 10 + i * 30);
            checkBoxGameTables[i, j].Text = text;
            checkBoxGameTables[i, j].TextAlign = ContentAlignment.MiddleLeft;
            if(s[2 * i + j] == '1')
            {
                //1表示有人
                checkBoxGameTables[i, j].Enabled = false;
                checkBoxGameTables[i, j].Checked = true;
            }
            else
            {
                //0表示没有人
                checkBoxGameTables[i, j].Enabled = true;
                checkBoxGameTables[i, j].Checked = false;
            }
            this.panel1.Controls.Add(checkBoxGameTables[i, j]);
            checkBoxGameTables[i, j].CheckedChanged +=
                 new EventHandler(checkBox_CheckedChanged);
        }
        //CheckBox的Checked属性发生变化时触发
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            //是否为服务器更新本桌情况
            if(isReceiveCommand == true)
            {
                return;
            }
            CheckBox checkbox = (CheckBox)sender;
            //若Checked为true，表示玩家坐到第i桌第j位
            if(checkbox.Checked == true)
            {
                int i = int.Parse(checkbox.Name.Substring(5, 4));
                int j = int.Parse(checkbox.Name.Substring(9, 4));
                side = j;
                //格式：SitDown，昵称，桌号，座位号
                service.SendToServer(string.Format("SitDown,{0},{1}", i, j));
                formPlaying = new FormPlaying(i, j, sw);
                formPlaying.Show();
                formPlaying.RePaint();
            }
        }
        //关闭窗口时触发的事件
        private void FormRoom_FromClosing(object sender, FormClosingEventArgs e)
        {
            if(client != null)
            {
                //不允许玩家从游戏桌直接退出整个程序
                //只允许先从游戏桌返回游戏室，再从游戏室退出
                if(side != -1)
                {
                    MessageBox.Show("请先从游戏桌站起，返回游戏室，然后再退出");
                    e.Cancel = true;
                }
                else
                {
                    //服务器停止服务时，normalExit为true，其他情况为false
                    if(normalExit == false){
                        normalExit = true;
                        service.SendToServer("Logout");
                    }
                    client.Close();
                }
            }
        }
    }
}

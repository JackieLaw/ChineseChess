using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class FormPlaying : Form
    {
        //桌子索引
        private int tableIndex;
        //座位
        private int side;
        //是否该自己下棋
        private bool order;
        //棋盘网格
        private int[,] grid = new int[10,9];
        //棋子半径
        private int r;
        //棋盘左上角坐标（baseX,baseY)，间隔为60，单位均为像素
        int baseX, baseY, interval, binterval;
        //棋子对应表
        private Dictionary<int, string> chess = new Dictionary<int, string>();
        //棋子原始坐标
        private int oriX, oriY;
        //棋子目的坐标
        private int endX, endY;
        //被选中的棋子
        int pick;
        //棋盘
        Bitmap bm = new Bitmap(700, 700);
        //命令是否来自服务器
        private bool isReceiveCommand = false;
        private Service service;
        delegate void LabelDelegate(Label label, string str);
        delegate void ButtonDelegate(Button button, bool flag);
        LabelDelegate labelDelegate;
        ButtonDelegate buttonDelegate;
        public FormPlaying(int TableIndex, int Side, StreamWriter sw)
        {
            InitializeComponent();
            this.tableIndex = TableIndex;
            this.side = Side;
            order = false;
            baseX = 50;
            baseY = 50;
            interval = 60;
            binterval = 30;
            r = 25;
            oriX = -1;
            oriY = -1;
            endX = -1;
            endY = -1;
            pick = -1;
            labelDelegate = new LabelDelegate(SetLabel);
            buttonDelegate = new ButtonDelegate(SetButton);
            service = new Client.Service(listBox1, sw);
        }
        private void FromPlaying_Load(object sender, EventArgs e)
        {
            initChess();
            initGrid();
            labelSide0.Text = "";
            labelSide1.Text = "";
            labelOrder.Text = "";
            pictureBox1.Image = bm;
        }
        //设置标签显示的信息
        public void SetLabel(Label label, string str)
        {
            if (label.InvokeRequired)
            {
                this.Invoke(labelDelegate, label, str);
            }
            else
            {
                label.Text = str;
            }
        }
        //设置按钮是否可用
        private void SetButton(Button button, bool flag)
        {
            if (button.InvokeRequired)
            {
                this.Invoke(buttonDelegate, button, flag);
            }
            else
            {
                button.Enabled = flag;
            }
        }
        //初始化棋子关联容器
        private void initChess()
        {
            chess.Add(0, "红,車,1");
            chess.Add(1, "红,马,1");
            chess.Add(2, "红,象,1");
            chess.Add(3, "红,士,1");
            chess.Add(4, "红,帅");
            chess.Add(5, "红,士,2");
            chess.Add(6, "红,象,2");
            chess.Add(7, "红,马,2");
            chess.Add(8, "红,車,2");
            chess.Add(9, "红,炮,1");
            chess.Add(10, "红,炮,2");
            chess.Add(11, "红,兵,1");
            chess.Add(12, "红,兵,2");
            chess.Add(13, "红,兵,3");
            chess.Add(14, "红,兵,4");
            chess.Add(15, "红,兵,5");
            chess.Add(16, "黑,車,1");
            chess.Add(17, "黑,马,1");
            chess.Add(18, "黑,相,1");
            chess.Add(19, "黑,士,1");
            chess.Add(20, "黑,将,1");
            chess.Add(21, "黑,士,2");
            chess.Add(22, "黑,相,2");
            chess.Add(23, "黑,马,2");
            chess.Add(24, "黑,車,2");
            chess.Add(25, "黑,炮,1");
            chess.Add(26, "黑,炮,2");
            chess.Add(27, "黑,卒,1");
            chess.Add(28, "黑,卒,2");
            chess.Add(29, "黑,卒,3");
            chess.Add(30, "黑,卒,4");
            chess.Add(31, "黑,卒,5");

        }
        //初始化棋盘数组
        public void initGrid()
        {
            int i, j;
            //先将数组全部置为-1
            for (i = 0; i <= grid.GetUpperBound(0); i++)
            {
                for (j = 0; j <= grid.GetUpperBound(1); j++)
                {
                    grid[i, j] = -1;
                }
            }
            //获取自己的颜色,黑0红1
            //自己是黑色,对面是红色
            if (side == 0)
            {
                //红色一排
                for(i = 0; i < 9; i++)
                {
                    grid[0,i] = i;
                }
                //红色两个炮
                grid[2, 1] = 9;
                grid[2, 7] = 10;
                //红色五个兵
                grid[3, 0] = 11;
                grid[3, 2] = 12;
                grid[3, 4] = 13;
                grid[3, 6] = 14;
                grid[3, 8] = 15;
                //黑色一排
                for(j = 0, i = 16; i <= 24; i++,j++)
                {
                    grid[9, j] = i;
                }
                //黑色两个炮
                grid[7, 1] = 25;
                grid[7, 7] = 26;
                //黑色五个卒
                grid[6, 0] = 27;
                grid[6, 2] = 28;
                grid[6, 4] = 29;
                grid[6, 6] = 30;
                grid[6, 8] = 31;
            }
            //自己是红色，对方是黑色
            else
            {
                //红色一排
                for (i = 0; i < 9; i++)
                {
                    grid[9, i] = i;
                }
                //红色两个炮
                grid[7, 1] = 9;
                grid[7, 7] = 10;
                //红色五个兵
                grid[6, 0] = 11;
                grid[6, 2] = 12;
                grid[6, 4] = 13;
                grid[6, 6] = 14;
                grid[6, 8] = 15;
                //黑色一排
                for (j = 0, i = 16; i <= 24; i++, j++)
                {
                    grid[0, j] = i;
                }
                //黑色两个炮
                grid[2, 1] = 25;
                grid[2, 7] = 26;
                //黑色五个卒
                grid[3, 0] = 27;
                grid[3, 2] = 28;
                grid[3, 4] = 29;
                grid[3, 6] = 30;
                grid[3, 8] = 31;
            }
        }
        //重新绘制棋盘和所有棋子
        public void RePaint()
        {
            Graphics g = Graphics.FromImage(bm);
            Pen pen = new Pen(Color.Black, 3);

            //棋盘外边框,分别是上横、下横、左竖、右竖
            g.DrawLine(pen, baseX - binterval, baseY - binterval, baseX + 8 * interval + binterval, baseY - binterval);
            g.DrawLine(pen, baseX - binterval, baseY + 9 * interval + binterval, baseX + 8 * interval + binterval, baseY + 9 * interval + binterval);
            g.DrawLine(pen, baseX - binterval, baseY - binterval, baseX - binterval, baseY + 9 * interval + binterval);
            g.DrawLine(pen, baseX + 8 * interval + binterval, baseY - binterval, baseX + 8 * interval + binterval, baseY + 9 * interval + binterval);

            //填充棋盘颜色
            SolidBrush brush = new SolidBrush(Color.Peru);
            g.FillRectangle(brush, baseX - binterval, baseY - binterval,
                            8 * interval + 2 * binterval,
                            9 * interval + 2 * binterval);
            //棋盘横线
            for (int i = 0; i < 10; i++)
            {
                Point p1 = new Point(baseX, baseY + interval * i);
                Point p2 = new Point(baseX + 8 * interval, baseY + interval * i);
                g.DrawLine(pen, p1, p2);
            }
            //棋盘两条边界竖线
            g.DrawLine(pen, baseX, baseY, baseX, baseY + 9 * interval);
            g.DrawLine(pen, baseX + 8 * interval, baseY, baseX + 8 * interval, baseY + 9 * interval);
            //棋盘其余竖线
            for (int i = 0; i < 7; i++)
            {
                Point p1 = new Point(baseX + (1 + i) * interval, baseY);
                Point p2 = new Point(baseX + (1 + i) * interval, baseY + 4 * interval);
                Point p3 = new Point(baseX + (1 + i) * interval, baseY + 5 * interval);
                Point p4 = new Point(baseX + (1 + i) * interval, baseY + 9 * interval);
                g.DrawLine(pen, p1, p2);
                g.DrawLine(pen, p3, p4);
            }
            //米字格
            g.DrawLine(pen, baseX + 3 * interval, baseY, baseX + 5 * interval, baseY + 2 * interval);
            g.DrawLine(pen, baseX + 3 * interval, baseY + 2 * interval, baseX + 5 * interval, baseY);
            g.DrawLine(pen, baseX + 3 * interval, baseY + 7 * interval, baseX + 5 * interval, baseY + 9 * interval);
            g.DrawLine(pen, baseX + 3 * interval, baseY + 9 * interval, baseX + 5 * interval, baseY + 7 * interval);
            //楚河汉界
            Font font = new Font("隶书", 40, FontStyle.Regular);
            Point pch = new Point(baseX + 1 * interval, baseY + 4 * interval);
            Point phj = new Point(baseX + 5 * interval, baseY + 4 * interval);
            g.DrawString("楚河", font, Brushes.Black, pch);
            g.DrawString("汉界", font, Brushes.Black, phj);
            //兵卒标志，四个横折
            int hz;//间隙
            hz = 5;
            for (int i = 0; i < 5; i++)
            {
                int px = baseX + 2 * i * interval;
                int py = baseY + 3 * interval;
                //短横
                g.DrawLine(pen, px - hz, py - hz, px - 2 * hz, py - hz);
                g.DrawLine(pen, px + hz, py - hz, px + 2 * hz, py - hz);
                g.DrawLine(pen, px - hz, py + hz, px - 2 * hz, py + hz);
                g.DrawLine(pen, px + hz, py + hz, px + 2 * hz, py + hz);
                //短竖
                g.DrawLine(pen, px - hz, py - hz, px - hz, py - 2 * hz);
                g.DrawLine(pen, px + hz, py - hz, px + hz, py - 2 * hz);
                g.DrawLine(pen, px - hz, py + hz, px - hz, py + 2 * hz);
                g.DrawLine(pen, px + hz, py + hz, px + hz, py + 2 * hz);
                px = baseX + 2 * i * interval;
                py = baseY + 6 * interval;
                //短横
                g.DrawLine(pen, px - hz, py - hz, px - 2 * hz, py - hz);
                g.DrawLine(pen, px + hz, py - hz, px + 2 * hz, py - hz);
                g.DrawLine(pen, px - hz, py + hz, px - 2 * hz, py + hz);
                g.DrawLine(pen, px + hz, py + hz, px + 2 * hz, py + hz);
                //短竖
                g.DrawLine(pen, px - hz, py - hz, px - hz, py - 2 * hz);
                g.DrawLine(pen, px + hz, py - hz, px + hz, py - 2 * hz);
                g.DrawLine(pen, px - hz, py + hz, px - hz, py + 2 * hz);
                g.DrawLine(pen, px + hz, py + hz, px + hz, py + 2 * hz);
            }
            //炮横折
            for (int i = 0; i < 2; i++)
            {
                int px = baseX + interval + 6 * i * interval;
                int py = baseY + 2 * interval;
                //短横
                g.DrawLine(pen, px - hz, py - hz, px - 2 * hz, py - hz);
                g.DrawLine(pen, px + hz, py - hz, px + 2 * hz, py - hz);
                g.DrawLine(pen, px - hz, py + hz, px - 2 * hz, py + hz);
                g.DrawLine(pen, px + hz, py + hz, px + 2 * hz, py + hz);
                //短竖
                g.DrawLine(pen, px - hz, py - hz, px - hz, py - 2 * hz);
                g.DrawLine(pen, px + hz, py - hz, px + hz, py - 2 * hz);
                g.DrawLine(pen, px - hz, py + hz, px - hz, py + 2 * hz);
                g.DrawLine(pen, px + hz, py + hz, px + hz, py + 2 * hz);
                px = baseX + interval + 6 * i * interval;
                py = baseY + 7 * interval;
                //短横
                g.DrawLine(pen, px - hz, py - hz, px - 2 * hz, py - hz);
                g.DrawLine(pen, px + hz, py - hz, px + 2 * hz, py - hz);
                g.DrawLine(pen, px - hz, py + hz, px - 2 * hz, py + hz);
                g.DrawLine(pen, px + hz, py + hz, px + 2 * hz, py + hz);
                //短竖
                g.DrawLine(pen, px - hz, py - hz, px - hz, py - 2 * hz);
                g.DrawLine(pen, px + hz, py - hz, px + hz, py - 2 * hz);
                g.DrawLine(pen, px - hz, py + hz, px - hz, py + 2 * hz);
                g.DrawLine(pen, px + hz, py + hz, px + hz, py + 2 * hz);
            }
            int x, y;
            for (int i = 0; i < 10; i++)
            {
                for(int j = 0; j < 9; j++)
                {
                    if(grid[i,j] != -1)
                    {
                        y = 50 + i * 60;
                        x = 50 + j * 60;
                        string[] splitString = chess[grid[i, j]].Split(',');
                        brush = new SolidBrush(Color.PapayaWhip);
                        font = new Font("隶书", 30, FontStyle.Regular);

                        Rectangle rt = new Rectangle(x - r, y - r, 2 * r, 2 * r);
                        g.DrawEllipse(pen, rt);
                        g.FillEllipse(brush, rt);

                        StringFormat sf = new StringFormat();
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;

                        Point p = new Point(x, y + r / 6);
                        if (splitString[0] == "红")
                        {
                            g.DrawString(splitString[1], font, Brushes.Red, p, sf);
                        }
                        else
                        {
                            g.DrawString(splitString[1], font, Brushes.Black, p, sf);
                        }
                    }
                }
            }
            g.Save();
            pictureBox1.Image = bm;
        }
        //重新开始游戏
        public void Restart(string str)
        {
            MessageBox.Show(str, "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            service.AddItemToListBox(str);
            initGrid();
            order = false;
            SetButton(buttonStart, true);
        }
        private void buttonStart_Click(object sender, EventArgs e)
        {
            service.SendToServer(string.Format("Start,{0},{1}", tableIndex, side));
            this.buttonStart.Enabled = false;
            initGrid();
            RePaint();
        }

        //设置玩家信息，格式：座位号，labelSide显示的信息，listbox显示的信息
        public void SetTableSideText(string sideString, string labelSideString, 
            string listBoxString)
        {
            string s = "红方";
            if(sideString == "0")
            {
                s = "黑方";
            }
            //判断自己是黑方还是红方
            if(sideString == side.ToString())
            {
                SetLabel(labelSide1, s + labelSideString);
            }
            else
            {
                SetLabel(labelSide0, s + labelSideString);
            }
            service.AddItemToListBox(listBoxString);
        }
        //聊天
        public void ShowTalk(string talkMan, string str)
        {
            service.AddItemToListBox(string.Format("{0}说：{1}", talkMan, str));
        }
        //显示信息
        public void ShowMessage(string str)
        {
            service.AddItemToListBox(str);
        }
        //退出按钮
        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //发送按钮
        private void buttonSend_Click(object sender, EventArgs e)
        {
            service.SendToServer(string.Format("Talk,{0},{1}", tableIndex, textBox1.Text));
            textBox1.Text = "";
        }
        //对话内容改变时的事件
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Enter)
            {
                service.SendToServer(string.Format("Talk,{0},{1}", tableIndex, textBox1.Text));
                textBox1.Text = "";
            }
        }
        //关闭窗体时的事件
        private void FromPlaying_FormClosing(object sender, FormClosingEventArgs e)
        {
            service.SendToServer(string.Format("GetUp,{0},{1}", tableIndex, side));
        }
        //关闭此窗体
        private void StopFormPlaying()
        {
            Application.Exit();
        }
        //鼠标按下触发的事件
        private void FormPlaying_MouseDown(object sender, MouseEventArgs e)
        {

            //如果是己方走棋
            if (order == true)
            {
                double xt = (e.X - baseX) / (double)interval;
                double yt = (e.Y - baseY) / (double)interval;
                //将绘图坐标转化为网格坐标
                int y = (int)Math.Round(xt);
                int x = (int)Math.Round(yt);
                //在棋盘范围
                if(!(x < 0 || x > 9 || y < 0 || y > 8))
                {
                    //选中的是自己的棋子
                    if ((grid[x, y] != -1) && ((side == 1 && grid[x,y] < 16) || (side == 0 && grid[x,y] > 15)))
                    {
                            oriX = x;
                            oriY = y;
                            pick = grid[x, y];
                            RePaint();
                            drawFrame("green", x, y);
                    }
                    //选中的是空位或者是对方的棋子
                    else
                    {
                        //如果原始位置已经确定
                        if(oriX != -1 && oriY != -1)
                        {
                            endX = x;
                            endY = y;
                            //如果符合规则
                            if( CheckRule(pick, oriX, oriY, endX, endY) == true)
                            {
                                service.SendToServer(string.Format("ChessInfo,{0},{1},{2},{3},{4},{5},{6}", tableIndex, side, pick,
                                    oriX, oriY, endX, endY));
                                oriX = -1;
                                oriY = -1;
                                endX = -1;
                                endY = -1;
                                pick = -1;
                                order = false;
                                return;
                            }
                            //不符合
                            else
                            {
                                endX = -1;
                                endY = -1;
                            }
                        }
                    }
                }
            }
        }
        //查看是否胜利
        public void CheckWin()
        {
            bool win = true;
            for(int i = 0; i <= 2; i++)
            {
                for(int j = 3; j <= 5; j++)
                {
                    if(grid[i,j] == 4 || grid[i,j] == 20)
                    {
                        win = false;
                    }
                }
            }
            if(win == true)
            {
                service.SendToServer(string.Format("win,{0},{1}", tableIndex, side));
            }
        }
        //给选中的棋子画个绿框
        public void drawFrame(string color, int x, int y)
        {
            int y1 = baseX + x * interval;
            int x1 = baseY + y * interval;
            Graphics g = Graphics.FromImage(bm);
            if(color == "green")
            {
                Pen pen = new Pen(Color.Lime, 3);
                g.DrawRectangle(pen, x1 - r, y1 - r, 2 * r, 2 * r);
            }
            else if(color == "blue")
            {
                Pen pen = new Pen(Color.DeepSkyBlue, 3);
                g.DrawRectangle(pen, x1 - r, y1 - r, 2 * r, 2 * r);
            }
            g.Save();
            pictureBox1.Image = bm;
        }
        //移动棋子
        public void ChangeChess(int cno, int x, int y)
        {
            grid[x, y] = cno;
        }
        //准备就绪，开始游戏
        public void Ready(int i)
        {
            if(i == 1)
            {
                order = true;
                SetLabel(labelOrder, "我方走棋");
            }
            else
            {
                order = false;
                SetLabel(labelOrder, "对方走棋");
            }
        }
        //更新Order
        public void ChangeOrder(int i)
        {
            //该对方走棋了
            if(i == side)
            {
                order = false;
                SetLabel(labelOrder, "对方走棋");
            }
            else
            {
                order = true;
                SetLabel(labelOrder, "我方走棋");
            }
        }
        //检查是否符合规则
        private bool CheckRule(int c, int x0, int y0, int x1, int y1)
        {
            int i;
            int miny, maxy;
            int minx, maxx;
            switch (c)
            {
                //車
                case 0:
                case 8:
                case 16:
                case 24:
                    //同一条横线上
                    if(x0 == x1)
                    {
                        //判断两点之间是否有棋子
                        miny = y0 < y1 ? y0 : y1;
                        maxy = y0 > y1 ? y0 : y1;
                        for(i = miny + 1; i < maxy; i++)
                        {
                            //有棋子就直接返回false
                            if (grid[x0, i] != -1)
                            {
                                return false;
                            }
                        }
                        if(i == maxy)
                        {
                            return true;
                        }
                    }
                    //同一条竖线上
                    else if(y0 == y1)
                    {
                        minx = x0 < x1 ? x0 : x1;
                        maxx = x0 > x1 ? x0 : x1;
                        for (i = minx + 1; i < maxx; i++)
                        {
                            if (grid[i, y0] != -1)
                            {
                                return false;
                            }
                        }
                        if (i == maxx)
                        {
                            return true;
                        }
                    }
                    return false;
                //马
                case 1:
                case 7:
                case 17:
                case 23:
                    //倒日
                    if (Math.Abs(y1 - y0) == 2 && Math.Abs(x1 - x0) == 1)
                    {
                        //不蹩马腿
                        if(grid[x0, (y0+y1)/2] == -1)
                        {
                            return true;
                        }
                    }
                    //立日
                    else if(Math.Abs(x1 - x0) == 2 && Math.Abs(y1 - y0) == 1)
                    {
                        if(grid[(x0+x1)/2, y0] == -1)
                        {
                            return true;
                        }
                    }
                    return false;
                //象、相
                case 2:
                case 6:
                case 18:
                case 22:
                    //象不能过河
                    if(x0 == 5 && x1 == 3)
                    {
                        return false;
                    }
                    else
                    {
                        //走田字
                        if(Math.Abs(x1 - x0) == 2 && Math.Abs(y1 - y0) == 2)
                        {
                            //不堵象眼儿
                            if(grid[(x0+x1)/2, (y0+y1)/2] == -1)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                //士
                case 3:
                case 5:
                case 19:
                case 21:
                    //士不出田字格
                    if(y1 < 3 || y1 > 5 || x1 < 7)
                    {
                        return false;
                    }
                    else
                    {
                        //斜走一格
                        if(Math.Abs(x1 - x0) == 1 && Math.Abs(y1 - y0) == 1)
                        {
                            return true;
                        }
                    }
                    return false;
                //将、帅
                case 4:
                case 20:
                    //将帅不能出田字格
                    if (y1 < 3 || y1 > 5 ||  x1 < 7)
                    {
                        return false;
                    }
                    else
                    {
                        //直着走一格
                        if ((Math.Abs(x1 - x0) == 1 && y0 == y1) || (Math.Abs(y1 - y0) == 1 && x0 == x1))
                        {
                            return true;
                        }
                    }
                    return false;
                //炮
                case 9:
                case 10:
                case 25:
                case 26:
                    //同一条横线上
                    if (x0 == x1)
                    {
                        miny = y0 < y1 ? y0 : y1;
                        maxy = y0 > y1 ? y0 : y1;
                        //移动
                        if (grid[x1, y1] == -1)
                        {
                            //判断两点之间是否有棋子
                            for (i = miny + 1; i < maxy; i++)
                            {
                                //有棋子就直接返回false
                                if (grid[x0, i] != -1)
                                {
                                    return false;
                                }
                            }
                            if (i == maxy)
                            {
                                return true;
                            }
                        }
                        //吃子
                        else
                        {
                            int n = 0;
                            for (i = miny + 1; i < maxy; i++)
                            {
                                //有棋子n++
                                if (grid[x0, i] != -1)
                                {
                                    n++;
                                }
                            }
                            if (n == 1)
                            {
                                return true;
                            }
                        }
                    }
                    //同一条竖线上
                    else if (y0 == y1)
                    {
                        minx = x0 < x1 ? x0 : x1;
                        maxx = x0 > x1 ? x0 : x1;
                        //移动
                        if (grid[x1, y1] == -1)
                        {
                            //判断两点之间是否有棋子
                            for (i = minx + 1; i < maxx; i++)
                            {
                                //有棋子就直接返回false
                                if (grid[i, y0] != -1)
                                {
                                    return false;
                                }
                            }
                            if (i == maxx)
                            {
                                return true;
                            }
                        }
                        //吃子
                        else
                        {
                            int n = 0;
                            for (i = minx + 1; i < maxx; i++)
                            {
                                //有棋子n++
                                if (grid[i, y0] != -1)
                                {
                                    n++;
                                }
                            }
                            if (n == 1)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                //兵、卒
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 27:
                case 28:
                case 29:
                case 30:
                case 31:
                    //兵卒不能后退
                    if(x1 > x0)
                    {
                        return false;
                    }
                    else
                    {
                        //未过河，只能前进一格
                        if(x0 >= 5)
                        {
                            if(Math.Abs(x1 - x0) == 1 && y0 == y1)
                            {
                                return true;
                            }
                        }
                        //过了河，能前进、左、右
                        else
                        {
                            if ((Math.Abs(x1 - x0) == 1 && y0 == y1) || (Math.Abs(y1 - y0) == 1 && x0 == x1))
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                default:
                    return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Configuration;
using System.Threading;

namespace Client
{
    public partial class ClientForm : Form
    {
        //private StateObject state;
        private Socket socket = null;
        private delegate void append(String msg);
        Thread thread; //接收数据线程
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);
        public ClientForm()
        {
            InitializeComponent();
        }

        //窗体加载事件
        private void ClientForm_Load(object sender, EventArgs e)
        {
            try
            {
                String ip = ConfigurationManager.AppSettings["serviceIp"].ToString();
                String port = ConfigurationManager.AppSettings["port"].ToString();
                IPEndPoint iep = new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //state = new StateObject();
                //state.socket = socket;
                socket.Connect(iep); //连接服务端
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            thread = new Thread(recevice); //接收数据线程
            thread.Start();
        }

        //发送按钮点击事件
        private void send_button_Click(object sender, EventArgs e)
        {
            if (!"".Equals(this.send_textBox.Text))
            {
                try
                {
                    if (socket.Connected)
                    {
                        //Socket socket = socket;
                        StateObject state = new StateObject();
                        state.socket = socket;
                        state.buffer = System.Text.UnicodeEncoding.UTF8.GetBytes(this.send_textBox.Text);
                        socket.BeginSend(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(sendCallback), state); //向服务端异步发送数据
                    }
                    else
                    {
                        throw new Exception("连接已断开");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
        }

        //窗体退出事件
        private void ClientForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            thread.Abort();//退出接受数据线程
        }
        
        //发送窗口按键事件
        private void send_textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                send_button_Click(sender, e);
            }
        }

        //异步发送回调函数
        private void sendCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                state.socket.EndSend(ar); //结束异步发送
                //state.socket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), state); //开始异步接收
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //接收数据
        private void recevice()
        {
            while (socket.Connected)
            {
                while (socket.Available > 0) //接收到的数据大于0
                {
                    StateObject state = new StateObject();
                    state.socket = socket;
                    state.socket.BeginReceive(state.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), state); //开始异步接收
                    receiveDone.WaitOne(); //阻止当前线程,等待接收数据回调
                }
            }
        }

        //异步接收回调
        private void receiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                state.socket.EndReceive(ar); //结束异步回调
                state.sb.Append(System.Text.UnicodeEncoding.UTF8.GetString(state.buffer));
                if (state.sb.Length > 0)
                {
                    this.Invoke(new append(appendToTexbox), state.socket.RemoteEndPoint.ToString() + ":" + state.sb.ToString()); //追加数据
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            receiveDone.Set();
        }

        //将数据追加到msg_textBox中
        private void appendToTexbox(String msg)
        {
            this.msg_textBox.AppendText(Environment.NewLine + msg);
        }

    }
}

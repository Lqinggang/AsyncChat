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

namespace Service
{
    public partial class ServerForm : Form
    {
        //private StateObject state;
        private Thread acceptThread;
        private Socket serviceSocket;
        private int maxScoket = 10;
        private static ManualResetEvent acceptDone = new ManualResetEvent(false);
        private static ManualResetEvent recevieDone = new ManualResetEvent(false);

        private delegate void append(String msg);

        public ServerForm()
        {
            InitializeComponent();
        }

        //窗体加载事件
        private void ServerForm_Load(object sender, EventArgs e)
        {
            try
            {
                //String ip = ConfigurationManager.AppSettings["serviceIp"].ToString();
                String port = ConfigurationManager.AppSettings["port"].ToString(); //从配置文件获取监听端口
                IPEndPoint iep = new IPEndPoint(IPAddress.Any, int.Parse(port));
                serviceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serviceSocket.Bind(iep); //绑定监听窗口
                serviceSocket.Listen(100);
                //state = new StateObject();
                //state.socket = serciveSocket;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            acceptThread = new Thread(accept); //创建一个线程去接收连接请求
            acceptThread.Start();
        }

        //窗体关闭事件
        private void ServerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            acceptThread.Abort(); //退出接受连接请求访问线程
        }

        //接收连接请求
        private void accept()
        {
            this.Invoke(new append(appendToTextbox), "等待连接...");
            try
            {
                //Socket serviceSocket = serciveSocket;
                serviceSocket.BeginAccept(new AsyncCallback(acceptCallback), serviceSocket); //异步操作接入的连接请求
                acceptDone.WaitOne(); //阻止当前线程，等待连接回调函数的执行
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //连接请求回调函数
        private void acceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket serviceSocket = (Socket)ar.AsyncState;
                acceptDone.Set(); //结束当前线程的停止状态，继续接受下一连接请求
                Socket socket = serviceSocket.EndAccept(ar); //异步接受传入的连接请求
                StateObject state = new StateObject();
                state.socket = socket;
                socket.BeginReceive(state.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), state); //开始从当前连接请求接收数据
                recevieDone.WaitOne(); //阻止当前异步接收线程，等待接收回调函数返回
                if ((maxScoket--) > 0)
                {
                    serviceSocket.BeginAccept(new AsyncCallback(acceptCallback), serviceSocket); //等待接收下一个连接请求
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //接收数据之后的回调
        private void receiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                int receiveBytes = state.socket.EndReceive(ar); //结束当前的接收数据请求
                if (receiveBytes > 0)
                {
                    state.sb.Append(System.Text.UnicodeEncoding.UTF8.GetString(state.buffer));
                    //String content = state.sb.ToString();
                    while (state.socket.Available > 0)
                    {
                        state.socket.BeginReceive(state.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), state);
                        state.sb.Append(System.Text.UnicodeEncoding.UTF8.GetString(state.buffer));
                        //content += state.sb.ToString();
                    }
                    if (state.sb.Length > 0)
                    {
                        this.Invoke(new append(appendToTextbox), state.socket.RemoteEndPoint.ToString() + ":" + state.sb.ToString() + Environment.NewLine);
                        //this.msg_textBox.Text += state.socket.RemoteEndPoint.ToString() + ":" + content;
                    }
                    recevieDone.Set();//继续异步接收线程
                    StateObject newstate = new StateObject();
                    newstate.socket = state.socket;
                    newstate.socket.BeginReceive(newstate.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), newstate); //准备下一次接收数据请求
                    state.socket.BeginSend(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(sendCallback), state.socket); //将数据返回
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //发送数据回调函数
        private void sendCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                int bytesSent = socket.EndSend(ar);
                StateObject state = new StateObject();
                state.socket = socket;
                state.socket.BeginReceive(state.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), state);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //向msg_textBox追加数据
        private void appendToTextbox(String msg)
        {
            this.msg_textBox.AppendText(Environment.NewLine + msg);

        }

    }
}

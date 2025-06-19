using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ZeroLevel.Services.Network
{
    public static class SocketExtensions
    {
        // Структура для настройки TCP KeepAlive (Windows)
        [StructLayout(LayoutKind.Sequential)]
        private struct TcpKeepAlive
        {
            public uint OnOff;
            public uint KeepAliveTime;
            public uint KeepAliveInterval;
        }

        // Метод расширения для настройки KeepAlive
        public static void SetKeepAlive(this Socket socket, bool on, uint keepAliveTime, uint keepAliveInterval)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows-специфичная настройка через IOControl
                var keepAlive = new TcpKeepAlive
                {
                    OnOff = on ? 1u : 0u,
                    KeepAliveTime = keepAliveTime,      // Время в миллисекундах до первой проверки
                    KeepAliveInterval = keepAliveInterval // Интервал между проверками в миллисекундах
                };

                int size = Marshal.SizeOf(keepAlive);
                IntPtr ptr = Marshal.AllocHGlobal(size);
                try
                {
                    Marshal.StructureToPtr(keepAlive, ptr, false);
                    byte[] buffer = new byte[size];
                    Marshal.Copy(ptr, buffer, 0, size);

                    socket.IOControl(IOControlCode.KeepAliveValues, buffer, null);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            else
            {
                // Для Linux/MacOS используем socket options
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, on);

                if (on)
                {
                    // На Linux эти параметры настраиваются через TCP socket options
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        // TCP_KEEPIDLE (секунды до первой проверки)
                        socket.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)4, (int)(keepAliveTime / 1000));
                        // TCP_KEEPINTVL (интервал между проверками в секундах)
                        socket.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)5, (int)(keepAliveInterval / 1000));
                        // TCP_KEEPCNT (количество проверок)
                        socket.SetSocketOption(SocketOptionLevel.Tcp, (SocketOptionName)6, 9);
                    }
                }
            }
        }
    }

}

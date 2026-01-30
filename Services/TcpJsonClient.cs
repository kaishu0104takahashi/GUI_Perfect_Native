using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GUI_Perfect.Services;

public class TcpJsonClient
{
    private TcpClient? _client;
    // 【修正】未使用だった _stream フィールド定義を削除しました
    private bool _isRunning;
    private readonly int _port;

    public event Action<string>? OnJsonReceived;
    public event Action<string>? OnStatusChanged;

    public TcpJsonClient(int port)
    {
        _port = port;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        Task.Run(ReceiveLoop);
    }

    public void Stop()
    {
        _isRunning = false;
        try { _client?.Close(); } catch { }
        _client = null;
    }

    private async Task ReceiveLoop()
    {
        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();
            OnStatusChanged?.Invoke($"ポート {_port} で待機中...");

            while (_isRunning)
            {
                try
                {
                    using var client = await listener.AcceptTcpClientAsync();
                    OnStatusChanged?.Invoke($"接続完了: {client.Client.RemoteEndPoint}");
                    
                    using var stream = client.GetStream();

                    while (_isRunning && client.Connected)
                    {
                        byte[] headerBuffer = new byte[4];
                        int bytesRead = await ReadExactAsync(stream, headerBuffer, 4);
                        
                        if (bytesRead == 0) break;

                        if (BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(headerBuffer);
                        }
                        int bodyLength = BitConverter.ToInt32(headerBuffer, 0);

                        if (bodyLength > 0)
                        {
                            if (bodyLength > 10 * 1024 * 1024) throw new Exception("データサイズが大きすぎます");

                            byte[] bodyBuffer = new byte[bodyLength];
                            await ReadExactAsync(stream, bodyBuffer, bodyLength);

                            string jsonStr = Encoding.UTF8.GetString(bodyBuffer);
                            OnJsonReceived?.Invoke(jsonStr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_isRunning) OnStatusChanged?.Invoke($"通信エラー: {ex.Message}");
                }
                finally
                {
                    OnStatusChanged?.Invoke($"待機中...");
                }
            }
        }
        catch (Exception ex)
        {
            OnStatusChanged?.Invoke($"起動エラー: {ex.Message}");
        }
        finally
        {
            listener?.Stop();
        }
    }

    private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int length)
    {
        int totalRead = 0;
        while (totalRead < length)
        {
            int read = await stream.ReadAsync(buffer, totalRead, length - totalRead);
            if (read == 0) return 0;
            totalRead += read;
        }
        return totalRead;
    }
}

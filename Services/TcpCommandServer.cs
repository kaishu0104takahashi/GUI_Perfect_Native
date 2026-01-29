using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GUI_Perfect.Services;

public class TcpCommandServer
{
    private TcpListener? _listener;
    private TcpClient? _currentClient;
    private const int Port = 55555;
    private bool _isRunning;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        Task.Run(ListenLoop);
    }

    private async Task ListenLoop()
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            Console.WriteLine($"[TCP Server] Listening on port {Port}...");

            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine($"[TCP Server] Connected: {client.Client.RemoteEndPoint}");
                    _currentClient?.Dispose();
                    _currentClient = client;
                }
                catch (ObjectDisposedException) { break; }
                catch (OperationCanceledException) { break; } // キャンセル時は静かに終了
                catch (Exception ex)
                {
                    if (_isRunning) Console.WriteLine($"[TCP Server] Accept Error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TCP Server] Startup Error: {ex.Message}");
        }
    }

    public async Task SendCommandAsync(string commandName, object argsObj)
    {
        // クライアントがいない場合は何もしない（エラーログも出さない）
        if (_currentClient == null || !_currentClient.Connected)
        {
            return; 
        }

        try
        {
            var cmdData = new { type = "cmd", command = commandName, args = argsObj };
            string jsonString = JsonSerializer.Serialize(cmdData);
            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            var stream = _currentClient.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
            Console.WriteLine($"[TCP Sent] {jsonString}");
        }
        catch
        {
            // 送信失敗時は切断扱いにする
            _currentClient?.Dispose();
            _currentClient = null;
        }
    }

    public void Stop()
    {
        _isRunning = false;
        try { _listener?.Stop(); } catch { }
        try { _currentClient?.Dispose(); } catch { }
    }
}

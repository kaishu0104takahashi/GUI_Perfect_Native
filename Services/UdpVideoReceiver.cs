using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace GUI_Perfect.Services;

public class UdpVideoReceiver
{
    private readonly int _port;
    private UdpClient? _udpClient;
    private bool _isRunning;

    public event Action<Bitmap?>? OnFrameReceived;
    public bool IsPaused { get; set; } = false;

    public UdpVideoReceiver(int port)
    {
        _port = port;
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        Task.Run(ReceiveLoop);
    }

    private async Task ReceiveLoop()
    {
        try
        {
            _udpClient = new UdpClient(_port);
            // 受信バッファを大きめに確保
            _udpClient.Client.ReceiveBufferSize = 1024 * 1024 * 5; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP Start Error: {ex.Message}");
            return;
        }

        while (_isRunning)
        {
            try
            {
                if (_udpClient == null) break;

                // 受信待機
                var result = await _udpClient.ReceiveAsync();
                
                if (IsPaused) continue;

                var bytes = result.Buffer;
                if (bytes != null && bytes.Length > 0)
                {
                    try 
                    {
                        using var stream = new MemoryStream(bytes);
                        var bitmap = new Bitmap(stream);
                        OnFrameReceived?.Invoke(bitmap);
                    }
                    catch
                    {
                        // 画像変換エラーは無視（警告変数 imgEx を削除）
                    }
                }
            }
            catch (ObjectDisposedException) { break; } // 終了時はループを抜ける
            catch (SocketException) { break; } 
            catch (Exception ex)
            {
                if (!_isRunning) break;
                Console.WriteLine($"UDP Receive Error: {ex.Message}");
                await Task.Delay(100);
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
        catch { /* 無視 */ }
        finally
        {
            _udpClient = null;
        }
    }
}

using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace GUI_Perfect.Services;

static class Constants
{
    public const int ALLOC_BYTE_SIZE = 4 * 1024 * 1024;
    public const int KERNEL_S_UDP_BUFFER_SIZE = 8 * 1024 * 1024;
}

public class UdpVideoReceiver
{
    private Socket? _socket;
    private bool _is_running;
    private readonly int _port;
    private readonly byte[] _reassembly_buffer = new byte[Constants.ALLOC_BYTE_SIZE];
    private int _current_payload_length = 0;
    private int _is_rendering = 0;

    // 【修正】volatileをつけて、停止フラグが即座にスレッド間で共有されるようにする
    public volatile bool IsPaused = false;

    // 【修正】FPS制限を緩和 (1ms = 1000fps理論値。実質PCの限界まで出す)
    private DateTime _lastFrameTime = DateTime.MinValue;
    private readonly TimeSpan _frameInterval = TimeSpan.FromMilliseconds(1); 

    public Action<Bitmap>? OnFrameReady;

    public UdpVideoReceiver(int port)
    {
        _port = port;
    }

    public void Start()
    {
        if (_is_running) return;
        _is_running = true;
        Task.Run(Receiver_loop);
    }

    private async Task Receiver_loop()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
        _socket.ReceiveBufferSize = Constants.KERNEL_S_UDP_BUFFER_SIZE;
        byte[] receive_buffer = ArrayPool<byte>.Shared.Rent(4096);
        
        try
        {
            EndPoint remote_endpoint = new IPEndPoint(IPAddress.Any, 0);
            while (_is_running)
            {
                var result = await _socket.ReceiveFromAsync(new Memory<byte>(receive_buffer), SocketFlags.None, remote_endpoint);
                int bytes_read = result.ReceivedBytes;

                if (bytes_read < 2) continue;

                ReadOnlySpan<byte> packet_span = receive_buffer.AsSpan(0, bytes_read);
                byte flag = packet_span[0];
                int payload_size = bytes_read - 1;

                if (_current_payload_length + payload_size > _reassembly_buffer.Length)
                {
                    _current_payload_length = 0;
                    continue;
                }

                packet_span.Slice(1, payload_size).CopyTo(_reassembly_buffer.AsSpan(_current_payload_length));
                _current_payload_length += payload_size;

                if (flag == 1)
                {
                    // ポーズ中は処理しない
                    if (IsPaused)
                    {
                        _current_payload_length = 0;
                        continue;
                    }

                    if (Interlocked.CompareExchange(ref _is_rendering, 1, 0) == 0)
                    {
                        var now = DateTime.Now;
                        // FPS制限チェック
                        if (now - _lastFrameTime >= _frameInterval)
                        {
                            _lastFrameTime = now;
                            Process_frame(_current_payload_length);
                        }
                        else
                        {
                            Interlocked.Exchange(ref _is_rendering, 0);
                        }
                    }
                    _current_payload_length = 0;
                }
            }
        }
        catch (Exception ex)
        {
             Console.WriteLine($"Socket Error : {ex.Message}");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receive_buffer);
            _socket?.Close();
        }
    }

    private void Process_frame(int length)
    {
        try
        {
            // Bitmap生成
            using (var ms = new MemoryStream(_reassembly_buffer, 0, length, writable: false))
            {
                var bitmap = new Bitmap(ms);

                Dispatcher.UIThread.Post(() =>
                {
                    // ポーズされていたらUI更新もしない（念のため）
                    if (!IsPaused)
                    {
                        OnFrameReady?.Invoke(bitmap);
                    }
                    Interlocked.Exchange(ref _is_rendering, 0);
                }, DispatcherPriority.Render);
            }
        }
        catch
        {
            Interlocked.Exchange(ref _is_rendering, 0);
        }
    }

    public void Stop()
    {
        _is_running = false;
        try
        {
            _socket?.Shutdown(SocketShutdown.Both);
            _socket?.Close();
        }
        catch { }
        finally
        {
            _socket?.Dispose();
        }
    }
}

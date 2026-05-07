using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ExhibitionServer.Application.Abstractions;
using QRCoder;

namespace ExhibitionServer.Application;

public sealed class PanelAccessService : IPanelAccessService
{
    private const int DefaultPort = 5225;
    private readonly ILogger<PanelAccessService> _logger;

    public PanelAccessService(ILogger<PanelAccessService> logger)
    {
        _logger = logger;
    }

    public string GetPanelUrl(HttpRequest request)
    {
        var scheme = request.Scheme;
        var port = ResolvePort(request);
        var host = ResolveHost(request);

        return port is 80 or 443
            ? $"{scheme}://{host}"
            : $"{scheme}://{host}:{port}";
    }

    public string CreatePanelQrSvg(HttpRequest request)
    {
        var url = GetPanelUrl(request);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(data);

        return qrCode.GetGraphic(
            pixelsPerModule: 8,
            darkColorHex: "#111111",
            lightColorHex: "#ffffff",
            drawQuietZones: true);
    }

    public byte[] CreatePanelQrPng(HttpRequest request)
    {
        var url = GetPanelUrl(request);

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);

        // pixelsPerModule=10 → 충분한 해상도, UE에서 픽셀 선명하게 표시
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }

    private static int ResolvePort(HttpRequest request)
    {
        if (request.Host.Port is { } requestPort)
        {
            return requestPort;
        }

        return request.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : DefaultPort;
    }

    private string ResolveHost(HttpRequest request)
    {
        if (IsUsableRemoteHost(request.Host.Host))
        {
            return request.Host.Host;
        }

        var localIp = TryGetLanIPv4Address();
        if (!string.IsNullOrWhiteSpace(localIp))
        {
            return localIp;
        }

        _logger.LogWarning("No LAN IPv4 address was found. Falling back to request host {Host}.", request.Host.Host);
        return string.IsNullOrWhiteSpace(request.Host.Host) ? "localhost" : request.Host.Host;
    }

    private static bool IsUsableRemoteHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        return !host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("[::1]", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryGetLanIPv4Address()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(adapter =>
                adapter.OperationalStatus == OperationalStatus.Up &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
            .Where(address =>
                address.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(address.Address) &&
                IsPrivateIPv4(address.Address))
            .Select(address => address.Address.ToString())
            .FirstOrDefault();
    }

    private static bool IsPrivateIPv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();

        return bytes[0] == 10
            || bytes[0] == 172 && bytes[1] is >= 16 and <= 31
            || bytes[0] == 192 && bytes[1] == 168;
    }
}

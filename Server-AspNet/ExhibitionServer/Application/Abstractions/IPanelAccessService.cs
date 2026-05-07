namespace ExhibitionServer.Application.Abstractions;

public interface IPanelAccessService
{
    string GetPanelUrl(HttpRequest request);

    string CreatePanelQrSvg(HttpRequest request);

    byte[] CreatePanelQrPng(HttpRequest request);
}

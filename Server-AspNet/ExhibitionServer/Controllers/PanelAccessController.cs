using ExhibitionServer.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ExhibitionServer.Controllers;

[ApiController]
[Route("api/panel")]
public sealed class PanelAccessController : ControllerBase
{
    private readonly IPanelAccessService _panelAccess;

    public PanelAccessController(IPanelAccessService panelAccess)
    {
        _panelAccess = panelAccess;
    }

    [HttpGet("url")]
    public IActionResult GetUrl()
    {
        var url = _panelAccess.GetPanelUrl(Request);
        return Ok(new { url });
    }

    [HttpGet("qr.svg")]
    public IActionResult GetQrSvg()
    {
        var svg = _panelAccess.CreatePanelQrSvg(Request);
        Response.Headers.CacheControl = "no-store";

        return Content(svg, "image/svg+xml");
    }

    /// <summary>
    /// UE HUD에서 HTTP로 다운로드해 UTexture2D로 변환하기 위한 PNG 엔드포인트.
    /// pixelsPerModule=10 기준 약 370×370px 출력.
    /// </summary>
    [HttpGet("qr.png")]
    public IActionResult GetQrPng()
    {
        var png = _panelAccess.CreatePanelQrPng(Request);
        Response.Headers.CacheControl = "no-store";

        return File(png, "image/png");
    }
}

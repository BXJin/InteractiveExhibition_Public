namespace ExhibitionServer.Realtime.Abstractions;

/// <summary>
/// Unreal Engine WebSocket 연결에 원시 JSON 문자열을 전송하는 인터페이스.
/// <para>
/// panelConnected / panelDisconnected 같은 패널 수명주기 이벤트처럼
/// ExhibitionCommand 계층 외부의 메시지를 Unreal로 전달할 때 사용합니다.
/// </para>
/// </summary>
public interface IRawUnrealBroadcaster
{
    /// <summary>연결된 모든 Unreal 인스턴스에 원시 JSON을 전송합니다.</summary>
    Task BroadcastRawAsync(string json, CancellationToken cancellationToken = default);
}
